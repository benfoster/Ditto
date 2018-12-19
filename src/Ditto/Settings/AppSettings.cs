using System;
using System.Collections.Generic;

namespace Ditto.Settings
{
    /// <summary>
    /// The application settings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the connection string to the source Event Store
        /// </summary>
        public string SourceEventStoreConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the connection string to the destination Event Store
        /// </summary>
        public string DestinationEventStoreConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the IDs of the streams that should be copied. Separated with a semi colon ";"
        /// </summary>
        /// <remarks>This property is now obsolete and is left for backwards compatibility. Use <see cref="StreamsToReplicateSettings"/> instead.</remarks>
        [Obsolete("Use StreamsToReplicateSettings instead.")]
        public string StreamIdentifiers { get; set; }

        /// <summary>
        /// Gets or sets the checkpoint manager retry count 
        /// </summary>
        public int CheckpointManagerRetryCount { get; set; }

        /// <summary>
        /// Gets or sets the checkpoint manager retry interval in milliseconds
        /// </summary>
        public int CheckpointManagerRetryInterval { get; set; }

        /// <summary>
        /// Gets or sets the interval in milliseconds that consumer checkpoints are saved
        /// </summary>
        public int CheckpointSavingInterval { get; set; }

        /// <summary>
        /// Gets or sets the time in milliseconds to wait between event handling
        /// </summary>
        public int? ReplicationThrottleInterval { get; set; }

        /// <summary>
        /// A collection of stream identifiiers to be replicated.
        /// </summary>
        /// <returns>List of stream identifiers.</returns>
        /// <remarks>This property is now obsolete and is left for backwards compatibility. Use <see cref="StreamsToReplicateSettings"/> instead.</remarks>
        [Obsolete("Use StreamsToReplicateSettings instead.")]
        public IEnumerable<string> GetStreamsToReplicate()
        {
            return (StreamIdentifiers ?? "").Trim().Split(';', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}