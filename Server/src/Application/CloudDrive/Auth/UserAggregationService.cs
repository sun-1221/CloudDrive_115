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
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions,
    ILogger<UserAggregationService> logger)
{
    /// <summary>
    /// 并行聚合用户主资料、VIP、空间和余额信息；单个子接口失败时对应字段保持空值。
    /// </summary>
    public async Task<CloudDriveUserProfileDto> AggregateUserProfileAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        Task<JsonElement?> myProfileTask = FetchMyProfileAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> spaceTask = FetchSpaceAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> vipLimitTask = FetchVipLimitAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> vipCardTask = FetchVipCardAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> balanceTask = FetchBalanceAsync(userId, cloudAccountUid, cancellationToken);

        await Task.WhenAll(myProfileTask, spaceTask, vipLimitTask, vipCardTask, balanceTask);

        JsonElement? myProfile = await myProfileTask;
        JsonElement? space = await spaceTask;
        JsonElement? vipLimit = await vipLimitTask;
        JsonElement? vipCard = await vipCardTask;
        JsonElement? balance = await balanceTask;

        if (myProfile is null
            && space is null
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
            Info = myProfile.HasValue
                ? MapProfile(myProfile, cloudAccountUid)
                : null,
            Storage = space.HasValue ? MapSpace() : null,
            Vip = MapVip(vipLimit, vipCard),
            Balance = MapBalance(vipCard, balance),
        };
    }

    /// <summary>
    /// 聚合用户基础信息。
    /// </summary>
    public async Task<CloudDriveUserInfoDto> AggregateUserInfoAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CloudDriveUserProfileDto profile = await AggregateUserProfileAsync(
            userId,
            cloudAccountUid,
            cancellationToken);

        return profile.Info ?? new CloudDriveUserInfoDto { Uid = cloudAccountUid };
    }

    private Task<JsonElement?> FetchMyProfileAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            TargetDomain = configOptions.Value.MyApiBaseUrl,
            Category = "user",
            QueryParameters = new Dictionary<string, string>
            {
                ["method"] = "user_info",
                ["uid"] = cloudAccountUid,
            },
        };

        return FetchAsync(
            () => openApiGateway.SendUserCenterRequestAsync<JsonElement>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "proapi/3.0/index.php",
                context,
                cancellationToken),
            "my/user_info",
            cloudAccountUid);
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

    private static CloudDriveSpaceInfoDto MapSpace()
    {
        return new CloudDriveSpaceInfoDto { SchemaStatus = "not_collected" };
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

        if (string.IsNullOrWhiteSpace(levelName)
            && cardSource.HasValue
            && ReadInt64(cardSource.Value, "vip_count") is long vipCount)
        {
            levelName = $"VIP x{vipCount}";
        }

        if (levelName is null && expire is null)
        {
            return null;
        }

        return new CloudDriveVipInfoDto
        {
            LevelName = levelName,
            Expire = expire,
        };
    }

    private static CloudDriveBalanceDto? MapBalance(JsonElement? vipCard, JsonElement? balance)
    {
        JsonElement? cardSource = null;
        if (vipCard.HasValue)
        {
            cardSource = TryGetObject(vipCard.Value, "data", out JsonElement data) ? data : vipCard.Value;
        }

        JsonElement? balanceSource = null;
        if (balance.HasValue)
        {
            balanceSource = TryGetObject(balance.Value, "data", out JsonElement data) ? data : balance.Value;
        }

        decimal? amount = cardSource.HasValue
            ? ReadDecimal(cardSource.Value, "00_balance") ?? ReadDecimal(cardSource.Value, "balance")
            : null;

        amount ??= balanceSource.HasValue
            ? ReadDecimal(balanceSource.Value, "00_balance") ?? ReadDecimal(balanceSource.Value, "balance")
            : null;

        string? displayText = cardSource.HasValue
            ? ReadString(cardSource.Value, "00_balance") ?? ReadString(cardSource.Value, "balance")
            : null;

        displayText ??= balanceSource.HasValue
            ? ReadString(balanceSource.Value, "00_balance") ?? ReadString(balanceSource.Value, "balance")
            : null;

        return amount.HasValue || !string.IsNullOrWhiteSpace(displayText)
            ? new CloudDriveBalanceDto
            {
                Balance = amount,
                DisplayText = displayText,
            }
            : null;
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
