namespace Ditto.Settings
{
    /// <summary>
    /// Settings related to a stream to be replicated.
    /// </summary>
    public class ReplicationSettings
    {
        /// <summary>
        /// Gets or sets the ID of the stream that should be copied
        /// </summary>
        public string StreamIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the initial checkpoint this stream should start replicating from
        /// if this is the start of the replication process and no checkpoint exists.
        /// </summary>
        public long? InitialCheckpoint { get; set; }

        /// <summary>
        /// A flag to denote if a version check should be perform when appending to the replicated
        /// stream. This is true when no initial checkpoint has been specified since the whole stream
        /// is expected to be replicated but otherwise false since it would be impossible to guarantee
        /// version parity between source and destination.
        /// </summary>
        public bool PerformVersionCheck => !InitialCheckpoint.HasValue;
    }
}