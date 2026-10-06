using System.Reflection;

using Domain.Entities;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// 115 云盘聚合在 SqlSugar CodeFirst 中使用的表名、列名和索引约定。
/// </summary>
internal static class CloudDriveSqlSugarMapping
{
    /// <summary>
    /// 为 115 云盘聚合应用数据库表名约定。
    /// </summary>
    public static void ApplyEntityName(Type entityType, EntityInfo entity)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(entity);

        if (entityType == typeof(CookieSession))
        {
            // Cookie 会话按 user_id + cloud_account_uid 唯一，防止同一 115 账号被重复保存为多条有效会话。
            entity.DbTableName = "cloud_drive_cookie_sessions";
            entity.TableDescription = "115 Cookie sessions";
            EnsureCookieSessionIndexes(entity);
            return;
        }

        if (entityType == typeof(User))
        {
            entity.DbTableName = "users";
            entity.TableDescription = "Personal users";
            EnsureUserIndexes(entity);
            return;
        }

        if (entityType == typeof(UserSession))
        {
            entity.DbTableName = "user_sessions";
            entity.TableDescription = "JWT login sessions";
            return;
        }

        if (entityType == typeof(CloudDriveJob))
        {
            // 任务表单独命名，避免与通用后台任务或 Quartz 表混淆。
            entity.DbTableName = "cloud_drive_jobs";
            entity.TableDescription = "115 cloud drive jobs";
        }
    }

    /// <summary>
    /// 为 115 云盘聚合应用数据库列约定。
    /// </summary>
    public static void ApplyColumn(PropertyInfo property, EntityColumnInfo column)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(column);

        Type? declaringType = property.DeclaringType;

        if (property.Name == nameof(Entity<Guid>.Id))
        {
            // 所有聚合继承 Entity<Guid>，统一把 Id 映射为数据库主键列 id。
            column.IsPrimarykey = true;
            column.DbColumnName = "id";
            return;
        }

        if (declaringType == typeof(CookieSession))
        {
            ApplyCookieSessionColumn(property, column);
            return;
        }

        if (declaringType == typeof(User))
        {
            switch (property.Name)
            {
                case nameof(User.UserName):
                    column.DbColumnName = "user_name";
                    column.Length = 128;
                    break;
                case nameof(User.PasswordHash):
                    column.DbColumnName = "password_hash";
                    column.Length = 512;
                    break;
                case nameof(User.CreationTime):
                    column.DbColumnName = "creation_time";
                    break;
                case nameof(User.LastModificationTime):
                    column.DbColumnName = "last_modification_time";
                    column.IsNullable = true;
                    break;
            }

            return;
        }

        if (declaringType == typeof(UserSession))
        {
            switch (property.Name)
            {
                case nameof(UserSession.UserId): column.DbColumnName = "user_id"; break;
                case nameof(UserSession.ActiveCloudAccountUid): column.DbColumnName = "active_cloud_account_uid"; column.Length = 128; column.IsNullable = true; break;
                case nameof(UserSession.ExpiresAt): column.DbColumnName = "expires_at"; break;
                case nameof(UserSession.RevokedAt): column.DbColumnName = "revoked_at"; column.IsNullable = true; break;
                case nameof(UserSession.CreationTime): column.DbColumnName = "creation_time"; break;
                case nameof(UserSession.LastSeenAt): column.DbColumnName = "last_seen_at"; break;
            }
        }

        if (declaringType == typeof(CloudDriveJob))
        {
            ApplyCloudDriveJobColumn(property, column);
        }
    }

    /// <summary>
    /// 应用 Cookie 会话表的列名和列类型约定。
    /// </summary>
    private static void ApplyCookieSessionColumn(PropertyInfo property, EntityColumnInfo column)
    {
        switch (property.Name)
        {
            case nameof(CookieSession.UserId):
                column.DbColumnName = "user_id";
                break;
            case nameof(CookieSession.CloudAccountUid):
                column.DbColumnName = "cloud_account_uid";
                column.Length = 128;
                break;
            case nameof(CookieSession.EncryptedCookiePayload):
                column.DbColumnName = "encrypted_cookie_payload";

                // Cookie 密文可能随 Data Protection 元数据增长，使用 TEXT 避免被默认长度截断。
                column.DataType = "TEXT";
                column.Length = 0;
                break;
            case nameof(CookieSession.KeyVersion):
                column.DbColumnName = "key_version";
                break;
            case nameof(CookieSession.Status):
                column.DbColumnName = "status";
                column.Length = 32;
                break;
            case nameof(CookieSession.InvalidationReason):
                column.DbColumnName = "invalidation_reason";
                column.Length = 512;
                column.IsNullable = true;
                break;
            case nameof(CookieSession.CooldownUntil):
                column.DbColumnName = "cooldown_until";
                column.IsNullable = true;
                break;
            case nameof(CookieSession.LastProbedAt):
                column.DbColumnName = "last_probed_at";
                column.IsNullable = true;
                break;
            case nameof(CookieSession.CreationTime):
                column.DbColumnName = "creation_time";
                break;
            case nameof(CookieSession.LastModificationTime):
                column.DbColumnName = "last_modification_time";
                column.IsNullable = true;
                break;
        }
    }

    /// <summary>
    /// 应用 115 云盘后台任务表的列名和列类型约定。
    /// </summary>
    private static void ApplyCloudDriveJobColumn(PropertyInfo property, EntityColumnInfo column)
    {
        switch (property.Name)
        {
            case nameof(CloudDriveJob.UserId):
                column.DbColumnName = "user_id";
                break;
            case nameof(CloudDriveJob.CloudAccountUid):
                column.DbColumnName = "cloud_account_uid";
                column.Length = 128;
                break;
            case nameof(CloudDriveJob.JobType):
                column.DbColumnName = "job_type";
                column.Length = 64;
                break;
            case nameof(CloudDriveJob.Status):
                column.DbColumnName = "status";
                column.Length = 32;
                break;
            case nameof(CloudDriveJob.ResourceId):
                column.DbColumnName = "resource_id";
                column.Length = 256;
                column.IsNullable = true;
                break;
            case nameof(CloudDriveJob.Progress):
                column.DbColumnName = "progress";
                break;
            case nameof(CloudDriveJob.ErrorMessage):
                column.DbColumnName = "error_message";
                column.Length = 1024;
                column.IsNullable = true;
                break;
            case nameof(CloudDriveJob.PayloadJson):
                column.DbColumnName = "payload_json";

                // 任务输入可能保存完整上游请求参数，使用 TEXT 兼容较大的 JSON 负载。
                column.DataType = "TEXT";
                column.Length = 0;
                column.IsNullable = true;
                break;
            case nameof(CloudDriveJob.CheckpointData):
                column.DbColumnName = "checkpoint_data";

                // 断点数据由不同任务类型自行定义，保持 TEXT 以承载可扩展 JSON。
                column.DataType = "TEXT";
                column.Length = 0;
                column.IsNullable = true;
                break;
            case nameof(CloudDriveJob.StartTime):
                column.DbColumnName = "start_time";
                column.IsNullable = true;
                break;
            case nameof(CloudDriveJob.EndTime):
                column.DbColumnName = "end_time";
                column.IsNullable = true;
                break;
            case nameof(CloudDriveJob.CreationTime):
                column.DbColumnName = "creation_time";
                break;
        }
    }

    /// <summary>
    /// 确保 Cookie 会话表存在用户账号唯一索引。
    /// </summary>
    private static void EnsureCookieSessionIndexes(EntityInfo entity)
    {
        const string IndexName = "ux_cloud_drive_cookie_sessions_user_account";

        entity.Indexs ??= [];

        if (entity.Indexs.Any(index => index.IndexName == IndexName))
        {
            // ApplyFrameworkConventions 可能被多次调用，重复添加同名索引会导致 CodeFirst 建表失败。
            return;
        }

        entity.Indexs.Add(new SugarIndexAttribute(
            IndexName,
            nameof(CookieSession.UserId),
            OrderByType.Asc,
            nameof(CookieSession.CloudAccountUid),
            OrderByType.Asc,
            isUnique: true));
    }

    private static void EnsureUserIndexes(EntityInfo entity)
    {
        const string indexName = "ux_users_user_name";
        entity.Indexs ??= [];
        if (entity.Indexs.Any(index => index.IndexName == indexName))
        {
            return;
        }

        entity.Indexs.Add(new SugarIndexAttribute(indexName, nameof(User.UserName), OrderByType.Asc, isUnique: true));
    }
}
