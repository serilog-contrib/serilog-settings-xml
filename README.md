# Serilog.Settings.Xml

[![Build status](https://ci.appveyor.com/api/projects/status/y8yctf5c6v22clqh?svg=true)](https://ci.appveyor.com/project/rsabirov/serilog-settings-xml) 
[![NuGet Version](http://img.shields.io/nuget/v/Serilog.Settings.Xml.svg?style=flat)](https://www.nuget.org/packages/Serilog.Settings.Xml/)
An XML config reader for [Serilog](https://serilog.net).

# Project is in active development stage

### Getting started

The package needs to be installed from NuGet:

```powershell
Install-Package Serilog.Settings.Xml
```

To read configuration from xml configuration file use the `ReadFrom.Xml(filePath)` extension method on your `LoggerConfiguration`:

```csharp
Log.Logger = new LoggerConfiguration()
  .ReadFrom.Xml("serilog.config")
  ... // Other configuration here, then
  .CreateLogger()
```

You can mix and match XML and code-based configuration, but each sink must be configured **either** using XML **or** in code - sinks added in code can't be modified via config file.
    
### Configuration sample

```xml
<?xml version="1.0" encoding="utf-8" ?>

<serilog>

    <using>
        <add name="Serilog.Enrichers.Thread" />
        <add name="Serilog.Enrichers.Process" />
        <add name="Serilog.Enrichers.Environment" />
    </using>

    <enrich>
        <enricher name="FromLogContext" />
        <enricher name="WithMachineName" />
        <enricher name="WithThreadId" />
        <enricher name="WithProcessId" />
        <enricher name="WithEnvironmentUserName" />
    </enrich>

    <properties>
        <property name="Application" value="Sample" />
        <property name="Path" value="%PATH%" />
    </properties>

    <minimumLevel default="Information">
        <override name="Microsoft" level="Warning" />
        <override name="Microsoft.AspNetCore.Mvc" level="Error" />
    </minimumLevel>

    <writeTo>
        <sink name="LiterateConsole" />
        <sink name="File">
            <arg name="path" value="%TEMP%\\Logs\\serilog-configuration-sample.txt" />
        </sink>
    </writeTo>

</serilog>```
