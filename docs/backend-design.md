# 115 网盘 Web 客户端 — 后端架构设计

> 技术栈：.NET 10 / ASP.NET Core 10 / C# 14 / DDD 四层架构  
> 前端契约基线：`web/src/api/*.ts` + `web/src/types/index.ts`  
> 115 API 基线：`docs/115-api-docs.md`（65 个接口 / 15 个域名）

---

## 一、架构总览

```
┌──────────────────────────────────────────────────────────┐
│                   Vue 3 SPA (web/src)                    │
│              baseURL: /api/cloud-drive                   │
└──────────────┬───────────────────────────────────────────┘
               │ HTTP
┌──────────────▼───────────────────────────────────────────┐
│                  ASP.NET Core WebApi                     │
│         Controllers / Middleware / ProblemDetails        │
├──────────────────────────────────────────────────────────┤
│                  Application Layer                       │
│       Commands / Queries / Handlers / Validators         │
│              MediatR Pipeline Behaviors                  │
├──────────────────────────────────────────────────────────┤
│                    Domain Layer                          │
│    Entities / ValueObjects / AggregateRoots / Events     │
│     Repository Interfaces / Domain Services              │
├──────────────────────────────────────────────────────────┤
│                Infrastructure Layer                      │
│  115ApiClient / MetadataProvider / Persistence / Cache   │
│   SqlSugar / Redis / HttpClientFactory / Quartz.NET      │
└──────────┬────────────────┬──────────────────────────────┘
           │                │
    ┌──────▼──────┐  ┌──────▼──────┐
    │ 115 官方 API │  │ TMDB / 豆瓣 │
    │ 15 个域名    │  │ 元数据 API  │
    └─────────────┘  └─────────────┘
```

### 依赖方向

```
Domain          （纯净，零外部依赖）
    ↑
Application     （仅依赖 Domain）
    ↑
Infrastructure  （实现 Domain 接口）
    ↑
WebApi          （依赖 Application + Infrastructure DI 注册）
```

---

## 二、解决方案结构

```
server/
├── CloudDrive115.sln
├── src/
│   ├── CloudDrive115.Domain/
│   │   ├── Aggregates/
│   │   │   ├── CloudAccount/
│   │   │   │   ├── CloudAccount.cs              # 聚合根
│   │   │   │   ├── CloudAccountCookie.cs         # 值对象
│   │   │   │   └── ICloudAccountRepository.cs
│   │   │   └── VideoSemantic/
│   │   │       # ℹ️ 设计决策：不建 CloudFile 聚合。
│   │   │       # 115 是文件的 System of Record，本项目只做代理+缓存，
│   │   │       # 不持久化文件元数据。FileCategory 枚举移至 Common/
│   │   │       ├── VideoSemanticProfile.cs        # 聚合根
│   │   │       ├── SemanticTag.cs                 # 值对象
│   │   │       ├── AnalysisStatus.cs              # 枚举
│   │   │       ├── MatchSource.cs                 # 枚举
│   │   │       ├── VideoAnalysisRequested.cs      # 领域事件
│   │   │       ├── VideoSemanticUpdated.cs        # 领域事件
│   │   │       └── IVideoSemanticRepository.cs
│   │   ├── Services/
│   │   │   ├── IFilenameParser.cs                 # 领域服务接口
│   │   │   └── FilenameParseResult.cs             # 值对象
│   │   ├── Exceptions/
│   │   │   ├── CloudDriveDomainException.cs
│   │   │   ├── AccountNotFoundException.cs
│   │   │   └── FileNotFoundException.cs
│   │   └── Common/
│   │       ├── Entity.cs
│   │       ├── AggregateRoot.cs
│   │       ├── ValueObject.cs
│   │       ├── IDomainEvent.cs
│   │       └── FileCategory.cs               # 从 CloudFile 聚合移入
│   │
│   ├── CloudDrive115.Application/
│   │   ├── Abstractions/
│   │   │   ├── ICloudDriveApiClient.cs            # 115 API 客户端抽象
│   │   │   ├── IMetadataProvider.cs               # 外部元数据匹配抽象
│   │   │   ├── ICloudDriveCache.cs                # 缓存抽象
│   │   │   └── ICurrentUser.cs                    # 当前用户上下文
│   │   │
│   │   │   # ℹ️ 设计决策：代理层模块（Files/Auth/Video/RecycleBin/Jobs）
│   │   │   # 不使用 Command/Query/Handler 结构，因为它们 90% 是直接代理 115 API，
│   │   │   # 无本地持久化、无业务不变量、无领域事件。
│   │   │   # Controller 直接注入 ICloudDriveApiClient。
│   │   │   # 仅 VideoSemantic 模块保留完整 DDD + MediatR（有本地状态机、领域事件、多数据源聚合）。
│   │   │
│   │   ├── Dtos/                              # 全局共享 DTO（代理层模块用）
│   │   │   ├── CloudDriveFileDto.cs
│   │   │   ├── CloudDriveFileDetailDto.cs
│   │   │   ├── CloudDriveSearchResultDto.cs
│   │   │   ├── CloudDriveFolderDto.cs
│   │   │   ├── CloudDriveDownloadDto.cs
│   │   │   ├── CloudDrivePkceAuthorizationDto.cs
│   │   │   ├── CloudDriveTokenDto.cs
│   │   │   ├── CloudDriveAccountSummaryDto.cs
│   │   │   ├── CloudDriveUserInfoDto.cs
│   │   │   ├── CloudDriveVideoPlayDto.cs
│   │   │   ├── CloudDriveSubtitleDto.cs
│   │   │   ├── CloudDriveVideoTranscodeDto.cs
│   │   │   ├── CloudDriveRecycleBinItemDto.cs
│   │   │   ├── CloudDriveJobDto.cs
│   │   │   ├── CloudDriveOperationResultDto.cs
│   │   │   └── CloudDriveSpaceInfoDto.cs
│   │   ├── VideoSemantic/                     # ✅ 保留 DDD + MediatR
│   │   │   ├── Queries/
│   │   │   │   ├── GetVideoList/
│   │   │   │   │   ├── GetVideoListQuery.cs
│   │   │   │   │   └── GetVideoListQueryHandler.cs
│   │   │   │   ├── GetVideoFacets/
│   │   │   │   │   ├── GetVideoFacetsQuery.cs
│   │   │   │   │   └── GetVideoFacetsQueryHandler.cs
│   │   │   │   └── GetSemanticProfile/
│   │   │   │       ├── GetSemanticProfileQuery.cs
│   │   │   │       └── GetSemanticProfileQueryHandler.cs
│   │   │   ├── Commands/
│   │   │   │   ├── AnalyzeVideos/
│   │   │   │   │   ├── AnalyzeVideosCommand.cs
│   │   │   │   │   └── AnalyzeVideosCommandHandler.cs
│   │   │   │   └── CorrectSemanticProfile/
│   │   │   │       ├── CorrectSemanticProfileCommand.cs
│   │   │   │       └── CorrectSemanticProfileCommandHandler.cs
│   │   │   └── Dtos/
│   │   │       ├── VideoSemanticListItemDto.cs
│   │   │       ├── VideoSemanticProfileDto.cs
│   │   │       ├── VideoFacetsDto.cs
│   │   │       └── SemanticTagDto.cs
│   │   ├── Behaviors/
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── UnhandledExceptionBehavior.cs
│   │   └── Mappings/
│   │       ├── FileMapping.cs                     # ToDto() / ToEntity() 扩展方法
│   │       ├── VideoMapping.cs
│   │       └── VideoSemanticMapping.cs
│   │
│   ├── CloudDrive115.Infrastructure/
│   │   ├── ExternalApis/
│   │   │   ├── Api115/
│   │   │   │   ├── Api115Client.cs                # 实现 ICloudDriveApiClient
│   │   │   │   ├── Api115Options.cs               # 配置项
│   │   │   │   ├── RawModels/                     # 115 原始响应模型
│   │   │   │   │   ├── Raw115FileItem.cs          # fid, n, s, pc, tp, tu, play_long...
│   │   │   │   │   ├── Raw115FileListResponse.cs
│   │   │   │   │   ├── Raw115VideoResponse.cs
│   │   │   │   │   └── Raw115SubtitleResponse.cs
│   │   │   │   └── Mappers/
│   │   │   │       └── Raw115Mapper.cs            # 115 原始字段 → Domain/DTO 映射
│   │   │   └── Metadata/
│   │   │       ├── TmdbMetadataProvider.cs         # 实现 IMetadataProvider
│   │   │       ├── TmdbOptions.cs
│   │   │       └── TmdbModels/
│   │   │           ├── TmdbSearchResult.cs
│   │   │           ├── TmdbMovieDetail.cs
│   │   │           └── TmdbCast.cs
│   │   ├── Persistence/
│   │   │   ├── SqlSugarSetup.cs                   # SqlSugar Scoped 注册
│   │   │   ├── Repositories/
│   │   │   │   ├── CloudAccountRepository.cs
│   │   │   │   └── VideoSemanticRepository.cs
│   │   │   └── Entities/                          # 数据库表映射
│   │   │       ├── CloudAccountEntity.cs
│   │   │       ├── VideoSemanticProfileEntity.cs
│   │   │       ├── VideoContentCategoryEntity.cs
│   │   │       └── VideoActorEntity.cs
│   │   ├── Services/
│   │   │   └── FilenameParser.cs                  # 实现 IFilenameParser
│   │   ├── Caching/
│   │   │   └── CloudDriveCache.cs                 # 实现 ICloudDriveCache
│   │   ├── Jobs/
│   │   │   ├── VideoAnalysisJob.cs                # Quartz Job
│   │   │   └── VideoAnalysisJobOptions.cs
│   │   └── DependencyInjection/
│   │       └── InfrastructureServiceExtensions.cs  # AddInfrastructure()
│   │
│   └── CloudDrive115.WebApi/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── FilesController.cs
│       │   ├── VideoController.cs
│       │   ├── VideoSemanticController.cs          # 新增：视频语义分类
│       │   ├── RecycleBinController.cs
│       │   └── JobsController.cs
│       ├── Middleware/
│       │   └── GlobalExceptionMiddleware.cs        # ProblemDetails 输出
│       ├── Extensions/
│       │   ├── WebApiServiceExtensions.cs          # AddWebApi()
│       │   └── CorsExtensions.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
├── tests/
│   ├── CloudDrive115.Domain.Tests/
│   ├── CloudDrive115.Application.Tests/
│   ├── CloudDrive115.Infrastructure.Tests/
│   └── CloudDrive115.WebApi.Tests/
│
└── Directory.Build.props                           # 全局包版本管理
```

