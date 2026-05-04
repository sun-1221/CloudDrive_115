using System.Text.Json;

using Application.Abstractions.CloudDrive;

namespace Application.Tests.Support;

internal sealed class RecordingCloudDriveOpenApiGateway : ICloudDriveOpenApiGateway
{
    private readonly Dictionary<string, object?> _webApiResponses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _userCenterResponses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _offlineResponses = new(StringComparer.Ordinal);

    public object? WebApiResponse { get; set; }

    public object? UserCenterResponse { get; set; }

    public object? OfflineResponse { get; set; }

    public object? UploadInitResponse { get; set; }

    public object? UploadFileResponse { get; set; }

    public List<CloudDriveGatewayRequest> WebApiRequests { get; } = [];

    public List<CloudDriveGatewayRequest> UserCenterRequests { get; } = [];

    public List<CloudDriveGatewayRequest> OfflineRequests { get; } = [];

    public List<CloudDriveApiContext> UploadInitContexts { get; } = [];

    public List<CloudDriveUploadRequest> UploadRequests { get; } = [];

    public void RespondWeb(string endpoint, object? response)
    {
        _webApiResponses[endpoint] = response;
    }

    public void RespondUserCenter(string endpoint, object? response)
    {
        _userCenterResponses[endpoint] = response;
    }

    public void RespondOffline(string endpoint, object? response)
    {
        _offlineResponses[endpoint] = response;
    }

    public Task<T> SendWebApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        WebApiRequests.Add(new CloudDriveGatewayRequest(userId, cloudAccountUid, method, endpoint, context));
        return Task.FromResult(Cast<T>(
            _webApiResponses.TryGetValue(endpoint, out object? response) ? response : WebApiResponse));
    }

    public Task<T> SendUserCenterRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        UserCenterRequests.Add(new CloudDriveGatewayRequest(userId, cloudAccountUid, method, endpoint, context));
        return Task.FromResult(Cast<T>(
            _userCenterResponses.TryGetValue(endpoint, out object? response) ? response : UserCenterResponse));
    }

    public Task<T> SendOfflineApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        OfflineRequests.Add(new CloudDriveGatewayRequest(userId, cloudAccountUid, method, endpoint, context));
        return Task.FromResult(Cast<T>(
            _offlineResponses.TryGetValue(endpoint, out object? response) ? response : OfflineResponse));
    }

    public Task<T> SendUploadInitRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        UploadInitContexts.Add(context);
        return Task.FromResult(Cast<T>(UploadInitResponse));
    }

    public async Task<T> UploadFileAsync<T>(
        Guid userId,
        string cloudAccountUid,
        string uploadUrl,
        Stream fileStream,
        string fileName,
        IDictionary<string, string>? formFields = null,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(fileStream);
        string content = await reader.ReadToEndAsync(cancellationToken);

        UploadRequests.Add(new CloudDriveUploadRequest(
            userId,
            cloudAccountUid,
            uploadUrl,
            fileName,
            formFields is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(formFields, StringComparer.Ordinal),
            content));

        return Cast<T>(UploadFileResponse);
    }

    private static T Cast<T>(object? value)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (typeof(T) == typeof(JsonElement))
        {
            if (value is string json)
            {
                using JsonDocument valueDocument = JsonDocument.Parse(json);
                return (T)(object)valueDocument.RootElement.Clone();
            }

            using JsonDocument document = JsonDocument.Parse("""{"state":true}""");
            return (T)(object)document.RootElement.Clone();
        }

        return default!;
    }
}
