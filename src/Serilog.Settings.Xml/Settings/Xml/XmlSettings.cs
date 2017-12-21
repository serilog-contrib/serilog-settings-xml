using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Serilog.Configuration;
using Serilog.Debugging;

namespace Serilog.Settings.Xml
{
    internal sealed class XmlSettings : ILoggerSettings
    {
        private const string UsingDirective = "using";
        private const string AuditToDirective = "audit-to";
        private const string WriteToDirective = "write-to";
        private const string MinimumLevelDirective = "minimum-level";
        private const string EnrichWithDirective = "enrich";
        private const string EnrichWithPropertyDirective = "enrich:with-property";

        private readonly string _filePath;

        public XmlSettings(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            _filePath = filePath;
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException(nameof(loggerConfiguration));

            if (!File.Exists(_filePath))
            {
                SelfLog.WriteLine("The specified configuration file `{0}` does not exist and will be ignored.", _filePath);
                //Q: should be we fail here?
                return;
            }

            var settings = new List<KeyValuePair<string, string>>();
            try
            {
                var document = XDocument.Load(_filePath);
                var root = document.Root;
                
                ProcessUsings(root.Element("using"), settings);
                ProcessEnrichers(root.Element("enrich"), settings);
                ProcessProperties(root.Element("properties"), settings);
                ProcessWriteTo(root.Element("writeTo"), settings);
                ProcessAuditTo(root.Element("auditTo"), settings);
                ProcessMinimumLevel(root.Element("minimumLevel"), settings);

                loggerConfiguration.ReadFrom.KeyValuePairs(settings);
            }
            catch (Exception e)
            {
                SelfLog.WriteLine($"Cannot load xml config '{_filePath}'. Exception: {e}");
                //Q: should we fail here?
                throw;
            }
        }

        private void ProcessMinimumLevel(XElement minimumLevelElement, List<KeyValuePair<string, string>> settings)
        {
            if (minimumLevelElement == null)
                return;

            var baseKey = MinimumLevelDirective;
            // default
            var defaultLevelAttr = minimumLevelElement.Attribute("default");
            if (!string.IsNullOrEmpty(defaultLevelAttr?.Value)) //&& ValidLevel(defaultLevelAttr.Value))
            {
                settings.Add(new KeyValuePair<string, string>(baseKey, defaultLevelAttr.Value));
            }

            // overrides
            var items = minimumLevelElement.Elements("override");
            foreach (var item in items)
            {
                if (!item.HasAttributes)
                    continue;

                var name = item.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    continue;

                var level = item.Attribute("level")?.Value;
                if (!string.IsNullOrEmpty(level))
                {
                    settings.Add(new KeyValuePair<string, string>($"{baseKey}:override:{name}", level));
                }
            }
        }

        private static void ProcessUsings(XElement usingElement, List<KeyValuePair<string, string>> settings)
        {
            if (usingElement == null)
                return;

            var items = usingElement.Elements("add");
            foreach (var element in items)
            {
                if (!element.HasAttributes)
                    continue;

                var name = element.Attribute("name")?.Value;
                settings.Add(new KeyValuePair<string, string>($"{UsingDirective}:{name}", name));
            }
        }

        private static void ProcessEnrichers(XElement enrichElement, List<KeyValuePair<string, string>> settings)
        {
            if (enrichElement == null)
                return;
            
            var items = enrichElement.Elements("enricher");
            foreach (var element in items)
            {
                if (!element.HasAttributes)
                    continue;

                var name = element.Attribute("name")?.Value;
                settings.Add(new KeyValuePair<string, string>($"{EnrichWithDirective}:{name}", ""));
            }
        }

        private static void ProcessProperties(XElement propertiesElement, List<KeyValuePair<string, string>> settings)
        {
            if (propertiesElement == null)
                return;

            var items = propertiesElement.Elements("property");
            foreach (var element in items)
            {
                if (!element.HasAttributes)
                    continue;

                var name = element.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    continue;

                var value = element.Attribute("value")?.Value;
                if (value != null)
                    value = Environment.ExpandEnvironmentVariables(value);

                settings.Add(new KeyValuePair<string, string>($"{EnrichWithPropertyDirective}:{name}", value));
            }
        }

        private static void ProcessWriteTo(XElement writeToElement, List<KeyValuePair<string, string>> settings)
        {
            ProcessSinks(writeToElement, settings, WriteToDirective);
        }

        private static void ProcessAuditTo(XElement auditToElement, List<KeyValuePair<string, string>> settings)
        {
            ProcessSinks(auditToElement, settings, AuditToDirective);
        }

        private static void ProcessSinks(XElement element, List<KeyValuePair<string, string>> settings, string writeToDirective)
        {
            if (element == null)
                return;

            var items = element.Elements("sink");
            foreach (var item in items)
            {
                if (!item.HasAttributes)
                    continue;

                var name = item.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    continue;

                var parameters = item.Elements("arg");

                var baseKey = $"{writeToDirective}:{name}";
                if (!parameters.Any())
                    settings.Add(new KeyValuePair<string, string>(baseKey, string.Empty));

                foreach (var parameter in parameters)
                {
                    var paramName = parameter.Attribute("name")?.Value;
                    if (string.IsNullOrEmpty(paramName))
                        continue;
                    var paramValue = parameter.Attribute("value")?.Value;
                    paramValue = Environment.ExpandEnvironmentVariables(paramValue);

                    settings.Add(new KeyValuePair<string, string>($"{baseKey}.{paramName}", paramValue));
                }
            }
        }
    }
}