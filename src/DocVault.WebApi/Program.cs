using DocVault.Core.Infrastructure;
using DocVault.Core.Infrastructure.Messaging;
using DocVault.Core.Infrastructure.Repositories;
using DocVault.Core.Infrastructure.Search;
using DocVault.Core.Infrastructure.Storage;
using DocVault.Core.ServiceAgents;
using DocVault.Core.Services;
using DocVault.WebApi.ExceptionHandling;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DocVaultDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DocVaultDb")));

// Repositories & Services (Salama / Dashaev), jeweils eigene Zeile wegen Merge-Konflikten:
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ITagService, TagService>();

// RabbitMQ: Singleton, damit die Verbindung zum Broker wiederverwendet wird
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IDocumentQueuePublisher, RabbitMqDocumentPublisher>();

// RustFS/S3: Singleton, der AmazonS3Client ist fuer Mehrfachverwendung ausgelegt
builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection(S3StorageOptions.SectionName));
builder.Services.AddSingleton<IDocumentStorage, S3DocumentStorage>();

// Elasticsearch: fuer die Volltextsuche ueber /documents/search
builder.Services.Configure<ElasticsearchOptions>(builder.Configuration.GetSection(ElasticsearchOptions.SectionName));
builder.Services.AddSingleton<ISearchIndexer, ElasticsearchIndexer>();

// Zentrale Fehlerbehandlung: Exceptions aller Schichten -> ProblemDetails mit passendem Statuscode
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocVaultDbContext>();

    // Nur in Development: DB bei jedem Start komplett leeren, damit man mit sauberen
    // Testdaten startet. In Production (oder wenn ASPNETCORE_ENVIRONMENT fehlt) NIE
    // ausfuehren, sonst gehen bei jedem Neustart alle Daten verloren.
    if (app.Environment.IsDevelopment())
        db.Database.EnsureDeleted();

    db.Database.Migrate();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();