---

## 三、领域模型

### 3.1 CloudAccount 聚合

```csharp
// Domain/Aggregates/CloudAccount/CloudAccount.cs
public class CloudAccount : AggregateRoot
{
    public string CloudAccountUid { get; private set; }
    public string Nickname { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreationTime { get; private set; }

    // Cookie 或 Token 管理（值对象）
    public CloudAccountCookie? Cookie { get; private set; }

    public void UpdateCookie(CloudAccountCookie cookie) { ... }
    public void Deactivate() { ... }
}

// Domain/Aggregates/CloudAccount/CloudAccountCookie.cs
public class CloudAccountCookie : ValueObject
{
    public string UID { get; }
    public string CID { get; }
    public string USSID { get; }
    public DateTime ExpiresAt { get; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
```

### 3.2 VideoSemanticProfile 聚合（核心新增）

```csharp
// Domain/Aggregates/VideoSemantic/VideoSemanticProfile.cs
public class VideoSemanticProfile : AggregateRoot
{
    public Guid Id { get; private set; }
    public string CloudAccountUid { get; private set; }
    public string FileId { get; private set; }
    public string PickCode { get; private set; }
    public string OriginalFileName { get; private set; }

    // 识别结果
    public string? RecognizedTitle { get; private set; }
    public string? OriginalTitle { get; private set; }
    public int? Year { get; private set; }
    public string? PosterUrl { get; private set; }        // TMDB/豆瓣海报（稳定 URL）
    public int? DurationSeconds { get; private set; }
    public string? Plot { get; private set; }

    // 状态
    public AnalysisStatus AnalysisStatus { get; private set; }
    public double? OverallConfidence { get; private set; }
    public MatchSource? MatchSource { get; private set; }
    public DateTime? LastAnalyzedAt { get; private set; }

    // 外部元数据 ID（用于去重和更新）
    public string? ExternalMetadataId { get; private set; }
    public string? ExternalMetadataSource { get; private set; }

    // 导航属性
    private readonly List<SemanticTag> _contentCategories = new();
    public IReadOnlyList<SemanticTag> ContentCategories => _contentCategories.AsReadOnly();

    private readonly List<SemanticTag> _actors = new();
    public IReadOnlyList<SemanticTag> Actors => _actors.AsReadOnly();

    // 工厂方法
    public static VideoSemanticProfile Create(
        string cloudAccountUid, string fileId,
        string pickCode, string originalFileName)
    {
        var profile = new VideoSemanticProfile
        {
            Id = Guid.NewGuid(),
            CloudAccountUid = cloudAccountUid,
            FileId = fileId,
            PickCode = pickCode,
            OriginalFileName = originalFileName,
            AnalysisStatus = AnalysisStatus.Pending,
        };
        profile.AddDomainEvent(new VideoAnalysisRequested(profile.Id, fileId));
        return profile;
    }

    // 识别完成
    public void ApplyAnalysisResult(
        string? recognizedTitle, string? originalTitle, int? year,
        string? posterUrl, string? plot,
        IEnumerable<SemanticTag> categories,
        IEnumerable<SemanticTag> actors,
        double confidence, MatchSource source,
        string? externalId, string? externalSource)
    {
        RecognizedTitle = recognizedTitle;
        OriginalTitle = originalTitle;
        Year = year;
        PosterUrl = posterUrl;
        Plot = plot;
        OverallConfidence = confidence;
        MatchSource = source;
        ExternalMetadataId = externalId;
        ExternalMetadataSource = externalSource;
        LastAnalyzedAt = DateTime.UtcNow;

        _contentCategories.Clear();
        _contentCategories.AddRange(categories);
        _actors.Clear();
        _actors.AddRange(actors);

        AnalysisStatus = (_contentCategories.Count > 0 && _actors.Count > 0)
            ? AnalysisStatus.Ready
            : AnalysisStatus.Partial;

        AddDomainEvent(new VideoSemanticUpdated(Id, FileId));
    }

    // 识别失败
    public string? FailureReason { get; private set; }

    public void MarkFailed(string? reason = null)
    {
        AnalysisStatus = AnalysisStatus.Failed;
        FailureReason = reason;
        LastAnalyzedAt = DateTime.UtcNow;
    }

    // 人工校正
    public void CorrectProfile(
        string? title, int? year,
        IEnumerable<SemanticTag>? categories,
        IEnumerable<SemanticTag>? actors)
    {
        if (title != null) RecognizedTitle = title;
        if (year != null) Year = year;
        if (categories != null) { _contentCategories.Clear(); _contentCategories.AddRange(categories); }
        if (actors != null) { _actors.Clear(); _actors.AddRange(actors); }
        MatchSource = MatchSource.Manual;
        AnalysisStatus = AnalysisStatus.Ready;
        AddDomainEvent(new VideoSemanticUpdated(Id, FileId));
    }
}
```

