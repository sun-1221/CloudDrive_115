using System.Reflection;

namespace Architecture.Tests;

internal static class ArchitectureConstants
{
    internal const string DomainNamespace = "Domain";
    internal const string ApplicationNamespace = "Application";
    internal const string InfrastructureNamespace = "Infrastructure";
    internal const string WebApiNamespace = "WebApi";

    internal static readonly Assembly DomainAssembly = typeof(Domain.Events.DomainEvent).Assembly;
    internal static readonly Assembly ApplicationAssembly = typeof(Application.Commands.ICommand).Assembly;
    internal static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection.ServiceCollectionExtensions).Assembly;
    internal static readonly Assembly WebApiAssembly = typeof(WebApi.Middleware.ExceptionHandlingMiddleware).Assembly;
}
