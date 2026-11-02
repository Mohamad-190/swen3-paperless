namespace DocVault.Core.Exceptions;

// Fehler der Messaging-Schicht (RabbitMQ nicht erreichbar, Verbindung abgebrochen, ...).
public class MessagingException : Exception
{
    public MessagingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
