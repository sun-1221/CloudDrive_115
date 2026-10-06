using System.Data;
using System.Globalization;

using Domain.Entities;

using Infrastructure.Persistence;

using SqlSugar;

namespace Infrastructure.Tests.Persistence;

/// <summary>
/// 115 网盘 CodeFirst 表结构映射测试。
/// </summary>
public sealed class CloudDriveCodeFirstMappingTests : IDisposable
{
    private readonly SqlSugarClient _db;

    /// <summary>
    /// 初始化内存数据库连接。
    /// </summary>
    public CloudDriveCodeFirstMappingTests()
    {
        var config = new ConnectionConfig
        {
            ConnectionString = "Data Source=:memory:",
            DbType = SqlSugar.DbType.Sqlite,
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute,
        };
        SqlSugarSetup.ApplyFrameworkConventions(config);
        _db = new SqlSugarClient(config);
    }

    /// <summary>
    /// 释放内存数据库连接。
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
    }

    /// <summary>
    /// 验证初始化 115 网盘聚合根表会创建对应数据表。
    /// </summary>
    [Fact]
    public void InitTables_ForCloudDriveAggregates_CreatesTables()
    {
        // Arrange（准备）

        // Act（执行）
        SqlSugarSetup.InitTables(_db, typeof(CookieSession), typeof(CloudDriveJob));

        // Assert（断言）
        Assert.True(_db.DbMaintenance.IsAnyTable("cloud_drive_cookie_sessions", false));
        Assert.True(_db.DbMaintenance.IsAnyTable("cloud_drive_jobs", false));
    }

    /// <summary>
    /// 验证 Cookie 会话映射使用主键、文本载荷和唯一账号索引。
    /// </summary>
    [Fact]
    public void CookieSessionMapping_WhenConfigured_UsesPrimaryKeyTextPayloadAndUniqueAccountIndex()
    {
        // Arrange（准备）
        SqlSugarSetup.InitTables(_db, typeof(CookieSession));

        // Act（执行）
        List<DbColumnInfo> columns = _db.DbMaintenance.GetColumnInfosByTableName("cloud_drive_cookie_sessions");
        DataTable indexes = _db.Ado.GetDataTable("PRAGMA index_list('cloud_drive_cookie_sessions')");

        // Assert（断言）
        DbColumnInfo idColumn = Assert.Single(columns, column => column.DbColumnName == "id");
        Assert.True(idColumn.IsPrimarykey);

        DbColumnInfo payloadColumn = Assert.Single(columns, column => column.DbColumnName == "encrypted_cookie_payload");
        Assert.Contains("TEXT", payloadColumn.DataType, StringComparison.OrdinalIgnoreCase);

        Assert.Contains(indexes.Rows.Cast<DataRow>(), row =>
            string.Equals(
                row["name"]?.ToString(),
                "ux_cloud_drive_cookie_sessions_user_account",
                StringComparison.OrdinalIgnoreCase)
            && Convert.ToInt32(row["unique"], CultureInfo.InvariantCulture) == 1);
    }

    /// <summary>
    /// 验证后台任务映射使用文本载荷和检查点字段。
    /// </summary>
    [Fact]
    public void CloudDriveJobMapping_WhenConfigured_UsesTextPayloadAndCheckpointColumns()
    {
        // Arrange（准备）
        SqlSugarSetup.InitTables(_db, typeof(CloudDriveJob));

        // Act（执行）
        List<DbColumnInfo> columns = _db.DbMaintenance.GetColumnInfosByTableName("cloud_drive_jobs");

        // Assert（断言）
        Assert.Contains(columns, column => column.DbColumnName == "payload_json"
            && column.DataType.Contains("TEXT", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, column => column.DbColumnName == "checkpoint_data"
            && column.DataType.Contains("TEXT", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, column => column.DbColumnName == "cloud_account_uid");
        Assert.Contains(columns, column => column.DbColumnName == "status");
    }
}
