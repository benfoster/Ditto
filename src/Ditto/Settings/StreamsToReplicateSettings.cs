using System.Collections.Generic;

namespace Ditto.Settings
{
    /// <summary>
    /// A collection of settings related to a stream to be replicated.
    /// </summary>
    public class StreamsToReplicateSettings
    {
        /// <summary>
        /// A collection of settings related to a stream to be replicated.
        /// </summary>
        public List<ReplicationSettings> Settings { get; set; }
    }
}