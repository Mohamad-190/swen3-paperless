namespace DocVault.Core.Exceptions;

// Fehler der Storage-Schicht (RustFS/S3 nicht erreichbar, Bucket fehlt, ...).
public class StorageException : Exception
{
    public StorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