### 3.3 值对象与枚举

```csharp
// Domain/Aggregates/VideoSemantic/SemanticTag.cs
public class SemanticTag : ValueObject
{
    public string Id { get; }
    public string Name { get; }
    public double? Confidence { get; }
    public string? Source { get; }  // "metadata" | "subtitle_nlp" | "vision" | "manual"

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
    }
}

// Domain/Aggregates/VideoSemantic/AnalysisStatus.cs
public enum AnalysisStatus
{
    Pending,       // 待识别
    Processing,    // 识别中
    Ready,         // 已识别
    Failed,        // 识别失败
    Partial,       // 部分识别（有题材无演员，或反之）
}

// Domain/Aggregates/VideoSemantic/MatchSource.cs
public enum MatchSource
{
    Filename,      // 文件名解析
    Metadata,      // 外部元数据匹配（TMDB/豆瓣）
    SubtitleNlp,   // 字幕 NLP
    Vision,        // 视觉识别
    Manual,        // 人工校正
}
```

### 3.4 领域服务接口

```csharp
// Domain/Services/IFilenameParser.cs
public interface IFilenameParser
{
    FilenameParseResult Parse(string filename);
}

// Domain/Services/FilenameParseResult.cs
public class FilenameParseResult : ValueObject
{
    public string? Title { get; init; }
    public string? OriginalTitle { get; init; }
    public int? Year { get; init; }
    public int? Season { get; init; }
    public int? Episode { get; init; }
    public string? Resolution { get; init; }    // "1080p", "4K"
    public string? ReleaseGroup { get; init; }
    public string? Source { get; init; }        // "BluRay", "WEB-DL"

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Title;
        yield return OriginalTitle;
        yield return Year;
        yield return Season;
        yield return Episode;
    }
}
```

### 3.5 仓储接口

```csharp
// Domain/Aggregates/VideoSemantic/IVideoSemanticRepository.cs
public interface IVideoSemanticRepository
{
    Task<VideoSemanticProfile?> GetByFileIdAsync(string cloudAccountUid, string fileId);
    Task<List<VideoSemanticProfile>> GetByFileIdsAsync(string cloudAccountUid, IEnumerable<string> fileIds);
    Task<(List<VideoSemanticProfile> Items, int TotalCount)> GetListAsync(
        VideoSemanticFilter filter);
    Task<VideoFacetsRaw> GetFacetsAsync(string cloudAccountUid);
    Task InsertAsync(VideoSemanticProfile profile);
    Task UpdateAsync(VideoSemanticProfile profile);
    Task<bool> ExistsByFileIdAsync(string cloudAccountUid, string fileId);
}

/// <summary>
/// 查询对象，封装筛选参数（避免仓储接口参数膨胀）
/// </summary>
public class VideoSemanticFilter
{
    public required string CloudAccountUid { get; init; }
    public string? Keyword { get; init; }
    public IEnumerable<string>? CategoryIds { get; init; }
    public IEnumerable<string>? ActorIds { get; init; }
    public int? Year { get; init; }
    public AnalysisStatus? Status { get; init; }
    public int Offset { get; init; }
    public int Limit { get; init; } = 50;
    public string? SortBy { get; init; }
}
```

---

## 四、Application 层关键设计

### 4.1 ICloudDriveApiClient — 115 API 抽象

```csharp
// Application/Abstractions/ICloudDriveApiClient.cs
// ℹ️ 设计决策：接口返回 Application 层 DTO，而非 Infrastructure Raw 模型。
// Raw115FileItem 等原始类型仅在 Infrastructure 实现内部使用，
// 由 Api115Client 在调用 115 API 后立即通过 Raw115Mapper.ToXxxDto() 转换。
// 这样 Application 层无需引用 Infrastructure，保证依赖方向正确。
public interface ICloudDriveApiClient
{
    // 文件管理
    Task<PagedResult<CloudDriveFileDto>> GetFileListAsync(string cloudAccountUid,
        string? cid = null, int offset = 0, int limit = 40,
        string? sortField = null, int? asc = null, int? type = null,
        CancellationToken ct = default);
    Task<CloudDriveFileDetailDto> GetFileDetailAsync(string cloudAccountUid, string fileId,
        CancellationToken ct = default);
    Task<PagedResult<CloudDriveSearchResultDto>> SearchFilesAsync(string cloudAccountUid,
        string keyword, int offset = 0, int limit = 40,
        CancellationToken ct = default);
    Task<List<CloudDriveFolderDto>> GetFolderTreeAsync(string cloudAccountUid, string? parentId = null,
        CancellationToken ct = default);

    // 文件操作
    Task<CloudDriveOperationResultDto> CreateFolderAsync(string cloudAccountUid, string parentId, string name,
        CancellationToken ct = default);
    Task<CloudDriveOperationResultDto> RenameFileAsync(string cloudAccountUid, string fileId, string newName,
        CancellationToken ct = default);
    Task<CloudDriveOperationResultDto> MoveFilesAsync(string cloudAccountUid, string[] fileIds, string targetId,
        CancellationToken ct = default);
    Task<CloudDriveOperationResultDto> CopyFilesAsync(string cloudAccountUid, string[] fileIds, string targetId,
        CancellationToken ct = default);
    Task<CloudDriveOperationResultDto> DeleteFilesAsync(string cloudAccountUid, string[] fileIds,
        CancellationToken ct = default);
    Task<CloudDriveDownloadDto> GetDownloadUrlAsync(string cloudAccountUid, string pickCode,
        CancellationToken ct = default);

    // 视频
    Task<CloudDriveVideoPlayDto> GetVideoPlayUrlAsync(string cloudAccountUid, string pickCode,
        string? quality = null, CancellationToken ct = default);
    Task<List<CloudDriveSubtitleDto>> GetSubtitlesAsync(string cloudAccountUid, string pickCode,
        CancellationToken ct = default);

    // 回收站
    Task<PagedResult<CloudDriveRecycleBinItemDto>> GetRecycleBinListAsync(string cloudAccountUid,
        int offset = 0, int limit = 30, CancellationToken ct = default);
    Task<CloudDriveOperationResultDto> RestoreFilesAsync(string cloudAccountUid, string[] tids,
        CancellationToken ct = default);
    Task<CloudDriveOperationResultDto> PermanentDeleteAsync(string cloudAccountUid, string[] tids,
        CancellationToken ct = default);
    Task<CloudDriveOperationResultDto> ClearRecycleBinAsync(string cloudAccountUid,
        CancellationToken ct = default);

    // 认证 / 用户
    Task<CloudDriveUserInfoDto> GetUserInfoAsync(string cloudAccountUid,
        CancellationToken ct = default);
    Task<CloudDriveSpaceInfoDto> GetSpaceInfoAsync(string cloudAccountUid,
        CancellationToken ct = default);
}

// Application/Abstractions/CloudDriveSpaceInfoDto.cs
public record CloudDriveSpaceInfoDto(
    long TotalBytes,
    long UsedBytes,
    long RemainingBytes);
```

