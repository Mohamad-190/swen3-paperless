using DocVault.Core.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace DocVault.OcrWorker;

// Abonniert die ocr-queue und gibt jede Nachricht an den IOcrMessageHandler weiter.
// Bestaetigt (Ack) erst nach der Verarbeitung, damit bei einem Absturz keine Nachricht verloren geht.
public sealed class OcrQueueWorker : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions _options;
    private readonly IOcrMessageHandler _handler;
    private readonly ILogger<OcrQueueWorker> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public OcrQueueWorker(IOptions<RabbitMqOptions> options, IOcrMessageHandler handler, ILogger<OcrQueueWorker> logger)
    {
        _options = options.Value;
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = await ConnectAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Gleiche Einstellungen wie beim Publisher, sonst lehnt RabbitMQ die Deklaration ab.
        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Immer nur eine unbestaetigte Nachricht pro Worker -> faire Verteilung bei mehreren Workern.
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;
        await _channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("OCR worker is listening on queue {Queue}", _options.QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normales Herunterfahren
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        bool processed;
        try
        {
            processed = await _handler.HandleAsync(args.Body, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing message {DeliveryTag} failed", args.DeliveryTag);
            processed = false;
        }

        if (processed)
            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
        else
            // Nicht verarbeitbar: verwerfen statt endlos neu zustellen (Dead-Letter-Queue folgt spaeter).
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
    }

    // RabbitMQ ist beim Start evtl. noch nicht bereit -> so lange erneut versuchen, bis es klappt.
    private async Task<IConnection> ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        while (true)
        {
            try
            {
                var connection = await factory.CreateConnectionAsync(ct);
                _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _options.Host, _options.Port);
                return connection;
            }
            catch (BrokerUnreachableException)
            {
                _logger.LogWarning("RabbitMQ at {Host}:{Port} is not reachable, retrying in {Delay} s",
                    _options.Host, _options.Port, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, ct);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
