using System.Reflection;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;

using Domain.Results;

using MediatR;

namespace Application.Behaviors;

/// <summary>
/// 在 FluentValidation 前为公开 115 请求补全当前唯一账号上下文。
/// </summary>
public sealed class CloudDriveAccountContextBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;

    public CloudDriveAccountContextBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        CloudDriveAccountScopedInput? accountScopedInput = GetAccountScopedInput(request);
        if (accountScopedInput is null
            || !string.IsNullOrWhiteSpace(accountScopedInput.CloudAccountUid)
            || IsExplicitAccountOperation(request))
        {
            return await next(cancellationToken);
        }

        ICloudDriveAccountContextResolver? resolver = _serviceProvider.GetService(
            typeof(ICloudDriveAccountContextResolver)) as ICloudDriveAccountContextResolver;
        if (resolver is null)
        {
            return await next(cancellationToken);
        }

        Guid userId = GetUserId(request);
        Result<CloudDriveAccountContext> contextResult = await resolver.ResolveAsync(userId, cancellationToken);
        if (contextResult.IsFailure)
        {
            return CreateFailureResponse(contextResult.Error);
        }

        accountScopedInput.CloudAccountUid = contextResult.Value.CloudAccountUid;
        return await next(cancellationToken);
    }

    private static CloudDriveAccountScopedInput? GetAccountScopedInput(TRequest request)
    {
        PropertyInfo? inputProperty = typeof(TRequest).GetProperty("Input");
        object? input = inputProperty?.GetValue(request);

        if (input is CloudDriveAccountScopedInput accountScopedInput)
        {
            return accountScopedInput;
        }

        if (input is GetJobListInput jobListInput)
        {
            return new JobListAccountScopedInput(jobListInput);
        }

        return null;
    }

    private static bool IsExplicitAccountOperation(TRequest request)
    {
        return request is UpsertCookieSessionCommand
            or EnterCookieCooldownCommand
            or RevokeCookieSessionCommand;
    }

    private static Guid GetUserId(TRequest request)
    {
        PropertyInfo? userIdProperty = typeof(TRequest).GetProperty("UserId");
        return userIdProperty?.GetValue(request) is Guid userId ? userId : Guid.Empty;
    }

    private static TResponse CreateFailureResponse(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (typeof(TResponse).IsGenericType
            && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            Type valueType = typeof(TResponse).GetGenericArguments()[0];
            MethodInfo method = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(info => info.Name == nameof(Result.Failure)
                    && info.IsGenericMethodDefinition);

            return (TResponse)method
                .MakeGenericMethod(valueType)
                .Invoke(null, new object[] { error })!;
        }

        throw new InvalidOperationException(
            $"CloudDrive account context failure cannot be converted to response type {typeof(TResponse).FullName}.");
    }

    private sealed class JobListAccountScopedInput(GetJobListInput input) : CloudDriveAccountScopedInput
    {
        public override string CloudAccountUid
        {
            get => input.CloudAccountUid ?? string.Empty;
            set => input.CloudAccountUid = value;
        }
    }
}
