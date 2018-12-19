using System;
using System.Linq;
using System.Linq.Expressions;
using Ditto.Settings;
using EventStore.ClientAPI;
using Microsoft.Extensions.Configuration;
using StructureMap;
using ILogger = Serilog.ILogger;

namespace Ditto
{
    /// <summary>
    /// Registry of application dependencies used to configure StructureMap containers
    /// </summary>
    public class AppRegistry : Registry
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AppRegistry"/>
        /// </summary>
        /// <param name="configuration">The application configuration to be registered with the container</param>
        /// <param name="logger">The application logger to be registered with the container</param>
        public AppRegistry(IConfiguration configuration, ILogger logger)
        {
            For<IConfiguration>().Use(configuration);
            
            // Binds the "Settings" section from appsettings.json to AppSettings
            var settings = Bind<AppSettings>(configuration, "Settings");
            ForSingletonOf<AppSettings>().Use(settings);

            // Binds the "StreamsToReplicate" section array from appsettings.json to StreamsToReplicateSettings
            var streamsToReplicate = Bind<StreamsToReplicateSettings>(configuration, "StreamsToReplicate");
            ForSingletonOf<StreamsToReplicateSettings>().Use(streamsToReplicate);

            // Automatically sets the Serilog source context to the requesting type
            For<ILogger>()
                .Use(ctx => logger.ForContext(ctx.ParentType ?? ctx.RootType))
                .AlwaysUnique();
            
            // Default
            ForSingletonOf<IEventStoreConnection>().Use(
                ctx => CreateEventStoreConnection(
                    ctx.GetInstance<Serilog.ILogger>(),
                    ctx.GetInstance<AppSettings>().SourceEventStoreConnectionString,
                    "Ditto:Source"
                ));

            ForSingletonOf<IEventStoreConnection>().Add(
                ctx => CreateEventStoreConnection(
                    ctx.GetInstance<Serilog.ILogger>(),
                    ctx.GetInstance<AppSettings>().DestinationEventStoreConnectionString,
                    "Ditto:Destination"
                ))
                .Named("Destination");

            For<ICheckpointManager>().Use<EventStoreCheckpointManager>(ctx =>
                new EventStoreCheckpointManager(ctx.GetInstance<IEventStoreConnection>("Destination"), ctx.GetInstance<AppSettings>(),ctx.GetInstance<ILogger>())
            );

            For<IConsumerManager>().Use<ConsumerManager>();

            // Register replicating consumers
            foreach (var replicationSettings in streamsToReplicate.Settings)
            {
                For<IConsumer>().Add(CreateReplicatingConsumer(replicationSettings));
            }

            // Kept for backwards compatibility
#pragma warning disable CS0618 // Type or member is obsolete
            var backwardsCompatibilityStreamsToReplicate = settings.GetStreamsToReplicate();
#pragma warning restore CS0618 // Type or member is obsolete
            if (backwardsCompatibilityStreamsToReplicate.Any())
            {
                logger.Warning("The StreamIdentifiers setting is obsolete but will be used for backwards compatibility.. Consider using the StreamsToReplicate setting instead.");
                foreach (var streamToReplicate in backwardsCompatibilityStreamsToReplicate)
                {
                    For<IConsumer>().Add(CreateReplicatingConsumer(new ReplicationSettings { StreamIdentifier = streamToReplicate }));
                }
            }
        }

        /// <summary>
        /// Convenience method for binding configuration settings to strongly typed objects
        /// </summary>
        /// <param name="configuration">The configuration to use for binding</param>
        /// <param name="key">The configuration section to bind from</param>
        /// <returns>The created settings object bound from the configuration</returns>
        private static TSettings Bind<TSettings>(IConfiguration configuration, string key) where TSettings : new()
        {
            var settings = new TSettings();
            configuration.Bind(key, settings);
            return settings;
        }

        private static Expression<Func<IContext, IConsumer>> CreateReplicatingConsumer(ReplicationSettings replicationSettings)
        {
            return ctx => 
                new ReplicatingConsumer(
                    ctx.GetInstance<IEventStoreConnection>("Destination"), 
                    ctx.GetInstance<ILogger>().ForContext<ReplicatingConsumer>(),
                    ctx.GetInstance<AppSettings>(),
                    replicationSettings
                );
        }

        private static IEventStoreConnection CreateEventStoreConnection(Serilog.ILogger logger, string connectionString, string connectionName)
        {
            var connectionSettings = ConnectionSettings.Create()
                .KeepReconnecting()
                .SetReconnectionDelayTo(TimeSpan.FromSeconds(3))
                .KeepRetrying()
                .PerformOnAnyNode()
                .PreferRandomNode()
                .UseCustomLogger(new SerilogEventStoreLogger(logger));

            var connection = EventStoreConnection
                .Create(connectionString, connectionSettings, connectionName);

            connection
                .Connected += (sender, args)
                    => logger.Information("Connected to Event Store {ConnectionName}", connectionName);

            connection.Closed += (sender, args) 
                => logger.Information("Connection to Event Store {ConnectionName} closed", connectionName);

            connection.Reconnecting += (sender, args) 
                => logger.Information("Attempting to connect to Event Store {ConnectionName}", connectionName);
            
            connection.ConnectAsync().GetAwaiter().GetResult();

            return connection;
        }
    }
}