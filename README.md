# Config Manager
Config manager component for .Net

## Sample Config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <sectionGroup name="Global">
      <section name="Proxy" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, Custom=null" />
    </sectionGroup>
  </configSections>
  <Global>
    <Proxy>
      <add key="UseProxy" value="False" />
      <add key="Host" value="192.168.1.1" />
      <add key="Port" value="1080" />
      <add key="Auth" value="False" />
      <add key="Username" value="test" />
      <add key="Password" value="test" />
    </Proxy>
  </Global>
</configuration>
```

## Example
```c#
using ParsElecom.ConfigManager;

Config config = new Config("Sample.config");
config.GroupName = "Global";
Console.WriteLine(config.GetValue("Proxy", "Host"));

config = new Config("Sample.config");
config.GroupName = "Global";
config.SetValue("Proxy", "Host", "10.10.10.1");
```

![](http://visit.parselecom.com/Api/Visit/26/DE4C8A)
