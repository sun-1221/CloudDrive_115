using Application.Abstractions.CloudDrive;

namespace Application.Tests.Support;

internal sealed class ThrowingCloudDriveOpenApiGateway(Exception exception) : ICloudDriveWebApiGateway
{
    public Task<T> SendWebApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        throw exception;
    }

    public Task<T> SendUserCenterRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        throw exception;
    }

    public Task<T> SendOfflineApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        throw exception;
    }

    public Task<T> SendUploadInitRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        throw exception;
    }

    public Task<T> UploadFileAsync<T>(
        Guid userId,
        string cloudAccountUid,
        string uploadUrl,
        Stream fileStream,
        string fileName,
        IDictionary<string, string>? formFields = null,
        CancellationToken cancellationToken = default)
    {
        throw exception;
    }
}
