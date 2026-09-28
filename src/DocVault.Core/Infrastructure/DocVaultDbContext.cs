using DocVault.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DocVault.Core.Infrastructure;

public class DocVaultDbContext : DbContext
{
    public DocVaultDbContext(DbContextOptions<DocVaultDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<FolderEntity> Folders => Set<FolderEntity>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Generische Regel: Standard-Loeschverhalten auf Restrict statt Cascade
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var key in entityType.GetForeignKeys())
                key.DeleteBehavior = DeleteBehavior.Restrict;
        }


        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Title).IsRequired().HasMaxLength(200);
            entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
            entity.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(d => d.FolderId);
        });

        modelBuilder.Entity<FolderEntity>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired().HasMaxLength(100);
            entity.Property(f => f.Description).HasMaxLength(500);
            entity.HasMany(f => f.Documents)
                .WithOne(d => d.Folder)
                .HasForeignKey(d => d.FolderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TagEntity>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Color).HasMaxLength(20);
            entity.HasIndex(t => t.Name).IsUnique();

            entity.HasMany(t => t.Documents)
                .WithMany(d => d.Tags)
                .UsingEntity(j => j.ToTable("DocumentTags"));
        });
    }
}