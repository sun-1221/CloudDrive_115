using Application.Abstractions.CloudDrive;

namespace Application.Tests.Support;

internal sealed record CloudDriveGatewayRequest(
    Guid UserId,
    string CloudAccountUid,
    HttpMethod Method,
    string Endpoint,
    CloudDriveApiContext Context);
