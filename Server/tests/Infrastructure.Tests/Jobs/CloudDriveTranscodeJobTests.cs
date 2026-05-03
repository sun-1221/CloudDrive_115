using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using Infrastructure.Http.Responses;
using Infrastructure.Jobs;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs;

public sealed class CloudDriveTranscodeJobTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task Execute_WhenProbeReturnsPlayableUrl_CompletesJobAsync()
    {
        // Arrange
        SubmitTranscodeInput input = CreateInput();
        CloudDriveJob job = CreateJob(input);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingTranscodeGateway();
        gateway.PlayResponses.Enqueue(new CloudDriveVideoPlayResponse
        {
            PickCode = input.PickCode,
            VideoUrls =
            [
                new CloudDriveVideoUrlVariantResponse
                {
                    Definition = 4,
                    Url = "https://play.example/video.m3u8"
                }
            ]
        });
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveTranscodeJob transcodeJob = CreateTranscodeJob(
            repository,
            gateway,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await transcodeJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        Assert.Null(job.ErrorMessage);
        Assert.Equal(4, transactionManager.CommitCount);
        Assert.Equal(["files/video_push", "files/video"], gateway.Endpoints);

        RecordingTranscodeRequest submit = gateway.Requests[0];
        Assert.Equal(HttpMethod.Get, submit.Method);
        Assert.Equal("pick-001", submit.QueryParameters["pickcode"]);
        Assert.Equal("vip_push", submit.QueryParameters["op"]);
        Assert.Equal("json", submit.QueryParameters["format"]);

        RecordingTranscodeRequest probe = gateway.Requests[1];
        Assert.Equal("0", probe.QueryParameters["share_id"]);
        Assert.Equal("1", probe.QueryParameters["local"]);

        using JsonDocument checkpoint = JsonDocument.Parse(job.CheckpointData!);
        Assert.Equal("ready", checkpoint.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, checkpoint.RootElement.GetProperty("probeAttempt").GetInt32());
    }

    [Fact]
    public async Task Execute_WhenProbeNeverReady_FailsWithTranscodingMessageAsync()
    {
        // Arrange
        SubmitTranscodeInput input = CreateInput();
        CloudDriveJob job = CreateJob(input);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingTranscodeGateway();
        for (int i = 0; i < 3; i++)
        {
            gateway.PlayResponses.Enqueue(new CloudDriveVideoPlayResponse
            {
                PickCode = input.PickCode,
                Status = "transcoding"
            });
        }
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveTranscodeJob transcodeJob = CreateTranscodeJob(
            repository,
            gateway,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await transcodeJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(90, job.Progress);
        Assert.Contains("尚未在本轮后台任务探测内完成", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(6, transactionManager.CommitCount);
        Assert.Equal(["files/video_push", "files/video", "files/video", "files/video"], gateway.Endpoints);
    }

    [Fact]
    public async Task Execute_WhenPickCodeMissing_FailsWithoutCallingGatewayAsync()
    {
        // Arrange
        SubmitTranscodeInput input = CreateInput();
        input.PickCode = null;
        CloudDriveJob job = CreateJob(input);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingTranscodeGateway();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveTranscodeJob transcodeJob = CreateTranscodeJob(
            repository,
            gateway,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await transcodeJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal("视频转码任务缺少有效 PickCode。", job.ErrorMessage);
        Assert.Empty(gateway.Requests);
        Assert.Equal(1, transactionManager.CommitCount);
    }

    private static SubmitTranscodeInput CreateInput()
    {
        return new SubmitTranscodeInput
        {
            CloudAccountUid = TestCloudAccountUid,
            FileId = "file-001",
            PickCode = "pick-001",
            Op = "vip_push"
        };
    }

    private static CloudDriveJob CreateJob(SubmitTranscodeInput input)
    {
        string payloadJson = JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return CloudDriveJob.Create(
            TestUserId,
            input.CloudAccountUid,
            CloudDriveJobType.Transcode,
            input.FileId ?? input.PickCode,
            payloadJson);
    }

    private static CloudDriveTranscodeJob CreateTranscodeJob(
        ICloudDriveJobRepository repository,
        ICloudDriveOpenApiGateway gateway,
        Application.Abstractions.ITransactionManager transactionManager,
        IPublisher publisher)
    {
        return new CloudDriveTranscodeJob(
            repository,
            gateway,
            transactionManager,
            publisher,
            new CloudDriveJobTestDoubles.RecordingJobAuditLogger(),
            NullLogger<CloudDriveTranscodeJob>.Instance);
    }

    private sealed class RecordingTranscodeGateway : ICloudDriveOpenApiGateway
    {
        public Queue<CloudDriveVideoPlayResponse> PlayResponses { get; } = [];

        public List<RecordingTranscodeRequest> Requests { get; } = [];

        public List<string> Endpoints { get; } = [];

        public Task<T> SendWebApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default)
        {
            Endpoints.Add(endpoint);
            Requests.Add(new RecordingTranscodeRequest(
                method,
                endpoint,
                context.QueryParameters is null
                    ? new Dictionary<string, string>(StringComparer.Ordinal)
                    : new Dictionary<string, string>(context.QueryParameters, StringComparer.Ordinal),
                context.Category));

            object response = endpoint switch
            {
                "files/video_push" => new CloudDriveVideoPushResponse { Data = "submitted" },
                "files/video" => PlayResponses.Dequeue(),
                _ => throw new NotSupportedException(endpoint)
            };

            return Task.FromResult((T)response);
        }

        public Task<T> SendUserCenterRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> SendOfflineApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> SendUploadInitRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> UploadFileAsync<T>(
            Guid userId,
            string cloudAccountUid,
            string uploadUrl,
            Stream fileStream,
            string fileName,
            IDictionary<string, string>? formFields = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed record RecordingTranscodeRequest(
        HttpMethod Method,
        string Endpoint,
        IReadOnlyDictionary<string, string> QueryParameters,
        string? Category);
}
