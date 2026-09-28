namespace DocVault.Core.Exceptions;

public class TagNotFoundException : Exception
{
    public TagNotFoundException(Guid id)
        : base($"Tag with id '{id}' was not found.")
    {
        TagId = id;
    }

    public Guid TagId { get; }
}