### 4.2 IMetadataProvider — 外部元数据匹配抽象

```csharp
// Application/Abstractions/IMetadataProvider.cs
public interface IMetadataProvider
{
    /// <summary>
    /// 根据文件名解析结果搜索匹配的影视作品
    /// </summary>
    Task<MetadataMatchResult?> SearchAndMatchAsync(FilenameParseResult parsed, CancellationToken ct = default);
}

public class MetadataMatchResult
{
    public string ExternalId { get; init; } = "";
    public string Source { get; init; } = "";          // "tmdb", "douban"
    public string? RecognizedTitle { get; init; }
    public string? OriginalTitle { get; init; }
    public int? Year { get; init; }
    public string? PosterUrl { get; init; }
    public string? Plot { get; init; }
    public double Confidence { get; init; }
    public List<MetadataGenre> Genres { get; init; } = new();
    public List<MetadataCastMember> Cast { get; init; } = new();
}

public record MetadataGenre(string Id, string Name);
public record MetadataCastMember(string Id, string Name, string? ProfileUrl, int Order);
```

### 4.3 视频语义分析 Command

```csharp
// Application/VideoSemantic/Commands/AnalyzeVideos/AnalyzeVideosCommand.cs
public record AnalyzeVideosCommand(
    string CloudAccountUid,
    string[] FileIds
) : IRequest<CloudDriveOperationResultDto>;

// Application/VideoSemantic/Commands/AnalyzeVideos/AnalyzeVideosCommandHandler.cs
public class AnalyzeVideosCommandHandler
    : IRequestHandler<AnalyzeVideosCommand, CloudDriveOperationResultDto>
{
    private readonly IVideoSemanticRepository _repo;
    private readonly ICloudDriveApiClient _apiClient;
    private readonly IFilenameParser _filenameParser;
    private readonly IMetadataProvider _metadataProvider;

    public async Task<CloudDriveOperationResultDto> Handle(
        AnalyzeVideosCommand request, CancellationToken ct)
    {
        // 1. 从 115 API 获取文件基础信息（文件名、pickCode 等）
        // 2. 对每个文件：
        //    a. FilenameParser.Parse(fileName) → 提取标题、年份
        //    b. MetadataProvider.SearchAndMatch(parsed) → 匹配外部元数据
        //    c. 创建或更新 VideoSemanticProfile
        //    d. profile.ApplyAnalysisResult(...)
        //    e. 持久化
        // 3. 对于大批量，发送到 Quartz Job 异步处理

        // 小批量（≤ 10）同步处理
        if (request.FileIds.Length <= 10)
        {
            foreach (var fileId in request.FileIds)
            {
                await AnalyzeSingleFileAsync(request.CloudAccountUid, fileId, ct);
            }
            return new CloudDriveOperationResultDto { Accepted = true };
        }

        // 大批量异步处理
        // 触发 Quartz Job
        return new CloudDriveOperationResultDto
        {
            Accepted = true,
            JobId = jobId,
            Message = $"已提交 {request.FileIds.Length} 个视频的分析任务"
        };
    }
}
```

### 4.4 视频列表查询

```csharp
// Application/VideoSemantic/Queries/GetVideoList/GetVideoListQuery.cs
public record GetVideoListQuery(
    string CloudAccountUid,
    string? DirectoryId = null,
    string? Keyword = null,
    string[]? CategoryIds = null,
    string[]? ActorIds = null,
    int? Year = null,
    AnalysisStatus? AnalysisStatus = null,
    int Offset = 0,
    int Limit = 50,
    string? SortBy = null
) : IRequest<PagedResult<VideoSemanticListItemDto>>;
```

### 4.5 视频筛选维度查询

```csharp
// Application/VideoSemantic/Queries/GetVideoFacets/GetVideoFacetsQuery.cs
public record GetVideoFacetsQuery(
    string CloudAccountUid
) : IRequest<VideoFacetsDto>;

// Application/VideoSemantic/Dtos/VideoFacetsDto.cs
public class VideoFacetsDto
{
    public List<FacetItem> Categories { get; init; } = new();
    public List<FacetItem> Actors { get; init; } = new();
    public List<FacetItem> Years { get; init; } = new();
    public List<FacetItem> AnalysisStatuses { get; init; } = new();
}

public record FacetItem(string Id, string Name, int Count);
```

---

## 五、Infrastructure 层关键设计

### 5.1 115 API 客户端

