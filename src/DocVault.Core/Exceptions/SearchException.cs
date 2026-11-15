namespace DocVault.Core.Exceptions;

// Fehler der Such-Schicht (Elasticsearch nicht erreichbar, Indexierung abgelehnt, ...).
public class SearchException : Exception
{
    public SearchException(string message)
        : base(message)
    {
    }

    public SearchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
