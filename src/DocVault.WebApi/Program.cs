using DocVault.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DocVaultDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DocVaultDb")));

// Repositories & Services (Salama / Dashaev), jeweils eigene Zeile wegen Merge-Konflikten:
// builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
// builder.Services.AddScoped<IDocumentService, DocumentService>();
// builder.Services.AddScoped<IFolderRepository, FolderRepository>();
// builder.Services.AddScoped<IFolderService, FolderService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocVaultDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();