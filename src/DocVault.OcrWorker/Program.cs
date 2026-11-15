using DocVault.Core.Infrastructure;
using DocVault.Core.Infrastructure.Messaging;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Infrastructure.Search;
using DocVault.Core.Infrastructure.Storage;
using DocVault.Core.ServiceAgents;
using DocVault.OcrWorker;
using DocVault.OcrWorker.Ocr;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Gleiche RabbitMQ-Konfiguration wie in der WebApi (Abschnitt "RabbitMQ" / RabbitMQ__* Env-Variablen)
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Gleiche DB wie die WebApi, damit der Worker den Status/Fortschritt eines Dokuments
// (Processing/Completed/Failed) selbst aktualisieren kann.
builder.Services.AddDbContext<DocVaultDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DocVaultDb")));
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Object Storage (RustFS/S3), aus dem die hochgeladenen Dateien geholt werden
builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection(S3StorageOptions.SectionName));
builder.Services.AddSingleton<IDocumentStorage, S3DocumentStorage>();

// Elasticsearch fuer die Volltextsuche
builder.Services.Configure<ElasticsearchOptions>(builder.Configuration.GetSection(ElasticsearchOptions.SectionName));
builder.Services.AddSingleton<ISearchIndexer, ElasticsearchIndexer>();

// OCR: Ghostscript + Tesseract als externe Prozesse
builder.Services.Configure<OcrOptions>(builder.Configuration.GetSection(OcrOptions.SectionName));
builder.Services.AddSingleton<ICommandRunner, ProcessCommandRunner>();
builder.Services.AddSingleton<IOcrEngine, TesseractOcrEngine>();

builder.Services.AddSingleton<IOcrMessageHandler, OcrMessageHandler>();
builder.Services.AddHostedService<OcrQueueWorker>();

builder.Build().Run();
