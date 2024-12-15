![DIME Logo](dime_logo.png)
  
Reshape and move data from enterprise and industrial sources to message queues, databases, and other sinks.  
  
<i>Data integration made easy!</i>™  
![DIME DataOps Diagram](dime_dataops_diagram.png)

Videos
- [Quick Introduction](https://www.youtube.com/watch?v=P5Gc5bKdiy4)  

## How to Run

### Windows

Download the latest [release](https://github.com/Ladder99/DIME/releases) and run `DIME.exe`.  Alternatively, `DIME.exe install` will install the DIME Windows Service.

### Docker

```sh
cd ~
git clone https://github.com/ladder99/DIME

mkdir -p volumes/dime/configs
mkdir -p volumes/dime/lua
mkdir -p volumes/dime/logs

cp DIME/DIME/nlog.config volumes/dime/nlog.config
cp DIME/DIME/Configs/Examples/Basic/* volumes/dime/configs
cp DIME/DIME/Lua/* volumes/dime/lua

docker run \
   -p 7878:7878 \
   -p 8080:8080 \
   -p 8081:8081 \
   -p 8082:8082 \
   -p 9998:9998 \
   -p 9999:9999 \
   -v ~/volumes/dime/nlog.config:/app/nlog.config \
   -v ~/volumes/dime/configs:/app/Configs \
   -v ~/volumes/dime/lua:/app/Lua \
   -v ~/volumes/dime/logs:/app/Logs \
   ladder99/dime:latest
```

### Status and Configuration Server

`GET http://localhost:9999/status` - Server status.  
`GET http://localhost:9999/config/yaml` - Running configuration, YAML formatted.  
`GET http://localhost:9999/config/json` - Running configuration, JSON formatted.  
`POST http://localhost:9999/config/yaml` - Upload new configuration, YAML formatted.  
`GET http://localhost:9999/service/restart` - Restart all connectors.  
`WS ws://localhost:9998/` - Connector status feed.

## Configuration Example

Below configuration moves data from a Rockwell PLC and an MQTT broker to an MQTT broker. 

```yaml
sinks:
  - name: mqttSink1
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    base_topic: ids
sources:
  - name: plcSource1
    connector: EthernetIP
    type: !!int 5
    address: 192.168.111.20
    path: 1,0
    items:
      - name: boolTag1
        type: bool
        address: B3:0/2
      - name: boolTag2
        type: bool
        address: B3:0/3
  - name: mqttSource1
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    items:
      - name: subscribe1
        enabled: !!bool true
        address: sharc/+/evt/#
```

## Connectors

<table><tr><td valign="top">

| Source                                                                    |
|---------------------------------------------------------------------------|
| Active MQ                                                                 |
| [ASC CPC](#asc-cpc)                                                       |
| Beckhoff ADS                                                              |
| Brother CNC                                                               |
| [Ethernet/IP](#ethernetip)                                                |
| Fanuc Focas [[external driver]](https://github.com/Ladder99/fanuc-driver) |
| Filesystem                                                                |
| [Haas SHDR](#haas-shdr)                                                   |
| HTTP Client                                                               |
| [HTTP Server](#http-server)                                               |
| [Modbus/TCP](#modbus-tcp)                                                 |
| [MQTT](#mqtt)                                                             |
| MS SQL Server                                                             |
| [MTConnect Agent](#mtconnect-agent)                                       |
| OPC-DA                                                                    |
| [OPC-UA](#opc-ua)                                                         |
| OPC XML-DA                                                                |
| Postgres                                                                  |
| [Redis](#redis)                                                           |
| [Script](#script)                                                         |
| Siemens S7                                                                |
| [SNMP](#snmp)                                                             |
| [Timebase Websocket](#timebase-websocket)                                 | 
| [Wintriss SmartPac](#wintriss-smartpac)                                   |
| Zenoh                                                                     |

</td><td valign="top">

| Sink                                  |
|---------------------------------------|
| ActiveMQ                              |
| [Console](#console)                   |
| [HTTP Server](#http-server)           |
| [Influx LP](#influx-lp)               |
| IoTDB                                 |
| [MQTT](#mqtt)                         |
| MS SQL Server                         |
| MTConnect Agent                       |
| [MTConnect SHDR](#mtconnect-shdr)     |
| Postgres                              |
| [Redis](#redis)                       |
| [Splunk EH SDK](#splunk-eh-sdk)       |
| [Splunk HEC](#splunk-hec)             |
| [SparkplugB](#sparkplugb)             |
| [TrakHound HTTP](#trakhound-http)     |
| [Websocket Server](#websocket-server) |
| Zenoh                                 |

</td></tr></table>

### ASC CPC

| Name            | Type         | Description                        |
|-----------------|--------------|------------------------------------|
| name            | string       | unique connector name              |
| enabled         | bool         | is connector enabled               |
| scan_interval   | int          | scanning frequency in milliseconds |
| rbe             | bool         | report by exception                |
| init_script     | string       | startup lua script                 |
| deinit_script   | string       | shutdown lua script                |
| enter_script    | string       | before loop script                 |
| exit_script     | string       | after loop script                  |
| connector       | string       | connector type, `AscCPC`           |
| address         | string       | computer hostname                  |
| port            | int          | port                               |
| bypass_ping     | bool         | bypass ping on connect             |
| items           | object array | cpc items                          |
| items[].name    | string       | unique item name                   |
| items[].enabled | bool         | is item enabled                    |
| items[].rbe     | bool         | report by exception override       |
| items[].address | string       | cpc item address                   |
| items[].script  | string       | lua script                         |

#### Source Example

```yaml
  - name: ascCpcSource1
    connector: AscCPC
    address: 192.168.111.12
    port: !!int 9999
    bypass_ping: !!bool true
    init_script: ~
    items:
      - name: Temperature
        address: .Autoclave.Inputs.AIRTC\Value
```

### Console

| Name          | Type   | Description                        |
|---------------|--------|------------------------------------|
| name          | string | unique connector name              |
| enabled       | bool   | is connector enabled               |
| scan_interval | int    | scanning frequency in milliseconds |
| rbe           | bool   | report by exception                |
| init_script   | string | startup lua script                 |
| deinit_script | string | shutdown lua script                |
| enter_script  | string | before loop script                 |
| exit_script   | string | after loop script                  |
| connector     | string | connector type, `Console`          |

#### Sink Example

```yaml
  - name: consoleSink1
    connector: Console
```

### Ethernet/IP

| Name            | Type         | Description                                                                 |
|-----------------|--------------|-----------------------------------------------------------------------------|
| name            | string       | unique connector name                                                       |
| enabled         | bool         | is connector enabled                                                        |
| scan_interval   | int          | scanning frequency in milliseconds                                          |
| rbe             | bool         | report by exception                                                         |
| init_script     | string       | startup lua script                                                          |
| deinit_script   | string       | shutdown lua script                                                         |
| enter_script    | string       | before loop script                                                          |
| exit_script     | string       | after loop script                                                           |
| connector       | string       | connector type, `EthernetIP`                                                |
| type            | int          | plc type (see: https://github.com/libplctag/libplctag)                      |
| address         | string       | plc hostname                                                                |
| path            | string       | plc path (see: https://github.com/libplctag/libplctag)                      |
| log             | int          | plc library log level (see: https://github.com/libplctag/libplctag)         |
| timeout         | int          | connection timeout in milliseconds                                          |
| bypass_ping     | bool         | bypass ping on connect                                                      |
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
    connector: EthernetIP
    type: !!int 5
    address: 192.168.111.20
    path: 1,0
    log: !!int 0
    timeout: !!int 1000
    bypass_ping: !!bool true
    items:
      - name: boolTag1
        type: bool
        address: B3:0/2
      - name: intTag2
        type: int
        address: N7:1
```

### Haas SHDR

| Name               | Type         | Description                          |
|--------------------|--------------|--------------------------------------|
| name               | string       | unique connector name                |
| enabled            | bool         | is connector enabled                 |
| scan_interval      | int          | scanning frequency in milliseconds   |
| rbe                | bool         | report by exception                  |
| init_script        | string       | startup lua script                   |
| deinit_script      | string       | shutdown lua script                  |
| enter_script       | string       | before loop script                   |
| exit_script        | string       | after loop script                    |
| itemized_read      | string       | iterate connector items              |
| connector          | string       | connector type, `HaasSHDR`           |
| address            | string       | machine hostname                     |
| port               | int          | machine port                         |
| timeout            | int          | connection timeout in milliseconds   |
| heartbeat_interval | int          | heartbeat frequency in milliseconds  |
| retry_interval     | int          | retry frequency in milliseconds      |
| items              | object array | device items                         |
| items[].name       | string       | unique item name                     |
| items[].enabled    | bool         | is item enabled                      |
| items[].rbe        | bool         | report by exception override         |
| items[].address    | string       | device item address                  |
| items[].script     | string       | lua script                           |

#### Source Example

```yaml
  - name: haasSource1
    connector: HaasSHDR
    itemized_read: !!bool false
    address: 192.168.111.221
    port: !!int 9998
    timeout: !!int 1000
    heartbeat_interval: !!int 4000
    retry_interval: !!int 10000
    items:
      - name: CPU
        enabled: !!bool true
        address: CPU
        script: |
          if tonumber(result) > 0.5 then
            return 'HIGH';
          else
            return 'LOW';
          end
```

### HTTP Server

| Name              | Type         | Description                        |
|-------------------|--------------|------------------------------------|
| name              | string       | unique connector name              |
| enabled           | bool         | is connector enabled               |
| scan_interval     | int          | scanning frequency in milliseconds |
| rbe               | bool         | report by exception                |
| init_script       | string       | startup lua script                 |
| deinit_script     | string       | shutdown lua script                |
| enter_script      | string       | before loop script                 |
| exit_script       | string       | after loop script                  |
| connector         | string       | connector type, `HTTPServer`       |
| uri               | string       | base uri                           |
| items             | object array | post items                         |
| items[].name      | string       | unique item name                   |
| items[].enabled   | bool         | is item enabled                    |
| items[].rbe       | bool         | report by exception override       |
| items[].address   | string       | uri path                           |
| items[].script    | string       | lua script                         |

#### Sink Example

```yaml
  - name: httpServerSink1
    connector: HttpServer
    uri: http://localhost:8080/
```

#### Source Example

```yaml
  - httpServerSource1: &httpServerSource1
    name: httpServerSource1
    connector: HTTPServer
    uri: http://localhost:8081/
    init_script: |
      json = require('json');
    items:
      - name: postData
        address: post/data
        script: |
          return json.decode(result).hello;
```

### Influx LP

| Name          | Type   | Description                        |
|---------------|--------|------------------------------------|
| name          | string | unique connector name              |
| enabled       | bool   | is connector enabled               |
| scan_interval | int    | scanning frequency in milliseconds |
| rbe           | bool   | report by exception                |
| init_script   | string | startup lua script                 |
| deinit_script | string | shutdown lua script                |
| enter_script  | string | before loop script                 |
| exit_script   | string | after loop script                  |
| connector     | string | connector type, `InfluxLP`         |
| address       | string | hostname                           |
| port          | int    | port                               |
| token         | string | api token                          |
| bucket_name   | string | bucket name                        |
| org_id        | string | organization id                    |

#### Sink Example

```yaml
  - name: influxLpSink1
    connector: InfluxLP
    address: 172.24.56.104
    port: !!int 8086
    token: abc123
    bucket_name: bucket1
    org_id: org1
```

### Modbus TCP

| Name            | Type         | Description                                                                              |
|-----------------|--------------|------------------------------------------------------------------------------------------|
| name            | string       | unique connector name                                                                    |
| enabled         | bool         | is connector enabled                                                                     |
| scan_interval   | int          | scanning frequency in milliseconds                                                       |
| rbe             | bool         | report by exception                                                                      |
| init_script     | string       | startup lua script                                                                       |
| deinit_script   | string       | shutdown lua script                                                                      |
| enter_script    | string       | before loop script                                                                       |
| exit_script     | string       | after loop script                                                                        |
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
    connector: ModbusTCP
    address: 192.168.111.20
    port: !!int 502
    slave: !!int 1
    timeout: !!int 1000
    init_script: |
      -- https://github.com/iryont/lua-struct
       struct = require('struct')
    items:
      - name: coilTags
        type: !!int 1
        address: !!int 1
        count: !!int 10
      - name: holdingTags
        type: !!int 3
        address: !!int 24
        count: !!int 2
        script: |
           -- https://www.scadacore.com/tools/programming-calculators/online-hex-converter/
           return struct.unpack('<I', struct.pack('<HH', result[0], result[1]));

```

### MQTT

| Name            | Type         | Description                          |
|-----------------|--------------|--------------------------------------|
| name            | string       | unique connector name                |
| enabled         | bool         | is connector enabled                 |
| scan_interval   | int          | scanning frequency in milliseconds   |
| rbe             | bool         | report by exception                  |
| init_script     | string       | startup lua script                   |
| deinit_script   | string       | shutdown lua script                  |
| enter_script    | string       | before loop script                   |
| exit_script     | string       | after loop script                    |
| connector       | string       | connector type, `MQTT`               |
| address         | string       | broker hostname                      |
| port            | int          | broker port                          |
| base_topic      | string       | base topic where to publish messages |
| qos             | int          | quality of service                   |
| retain          | bool         | published retained                   |
| items           | object array | subscription topics                  |
| items[].name    | string       | unique item name                     |
| items[].enabled | bool         | is item enabled                      |
| items[].rbe     | bool         | report by exception override         |
| items[].address | string       | topic                                |

#### Sink Example

```yaml
  - name: mqttSink1
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    base_topic: ids
    qos: !!int 0
    retain: !!bool true
```

#### Source Example

```yaml
  - name: mqttSource1
    connector: MQTT
    address: wss.sharc.tech
    port: !!int 1883
    items:
      - name: subscribe1
        address: sharc/+/evt/#
```

### MTConnect Agent

| Name             | Type         | Description                        |
|------------------|--------------|------------------------------------|
| name             | string       | unique connector name              |
| enabled          | bool         | is connector enabled               |
| scan_interval    | int          | scanning frequency in milliseconds |
| rbe              | bool         | report by exception                |
| init_script      | string       | startup lua script                 |
| deinit_script    | string       | shutdown lua script                |
| enter_script     | string       | before loop script                 |
| exit_script      | string       | after loop script                  |
| itemized_read    | string       | iterate connector items            |
| connector        | string       | connector type, `MTConnectAgent`   |
| address          | string       | agent address                      |
| port             | int          | agent port                         |
| items            | object array | data items                         |
| items[].name     | string       | unique item name                   |
| items[].enabled  | bool         | is item enabled                    |
| items[].rbe      | bool         | report by exception override       |
| items[].address  | string       | data item id                       |

#### Sink Example

```yaml
  - name: mtConnectSink1
    connector: MTConnectAgent
    port: !!int 5000
    device_uuid: 123
    device_id: device1
    device_name: device1
    device_manufacturer: acme
    device_model: 123
    device_serial_number: 123
```

#### Source Example

```yaml
  - name: mtConnectSource1
    connector: MTConnectAgent
    address: mtconnect.mazakcorp.com
    port: !!int 5719
    interval: !!int 100
    items:
      - name: PathPositionSample
        address: pathpos
        script: |
          return result[0].Value;
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
    connector: MTConnectSHDR
    port: !!int 7878
    device_key: ~
    heartbeat_interval: !!int 10000
    filter_duplicates: !!bool true
```

### OPC-UA

| Name              | Type         | Description                        |
|-------------------|--------------|------------------------------------|
| name              | string       | unique connector name              |
| enabled           | bool         | is connector enabled               |
| scan_interval     | int          | scanning frequency in milliseconds |
| rbe               | bool         | report by exception                |
| init_script       | string       | startup lua script                 |
| deinit_script     | string       | shutdown lua script                |
| enter_script      | string       | before loop script                 |
| exit_script       | string       | after loop script                  |
| connector         | string       | connector type, `OpcUA`            |
| address           | string       | hostname                           |
| port              | int          | port                               |
| timeout           | int          | timeout                            |
| anonymous         | bool         | anonymous user                     |
| username          | string       | username                           |
| password          | string       | password                           |
| bypass_ping       | bool         | bypass ping on connect             |
| items             | object array | cpc items                          |
| items[].name      | string       | unique item name                   |
| items[].enabled   | bool         | is item enabled                    |
| items[].rbe       | bool         | report by exception override       |
| items[].address   | string       | node address                       |
| items[].script    | string       | lua script                         |
| items[].namespace | int          | namespace index                    |

#### Source Example

```yaml
  - name: opcUaSource1
    connector: OpcUA
    address: localhost
    port: !!int 49320
    timeout: !!int 1000
    anonymous: !!bool false
    username: chris
    password: passwordpassword
    items:
      - name: DateTime
        namespace: !!int 2
        address: _System._DateTime
      - name: Random
        namespace: !!int 2
        address: Simulation Examples.Functions.Random6
```

### Redis

| Name            | Type         | Description                        |
|-----------------|--------------|------------------------------------|
| name            | string       | unique connector name              |
| enabled         | bool         | is connector enabled               |
| scan_interval   | int          | scanning frequency in milliseconds |
| rbe             | bool         | report by exception                |
| init_script     | string       | startup lua script                 |
| deinit_script   | string       | shutdown lua script                |
| enter_script    | string       | before loop script                 |
| exit_script     | string       | after loop script                  |
| connector       | string       | connector type, `Redis`            |
| address         | string       | hostname                           |
| port            | int          | port                               |
| database        | int          | database id                        |
| items           | object array | items                              |
| items[].name    | string       | unique item name                   |
| items[].enabled | bool         | is item enabled                    |
| items[].rbe     | bool         | report by exception override       |
| items[].address | string       | cache path                         |

#### Sink Example

```yaml
  - name: redisSink1
    connector: Redis
    address: 172.24.56.104
    port: !!int 6379
    database: !!int 0
```

#### Source Example

```yaml
  - name: redisSink1
    connector: Redis
    address: 172.24.56.104
    port: !!int 6379
    database: !!int 0
    items:
      - name: plcGoodPartCount
        address: eipSource1/GoodPartCount
```

### Script

| Name            | Type         | Description                        |
|-----------------|--------------|------------------------------------|
| name            | string       | unique connector name              |
| enabled         | bool         | is connector enabled               |
| scan_interval   | int          | scanning frequency in milliseconds |
| rbe             | bool         | report by exception                |
| init_script     | string       | startup lua script                 |
| deinit_script   | string       | shutdown lua script                |
| enter_script    | string       | before loop script                 |
| exit_script     | string       | after loop script                  |
| connector       | string       | connector type, `Script`           |
| items           | object array | read items                         |
| items[].name    | string       | unique item name                   |
| items[].enabled | bool         | is item enabled                    |
| items[].rbe     | bool         | report by exception override       |
| items[].script  | string       | lua script                         |

#### Source Example

```yaml
  - name: scriptSource1
    connector: Script
    init_script: ~
    deinit_script: ~
    enter_script: ~
    exit_script: ~
    items:
      - name: Temperature
        script: |
          return math.random(100);
```

### SNMP

| Name            | Type         | Description                        |
|-----------------|--------------|------------------------------------|
| name            | string       | unique connector name              |
| enabled         | bool         | is connector enabled               |
| scan_interval   | int          | scanning frequency in milliseconds |
| rbe             | bool         | report by exception                |
| init_script     | string       | startup lua script                 |
| deinit_script   | string       | shutdown lua script                |
| enter_script    | string       | before loop script                 |
| exit_script     | string       | after loop script                  |
| connector       | string       | connector type, `SNMP`             |
| address         | string       | device hostname                    |
| community       | string       | community                          |
| timeout         | int          | read timeout                       |
| items           | object array | device items                       |
| items[].name    | string       | unique item name                   |
| items[].enabled | bool         | is item enabled                    |
| items[].rbe     | bool         | report by exception override       |
| items[].address | string       | oid address                        |
| items[].script  | string       | lua script                         |

#### Source Example

```yaml
  - name: snmpSource1
    connector: SNMP
    address: 192.168.150.143
    port: !!int 161
    community: public
    timeout: !!int 1000
    items:
      - name: Temperature
        address: 1.3.6.1.4.1.6574.1.2.0
      - name: Model
        address: 1.3.6.1.4.1.6574.1.5.1.0
      - name: SerialNumber
        address: 1.3.6.1.4.1.6574.1.5.2.0
```

### SparkplugB

| Name               | Type   | Description                          |
|--------------------|--------|--------------------------------------|
| name               | string | unique connector name                |
| enabled            | bool   | is connector enabled                 |
| scan_interval      | int    | scanning frequency in milliseconds   |
| rbe                | bool   | report by exception                  |
| init_script        | string | startup lua script                   |
| deinit_script      | string | shutdown lua script                  |
| enter_script       | string | before loop script                   |
| exit_script        | string | after loop script                    |
| connector          | string | connector type, `SparkplugB`         |
| address            | string | broker hostname                      |
| port               | int    | broker port                          |
| username           | string | broker username                      |
| password           | string | broker password                      |
| host_id            | string | host id                              |
| group_id           | string | group id                             |
| node_id            | string | node id                              |
| device_id          | string | device_id                            |
| reconnect_interval | int    | reconnect interval                   |
| birth_delay        | int    | wait after connection to send birth  |

#### Sink Example

```yaml
  - name: sparkplugBSink1
    connector: SparkplugB
    address: localhost
    port: !!int 1883
    username: admin
    password: admin
    host_id: dime
    group_id: dime
    node_id: dime
    device_id: dime
    reconnect_interval: !!int 15000
    birth_delay: !!int 10000
```

### Splunk EH SDK

| Name               | Type   | Description                        |
|--------------------|--------|------------------------------------|
| name               | string | unique connector name              |
| enabled            | bool   | is connector enabled               |
| scan_interval      | int    | scanning frequency in milliseconds |
| rbe                | bool   | report by exception                |
| init_script        | string | startup lua script                 |
| deinit_script      | string | shutdown lua script                |
| enter_script       | string | before loop script                 |
| exit_script        | string | after loop script                  |
| connector          | string | connector type, `SplunkEhSDK`      |
| address            | string | address                            |
| port               | int    | port                               | 
| numbers_to_metrics | bool   | write numbers as metrics           |

#### Sink Example

```yaml
  - name: splunkEhSdk
    connector: SplunkEhSDK
    address: http://host.docker.internal
    port: !!int 50051
    numbers_to_metrics: !!bool true
```

### Splunk HEC

| Name            | Type   | Description                        |
|-----------------|--------|------------------------------------|
| name            | string | unique connector name              |
| enabled         | bool   | is connector enabled               |
| scan_interval   | int    | scanning frequency in milliseconds |
| rbe             | bool   | report by exception                |
| init_script     | string | startup lua script                 |
| deinit_script   | string | shutdown lua script                |
| enter_script    | string | before loop script                 |
| exit_script     | string | after loop script                  |
| connector       | string | connector type, `SplunkHEC`        |
| address         | string | hostname                           |
| port            | int    | port                               |
| use_ssl         | bool   | use ssl                            |
| token           | string | token                              |
| event_or_metric | string | `event`, `metric`                  | 
| source          | string | source                             |
| source_type     | string | source_type                        |

#### Sink Example

```yaml
  - name: splunkHecSink1
    connector: SplunkHEC
    address: localhost
    port: 8088
    use_ssl: false
    token: abc123
    event_or_metric: event
    source: source1
    source_type: _json
```

### Timebase Websocket

| Name            | Type         | Description                        |
|-----------------|--------------|------------------------------------|
| name            | string       | unique connector name              |
| enabled         | bool         | is connector enabled               |
| scan_interval   | int          | scanning frequency in milliseconds |
| rbe             | bool         | report by exception                |
| init_script     | string       | startup lua script                 |
| deinit_script   | string       | shutdown lua script                |
| enter_script    | string       | before loop script                 |
| exit_script     | string       | after loop script                  |
| connector       | string       | connector type, `TimebaseWS`       |
| address         | string       | broker hostname                    |
| port            | int          | broker port                        |
| items           | object array | subscription topics                |
| items[].name    | string       | unique item name                   |
| items[].enabled | bool         | is item enabled                    |
| items[].rbe     | bool         | report by exception override       |
| items[].address | string       | historian address                  |
| items[].group   | string       | historian dataset group            |

#### Source Example

```yaml
   - name: timebaseWsSource1
     connector: TimebaseWS
     address: localhost
     port: 4511
     items:
       - name: plcExecution
         group: MQTT Data
         address: dime/eipSource1/Execution/Data
```

### Trakhound HTTP

| Name          | Type   | Description                        |
|---------------|--------|------------------------------------|
| name          | string | unique connector name              |
| enabled       | bool   | is connector enabled               |
| scan_interval | int    | scanning frequency in milliseconds |
| rbe           | bool   | report by exception                |
| init_script   | string | startup lua script                 |
| deinit_script | string | shutdown lua script                |
| enter_script  | string | before loop script                 |
| exit_script   | string | after loop script                  |
| connector     | string | connector type, `TrakhoundHTTP`    |
| address       | string | hostname                           |
| port          | int    | port                               |
| use_ssl       | bool   | use ssl                            |
| router        | string | router                             |
| base_path     | string | base path                          |

#### Sink Example

```yaml
  - name: trakhoundHttpSink1
    enabled: !!bool false
    scan_interval: !!int 1000
    connector: TrakHoundHTTP
    address: localhost
    port: 8472
    use_ssl: false
    router: default
    base_path: Ladder99:/DIME/HttpSink
```

### Websocket Server

| Name              | Type         | Description                        |
|-------------------|--------------|------------------------------------|
| name              | string       | unique connector name              |
| enabled           | bool         | is connector enabled               |
| scan_interval     | int          | scanning frequency in milliseconds |
| rbe               | bool         | report by exception                |
| init_script       | string       | startup lua script                 |
| deinit_script     | string       | shutdown lua script                |
| enter_script      | string       | before loop script                 |
| exit_script       | string       | after loop script                  |
| connector         | string       | connector type, `WebsocketServer`  |
| uri               | string       | base uri                           |

#### Sink Example

```yaml
  - name: wsServerSink1
    connector: WebsocketServer
    uri: ws://127.0.0.1:8082/
```

### Wintriss SmartPac

| Name            | Type         | Description                        |
|-----------------|--------------|------------------------------------|
| name            | string       | unique connector name              |
| enabled         | bool         | is connector enabled               |
| scan_interval   | int          | scanning frequency in milliseconds |
| rbe             | bool         | report by exception                |
| init_script     | string       | startup lua script                 |
| deinit_script   | string       | shutdown lua script                |
| enter_script    | string       | before loop script                 |
| exit_script     | string       | after loop script                  |
| connector       | string       | connector type, `SmartPAC`         |
| address         | string       | device hostname                    |
| port            | int          | port                               |
| items           | object array | subscription topics                |
| items[].name    | string       | unique item name                   |
| items[].enabled | bool         | is item enabled                    |
| items[].rbe     | bool         | report by exception override       |
| items[].script  | string       | item script                        |

#### Source Example

```yaml
  - name: smartpacSource1
    connector: SmartPAC
    address: 172.16.200.18
    port: !!int 1007
    items:
      - name: PressType
        enabled: !!bool true
        script: return result[0];
      - name: PressName
        enabled: !!bool true
        script: return result[1];
```

## Scripting

Each connector configuration allows for Lua script execution.  The `init_script` property is executed on 
startup and is used to import additional .NET or Lua libraries.  The `deinit_script` property is executed on shutdown. 
The `enter_script` and `exit_script` properties are executed before and after reading all items, respectively. 
Within each item script, the caches can be accessed using the `cache(path, defaultValue)` and 
`cache_ts(path, defaultValue)` function calls. The `path` refers to the item's unique path which is a combination of 
the connector's and item's name (e.g. `eipSource1/boolTag2`, `mqttSource1/ffe4Sensor`). Within the connector's execution context, 
the connector name can be omitted or replaced with a period, `./boolTag2`. A secondary user cache can be accessed 
using the `cache(key, defaultValue)` and `set(key, value)` function calls.  The connector and configuration objects 
can be accessed using the `connector()` and `configuration()` function calls, respectively.  

### Functions

`value = cache(path, defaultValue)` - retrieve value from caches, shared across connectors.  
`value, timestamp = cache_ts(path, defaultValue)` - retrieve value and timestamp from caches.   
`value = set(key, value)` - set value into user cache.  
`connector = connector()` - retrieve the connector instance.  
`configuration = configuration()` - retrieve the connector's configuration instance.  

### Example

```yaml
mqttSink1: &mqttSink1
   name: mqttSink1
   connector: MQTT
   address: wss.sharc.tech
   port: !!int 1883
   base_topic: ids

eipSource1: &eipSource1
   name: eipSource1
   connector: EthernetIP
   type: !!int 5
   address: 192.168.111.20
   path: 1,0
   log: !!int 0
   timeout: !!int 1000
   init_script: ~
   deinit_script: ~
   enter_script: ~
   exit_script: ~
   items:
      - name: boolSetUserCacheOnly
        enabled: !!bool true
        type: bool
        address: B3:0/2
        script: |
           set('boolTag', result);
           return nil;
      - name: boolGetUserCache
        enabled: !!bool true
        script: |
           return cache('boolTag', false);
      - name: Execution
        enabled: !!bool true
        type: bool
        address: B3:0/3
        script: |
           local m = { [0]='Ready', [1]='Active' };
           return m[result and 1 or 0];
        
mqttSource1: &mqttSource1
   name: mqttSource1
   connector: MQTT
   itemized_read: !!bool true
   address: wss.sharc.tech
   port: !!int 1883
   init_script: |
      -- https://github.com/rxi/json.lua
      json = require('json');
   items:
      - name: subscribe1
        enabled: !!bool false
        address: sharc/+/evt/#
      - name: ffe4Sensor
        rbe: !!bool false
        address: sharc/08d1f953ffe4/evt/io/s1
        script: |
           return json.decode(result).v.s1.v;
      - name: ffe4SensorAndDelta
        rbe: !!bool false
        address: sharc/08d1f953ffe4/evt/io/s1
        script: |
           return json.decode(result).v.s1.v, json.decode(result).v.s1.d;

scriptSource1: &scriptSource1
   name: scriptSource1
   connector: Script
   init_script: |
      luanet.load_assembly("System")
      CLR = {
        env = luanet.import_type("System.Environment")
      };
      -- https://github.com/rxi/json.lua
      json = require('json');
      -- https://github.com/Yonaba/Moses
      moses = require('moses');
      pcArray = {}
   items:
      - name: machineNameDiscrete
        rbe: !!bool false
        script: |
           return CLR.env.MachineName;
      - name: machineNameByRefRbe
        script: |
           return cache('./machineNameDiscrete', nil);
      - name: dateTime
        script: |
           return os.date("%Y-%m-%d %H:%M:%S");
      - name: randomUserCacheOnly
        script: |
           set('random', math.random(500));
           return nil;
      - name: randomFromUserCache
        script: |
           return cache('random', -1);
      - name: mqttSensorReading
        script: |
           return cache('mqttSource1/ffe4Sensor', nil);
      - name: mqttSensorReadingMedian
        script: |
           table.insert(pcArray, cache('mqttSource1/ffe4Sensor', 0));
           pcArray = moses.last(pcArray, 100);
           return moses.median(pcArray);
      - name: AcmeCorp/ChicagoPlant/AssemblyArea/Line1/PartCount
        rbe: !!bool false
        script: |
           return cache('mqttSource1/ffe4Sensor', nil)
      - name: AcmeCorp/ChicagoPlant/AssemblyArea/Line1/ReportPeriod
        rbe: !!bool false
        script: |
           return cache('mqttSource1/ffe4SensorAndDelta', {0, 0})[1]
      - name: AcmeCorp/ChicagoPlant/AssemblyArea/Line1/Execution
        rbe: !!bool true
        script: |
           return cache('eipSource1/Execution', nil)
      - name: OverallAvailabilityArrayNonRbe
        rbe: !!bool false
        script: |
           local n = cache('eipSource1/$SYSTEM/IsConnected', nil);
           return n, n==true;
      - name: OverallAvailabilityArrayRbe
        script: |
           local n = cache('eipSource1/$SYSTEM/IsConnected', nil);
           return n, n==true;
      - name: OverallAvailabilityRbe
        script: |
           local n = cache('eipSource1/$SYSTEM/IsConnected', nil);
           return n==true and 'Available' or 'Unavailable';

sinks:
   - *mqttSink1
sources:
   - *eipSource1
   - *mqttSource1
   - *scriptSource1
```

## Manual Build

```sh
wget https://dot.net/v1/dotnet-install.sh
bash ./dotnet-install.sh --channel 8.0
export PATH="$HOME/.dotnet/:$PATH"

cd ~
git clone https://github.com/ladder99/DIME

cd DIME/DIME
dotnet restore
dotnet publish -c Release -o out
dotnet DIME.dll
```

## Docker

```sh
cd ~
git clone https://github.com/ladder99/DIME
docker login

docker run --privileged --rm tonistiigi/binfmt --install all
docker buildx create --name multi-arch-builder --use

cd DIME/DIME
docker buildx build --platform linux/amd64,linux/arm64 -t ladder99/dime:1.0.0 -t ladder99/dime:latest --push .
#docker build -f Dockerfile --tag=ladder99/dime:1.0.0 --tag=ladder99/dime:latest .
#docker run --rm -it -v /var/run/docker.sock:/var/run/docker.sock wagoodman/dive:latest ladder99/dime:latest

cd ~
mkdir -p volumes/dime/configs
mkdir -p volumes/dime/lua
mkdir -p volumes/dime/logs
cp DIME/DIME/nlog.config volumes/dime/nlog.config
cp DIME/DIME/Configs/Examples/Basic/* volumes/dime/configs
cp DIME/DIME/Lua/* volumes/dime/lua

docker run \
   -p 7878:7878 \
   -p 8080:8080 \
   -p 8081:8081 \
   -p 8082:8082 \
   -p 9998:9998 \
   -p 9999:9999 \
   -v ~/volumes/dime/nlog.config:/app/nlog.config \
   -v ~/volumes/dime/configs:/app/Configs \
   -v ~/volumes/dime/lua:/app/Lua \
   -v ~/volumes/dime/logs:/app/Logs \
   ladder99/dime:latest
   
docker run \
   -v ~/volumes/dime/nlog.config:/app/nlog.config \
   ladder99/dime:latest
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
   b. `Sink.cs` - inherits from `IDS.Connectors.SinkConnector<IDS.Transporter.Configuration.{new_connector}.ConnectorConfiguration, IDS.Transporter.Configuration.{new_connector}.ConnectorItem>`.

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