```csharp
// Infrastructure/ExternalApis/Api115/Api115Client.cs
public class Api115Client : ICloudDriveApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICloudAccountRepository _accountRepo;
    private readonly ICloudDriveCache _cache;
    private readonly Api115Options _options;

    /// <summary>
    /// 构建带 Cookie 认证的 HttpRequestMessage（per-request headers，避免池化泄漏）
    /// ⚠️ 禁止在 HttpClient.DefaultRequestHeaders 上设置 Cookie，
    ///    因为 IHttpClientFactory 返回的实例可能被复用，会导致跨用户 Cookie 泄漏
    /// </summary>
    private async Task<(HttpClient Client, string CookieHeader)> PrepareAuthAsync(
        string cloudAccountUid, string baseUrl)
    {
        var account = await _accountRepo.GetByUidAsync(cloudAccountUid)
            ?? throw new AccountNotFoundException(cloudAccountUid);

        if (account.Cookie is null || account.Cookie.IsExpired)
            throw new CloudDriveDomainException($"账户 {cloudAccountUid} 的 Cookie 已过期");

        var client = _httpClientFactory.CreateClient("Api115");
        client.BaseAddress = new Uri(baseUrl);

        var cookie = $"UID={account.Cookie.UID}; CID={account.Cookie.CID}; USSID={account.Cookie.USSID}";
        return (client, cookie);
    }

    /// <summary>
    /// 使用 HttpRequestMessage 发送请求（Cookie 仅在当前请求生效）
    /// </summary>
    private async Task<T> SendAsync<T>(
        string cloudAccountUid, string baseUrl,
        HttpMethod method, string relativeUrl,
        HttpContent? content = null,
        CancellationToken ct = default)
    {
        var (client, cookie) = await PrepareAuthAsync(cloudAccountUid, baseUrl);

        var request = new HttpRequestMessage(method, relativeUrl);
        request.Headers.Add("Cookie", cookie);
        if (content is not null) request.Content = content;

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new InvalidOperationException($"115 API 返回了 null: {relativeUrl}");
    }

    public async Task<PagedResult<CloudDriveFileDto>> GetFileListAsync(
        string cloudAccountUid,
        string? cid = null, int offset = 0, int limit = 40,
        string? sortField = null, int? asc = null, int? type = null,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["aid"] = "1",
            ["cid"] = cid ?? "0",
            ["o"] = MapSortField(sortField),
            ["asc"] = (asc ?? 0).ToString(),
            ["offset"] = offset.ToString(),
            ["show_dir"] = "1",
            ["limit"] = limit.ToString(),
            ["natsort"] = "1",
            ["format"] = "json",
        };
        if (type.HasValue) query["type"] = type.Value.ToString();

        var url = QueryHelpers.AddQueryString("/files", query);

        // Raw 模型仅在 Infrastructure 内部使用，返回 Application DTO
        var raw = await SendAsync<Raw115FileListResponse>(
            cloudAccountUid, "https://webapi.115.com", HttpMethod.Get, url, ct: ct);

        return new PagedResult<CloudDriveFileDto>
        {
            Items = raw.Data?.Select(r => r.ToFileDto()).ToList() ?? new(),
            TotalCount = raw.Count,
            Offset = offset,
            Limit = limit,
        };
    }
}
```

### 5.2 115 原始字段映射

```csharp
// Infrastructure/ExternalApis/Api115/RawModels/Raw115FileItem.cs
public class Raw115FileItem
{
    [JsonPropertyName("fid")] public string? FileId { get; set; }
    [JsonPropertyName("cid")] public string? CategoryId { get; set; }
    [JsonPropertyName("n")]   public string? Name { get; set; }
    [JsonPropertyName("s")]   public long Size { get; set; }
    [JsonPropertyName("pc")]  public string? PickCode { get; set; }
    [JsonPropertyName("t")]   public string? ModifiedTime { get; set; }
    [JsonPropertyName("te")]  public string? CreatedTime { get; set; }
    [JsonPropertyName("tp")]  public string? ThumbnailType { get; set; }    // 缩略图类型标记
    [JsonPropertyName("tu")]  public string? ThumbnailUrl { get; set; }     // 缩略图 URL
    [JsonPropertyName("ico")] public string? IconType { get; set; }
    [JsonPropertyName("class")] public string? FileClass { get; set; }
    [JsonPropertyName("sha")] public string? Sha1 { get; set; }
    [JsonPropertyName("play_long")] public int PlayDuration { get; set; }   // 视频时长（秒）
    [JsonPropertyName("iv")]  public int IsVideo { get; set; }
    [JsonPropertyName("d")]   public int IsDirectory { get; set; }          // 0=文件, 1=文件夹
    // ... 其他字段
}

// Infrastructure/ExternalApis/Api115/Mappers/Raw115Mapper.cs
public static class Raw115Mapper
{
    public static CloudDriveFileDto ToFileDto(this Raw115FileItem raw)
    {
        return new CloudDriveFileDto
        {
            FileId = raw.FileId ?? "",
            Name = raw.Name ?? "",
            Size = raw.Size,
            FileType = ExtractExtension(raw.Name, raw.IconType),
            IsDirectory = raw.IsDirectory == 1 || raw.FileId?.StartsWith("dir") == true,
            ModifiedTime = ParseTimestamp(raw.ModifiedTime),
            ThumbnailUrl = string.IsNullOrEmpty(raw.ThumbnailUrl) ? null : raw.ThumbnailUrl,
        };
    }

    /// <summary>
    /// 关键映射：115 原始字段 tu → thumbnailUrl
    /// 115 对有缩略图的文件（图片、视频）会在 tu 字段返回缩略图 URL
    /// </summary>
    private static string ExtractExtension(string? name, string? ico)
    {
        if (string.IsNullOrEmpty(name)) return ico ?? "";
        var dot = name.LastIndexOf('.');
        return dot >= 0 ? name[(dot + 1)..].ToLowerInvariant() : ico ?? "";
    }
}
```

### 5.3 TMDB 元数据提供者

```csharp
// Infrastructure/ExternalApis/Metadata/TmdbMetadataProvider.cs
public class TmdbMetadataProvider : IMetadataProvider
{
    private readonly HttpClient _client;
    private readonly TmdbOptions _options;

    public async Task<MetadataMatchResult?> SearchAndMatchAsync(
        FilenameParseResult parsed, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(parsed.Title)) return null;

        // 1. 搜索 TMDB
        var searchUrl = $"/3/search/movie?api_key={_options.ApiKey}" +
            $"&query={Uri.EscapeDataString(parsed.Title)}" +
            (parsed.Year.HasValue ? $"&year={parsed.Year}" : "") +
            "&language=zh-CN";

        var searchResult = await _client.GetFromJsonAsync<TmdbSearchResponse>(searchUrl, ct);
        if (searchResult?.Results is not { Count: > 0 }) return null;

        var best = searchResult.Results[0];

        // 2. 获取详情（含演员表）
        var detailUrl = $"/3/movie/{best.Id}?api_key={_options.ApiKey}" +
            "&language=zh-CN&append_to_response=credits";
        var detail = await _client.GetFromJsonAsync<TmdbMovieDetail>(detailUrl, ct);

        // 3. 组装结果
        return new MetadataMatchResult
        {
            ExternalId = best.Id.ToString(),
            Source = "tmdb",
            RecognizedTitle = detail?.Title ?? best.Title,
            OriginalTitle = detail?.OriginalTitle,
            Year = ParseYear(detail?.ReleaseDate),
            PosterUrl = detail?.PosterPath != null
                ? $"https://image.tmdb.org/t/p/w500{detail.PosterPath}" : null,
            Plot = detail?.Overview,
            Confidence = CalculateConfidence(parsed, best),
            Genres = detail?.Genres?.Select(g =>
                new MetadataGenre(g.Id.ToString(), g.Name)).ToList() ?? new(),
            Cast = detail?.Credits?.Cast?
                .OrderBy(c => c.Order).Take(10)
                .Select(c => new MetadataCastMember(
                    c.Id.ToString(), c.Name, c.ProfilePath, c.Order))
                .ToList() ?? new(),
        };
    }
}
```

