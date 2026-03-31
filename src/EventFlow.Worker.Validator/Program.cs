using EventFlow.Application.DependencyInjection;
using EventFlow.Infrastructure.DependencyInjection;
using EventFlow.Worker.Validator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<ValidatorWorker>();

var host = builder.Build();
host.Run();