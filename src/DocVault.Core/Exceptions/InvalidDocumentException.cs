namespace DocVault.Core.Exceptions;

public class InvalidDocumentException : BusinessValidationException
{
    public InvalidDocumentException(string message) : base(message)
    {
    }
}
