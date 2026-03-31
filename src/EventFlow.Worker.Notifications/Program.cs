using EventFlow.Application.DependencyInjection;
using EventFlow.Infrastructure.DependencyInjection;
using EventFlow.Worker.Notifications;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<NotificationsWorker>();

var host = builder.Build();
host.Run();