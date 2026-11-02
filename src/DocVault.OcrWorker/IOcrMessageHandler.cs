namespace DocVault.OcrWorker;

public interface IOcrMessageHandler
{
    // true = Nachricht verarbeitet (Ack), false = ungueltige Nachricht (wird verworfen)
    Task<bool> HandleAsync(ReadOnlyMemory<byte> body, CancellationToken ct = default);
}
