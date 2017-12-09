using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
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
                return;
            }

            var settings = new List<KeyValuePair<string, string>>();
            try
            {
                var document = XDocument.Load(_filePath);

                ProcessUsings(document, settings);
                ProcessEnrichers(document, settings);
                ProcessProperties(document, settings);
                ProcessWriteTo(document, settings);
                ProcessAuditTo(document, settings);
                ProcessMinimumLevel(document, settings);

                loggerConfiguration.ReadFrom.KeyValuePairs(settings);
            }
            catch (Exception e)
            {
                SelfLog.WriteLine($"Cannot load xml config '{_filePath}'. Exception: {e}");
                throw;
            }
        }

        private void ProcessMinimumLevel(XDocument document, List<KeyValuePair<string, string>> settings)
        {
            var minimumLevelElement = document.XPathSelectElement("/serilog/minimumLevel");
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
            var items = minimumLevelElement.XPathSelectElements("override");
            foreach (var element in items)
            {
                if (!element.HasAttributes)
                    continue;

                var name = element.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    continue;

                var level = element.Attribute("level")?.Value;
                if (!string.IsNullOrEmpty(level))
                {
                    settings.Add(new KeyValuePair<string, string>($"{baseKey}:override:{name}", level));
                }
            }
        }

        private static void ProcessUsings(XDocument document, List<KeyValuePair<string, string>> settings)
        {
            var items = document.XPathSelectElements("/serilog/using/add");
            foreach (var element in items)
            {
                if (!element.HasAttributes)
                    continue;

                var name = element.Attribute("name")?.Value;
                settings.Add(new KeyValuePair<string, string>($"{UsingDirective}:{name}", name));
            }
        }

        private static void ProcessEnrichers(XDocument document, List<KeyValuePair<string, string>> settings)
        {
            var items = document.XPathSelectElements("/serilog/enrich/enricher");
            foreach (var element in items)
            {
                if (!element.HasAttributes)
                    continue;

                var name = element.Attribute("name")?.Value;
                settings.Add(new KeyValuePair<string, string>($"{EnrichWithDirective}:{name}", ""));
            }
        }

        private static void ProcessProperties(XDocument document, List<KeyValuePair<string, string>> settings)
        {
            var items = document.XPathSelectElements("/serilog/properties/property");
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

        private static void ProcessWriteTo(XDocument document, List<KeyValuePair<string, string>> settings)
        {
            ProcessSinks(document, settings, "/serilog/writeTo/sink", WriteToDirective);
        }

        private static void ProcessAuditTo(XDocument document, List<KeyValuePair<string, string>> settings)
        {
            ProcessSinks(document, settings, "/serilog/auditTo/sink", AuditToDirective);
        }

        private static void ProcessSinks(XDocument document, List<KeyValuePair<string, string>> settings, string sinksXPath, string writeToDirective)
        {
            var items = document.XPathSelectElements(sinksXPath);
            foreach (var element in items)
            {
                if (!element.HasAttributes)
                    continue;

                var name = element.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name))
                    continue;

                var parameters = element.XPathSelectElements("arg");

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