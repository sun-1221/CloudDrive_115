using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Infrastructure.Http;

namespace Infrastructure.Tests.Http;

public sealed class ErrorFieldAdapterTests
{
    [Theory]
    [MemberData(nameof(ErrorShapeCases))]
    public void TryParse_WhenRegisteredUrlMatches_ReadsConfiguredFields(
        string requestUri,
        string json,
        int? expectedCode,
        string expectedMessage,
        string expectedRawField)
    {
        // 准备
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse(json);

        // 执行
        CloudDriveApiError? error = adapter.TryParse(new Uri(requestUri), document);

        // 断言
        Assert.NotNull(error);
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(expectedRawField, error.RawField);
    }

    [Fact]
    public void TryParse_WhenDefaultRuleSeesStateFalseWithoutKnownFields_ReturnsUnknownShape()
    {
        // 准备
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse("""{"state":false,"unexpected":"failure"}""");

        // 执行
        CloudDriveApiError? error = adapter.TryParse(new Uri("https://webapi.115.com/new/endpoint"), document);

        // 断言
        Assert.NotNull(error);
        Assert.Null(error.Code);
        Assert.Equal("unknown_error_shape", error.RawField);
    }

    [Fact]
    public void TryParse_WhenDefaultRuleSeesSuccess_ReturnsNull()
    {
        // 准备
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse("""{"state":true,"code":0,"message":"ok"}""");

        // 执行
        CloudDriveApiError? error = adapter.TryParse(new Uri("https://webapi.115.com/new/endpoint"), document);

        // 断言
        Assert.Null(error);
    }

    [Fact]
    public void RegisteredDescriptors_IncludesRequiredPatterns()
    {
        // 准备
        IReadOnlyDictionary<string, ErrorFieldDescriptor> descriptors = ErrorFieldAdapter.RegisteredDescriptors;

        // 执行 & 断言
        foreach (string pattern in RequiredDescriptorPatterns())
        {
            Assert.Contains(pattern, descriptors.Keys);
        }
    }

    public static TheoryData<string, string, int?, string, string> ErrorShapeCases() => new()
    {
        {
            "https://webapi.115.com/files?cid=0",
            """{"state":false,"errNo":401,"error":"login_required"}""",
            401,
            "login_required",
            "errNo"
        },
        {
            "https://webapi.115.com/files/search",
            """{"state":false,"errCode":1001,"error":"search failed"}""",
            1001,
            "search failed",
            "errCode"
        },
        {
            "https://webapi.115.com/files/video",
            """{"state":false,"errno":404,"error":"not found"}""",
            404,
            "not found",
            "errno"
        },
        {
            "https://webapi.115.com/movies/subtitle",
            """{"state":false,"errNo":2,"error":"subtitle unavailable"}""",
            2,
            "subtitle unavailable",
            "errNo"
        },
        {
            "https://webapi.115.com/rb",
            """{"state":false,"error":"rb failed"}""",
            null,
            "rb failed",
            "error"
        },
        {
            "https://webapi.115.com/share/slist",
            """{"state":false,"errno":9,"error":"share expired"}""",
            9,
            "share expired",
            "errno"
        },
        {
            "https://webapi.115.com/share/send",
            """{"state":false,"errNo":11,"errno":22,"error":"send failed"}""",
            11,
            "send failed",
            "errNo"
        },
        {
            "https://webapi.115.com/user/vip_limit",
            """{"state":false,"code":"vip_denied","message":"vip required"}""",
            null,
            "vip required",
            "code"
        },
        {
            "https://my.115.com/proapi/3.0/index.php",
            """{"state":false,"code":300,"message":"profile failed"}""",
            300,
            "profile failed",
            "code"
        },
        {
            "https://pay.115.com/?ct=balance",
            """{"state":false,"code":5,"msg":"pay failed"}""",
            5,
            "pay failed",
            "code"
        },
        {
            "https://vip.115.com/?ct=order&ac=get_yun_card",
            """{"state":false,"msg":"vip failed"}""",
            null,
            "vip failed",
            "msg"
        },
        {
            "https://115.com/web/lixian/?ct=lixian&ac=task_lists",
            """{"state":false,"errcode":429,"error_msg":"too frequent"}""",
            429,
            "too frequent",
            "errcode"
        },
        {
            "https://act.115.com/api",
            """{"state":0,"code":7,"message":"act failed"}""",
            7,
            "act failed",
            "code"
        },
    };

    private static string[] RequiredDescriptorPatterns() =>
    [
        "webapi.115.com/files",
        "webapi.115.com/files/search",
        "webapi.115.com/files/video",
        "webapi.115.com/movies/subtitle",
        "webapi.115.com/rb",
        "webapi.115.com/share/slist",
        "webapi.115.com/share/send",
        "webapi.115.com/share/receive",
        "webapi.115.com/user/vip_limit",
        "webapi.115.com/files/index_info",
        "my.115.com/proapi/",
        "vip.115.com/",
        "pay.115.com/",
        "act.115.com/",
    ];
}
