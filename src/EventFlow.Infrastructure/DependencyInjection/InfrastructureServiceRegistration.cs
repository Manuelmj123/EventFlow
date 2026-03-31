using EventFlow.Application.Abstractions;
using EventFlow.Infrastructure.Messaging;
using EventFlow.Infrastructure.Options;
using EventFlow.Infrastructure.Persistence;
using EventFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddDbContext<EventFlowDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString));
        });

        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<EventFlowDbContext>());

        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowEventLogRepository, WorkflowEventLogRepository>();
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<RabbitMqTopologyInitializer>();
        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

        return services;
    }
}