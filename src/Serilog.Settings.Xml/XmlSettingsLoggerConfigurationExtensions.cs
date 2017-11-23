using System;
using Serilog.Configuration;
using Serilog.Settings.Xml;

// ReSharper disable once CheckNamespace
namespace Serilog
{
    /// <summary>
    /// Extends <see cref="LoggerConfiguration"/> with support for loading xml files.
    /// </summary>
    public static class XmlSettingsLoggerConfigurationExtensions
    {
        /// <summary>
        /// Reads the xml file.
        /// </summary>
        /// <param name="settingConfiguration">Logger setting configuration.</param>
        /// <param name="filePath">Specify the path to xml file location. 
        /// If the file does not exist it will be ignored.</param>
        /// <returns>An object allowing configuration to continue.</returns>
        public static LoggerConfiguration Xml(this LoggerSettingsConfiguration settingConfiguration, string filePath = null)
        {
            if (settingConfiguration == null)
                throw new ArgumentNullException(nameof(settingConfiguration));

            return settingConfiguration.Settings(new XmlSettings(filePath));
        }
    }
}
