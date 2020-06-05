using System;
using System.Threading;
using EventStore.ClientAPI;
using SerilogTimings.Extensions;

namespace Ditto
{
    public class ReplicatingConsumer : ICompetingConsumer
    {
        private readonly IEventStoreConnection _connection;
        private readonly Serilog.ILogger _logger;
        private readonly AppSettings _settings;

        public ReplicatingConsumer(
            IEventStoreConnection connection, Serilog.ILogger logger, AppSettings settings, string streamName, string groupName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        }

        public string StreamName { get; }
        public string GroupName { get; }
        public bool CanConsume(string eventType) => true;

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
                _connection.AppendToStreamAsync(e.Event.EventStreamId, e.Event.EventNumber - 1, eventData).GetAwaiter().GetResult();
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