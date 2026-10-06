namespace Application.Tests.Support;

internal sealed record CloudDriveUploadRequest(
    Guid UserId,
    string CloudAccountUid,
    string UploadUrl,
    string FileName,
    IReadOnlyDictionary<string, string> FormFields,
    string Content);
