namespace DocVault.Core.Exceptions;

public class InvalidTagException : Exception
{
    public InvalidTagException(string message) : base(message)
    {
    }
}
