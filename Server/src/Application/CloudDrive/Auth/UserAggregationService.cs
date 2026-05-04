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
    /// 并行聚合用户主资料、VIP、空间等信息；单个子接口失败时对应字段保持空值。
    /// </summary>
    public async Task<CloudDriveUserInfoDto> AggregateUserInfoAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        Task<JsonElement?> profileTask = FetchProfileAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> myProfileTask = FetchMyProfileAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> spaceTask = FetchSpaceAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> vipLimitTask = FetchVipLimitAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> vipCardTask = FetchVipCardAsync(userId, cloudAccountUid, cancellationToken);
        Task<JsonElement?> balanceTask = FetchBalanceAsync(userId, cloudAccountUid, cancellationToken);

        await Task.WhenAll(profileTask, myProfileTask, spaceTask, vipLimitTask, vipCardTask, balanceTask);

        JsonElement? profile = await profileTask;
        JsonElement? myProfile = await myProfileTask;
        JsonElement? space = await spaceTask;
        JsonElement? vipLimit = await vipLimitTask;
        JsonElement? vipCard = await vipCardTask;
        JsonElement? balance = await balanceTask;

        if (profile is null
            && myProfile is null
            && space is null
            && vipLimit is null
            && vipCard is null
            && balance is null)
        {
            throw new CloudDriveApiException(
                $"115 用户信息聚合全部子接口失败 CloudAccountUid={cloudAccountUid}",
                "user_aggregation_all_sources_failed");
        }

        CloudDriveUserInfoDto result = profile.HasValue || myProfile.HasValue
            ? MapProfile(profile, myProfile, cloudAccountUid)
            : new CloudDriveUserInfoDto { CloudAccountUid = cloudAccountUid };

        result.SpaceInfo = space.HasValue ? MapSpace(space.Value) : null;
        result.VipInfo = MapVip(vipLimit, vipCard, balance);

        return result;
    }

    private Task<JsonElement?> FetchProfileAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            Category = "user",
        };

        return FetchAsync(
            () => openApiGateway.SendWebApiRequestAsync<JsonElement>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "user/info",
                context,
                cancellationToken),
            "user/info",
            cloudAccountUid);
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

    private static CloudDriveUserInfoDto MapProfile(JsonElement? webProfile, JsonElement? myProfile, string cloudAccountUid)
    {
        JsonElement? webSource = null;
        if (webProfile.HasValue)
        {
            webSource = TryGetObject(webProfile.Value, "data", out JsonElement data) ? data : webProfile.Value;
        }

        JsonElement? mySource = null;
        if (myProfile.HasValue)
        {
            mySource = TryGetObject(myProfile.Value, "data", out JsonElement data) ? data : myProfile.Value;
        }

        return new CloudDriveUserInfoDto
        {
            CloudAccountUid = cloudAccountUid,
            UserName = ReadString(webSource, "user_name")
                ?? ReadString(mySource, "user_name")
                ?? ReadString(webSource, "nickname")
                ?? ReadString(mySource, "nickname")
                ?? string.Empty,
            AvatarSmallUrl = ReadString(webSource, "user_face_s")
                ?? ReadString(mySource, "user_face_s")
                ?? ReadString(webSource, "user_face")
                ?? ReadString(mySource, "user_face"),
            AvatarMediumUrl = ReadString(webSource, "user_face_m") ?? ReadString(mySource, "user_face_m"),
            AvatarLargeUrl = ReadString(webSource, "user_face_l") ?? ReadString(mySource, "user_face_l"),
        };
    }

    private static CloudDriveSpaceInfoDto? MapSpace(JsonElement response)
    {
        JsonElement source = TryGetObject(response, "data", out JsonElement data) ? data : response;

        string? total = ReadString(source, "all_total") ?? ReadString(source, "total");
        string? used = ReadString(source, "all_use") ?? ReadString(source, "used");
        string? remain = ReadString(source, "all_remain") ?? ReadString(source, "remain");
        long totalBytes = ReadInt64(source, "all_total_size")
            ?? ReadInt64(source, "all_total_bytes")
            ?? ReadInt64(source, "total_size")
            ?? 0;
        long usedBytes = ReadInt64(source, "all_use_size")
            ?? ReadInt64(source, "all_use_bytes")
            ?? ReadInt64(source, "use_size")
            ?? 0;
        long remainBytes = ReadInt64(source, "all_remain_size")
            ?? ReadInt64(source, "all_remain_bytes")
            ?? ReadInt64(source, "remain_size")
            ?? 0;

        if (total is null && used is null && remain is null && totalBytes == 0 && usedBytes == 0 && remainBytes == 0)
        {
            return null;
        }

        return new CloudDriveSpaceInfoDto
        {
            AllTotal = total ?? string.Empty,
            AllUse = used ?? string.Empty,
            AllRemain = remain ?? string.Empty,
            AllTotalBytes = totalBytes,
            AllUseBytes = usedBytes,
            AllRemainBytes = remainBytes,
        };
    }

    private static CloudDriveVipInfoDto? MapVip(JsonElement? vipLimit, JsonElement? vipCard, JsonElement? balance)
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

        if (levelName is null
            && cardSource.HasValue
            && ReadInt64(cardSource.Value, "00_balance") is long cardBalance)
        {
            levelName = $"Balance {cardBalance}";
        }

        if (levelName is null
            && balance.HasValue
            && ReadInt64(TryGetObject(balance.Value, "data", out JsonElement balanceData) ? balanceData : balance.Value, "00_balance")
                is long payBalance)
        {
            levelName = $"Balance {payBalance}";
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
