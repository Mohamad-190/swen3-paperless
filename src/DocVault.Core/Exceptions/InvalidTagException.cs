namespace DocVault.Core.Exceptions;

public class InvalidTagException : BusinessValidationException
{
    public InvalidTagException(string message) : base(message)
    {
    }
}
