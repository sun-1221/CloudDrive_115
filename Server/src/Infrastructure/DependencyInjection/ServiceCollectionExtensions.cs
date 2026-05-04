using System.Reflection;

using Application.Abstractions;
using Application.Behaviors;

using Domain.Entities;
using Domain.Repositories;

using Infrastructure.Persistence;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

/// <summary>
/// 基础设施层 DI 注册聚合入口。将各基础设施模块（SqlSugar、Quartz、Caching、HttpClient、Secrets、
/// Application 服务与 Repository）的注册扩展方法集中转发，方便 <c>Program.cs</c> 使用。
/// </summary>
/// <remarks>
/// 按设计 §MediatR Pipeline Behaviors + §8. WebApi 启动与认证 要求：<br/>
/// - Pipeline Behaviors 按 LoggingBehavior → ValidationBehavior → TransactionBehavior →
///   DomainEventDispatchBehavior 顺序注册（R3.8）<br/>
/// - 仓储开放泛型 <see cref="IRepository{TEntity,TId}"/> → <see cref="Repository{TEntity,TId}"/>
///   注册为 <see cref="ServiceLifetime.Scoped"/>（R12.1）<br/>
/// - 机密保护通过 <see cref="Security.SecretsSetup.AddSecretsProtection"/> 提供入口，
///   支持 key ring 持久化（R12.7）。
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Application 层核心服务：MediatR、Pipeline Behaviors、FluentValidation
    /// 及其依赖的 <see cref="IDomainEventSource"/> / <see cref="ITransactionManager"/>。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="applicationAssembly">Application 程序集，用于扫描 MediatR Handler 与 FluentValidation Validator。</param>
    /// <returns>原始服务集合。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="applicationAssembly"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        Assembly applicationAssembly)
    {
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
        });

        // Pipeline Behaviors 必须按 Logging → Validation → Transaction → DomainEventDispatch 顺序注册
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CloudDriveAccountContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainEventDispatchBehavior<,>));

        services.AddValidators(applicationAssembly);

        services.AddScoped<IDomainEventSource, DomainEventSource>();
        services.AddScoped<ITransactionManager, SqlSugarTransactionManager>();

        return services;
    }

    /// <summary>
    /// 注册仓储开放泛型 <see cref="IRepository{TEntity,TId}"/> → <see cref="Repository{TEntity,TId}"/>
    /// 为 <see cref="ServiceLifetime.Scoped"/>。业务模块可在自身 DI 扩展中覆盖为更具体的实现。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>原始服务集合。</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<ICookieSessionRepository, CookieSessionRepository>();
        services.AddScoped<ICloudDriveJobRepository, CloudDriveJobRepository>();
        return services;
    }
}
