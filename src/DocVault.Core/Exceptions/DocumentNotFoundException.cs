namespace DocVault.Core.Exceptions;

public class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(Guid id)
        : base($"Document with id '{id}' was not found.")
    {
        DocumentId = id;
    }

    public Guid DocumentId { get; }
}