### 5.4 文件名解析器

```csharp
// Infrastructure/Services/FilenameParser.cs
public class FilenameParser : IFilenameParser
{
    // 常见模式：
    // "Infernal.Affairs.2002.1080p.BluRay.x264-GROUP.mkv"
    // "[字幕组] 无间道 Infernal Affairs (2002) [1080p].mkv"
    // "无间道.Infernal.Affairs.2002.mkv"

    private static readonly Regex YearPattern =
        new(@"[\.\s\(\[]((?:19|20)\d{2})[\.\s\)\]]", RegexOptions.Compiled);

    private static readonly Regex ResolutionPattern =
        new(@"(2160p|1080p|720p|480p|4K|UHD)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SeasonEpisodePattern =
        new(@"S(\d{1,2})E(\d{1,3})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public FilenameParseResult Parse(string filename)
    {
        // 去掉扩展名
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

        // 提取年份
        var yearMatch = YearPattern.Match(nameWithoutExt);
        int? year = yearMatch.Success ? int.Parse(yearMatch.Groups[1].Value) : null;

        // 提取分辨率
        var resMatch = ResolutionPattern.Match(nameWithoutExt);
        string? resolution = resMatch.Success ? resMatch.Value : null;

        // 提取季集
        var seMatch = SeasonEpisodePattern.Match(nameWithoutExt);
        int? season = seMatch.Success ? int.Parse(seMatch.Groups[1].Value) : null;
        int? episode = seMatch.Success ? int.Parse(seMatch.Groups[2].Value) : null;

        // 提取标题（年份之前的部分）
        string title = ExtractTitle(nameWithoutExt, yearMatch);

        return new FilenameParseResult
        {
            Title = title,
            Year = year,
            Season = season,
            Episode = episode,
            Resolution = resolution,
        };
    }
}
```

### 5.5 数据库表设计

```sql
-- 视频语义资料主表
CREATE TABLE video_semantic_profiles (
    id                      TEXT PRIMARY KEY,   -- GUID
    cloud_account_uid       TEXT NOT NULL,
    file_id                 TEXT NOT NULL,
    pick_code               TEXT NOT NULL,
    original_file_name      TEXT NOT NULL,
    recognized_title        TEXT,
    original_title          TEXT,
    year                    INTEGER,
    poster_url              TEXT,               -- TMDB/豆瓣海报（稳定 URL，可持久化）
    -- ℹ️ 115 原始 tu 字段为临时 URL，不持久化，每次从 115 API 实时获取
    duration_seconds        INTEGER,
    plot                    TEXT,
    analysis_status         INTEGER NOT NULL DEFAULT 0,  -- 枚举
    overall_confidence      REAL,
    match_source            INTEGER,                     -- 枚举
    failure_reason          TEXT,                        -- 识别失败原因
    external_metadata_id    TEXT,
    external_metadata_source TEXT,
    last_analyzed_at        TEXT,
    created_at              TEXT NOT NULL,               -- ISO 8601 格式
    updated_at              TEXT NOT NULL,               -- ISO 8601 格式

    UNIQUE(cloud_account_uid, file_id)
);

-- 内容分类标签（多对多）
CREATE TABLE video_content_categories (
    id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    profile_id              TEXT NOT NULL REFERENCES video_semantic_profiles(id),
    tag_id                  TEXT NOT NULL,
    tag_name                TEXT NOT NULL,
    confidence              REAL,
    source                  TEXT
);

CREATE INDEX ix_vcc_profile ON video_content_categories(profile_id);
CREATE INDEX ix_vcc_tag     ON video_content_categories(tag_id);

-- 演员标签（多对多）
CREATE TABLE video_actors (
    id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    profile_id              TEXT NOT NULL REFERENCES video_semantic_profiles(id),
    actor_id                TEXT NOT NULL,
    actor_name              TEXT NOT NULL,
    confidence              REAL,
    source                  TEXT
);

CREATE INDEX ix_va_profile ON video_actors(profile_id);
CREATE INDEX ix_va_actor   ON video_actors(actor_id);

-- 云账户表
-- ⚠️ Cookie 必须加密存储，使用 Microsoft.AspNetCore.DataProtection
-- 读写时通过 IDataProtector.Protect() / Unprotect() 加解密
CREATE TABLE cloud_accounts (
    cloud_account_uid       TEXT PRIMARY KEY,
    nickname                TEXT,
    avatar_url              TEXT,
    is_active               INTEGER NOT NULL DEFAULT 1,
    cookie_encrypted        BLOB,               -- DataProtection 加密后的 Cookie JSON
    cookie_expires_at       TEXT,                -- ISO 8601 格式
    creation_time           TEXT NOT NULL        -- ISO 8601 格式
);
```

---

## 六、WebApi 层 — Controller 与路由设计

### 6.1 路由前缀

所有 Controller 统一使用 `[Route("api/cloud-drive")]` 前缀，与前端 `baseURL: '/api/cloud-drive'` 对齐。

### 6.2 前端 → 后端 API 完整映射

