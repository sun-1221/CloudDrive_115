using System.Reflection;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Results;

using MediatR;

namespace Application.Behaviors;

/// <summary>
/// 在 FluentValidation 前为公开 115 请求解析唯一账号上下文并写入
/// <see cref="ICloudDriveAccountContextAccessor"/>，Handler 通过 Accessor 获取账号 UID。
/// </summary>
public sealed class CloudDriveAccountContextBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICloudDriveAccountContextResolver _resolver;
    private readonly ICloudDriveAccountContextAccessor _accessor;

    public CloudDriveAccountContextBehavior(
        ICloudDriveAccountContextResolver resolver,
        ICloudDriveAccountContextAccessor accessor)
    {
        _resolver = resolver;
        _accessor = accessor;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (request is not ICloudDriveAccountScopedRequest scopedRequest)
        {
            return await next(cancellationToken);
        }

        Result<CloudDriveAccountContext> contextResult =
            await _resolver.ResolveAsync(scopedRequest.UserId, cancellationToken);

        if (contextResult.IsFailure)
        {
            return CreateFailureResponse(contextResult.Error);
        }

        _accessor.SetContext(contextResult.Value);
        return await next(cancellationToken);
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
}
