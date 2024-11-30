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

## Connectors

### Ethernet/IP

| Name            | Type         | Description                                                                 |
|-----------------|--------------|-----------------------------------------------------------------------------|
| name            | string       | unique connector name                                                       |
| enabled         | bool         | is connector enabled                                                        |
| scan_interval   | int          | scanning frequency in milliseconds                                          |
| rbe             | bool         | report by exception                                                         |
| init_script     | string       | startup lua script                                                          |
| connector       | string       | connector type, `EthernetIP`                                                |
| type            | int          | plc type (see: https://github.com/libplctag/libplctag)                      |
| address         | string       | plc hostname                                                                |
| path            | string       | plc path (see: https://github.com/libplctag/libplctag)                      |
| log             | int          | plc library log level (see: https://github.com/libplctag/libplctag)         |
| timeout         | int          | connection timeout in milliseconds                                          |
| items           | object array | plc items                                                                   |
| items[].name    | string       | unique item name                                                            |
| items[].enabled | bool         | is item enabled                                                             |
| items[].rbe     | bool         | report by exception override                                                |
| items[].type    | string       | plc register type (`bool`, `sint`, `int`, `dint`, `lint`, `real`, `string`) |
| items[].address | string       | plc register address                                                        |
| items[].script  | string       | lua script                                                                  |

#### Source Example

```yaml
  - name: plcSource1
    enabled: !!bool false
    scan_interval: !!int 1000
    rbe: !!bool true
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
```

### Haas SHDR

| Name               | Type         | Description                                                 |
|--------------------|--------------|-------------------------------------------------------------|
| name               | string       | unique connector name                                       |
| enabled            | bool         | is connector enabled                                        |
| scan_interval      | int          | scanning frequency in milliseconds                          |
| rbe                | bool         | report by exception                                         |
| init_script        | string       | startup lua script                                          |
| itemized_read      | string       | iterate connector items                                     |
| connector          | string       | connector type, `HaasSHDR`                                  |
| address            | string       | machine hostname                                            |
| port               | int          | machine port                                                |
| timeout            | int          | connection timeout in milliseconds                          |
| heartbeat_interval | int          | heartbeat frequency in milliseconds                         |
| retry_interval     | int          | retry frequency in milliseconds                             |
| items              | object array | device items                                                |
| items[].name       | string       | unique item name                                            |
| items[].enabled    | bool         | is item enabled                                             |
| items[].rbe        | bool         | report by exception override                                |
| items[].address    | string       | device item address                                         |
| items[].script     | string       | lua script                                                  |

#### Source Example

```yaml
  - name: haasSource1
    enabled: !!bool true
    scan_interval: !!int 1000
    connector: HaasSHDR
    rbe: !!bool true
    address: 192.168.111.221
    port: !!int 9998
    timeout: !!int 1000
    heartbeat_interval: !!int 4000
    retry_interval: !!int 10000
```

### Modbus TCP

| Name            | Type         | Description                                                                              |
|-----------------|--------------|------------------------------------------------------------------------------------------|
| name            | string       | unique connector name                                                                    |
| enabled         | bool         | is connector enabled                                                                     |
| scan_interval   | int          | scanning frequency in milliseconds                                                       |
| rbe             | bool         | report by exception                                                                      |
| connector       | string       | connector type, `ModbusTCP`                                                              |
| address         | string       | device hostname                                                                          |
| port            | int          | device port                                                                              |
| slave           | int          | device slave id                                                                          |
| items           | object array | subscription topics                                                                      |
| items[].name    | string       | unique item name                                                                         |
| items[].enabled | bool         | is item enabled                                                                          |
| items[].rbe     | bool         | report by exception override                                                             |
| items[].address | string       | device register address                                                                  |
| items[].type    | int          | device register type (`1`: coil, `2`: input, `3`: holding register, `4`: input register) |
| items[].count   | int          | count of registers                                                                       |

#### Source Example

```yaml
  - name: modbusSource1
    enabled: !!bool false
    scan_interval: !!int 1000
    connector: ModbusTCP
    rbe: !!bool true
    address: 192.168.111.20
    port: !!int 502
    slave: !!int 1
    timeout: !!int 1000
    items:
      - name: coilTag1
        enabled: !!bool true
        type: !!int 1
        address: !!int 1
        count: !!int 10
```

### MQTT

| Name            | Type         | Description                            |
|-----------------|--------------|----------------------------------------|
| name            | string       | unique connector name                  |
| enabled         | bool         | is connector enabled                   |
| scan_interval   | int          | scanning frequency in milliseconds     |
| rbe             | bool         | report by exception                    |
| connector       | string       | connector type, `MQTT`                 |
| address         | string       | broker hostname                        |
| port            | int          | broker port                            |
| base_topic      | string       | base topic where to publish messages   |
| items           | object array | subscription topics                    |
| items[].name    | string       | unique item name                       |
| items[].enabled | bool         | is item enabled                        |
| items[].rbe     | bool         | report by exception override           |
| items[].address | string       | topic                                  |

#### Sink Example

```yaml
  - name: mqttSink1
    enabled: !!bool true
    scan_interval: !!int 1000
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    base_topic: ids
```

#### Source Example

```yaml
  - name: mqttSource1
    enabled: !!bool false
    scan_interval: !!int 1000
    rbe: !!bool true
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    items:
      - name: subscribe1
        enabled: !!bool true
        address: sharc/+/evt/#
```

### MTConnect SHDR

| Name               | Type   | Description                                |
|--------------------|--------|--------------------------------------------|
| name               | string | unique connector name                      |
| enabled            | bool   | is connector enabled                       |
| scan_interval      | int    | scanning frequency in milliseconds         |
| rbe                | bool   | report by exception                        |
| connector          | string | connector type, `MTConnectSHDR`            |
| port               | int    | tcp listener port                          |
| device_key         | string | mtconnect device key                       |
| heartbeat_interval | int    | heartbeat frequency in milliseconds        |
| filter_duplicates  | bool   | filter duplicate data items at the adapter |

#### Sink Example

```yaml
  - name: shdrSink1
    enabled: !!bool false
    scan_interval: !!int 1000
    connector: MTConnectSHDR
    port: !!int 7878
    device_key: ~
    heartbeat_interval: !!int 10000
    filter_duplicates: !!bool true
```

## Creating a New Connector

1. Add configuration mapper classes in `Configuration.{new_connector}` folder.  
   a. `ConnectorConfiguration.cs` - inherits from `IDS.Transporter.Configuration.ConnectorConfiguration`.  
   b. `ConnectorItem.cs` - inherits from `IDS.Transporter.Configuration.ConnectorItem`.
2. Add a configurator factory for the new connector in `Configurator.{new_connector}` folder.  
   a. `Source.cs` - static class in `IDS.Transporter.Configurator.{new_connector}` folder.  
   b. `Sink.cs` - static class in `IDS.Transporter.Configurator.{new_connector}` folder.  
   c. Update `SourceConnectorFactory.cs` or `SourceConnectorFactory.cs`.
3. Add connector implementation in `Connectors.{new_connector}` folder.  
   a. `Source.cs` - inherits from `IDS.Connectors.SourceConnector<IDS.Transporter.Configuration.{new_connector}.ConnectorConfiguration, IDS.Transporter.Configuration.{new_connector}.ConnectorItem>`.  
   b. `Source.cs` - inherits from `IDS.Connectors.SourceConnector<IDS.Transporter.Configuration.{new_connector}.ConnectorConfiguration, IDS.Transporter.Configuration.{new_connector}.ConnectorItem>`.

```
solution
 |
 |- Configuration (1)
 |    |- NewConnector
 |         |- ConnectorConfiguration.cs
 |         |- ConnectorItem.cs
 |- Configurator (2)
 |    |- NewConnector
 |    |    |- Source.cs
 |    |    |- Sink.cs
 |    |- SourceConnectorFactory.cs
 |    |- SinkConnectorFactory.cs   
 |- Connectors (3)
      |- NewConnector
           |- Source.cs
           |- Sink.cs
```

## Scripting

Each connector configuration provides allows for Lua script execution.  The `init_script` property is executed on 
startup and is used to import additional .NET or Lua libraries.  Within each item script, the primary cache can be 
accessed using the `cache(path, defaultValue)` function call.  The `path` refers to the item's unique path which 
is a combination of the connector's and item's name (e.g. `eipSource1/boolTag2`, `mqttSource1/ffe4Sensor`). 
Within the connector's execution context, the connector name can be omitted and replaced with a period, `./boolTag2`. 
A secondary cache can be accessed using the `get(key, defaultValue)` and `set(key, value)` function calls.  This 
user defined cache is scoped to the individual connector.

```yaml
eipSource1: &eipSource1
  name: eipSource1
  enabled: !!bool true
  scan_interval: !!int 1000
  connector: EthernetIP
  rbe: !!bool true
  type: !!int 5
  address: 192.168.111.20
  path: 1,0
  log: !!int 0
  timeout: !!int 1000
  init_script: |
    luanet.load_assembly("System")
    CLR = {
      env = luanet.import_type("System.Environment")
    };
  items:
    - name: scriptTag1
      enabled: !!bool true
      rbe: !!bool true
      script: return CLR.env.MachineName;
    - name: scriptTag2
      enabled: !!bool true
      script: return os.date("%Y-%m-%d %H:%M:%S")
    - name: scriptTag3
      script: |
        local a = cache('./scriptTag1', nil);
        return a;
    - name: scriptTag4
      script: |
        local a = cache('mqttSource1/ffe4Sensor', nil);
        return a;
    - name: scriptTag5
      script: |
        local a = set('random', math.random(500));
        return a;
    - name: scriptTag6
      script: |
        return get('random', -1);
    - name: boolTag1
      enabled: !!bool false
      type: bool
      address: B3:0/2
    - name: boolTag2
      enabled: !!bool false
      type: bool
      address: B3:0/3
      script: |
        local m = { [0]='Ready', [1]='Active' };
        return m[result and 1 or 0];
        
mqttSource1: &mqttSource1
  name: mqttSource1
  enabled: !!bool true
  scan_interval: !!int 1000
  connector: MQTT
  rbe: !!bool true
  itemized_read: !!bool true
  address: wss.sharc.tech
  port: !!int 1883
  init_script: |
    luanet.load_assembly("System")
    CLR = {
      env = luanet.import_type("System.Environment")
    };
    -- https://github.com/rxi/json.lua
    json = require('json');
  items:
    - name: ffe4Sensor
      enabled: !!bool true
      rbe: !!bool false
      address: sharc/08d1f953ffe4/evt/io/s1
      script: |
        return json.decode(result).v.s1.v;
    - name: randomNumberFromEip
      enabled: !!bool true
      script: |
        return cache('eipSource1/scriptTag5', -1);

sinks:
   - *mqttSink1
   - *shdrSink1
sources:
   - *eipSource1
   - *mqttSource1
```
