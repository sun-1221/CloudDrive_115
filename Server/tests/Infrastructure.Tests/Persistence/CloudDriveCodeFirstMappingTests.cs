using System.Data;
using System.Globalization;

using Domain.Entities;

using Infrastructure.Persistence;

using SqlSugar;

namespace Infrastructure.Tests.Persistence;

public sealed class CloudDriveCodeFirstMappingTests : IDisposable
{
    private readonly SqlSugarClient _db;

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

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public void InitTables_ForCloudDriveAggregates_CreatesTables()
    {
        // 准备

        // 执行
        SqlSugarSetup.InitTables(_db, typeof(CookieSession), typeof(CloudDriveJob));

        // 断言
        Assert.True(_db.DbMaintenance.IsAnyTable("cloud_drive_cookie_sessions", false));
        Assert.True(_db.DbMaintenance.IsAnyTable("cloud_drive_jobs", false));
    }

    [Fact]
    public void CookieSessionMapping_UsesPrimaryKeyTextPayloadAndUniqueAccountIndex()
    {
        // 准备
        SqlSugarSetup.InitTables(_db, typeof(CookieSession));

        // 执行
        List<DbColumnInfo> columns = _db.DbMaintenance.GetColumnInfosByTableName("cloud_drive_cookie_sessions");
        DataTable indexes = _db.Ado.GetDataTable("PRAGMA index_list('cloud_drive_cookie_sessions')");

        // 断言
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

    [Fact]
    public void CloudDriveJobMapping_UsesTextPayloadAndCheckpointColumns()
    {
        // 准备
        SqlSugarSetup.InitTables(_db, typeof(CloudDriveJob));

        // 执行
        List<DbColumnInfo> columns = _db.DbMaintenance.GetColumnInfosByTableName("cloud_drive_jobs");

        // 断言
        Assert.Contains(columns, column => column.DbColumnName == "payload_json"
            && column.DataType.Contains("TEXT", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, column => column.DbColumnName == "checkpoint_data"
            && column.DataType.Contains("TEXT", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(columns, column => column.DbColumnName == "cloud_account_uid");
        Assert.Contains(columns, column => column.DbColumnName == "status");
    }
}
