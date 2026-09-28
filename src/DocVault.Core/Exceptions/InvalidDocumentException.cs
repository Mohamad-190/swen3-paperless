namespace DocVault.Core.Exceptions;

public class InvalidDocumentException : Exception
{
    public InvalidDocumentException(string message) : base(message)
    {
    }
}
