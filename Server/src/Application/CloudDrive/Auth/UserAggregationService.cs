using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Exceptions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 115 用户中心聚合服务。按 UserId + CloudAccountUid 上下文并行调用用户资料、空间与 VIP 相关接口。
/// </summary>
public class UserAggregationService(
    ICloudDriveWebApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions,
    ILogger<UserAggregationService> logger)
{
    /// <summary>
    /// 并行聚合用户主资料、VIP、空间和余额信息；单个子接口失败时对应字段保持空值。
    /// </summary>
    /// <returns>返回聚合后的 115 用户资料。</returns>
    public async Task<CloudDriveUserProfileDto> AggregateUserProfileAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        Task<JsonElement?> spaceTask = FetchSpaceAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> vipLimitTask = FetchVipLimitAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> vipCardTask = FetchVipCardAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> balanceTask = FetchBalanceAsync(userId, cloudAccountUid, cancellationToken);

        await Task.WhenAll(spaceTask, vipLimitTask, vipCardTask, balanceTask);

        JsonElement? space = await spaceTask;
        JsonElement? vipLimit = await vipLimitTask;
        JsonElement? vipCard = await vipCardTask;
        JsonElement? balance = await balanceTask;

        if (space is null
            && vipLimit is null
            && vipCard is null
            && balance is null)
        {
            throw new CloudDriveApiException(
                $"115 用户信息聚合全部子接口失败 CloudAccountUid={cloudAccountUid}",
                "user_aggregation_all_sources_failed");
        }

        return new CloudDriveUserProfileDto
        {
            Info = null,
            Storage = space.HasValue ? MapSpace(space.Value) : null,
            Vip = MapVip(vipLimit, vipCard),
            Balance = MapBalance(balance),
        };
    }

    /// <summary>
    /// 聚合用户基础信息。
    /// </summary>
    /// <returns>返回聚合后的 115 用户基础信息。</returns>
    public async Task<CloudDriveUserInfoDto> AggregateUserInfoAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CloudDriveUserProfileDto profile = await AggregateUserProfileAsync(
            userId,
            cloudAccountUid,
            cancellationToken);

        return profile.Info ?? new CloudDriveUserInfoDto { SchemaStatus = "not_collected" };
    }

    private Task<JsonElement?> FetchSpaceAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            Category = "user",
            QueryParameters = new Dictionary<string, string>
            {
                ["count_space_nums"] = "1",
            },
        };

        return FetchAsync(
            () => openApiGateway.SendWebApiRequestAsync<JsonElement>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "files/index_info",
                context,
                cancellationToken),
            "files/index_info",
            cloudAccountUid);
    }

    private Task<JsonElement?> FetchVipLimitAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            Category = "user",
            QueryParameters = new Dictionary<string, string>
            {
                ["feature"] = "2",
            },
        };

        return FetchAsync(
            () => openApiGateway.SendWebApiRequestAsync<JsonElement>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "user/vip_limit",
                context,
                cancellationToken),
            "user/vip_limit",
            cloudAccountUid);
    }

    private Task<JsonElement?> FetchVipCardAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            TargetDomain = configOptions.Value.VipApiBaseUrl,
            Category = "user",
            QueryParameters = new Dictionary<string, string>
            {
                ["ct"] = "order",
                ["ac"] = "get_yun_card",
            },
        };

        return FetchAsync(
            () => openApiGateway.SendUserCenterRequestAsync<JsonElement>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                ".",
                context,
                cancellationToken),
            "vip/get_yun_card",
            cloudAccountUid);
    }

    private Task<JsonElement?> FetchBalanceAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            TargetDomain = configOptions.Value.PayApiBaseUrl,
            Category = "user",
            QueryParameters = new Dictionary<string, string>
            {
                ["ct"] = "balance",
                ["ac"] = "getBalance",
            },
        };

        return FetchAsync(
            () => openApiGateway.SendUserCenterRequestAsync<JsonElement>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                ".",
                context,
                cancellationToken),
            "pay/balance",
            cloudAccountUid);
    }

    private async Task<JsonElement?> FetchAsync(
        Func<Task<JsonElement>> fetch,
        string subEndpoint,
        string cloudAccountUid)
    {
        try
        {
            return await fetch();
        }
        catch (CloudDriveCookieInvalidatedException)
        {
            throw;
        }
        catch (CloudDriveConfigurationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "115 用户聚合子接口失败 SubEndpoint={SubEndpoint}, CloudAccountUid={CloudAccountUid}",
                subEndpoint,
                cloudAccountUid);

            return null;
        }
    }

    private static CloudDriveUserInfoDto MapProfile(JsonElement? myProfile, string cloudAccountUid)
    {
        JsonElement? mySource = null;
        if (myProfile.HasValue)
        {
            mySource = TryGetObject(myProfile.Value, "data", out JsonElement data) ? data : myProfile.Value;
        }

        return new CloudDriveUserInfoDto
        {
            Uid = ReadString(mySource, "user_id")
                ?? cloudAccountUid,
            Nickname = ReadString(mySource, "user_name")
                ?? ReadString(mySource, "nickname")
                ?? string.Empty,
            AvatarUrl = ReadString(mySource, "user_face")
                ?? ReadString(mySource, "user_face_s")
                ?? ReadString(mySource, "user_face_m")
                ?? ReadString(mySource, "user_face_l"),
            IsVip = ReadBoolean(mySource, "is_vip") ?? false,
            MaskedPhone = ReadString(mySource, "pub_mobile_mask"),
            MaskedEmail = ReadString(mySource, "pub_email_mask"),
        };
    }

    private static CloudDriveSpaceInfoDto MapSpace(JsonElement spaceResponse)
    {
        JsonElement source = TryGetObject(spaceResponse, "data", out JsonElement data)
            ? data
            : spaceResponse;
        JsonElement spaceInfo = TryGetObject(source, "space_info", out JsonElement info) ? info : source;
        return new CloudDriveSpaceInfoDto
        {
            SchemaStatus = "available",
            AllTotal = ReadSpaceUsage(spaceInfo, "all_total"),
            AllUse = ReadSpaceUsage(spaceInfo, "all_use"),
            AllRemain = ReadSpaceUsage(spaceInfo, "all_remain"),
            TypeCounts = ReadTypeCounts(source),
            TypeSummaries = ReadTypeSummaries(source),
        };
    }

    private static CloudDriveVipInfoDto? MapVip(JsonElement? vipLimit, JsonElement? vipCard)
    {
        JsonElement? limitSource = null;
        if (vipLimit.HasValue)
        {
            limitSource = TryGetObject(vipLimit.Value, "data", out JsonElement data) ? data : vipLimit.Value;
        }

        JsonElement? cardSource = null;
        if (vipCard.HasValue)
        {
            cardSource = TryGetObject(vipCard.Value, "data", out JsonElement data) ? data : vipCard.Value;
        }

        string? levelName = limitSource.HasValue
            ? ReadString(limitSource.Value, "level_name")
                ?? ReadString(limitSource.Value, "vip_name")
                ?? ReadString(limitSource.Value, "name")
            : null;

        DateTime? expire = limitSource.HasValue
            ? ReadDateTime(limitSource.Value, "expire")
                ?? ReadDateTime(limitSource.Value, "expire_time")
                ?? ReadDateTime(limitSource.Value, "vip_expire")
            : null;

        CloudDriveYunCardDto? yunCard = cardSource.HasValue ? MapYunCard(cardSource.Value) : null;
        if (string.IsNullOrWhiteSpace(levelName)
            && cardSource.HasValue
            && ReadInt64(cardSource.Value, "vip_count") is long vipCount)
        {
            levelName = $"VIP x{vipCount}";
        }

        if (levelName is null && expire is null && yunCard is null)
        {
            return null;
        }

        return new CloudDriveVipInfoDto
        {
            LevelName = levelName,
            Expire = expire,
            UserLimit = limitSource.HasValue ? MapVipLimit(limitSource.Value, "user_limit") : null,
            LimitList = limitSource.HasValue ? MapVipLimits(limitSource.Value) : [],
            VipList = ReadObjectDictionary(limitSource, "vip_list"),
            YunCard = yunCard,
        };
    }

    private static CloudDriveBalanceDto? MapBalance(JsonElement? balance)
    {
        JsonElement? balanceSource = null;
        if (balance.HasValue)
        {
            balanceSource = TryGetObject(balance.Value, "data", out JsonElement data) ? data : balance.Value;
        }

        if (!balanceSource.HasValue)
        {
            return null;
        }

        string? displayText = ReadString(balanceSource, "money");
        decimal? amount = ReadDecimal(balanceSource.Value, "money");

        return new CloudDriveBalanceDto
            {
                Balance = amount,
                DisplayText = displayText,
                CanWithdraw = ReadString(balanceSource, "can_withdraw"),
                CantWithdraw = ReadString(balanceSource, "cant_withdraw"),
                Detail = ReadDouble(balanceSource, "detail"),
                FeedbackActMoney = ReadDouble(balanceSource, "feedback_act_money"),
                Money = ReadString(balanceSource, "money"),
            };
    }

    private static CloudDriveSpaceUsageDto? ReadSpaceUsage(JsonElement source, string propertyName)
    {
        if (!TryGetObject(source, propertyName, out JsonElement value))
        {
            return null;
        }

        return new CloudDriveSpaceUsageDto
        {
            Size = ReadInt64(value, "size") ?? 0,
            SizeFormat = ReadString(value, "size_format"),
        };
    }

    private static IDictionary<string, long> ReadTypeCounts(JsonElement source)
    {
        if (!TryGetObject(source, "count_space_nums", out JsonElement counts)
            || !TryGetObject(counts, "type_nums", out JsonElement typeNums))
        {
            return new Dictionary<string, long>();
        }

        Dictionary<string, long> result = new(StringComparer.Ordinal);
        foreach (JsonProperty property in typeNums.EnumerateObject())
        {
            result[property.Name] = ReadInt64(property.Value, "count") ?? 0;
        }

        return result;
    }

    private static IDictionary<string, CloudDriveTypeSummaryDto> ReadTypeSummaries(JsonElement source)
    {
        if (!TryGetObject(source, "count_space_nums", out JsonElement counts)
            || !TryGetObject(counts, "type_summury", out JsonElement summaries))
        {
            return new Dictionary<string, CloudDriveTypeSummaryDto>();
        }

        Dictionary<string, CloudDriveTypeSummaryDto> result = new(StringComparer.Ordinal);
        foreach (JsonProperty property in summaries.EnumerateObject())
        {
            result[property.Name] = new CloudDriveTypeSummaryDto
            {
                Count = ReadInt64(property.Value, "count") ?? 0,
                Pecent = ReadDouble(property.Value, "pecent") ?? 0,
                Size = ReadInt64(property.Value, "size") ?? 0,
                SizeFormat = ReadString(property.Value, "size_format"),
            };
        }

        return result;
    }

    private static CloudDriveYunCardDto MapYunCard(JsonElement source)
    {
        return new CloudDriveYunCardDto
        {
            Balance00 = ReadDouble(source, "00_balance") ?? 0,
            ForeverCount = ReadDouble(source, "forever_count") ?? 0,
            Num = ReadDouble(source, "num") ?? 0,
            SpaceCount = ReadDouble(source, "space_count") ?? 0,
            SqcodeCancelCount = ReadString(source, "sqcode_cancel_count"),
            SqcodeCount = ReadString(source, "sqcode_count"),
            Url = ReadString(source, "url"),
            VipCount = ReadDouble(source, "vip_count") ?? 0,
        };
    }

    private static CloudDriveVipLimitDto? MapVipLimit(JsonElement source, string propertyName)
    {
        return TryGetObject(source, propertyName, out JsonElement value)
            ? MapVipLimitValue(value)
            : null;
    }

    private static IReadOnlyList<CloudDriveVipLimitDto> MapVipLimits(JsonElement source)
    {
        if (!source.TryGetProperty("limit_list", out JsonElement list) || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return list.EnumerateArray().Select(MapVipLimitValue).ToArray();
    }

    private static CloudDriveVipLimitDto MapVipLimitValue(JsonElement source)
    {
        return new CloudDriveVipLimitDto
        {
            Feature = ReadString(source, "feature"),
            Level = ReadString(source, "level"),
            LevelName = ReadString(source, "level_name"),
            LimitNumbers = ReadString(source, "limit_numbers"),
        };
    }

    private static IDictionary<string, object?> ReadObjectDictionary(JsonElement? source, string propertyName)
    {
        if (!source.HasValue || !source.Value.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(value.GetRawText())
            ?? new Dictionary<string, object?>();
    }

    private static bool TryGetObject(JsonElement source, string propertyName, out JsonElement value)
    {
        if (source.ValueKind == JsonValueKind.Object
            && source.TryGetProperty(propertyName, out value)
            && value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string? ReadString(JsonElement? source, string propertyName)
    {
        if (!source.HasValue
            || source.Value.ValueKind != JsonValueKind.Object
            || !source.Value.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    private static long? ReadInt64(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), out long stringNumber))
        {
            return stringNumber;
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && decimal.TryParse(
                value.GetString(),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal stringNumber)
                ? stringNumber
                : null;
    }

    private static double? ReadDouble(JsonElement? source, string propertyName)
    {
        if (!source.HasValue || source.Value.ValueKind != JsonValueKind.Object
            || !source.Value.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : null;
    }

    private static bool? ReadBoolean(JsonElement? source, string propertyName)
    {
        if (!source.HasValue
            || source.Value.ValueKind != JsonValueKind.Object
            || !source.Value.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
        {
            return number != 0;
        }

        return value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed)
            ? parsed
            : null;
    }

    private static DateTime? ReadDateTime(JsonElement source, string propertyName)
    {
        string? value = ReadString(source, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (long.TryParse(value, out long unixSeconds) && unixSeconds > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }

        return DateTime.TryParse(value, out DateTime parsed)
            ? parsed.ToUniversalTime()
            : null;
    }
}
