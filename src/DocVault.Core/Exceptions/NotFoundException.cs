namespace DocVault.Core.Exceptions;

// Basisklasse der Business-Schicht: angefragtes Objekt existiert nicht (-> 404).
public abstract class NotFoundException : Exception
{
    protected NotFoundException(string message) : base(message)
    {
    }
}