| 前端调用 | HTTP 方法 | 后端路由 | Controller | 115 官方 API |
|---|---|---|---|---|
| `getAuthorizationUrl()` | GET | `/auth/authorize` | AuthController | — |
| `createPkceSession()` | POST | `/auth/pkce/session` | AuthController | — |
| `exchangeCode()` | POST | `/auth/callback` | AuthController | — |
| `listAccounts()` | GET | `/auth/accounts` | AuthController | — |
| `getUserInfo()` | GET | `/auth/user-info` | AuthController | `GET my.115.com/proapi/...` |
| `disconnect()` | DELETE | `/auth/disconnect` | AuthController | — |
| `getFileList()` | GET | `/files` | FilesController | `GET webapi.115.com/files` |
| `getFileDetail()` | GET | `/files/{fileId}` | FilesController | `GET /open/ufile/info` |
| `createFolder()` | POST | `/files/folder` | FilesController | `POST /open/ufile/add` |
| `renameFile()` | PUT | `/files/{fileId}/rename` | FilesController | `POST /open/ufile/update` |
| `moveFiles()` | POST | `/files/move` | FilesController | `POST /open/ufile/move` |
| `copyFiles()` | POST | `/files/copy` | FilesController | `POST /open/ufile/copy` |
| `deleteFiles()` | POST | `/files/delete` | FilesController | `POST /open/ufile/delete` |
| `getDownloadUrl()` | GET | `/files/{fileId}/download` | FilesController | `POST /open/ufile/downurl` |
| `upload()` | POST | `/files/upload` | FilesController | `POST /open/upload/init` |
| `refreshDirectory()` | POST | `/files/refresh` | FilesController | — |
| `getFolderTree()` | GET | `/files/folders` | FilesController | `GET webapi.115.com/files` (show_dir) |
| `searchFiles()` | GET | `/files/search` | FilesController | `GET webapi.115.com/files/search` |
| `getImagePreview()` | GET | `/files/{fileId}/preview/image` | FilesController | — |
| `getDocPreview()` | GET | `/files/{fileId}/preview/document` | FilesController | — |
| `getPlayUrl()` | POST | `/video/play` | VideoController | `GET webapi.115.com/files/video` |
| `submitTranscode()` | POST | `/video/transcode` | VideoController | `POST /open/video/push` |
| `getSubtitles()` | GET | `/video/{fileId}/subtitles` | VideoController | `GET webapi.115.com/movies/subtitle` |
| `getRecycleBinList()` | GET | `/recycle-bin` | RecycleBinController | `GET webapi.115.com/rb` |
| `restoreFiles()` | POST | `/recycle-bin/restore` | RecycleBinController | `POST /open/rb/revert` |
| `permanentDelete()` | POST | `/recycle-bin/permanent-delete` | RecycleBinController | `POST /open/rb/del` |
| `clearRecycleBin()` | POST | `/recycle-bin/clear` | RecycleBinController | `POST /open/rb/del` |
| `getJobList()` | GET | `/jobs` | JobsController | 本地任务管理 |
| `getJobDetail()` | GET | `/jobs/{jobId}` | JobsController | 本地任务管理 |
| **新增** `getVideoList()` | GET | `/videos` | VideoSemanticController | 本地 DB + 115 API |
| **新增** `getVideoFacets()` | GET | `/videos/facets` | VideoSemanticController | 本地 DB |
| **新增** `getSemanticProfile()` | GET | `/videos/{fileId}/semantic-profile` | VideoSemanticController | 本地 DB |
| **新增** `analyzeVideos()` | POST | `/videos/analyze` | VideoSemanticController | 115 API + TMDB |
| **新增** `correctProfile()` | PATCH | `/videos/{fileId}/semantic-profile` | VideoSemanticController | 本地 DB |
| **预留 Phase N** `getOfflineTasks()` | GET | `/offline/tasks` | OfflineController | `POST 115.com/web/lixian/` |
| **预留 Phase N** `getOfflineQuota()` | GET | `/offline/quota` | OfflineController | `GET 115.com/web/lixian/` |
| **预留 Phase N** `getShareList()` | GET | `/shares` | ShareController | `GET webapi.115.com/share/slist` |
| **预留 Phase N** `getSentShares()` | GET | `/shares/sent` | ShareController | `GET webapi.115.com/share/send` |
| **预留 Phase N** `getReceivedShares()` | GET | `/shares/received` | ShareController | `GET webapi.115.com/share/receive` |

### 6.3 VideoSemanticController 示例

```csharp
[ApiController]
[Route("api/cloud-drive/videos")]
public class VideoSemanticController : ControllerBase
{
    private readonly ISender _sender;

    public VideoSemanticController(ISender sender) => _sender = sender;

    /// <summary>
    /// 视频语义列表（支持分类/演员/年份/状态筛选）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<VideoSemanticListItemDto>>> GetVideoListAsync(
        [FromQuery] string cloudAccountUid,
        [FromQuery] string? directoryId,
        [FromQuery] string? keyword,
        [FromQuery] string? categoryIds,    // 逗号分隔
        [FromQuery] string? actorIds,       // 逗号分隔
        [FromQuery] int? year,
        [FromQuery] AnalysisStatus? analysisStatus,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        [FromQuery] string? sortBy = null)
    {
        var query = new GetVideoListQuery(
            cloudAccountUid, directoryId, keyword,
            categoryIds?.Split(','), actorIds?.Split(','),
            year, analysisStatus, offset, limit, sortBy);
        return Ok(await _sender.Send(query));
    }

    /// <summary>
    /// 筛选维度统计（分类、演员、年份、识别状态）
    /// </summary>
    [HttpGet("facets")]
    public async Task<ActionResult<VideoFacetsDto>> GetFacetsAsync(
        [FromQuery] string cloudAccountUid)
    {
        return Ok(await _sender.Send(new GetVideoFacetsQuery(cloudAccountUid)));
    }

    /// <summary>
    /// 单文件语义详情
    /// </summary>
    [HttpGet("{fileId}/semantic-profile")]
    public async Task<ActionResult<VideoSemanticProfileDto>> GetProfileAsync(
        string fileId, [FromQuery] string cloudAccountUid)
    {
        var result = await _sender.Send(
            new GetSemanticProfileQuery(cloudAccountUid, fileId));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// 触发视频分析
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<CloudDriveOperationResultDto>> AnalyzeAsync(
        [FromBody] AnalyzeVideosRequest request)
    {
        var result = await _sender.Send(
            new AnalyzeVideosCommand(request.CloudAccountUid, request.FileIds));
        return Accepted(result);
    }

    /// <summary>
    /// 人工校正语义资料
    /// </summary>
    [HttpPatch("{fileId}/semantic-profile")]
    public async Task<IActionResult> CorrectProfileAsync(
        string fileId, [FromBody] CorrectProfileRequest request)
    {
        await _sender.Send(new CorrectSemanticProfileCommand(
            request.CloudAccountUid, fileId,
            request.Title, request.Year,
            request.Categories, request.Actors));
        return NoContent();
    }
}
```

---

## 七、115 原始字段映射清单

前端 DTO 字段与 115 原始字段的完整对应关系：

| 前端 DTO 字段 | 115 原始字段 | 说明 |
|---|---|---|
| `fileId` | `fid` | 文件 ID |
| `name` | `n` | 文件名 |
| `size` | `s` | 文件大小（字节） |
| `fileType` | `ico` / 从 `n` 提取扩展名 | 文件类型 |
| `isDirectory` | 响应中的文件夹对象含 `cid` 且无 `fid`；文件对象含 `fid` | 目录判断（需用真实 115 API 响应验证） |
| `modifiedTime` | `t` | 修改时间 |
| `thumbnailUrl` | `tu` | 115 对图片和视频返回缩略图 URL（**临时 URL，有时效，不应持久化**） |
| `pickCode` | `pc` | 视频播放 / 下载凭证 |
| `duration` | `play_long` | 视频时长（秒） |
| `sha1` | `sha` | 文件哈希 |
| `parentId` | `cid` | 父目录 ID |

---

## 八、关键技术决策

### 8.1 持久化选型

| 方案 | 说明 |
|---|---|
| **SQLite**（默认推荐） | 单文件部署，零运维，适合个人/小团队使用。第一阶段用 SQLite 足够 |
| **PostgreSQL**（可选） | 如需多用户并发、全文搜索、JSON 查询，后续可切换 |
| **SqlSugar** | ORM 层使用 SqlSugar，注册为 Scoped |

### 8.1a 弹性策略（Polly v8）

115 API 和 TMDB API 都可能超时、限流、间歇性失败，必须配置弹性策略：

