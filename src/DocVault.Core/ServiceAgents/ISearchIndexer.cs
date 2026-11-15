namespace DocVault.Core.ServiceAgents;

public interface ISearchIndexer
{
    // Legt den Eintrag an oder ueberschreibt ihn (gleiche DocumentId = gleiches Dokument im Index).
    Task IndexAsync(DocumentSearchEntry entry, CancellationToken ct = default);
}
