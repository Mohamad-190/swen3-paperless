namespace DocVault.Core.Exceptions;

// Fehler der Datenzugriffsschicht (Datenbank nicht erreichbar, Constraint verletzt, ...).
// Kapselt die EF-Core-/Npgsql-Exceptions, damit obere Schichten nichts von EF wissen muessen.
public class DataAccessException : Exception
{
    public DataAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
