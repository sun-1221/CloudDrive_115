using Application.Behaviors;
using Application.Commands;

using Infrastructure.DependencyInjection;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Architecture.Tests;

public sealed class PipelineBehaviorOrderTests
{
    [Fact]
    public void PipelineBehaviors_WhenRegisteredWithoutModuleExtension_UseCoreOrder()
    {
        // Arrange（准备）
        var services = new ServiceCollection();

        // 注册最小必要依赖以避免 DI 异常
        services.AddLogging();
        services.AddApplicationServices(typeof(ICommand).Assembly);

        Type[] expectedOrder =
        [
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(TransactionBehavior<,>),
            typeof(DomainEventDispatchBehavior<,>),
        ];

        // Act（执行）
        List<Type> actualOrder = GetPipelineBehaviorOrder(services);

        // Assert（断言）
        Assert.Equal(expectedOrder.Length, actualOrder.Count);

        for (int i = 0; i < expectedOrder.Length; i++)
        {
            Assert.Equal(expectedOrder[i], actualOrder[i]);
        }
    }

    [Fact]
    public void PipelineBehaviors_WhenPreValidationBehaviorConfigured_InsertBeforeValidation()
    {
        // Arrange（准备）
        var services = new ServiceCollection();

        // 注册最小必要依赖以避免 DI 异常
        services.AddLogging();
        services.AddApplicationServices(
            typeof(ICommand).Assembly,
            [typeof(CloudDriveAccountContextBehavior<,>)]);

        Type[] expectedOrder =
        [
            typeof(LoggingBehavior<,>),
            typeof(CloudDriveAccountContextBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(TransactionBehavior<,>),
            typeof(DomainEventDispatchBehavior<,>),
        ];

        // Act（执行）
        List<Type> actualOrder = GetPipelineBehaviorOrder(services);

        // Assert（断言）
        Assert.Equal(expectedOrder.Length, actualOrder.Count);

        for (int i = 0; i < expectedOrder.Length; i++)
        {
            Assert.Equal(expectedOrder[i], actualOrder[i]);
        }
    }

    private static List<Type> GetPipelineBehaviorOrder(ServiceCollection services)
    {
        return services
            .Where(sd => sd.ServiceType.IsGenericType
                && sd.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .Select(sd => sd.ImplementationType ?? sd.ServiceType)
            .ToList();
    }
}
