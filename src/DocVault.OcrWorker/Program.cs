using DocVault.Core.Infrastructure.Messaging;
using DocVault.OcrWorker;

var builder = Host.CreateApplicationBuilder(args);

// Gleiche RabbitMQ-Konfiguration wie in der WebApi (Abschnitt "RabbitMQ" / RabbitMQ__* Env-Variablen)
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddSingleton<IOcrMessageHandler, OcrMessageHandler>();
builder.Services.AddHostedService<OcrQueueWorker>();

builder.Build().Run();
