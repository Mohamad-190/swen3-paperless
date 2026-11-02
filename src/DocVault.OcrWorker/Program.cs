using DocVault.Core.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// Gleiche RabbitMQ-Konfiguration wie in der WebApi (Abschnitt "RabbitMQ" / RabbitMQ__* Env-Variablen)
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Build().Run();
