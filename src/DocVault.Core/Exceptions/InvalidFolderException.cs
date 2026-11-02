namespace DocVault.Core.Exceptions;

public class InvalidFolderException : BusinessValidationException
{
    public InvalidFolderException(string message) : base(message)
    {
    }
}