```csharp
// Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs
services.AddHttpClient("Api115")
    .AddResilienceHandler("api115-pipeline", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
        });
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(15),
        });
        builder.AddTimeout(TimeSpan.FromSeconds(30));
    });

services.AddHttpClient("Tmdb")
    .AddResilienceHandler("tmdb-pipeline", builder =>
    {
        builder.AddRateLimiter(new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 4,                    // TmdbRateLimitPerSecond
                Window = TimeSpan.FromSeconds(1),
                SegmentsPerWindow = 4,
            }));
        builder.AddRetry(new HttpRetryStrategyOptions { MaxRetryAttempts = 2 });
        builder.AddTimeout(TimeSpan.FromSeconds(15));
    });
```

### 8.2 缓存策略

| 数据类型 | 缓存方式 | TTL |
|---|---|---|
| 115 文件列表 | 内存缓存（IMemoryCache） | 30 秒 |
| 115 缩略图 URL | 不缓存（临时 URL，有时效） | — |
| TMDB 搜索结果 | Redis / 内存缓存 | 24 小时 |
| 视频语义资料 | SQLite 持久化 + 内存热缓存 | 持久 |
| Facets 统计 | 内存缓存 | 5 分钟 |

### 8.3 认证方案

115 使用 Cookie 认证，无标准 OAuth。后端需要：

1. **首次登录**：用户在 115 官网登录后，通过浏览器扩展或手动输入方式将 Cookie 传给后端
2. **Cookie 存储**：通过 `Microsoft.AspNetCore.DataProtection` 的 `IDataProtector` 加密后存储在 SQLite `cloud_accounts.cookie_encrypted` 列
3. **请求代理**：每次调用 115 API 时，从 DB 读取并解密 Cookie，通过 `HttpRequestMessage.Headers` 附加（per-request，不用 DefaultRequestHeaders）
4. **过期检测**：115 API 返回 401/特定错误码时，通知前端重新登录

### 8.4 异步任务

| 场景 | 方案 |
|---|---|
| 小批量视频分析（≤ 10 个） | 同步在 Handler 中完成 |
| 大批量视频分析（> 10 个） | Quartz.NET Job 异步处理 |
| 全量目录扫描 | 定时 Quartz Job，增量扫描 |
| 任务进度查询 | 通过 `/jobs/{jobId}` 轮询 |

**Quartz Job 与 MediatR 的交互方式**：

```csharp
// Infrastructure/Jobs/VideoAnalysisJob.cs
public class VideoAnalysisJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;

    public async Task Execute(IJobExecutionContext context)
    {
        // 通过 IServiceScopeFactory 创建 scope，复用 MediatR Handler 逻辑
        // 这样 Pipeline Behavior（日志、验证）也能生效
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var fileIds = context.MergedJobDataMap.GetString("FileIds")?.Split(',') ?? [];
        var uid = context.MergedJobDataMap.GetString("CloudAccountUid") ?? "";

        await sender.Send(new AnalyzeVideosCommand(uid, fileIds));
    }
}
```

### 8.5 外部元数据 API

| 来源 | 优先级 | 说明 |
|---|---|---|
| **TMDB** | P0 | 免费 API，覆盖全球影视，有中文数据，演员信息完整 |
| **豆瓣**（非官方） | P2 | 中文影视覆盖更好，但无官方 API，需要爬取或第三方 |
| **Bangumi** | P3 | 动画/番剧元数据 |

**TMDB API Key 获取**：https://www.themoviedb.org/settings/api （免费注册）

---

## 九、分阶段实施计划

### Phase 1 — 基础代理层（P0）

**目标**：前端能跑通所有现有功能

- 创建 .NET 解决方案骨架
- 实现 `Api115Client`（115 API 代理 + 字段映射）
- 实现 `FilesController`、`AuthController`、`VideoController`、`RecycleBinController`、`JobsController`
- 实现全局异常中间件（ProblemDetails）
- 配置 CORS（允许前端 dev server）
- 实现 Cookie 管理和存储

**验收**：前端 `web/src` 连接后端，文件列表、视频播放、搜索、回收站功能可用

### Phase 2 — 视频语义分析（P1）

**目标**：视频能被自动识别和分类

- 实现 `FilenameParser`
- 实现 `TmdbMetadataProvider`
- 实现 `VideoSemanticProfile` 聚合和持久化
- 实现 `AnalyzeVideosCommand` / Handler
- 实现 `VideoSemanticController`
- 实现 `GetVideoListQuery`、`GetVideoFacetsQuery`

**验收**：上传视频文件后，后端能解析文件名 → 匹配 TMDB → 返回题材和演员

### Phase 3 — 前端视频页（P1）

**目标**：前端新增视频分类页面

- 新增 `/videos` 路由和页面
- 左侧筛选栏（分类、演员、年份、状态）
- 视频卡片（封面、标题、时长、分类标签、演员标签）
- 详情抽屉（语义资料、识别来源、人工校正入口）
- 批量分析入口

### Phase 4 — 增强功能（P2）

- 人工校正接口和 UI
- 全量目录扫描 Quartz Job
- 缓存优化
- 豆瓣 / Bangumi 补充数据源
- 字幕 NLP 增强

---

## 十、配置文件结构

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Default": "Data Source=cloud-drive.db"
  },
  "Api115": {
    "DefaultBaseUrl": "https://webapi.115.com",
    "UserInfoBaseUrl": "https://my.115.com",
    "OfflineBaseUrl": "https://115.com",
    "RequestTimeout": 30,
    "MaxRetries": 3
  },
  "Tmdb": {
    "ApiKey": "",
    "BaseUrl": "https://api.themoviedb.org",
    "Language": "zh-CN",
    "ImageBaseUrl": "https://image.tmdb.org/t/p/w500"
  },
  "VideoAnalysis": {
    "SyncBatchLimit": 10,
    "CronSchedule": "0 0 2 * * ?",
    "MaxConcurrency": 3,
    "TmdbRateLimitPerSecond": 4
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

---

## 十一、NuGet 包依赖

| 包 | 层 | 用途 |
|---|---|---|
| `MediatR` | Application | CQRS 管道 |
| `FluentValidation` | Application | 命令/查询验证 |
| `SqlSugarCore` | Infrastructure | ORM |
| `Quartz` + `Quartz.Extensions.Hosting` | Infrastructure | 定时任务 |
| `Microsoft.Extensions.Http` | Infrastructure | IHttpClientFactory |
| `Microsoft.Extensions.Caching.Memory` | Infrastructure | 内存缓存 |
| `StackExchange.Redis`（可选） | Infrastructure | Redis 缓存 |
| `Serilog` + Sinks | WebApi | 结构化日志 |
| `Microsoft.AspNetCore.OpenApi` | WebApi | 内置 OpenAPI 3.1 文档生成（替代 Swashbuckle） |
| `Microsoft.Extensions.Http.Resilience` | Infrastructure | Polly v8 弹性策略 |
| `System.Threading.RateLimiting` | Infrastructure | TMDB 限流 |
