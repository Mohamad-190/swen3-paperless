namespace DocVault.Core.Exceptions;

// Basisklasse der Business-Schicht: Eingabe verletzt eine fachliche Regel (-> 400).
public abstract class BusinessValidationException : Exception
{
    protected BusinessValidationException(string message) : base(message)
    {
    }
}
