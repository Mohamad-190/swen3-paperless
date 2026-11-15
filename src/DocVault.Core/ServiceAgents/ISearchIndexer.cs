namespace DocVault.Core.ServiceAgents;

public interface ISearchIndexer
{
    // Legt den Eintrag an oder ueberschreibt ihn (gleiche DocumentId = gleiches Dokument im Index).
    Task IndexAsync(DocumentSearchEntry entry, CancellationToken ct = default);

    // Volltext-/Fuzzy-Suche ueber Content, Title und FileName. Liefert Dokument-IDs,
    // absteigend nach Relevanz sortiert.
    Task<IReadOnlyList<Guid>> SearchAsync(string query, CancellationToken ct = default);
}
