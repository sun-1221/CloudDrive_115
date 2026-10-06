using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Infrastructure.Http;

namespace Infrastructure.Tests.Http;

/// <summary>
/// 115 网盘错误字段适配器测试。
/// </summary>
public sealed class ErrorFieldAdapterTests
{
    /// <summary>
    /// 验证注册 URL 匹配时会读取配置的错误字段。
    /// </summary>
    [Theory]
    [MemberData(nameof(ErrorShapeCases))]
    public void TryParse_WhenRegisteredUrlMatches_ReadsConfiguredFields(
        string requestUri,
        string json,
        int? expectedCode,
        string? expectedMessage,
        string expectedRawField)
    {
        // Arrange（准备）
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse(json);

        // Act（执行）
        CloudDriveApiError? error = adapter.TryParse(HttpMethod.Get, new Uri(requestUri), document);

        // Assert（断言）
        Assert.NotNull(error);
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(expectedRawField, error.RawField);
    }

    /// <summary>
    /// 验证默认规则遇到未知 state=false 结构时返回未知错误结构。
    /// </summary>
    [Fact]
    public void TryParse_WhenDefaultRuleSeesStateFalseWithoutKnownFields_ReturnsUnknownShape()
    {
        // Arrange（准备）
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse("""{"state":false,"unexpected":"failure"}""");

        // Act（执行）
        CloudDriveApiError? error = adapter.TryParse(HttpMethod.Get, new Uri("https://webapi.115.com/new/endpoint"), document);

        // Assert（断言）
        Assert.NotNull(error);
        Assert.Null(error.Code);
        Assert.Equal("unknown_error_shape", error.RawField);
    }

    /// <summary>
    /// 验证默认规则遇到成功响应时返回空错误。
    /// </summary>
    [Fact]
    public void TryParse_WhenDefaultRuleSeesSuccess_ReturnsNull()
    {
        // Arrange（准备）
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse("""{"state":true,"code":0,"message":"ok"}""");

        // Act（执行）
        CloudDriveApiError? error = adapter.TryParse(HttpMethod.Get, new Uri("https://webapi.115.com/new/endpoint"), document);

        // Assert（断言）
        Assert.Null(error);
    }

    /// <summary>
    /// 验证 method、path 与关键 query 任一不匹配时不会复用其他接口规则。
    /// </summary>
    [Theory]
    [InlineData("GET", "https://webapi.115.com/files/add", "error")]
    [InlineData("POST", "https://webapi.115.com/files", "errNo")]
    [InlineData("GET", "https://webapi.115.com/user/vip_limit?feature=1", "code")]
    public void TryParse_WhenRequestKeyDoesNotMatch_FailsClosed(
        string method,
        string requestUri,
        string errorField)
    {
        // Arrange（准备）
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse($$"""{"state":false,"{{errorField}}":1}""");

        // Act（执行）
        CloudDriveApiError? error = adapter.TryParse(new HttpMethod(method), new Uri(requestUri), document);

        // Assert（断言）
        Assert.NotNull(error);
        Assert.Null(error.Code);
        Assert.Equal("unknown_error_shape", error.RawField);
    }

    /// <summary>
    /// 验证已注册错误描述符包含必需 URL 模式。
    /// </summary>
    [Fact]
    public void RegisteredDescriptors_WhenLoaded_IncludesRequiredPatterns()
    {
        // Arrange（准备）
        IReadOnlyDictionary<string, ErrorFieldDescriptor> descriptors = ErrorFieldAdapter.RegisteredDescriptors;

        // Act（执行）
        foreach (string pattern in RequiredDescriptorPatterns())
        {
            // Assert（断言）
            Assert.Contains(pattern, descriptors.Keys);
        }
    }

    public static TheoryData<string, string, int?, string?, string> ErrorShapeCases() => new()
    {
        {
            "https://webapi.115.com/files?cid=0",
            """{"state":false,"errNo":401,"error":"login_required"}""",
            401,
            "login_required",
            "errNo"
        },
        {
            "https://webapi.115.com/files/download",
            """{"state":false,"msg_code":1001,"msg":"download failed"}""",
            1001,
            "download failed",
            "msg_code"
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
            "https://webapi.115.com/user/vip_limit?feature=2",
            """{"state":false,"code":"vip_denied","message":"vip required"}""",
            null,
            "vip required",
            "code"
        },
        {
            "https://webapi.115.com/user/setting?format=json",
            """{"state":false,"errno":300,"error":"setting failed"}""",
            300,
            "setting failed",
            "errno"
        },
        {
            "https://pay.115.com/?ct=balance&ac=getBalance",
            """{"state":false,"code":5,"msg":"pay failed"}""",
            5,
            "pay failed",
            "code"
        },
        {
            "https://vip.115.com/?ct=order&ac=get_yun_card",
            """{"state":false,"msg":"vip failed"}""",
            null,
            null,
            "unknown_error_shape"
        },
        {
            "https://webapi.115.com/files/index_info?count_space_nums=1",
            """{"state":false,"code":99,"error":"index info error"}""",
            99,
            "index info error",
            "code"
        },
        {
            "https://vip.115.com/?ct=product&ac=product_list",
            """{"state":false,"code":429,"msg":"too frequent"}""",
            429,
            "too frequent",
            "code"
        },
        {
            "https://vip.115.com/?ct=act_coupon&ac=get_coupon_yhq_list",
            """{"state":false,"code":7}""",
            7,
            "7",
            "code"
        },
    };

    private static string[] RequiredDescriptorPatterns() =>
    [
        "GET webapi.115.com/files",
        "POST webapi.115.com/files/add",
        "POST webapi.115.com/files/batch_rename",
        "POST webapi.115.com/files/copy",
        "POST webapi.115.com/files/star",
        "GET webapi.115.com/files/download",
        "GET webapi.115.com/files/video",
        "GET webapi.115.com/movies/subtitle",
        "GET webapi.115.com/rb",
        "GET webapi.115.com/share/slist",
        "GET webapi.115.com/share/send",
        "GET webapi.115.com/share/receive",
        "GET webapi.115.com/user/vip_limit?feature=2",
        "GET webapi.115.com/files/index_info?count_space_nums=1",
        "GET webapi.115.com/user/setting?format=json",
        "GET pay.115.com/?ct=balance&ac=getBalance",
        "GET vip.115.com/?ct=order&ac=get_yun_card",
        "GET vip.115.com/?ct=product&ac=product_list",
        "GET vip.115.com/?ct=act_coupon&ac=get_coupon_yhq_list",
        "POST clouddownload.115.com/web/?ac=task_lists",
        "POST clouddownload.115.com/web/?ac=task_count",
        "POST clouddownload.115.com/web/?ac=get_quota_package_info",
        "POST clouddownload.115.com/web/?ac=add_task_urls",
        "GET webapi.115.com/files/history",
    ];
}
