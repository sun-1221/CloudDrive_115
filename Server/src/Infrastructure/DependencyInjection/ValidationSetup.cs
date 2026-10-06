using System.Reflection;

using Application.Abstractions;
using Application.Behaviors;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

/// <summary>
/// FluentValidation 注册入口。
/// </summary>
/// <remarks>
/// 按设计 §4. FluentValidation + MediatR 集成实现：<br/>
/// - 通过 <see cref="ValidationSetup.AddValidators(IServiceCollection, Assembly, ServiceLifetime)"/>
///   自动扫描 Application 程序集中所有 <see cref="IValidator{T}"/> 实现并以 Scoped 生命周期注册（R8.1）<br/>
/// - 业务模块添加新 Validator 时无需手动注册，只要保持在同一程序集内即可。
/// </remarks>
public static class ValidationSetup
{
    /// <summary>
    /// 扫描并注册 <paramref name="assembly"/> 中的所有 FluentValidation 验证器。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="assembly">要扫描的程序集（通常为 Application 程序集）。</param>
    /// <param name="lifetime">验证器生命周期。默认 <see cref="ServiceLifetime.Scoped"/>。</param>
    /// <returns>返回当前操作的处理结果。</returns>
    public static IServiceCollection AddValidators(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        services.AddValidatorsFromAssembly(assembly, lifetime: lifetime, includeInternalTypes: true);
        services.AddInputValidatorAdapters(assembly, lifetime);
        return services;
    }

    /// <summary>
    /// 扫描并注册多个程序集中的所有 FluentValidation 验证器。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    public static IServiceCollection AddValidators(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (Assembly assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly, lifetime: lifetime, includeInternalTypes: true);
            services.AddInputValidatorAdapters(assembly, lifetime);
        }

        return services;
    }

    /// <summary>
    /// 扫描程序集中实现 <see cref="IHasInput{TInput}"/> 的请求类型，
    /// 为每个类型自动注册 <see cref="InputPropertyValidator{TRequest, TInput}"/>
    /// 作为 <see cref="IValidator{T}"/>，在 DI 阶段完成 Input DTO 验证桥接。
    /// </summary>
    private static IServiceCollection AddInputValidatorAdapters(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime)
    {
        Type hasInputOpen = typeof(IHasInput<>);
        Type adapterOpen = typeof(InputPropertyValidator<,>);

        foreach (Type requestType in assembly.GetTypes())
        {
            if (requestType.IsAbstract || requestType.IsInterface)
            {
                continue;
            }

            foreach (Type iface in requestType.GetInterfaces())
            {
                if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != hasInputOpen)
                {
                    continue;
                }

                Type inputType = iface.GetGenericArguments()[0];
                Type closedAdapter = adapterOpen.MakeGenericType(requestType, inputType);
                Type validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);

                services.Add(new ServiceDescriptor(validatorInterface, closedAdapter, lifetime));
            }
        }

        return services;
    }
}
