namespace DocVault.Core.Exceptions;

public class FolderNotFoundException : NotFoundException
{
    public FolderNotFoundException(Guid id)
        : base($"Folder with id '{id}' was not found.")
    {
        FolderId = id;
    }

    public Guid FolderId { get; }
}
