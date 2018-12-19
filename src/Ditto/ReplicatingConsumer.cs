using Ditto.Settings;
using EventStore.ClientAPI;
using SerilogTimings.Extensions;
using System.Threading;

namespace Ditto
{
    public class ReplicatingConsumer : IConsumer
    {
        private readonly IEventStoreConnection _connection;
        private readonly Serilog.ILogger _logger;
        private readonly AppSettings _settings;
        private readonly ReplicationSettings _replicationSettings;

        public ReplicatingConsumer(
            IEventStoreConnection connection, Serilog.ILogger logger, AppSettings settings, ReplicationSettings replicationSettings)
        {
            _connection = connection ?? throw new System.ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _replicationSettings = replicationSettings ?? throw new System.ArgumentNullException(nameof(replicationSettings));
        }

        public string StreamName => _replicationSettings.StreamIdentifier;

        public long? InitialCheckpoint => _replicationSettings.InitialCheckpoint;

        public bool CanConsume(string eventType)
        {
            return true;
        }

        public void Consume(string eventType, ResolvedEvent e)
        {
            var eventData = new EventData(
                e.Event.EventId,
                e.Event.EventType,
                true,
                e.Event.Data,
                e.Event.Metadata
            );

            using (_logger.TimeOperation("Replicating {EventType} #{EventNumber} from {StreamName}", 
                e.Event.EventType, 
                e.Event.EventNumber, 
                e.Event.EventStreamId))
            {
                if (_replicationSettings.PerformVersionCheck)
                {
                    _connection.AppendToStreamAsync(e.Event.EventStreamId, e.Event.EventNumber - 1, eventData).GetAwaiter().GetResult();
                }
                else
                {
                    _connection.AppendToStreamAsync(e.Event.EventStreamId, ExpectedVersion.Any, eventData).GetAwaiter().GetResult();
                }
            }

            if (_settings.ReplicationThrottleInterval.GetValueOrDefault() > 0)
                Thread.Sleep(_settings.ReplicationThrottleInterval.Value);
        }

        public override string ToString()
        {
            var normalised = StreamName
                .Replace("$", "")
                .Replace("-", "_"); // To avoid category stream breaking down checkpoint streams

            return $"ReplicatingConsumer_{normalised}";
        }
    }
}