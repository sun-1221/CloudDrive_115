using Application.Abstractions.CloudDrive;
using Application.Behaviors;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

namespace Application.Tests.Behaviors;

public sealed class CloudDriveAccountContextBehaviorTests
{
    [Fact]
    public async Task Handle_WhenInputAccountMissing_ResolvesAccountBeforeNext()
    {
        var resolver = new StubResolver(Result.Success(new CloudDriveAccountContext(
            TestQuery.UserIdValue,
            "115001",
            "115001")));
        var accessor = new CloudDriveAccountContextAccessor();
        var behavior = new CloudDriveAccountContextBehavior<TestQuery, Result<string>>(resolver, accessor);
        var input = new GetFileListInput();
        var query = new TestQuery(TestQuery.UserIdValue, input);

        Result<string> result = await behavior.Handle(
            query,
            _ => Task.FromResult(Result.Success(accessor.CloudAccountUid)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("115001", result.Value);
        Assert.Equal("115001", accessor.CloudAccountUid);
    }

    [Fact]
    public async Task Handle_WhenResolverFails_ReturnsFailureWithoutCallingNext()
    {
        var resolver = new StubResolver(Result.Failure<CloudDriveAccountContext>(new Error(
            "cloud_drive_account_context_unavailable",
            "missing")));
        var accessor = new CloudDriveAccountContextAccessor();
        var behavior = new CloudDriveAccountContextBehavior<TestQuery, Result<string>>(resolver, accessor);

        Result<string> result = await behavior.Handle(
            new TestQuery(TestQuery.UserIdValue, new GetFileListInput()),
            _ => throw new InvalidOperationException("next should not run"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_account_context_unavailable", result.Error.Code);
    }

    private sealed record TestQuery(Guid UserId, GetFileListInput Input) : IQuery<string>, ICloudDriveAccountScopedRequest
    {
        public static readonly Guid UserIdValue = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }

    private sealed class StubResolver(Result<CloudDriveAccountContext> result) : ICloudDriveAccountContextResolver
    {
        public Task<Result<CloudDriveAccountContext>> ResolveAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }

}
