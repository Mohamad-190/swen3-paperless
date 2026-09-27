using Microsoft.EntityFrameworkCore;

namespace DocVault.Core.Infrastructure;

public class DocVaultDbContext : DbContext
{
    public DocVaultDbContext(DbContextOptions<DocVaultDbContext> options) : base(options)
    {
    }

    // Salama: public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    // Dashaev: public DbSet<FolderEntity> Folders => Set<FolderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Generische Regel: Standard-Löschverhalten auf Restrict statt Cascade
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var key in entityType.GetForeignKeys())
                key.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Hier tragen Salama und Dashaev ihre eigenen Beziehungen/Konfigurationen ein,
        // z. B. Folder -> Documents, Enum-Konvertierungen etc.
    }
}