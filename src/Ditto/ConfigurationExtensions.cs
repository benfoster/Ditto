using System;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Ditto
{
    /// <summary>
    /// Extensions for <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Dumps the configuration to a string for logging purposes
        /// </summary>
        /// <param name="configuration">The configuration to dump</param>
        /// <returns>A string representation of the configuration</returns>
        public static string Dump(this IConfiguration configuration)
        {
            var log = new StringBuilder();

            log.Append("Configuration");
            log.AppendLine();

            foreach (var section in configuration.GetChildren())
            {
                DumpSection(section, log, 0, true);
            }

            return log.ToString();
        }

        private static void DumpSection(IConfigurationSection section, StringBuilder log, int depth, bool rootSection = false)
        {
            log.Append('\t');
            log.Append(' ', depth * 2);
            log.AppendFormat("{0}: {1}\n", section.Key, section.Value);

            foreach (var child in section.GetChildren())
            {
                DumpSection(child, log, depth + 1);
            }

            if (rootSection)
                log.AppendLine();
        }

        public static TOptions Bind<TOptions>(this IConfiguration configuration, string sectionName) where TOptions : class, new()
        {
            var options = new TOptions();
            configuration.Bind(sectionName, options);
            return options;
        }
    }
}