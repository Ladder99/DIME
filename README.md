# Industrial Data Transporter

Move data from industrial sources to message queues, databases, and other sinks.  

## Configuration Example

Below configuration moves data from a Rockwell PLC and an MQTT broker to an MQTT broker. 

```yaml
sinks:
  - name: mqttSink1
    enabled: !!bool true
    scan_interval: !!int 1000
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    base_topic: ids
sources:
  - name: plcSource1
    enabled: !!bool true
    scan_interval: !!int 1000
    connector: EthernetIP
    type: !!int 5
    address: 192.168.111.20
    path: 1,0
    log: !!int 0
    timeout: !!int 1000
    items:
      - name: boolTag1
        enabled: !!bool true
        type: bool
        address: B3:0/2
      - name: boolTag2
        enabled: !!bool true
        type: bool
        address: B3:0/3
  - name: mqttSource1
    enabled: !!bool true
    scan_interval: !!int 1000
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    items:
      - name: subscribe1
        enabled: !!bool true
        address: sharc/+/evt/#
```

## Creating a New Connector

1. Add configuration mapper classes in `Configuration.{new_connector}` folder.  
    a. `ConnectorConfiguration.cs` - inherits from `IDS.Transporter.Configuration.ConnectorConfiguration`.  
    b. `ConnectorItem.cs` - inherits from `IDS.Transporter.Configuration.ConnectorItem`.  
2. Add a configurator factory for the new connector in `Configurator.{new_connector}` folder.  
    a. `Source.cs` - static class in `IDS.Transporter.Configurator.{new_connector}` folder.  
    b. `Sink.cs` - static class in `IDS.Transporter.Configurator.{new_connector}` folder.  
3. Add connector implementation in `Connectors.{new_connector}` folder.  
    a. `Source.cs` - inherits from `IDS.Connectors.SourceConnector<IDS.Transporter.Configuration.{new_connector}.ConnectorConfiguration, IDS.Transporter.Configuration.{new_connector}.ConnectorItem>`.  
    b. `Source.cs` - inherits from `IDS.Connectors.SourceConnector<IDS.Transporter.Configuration.{new_connector}.ConnectorConfiguration, IDS.Transporter.Configuration.{new_connector}.ConnectorItem>`.

```
solution
 |
 |- Configuration
 |    |- NewConnector
 |         |- ConnectorConfiguration.cs
 |         |- ConnectorItem.cs
 |- Configurator
 |    |- NewConnector
 |         |- Source.cs
 |         |- Sink.cs
 |- Connectors
      |- NewConnector
           |- Source.cs
           |- Sink.cs
```
