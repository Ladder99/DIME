using ConsoleApp2.Configuration;

var eipConfig = new EthernetIpConnectorConfiguration();
eipConfig.ConnectorType = "EthernetIp";
eipConfig.Direction = ConnectorDirection.Source;
eipConfig.Enabled = true;
eipConfig.ScanInterval = 1000;
eipConfig.Name = "eipPlc1";
eipConfig.PlcType = 5;
eipConfig.IpAddress = "192.168.111.20";
eipConfig.Path = "1,0";
eipConfig.Timeout = 1000;
eipConfig.Items = new List<EthernetIpConnectorItem>();
eipConfig.Items.Add(new EthernetIpConnectorItem()
{
    Name = "eipBool1",
    Enabled = true,
    Address = "B3:0/2",
    Type = "bool"
});

var eip = new ConsoleApp2.Connectors.EthernetIp.Source();
eip.Initialize(eipConfig);
eip.Create();
eip.Connect();

while (true)
{
    eip.Read(); 
}

eip.Disconnect();

var mqttConfig = new MqttConnectorConfiguration();
mqttConfig.ConnectorType = "Mqtt";
mqttConfig.Direction = ConnectorDirection.Source;
mqttConfig.Enabled = true;
mqttConfig.ScanInterval = 1000;
mqttConfig.Name = "mqtt1";
mqttConfig.IpAddress = "wss.sharc.tech";
mqttConfig.Port = 1883;
mqttConfig.CleanSession = true;
mqttConfig.Username = string.Empty;
mqttConfig.Password = string.Empty;
mqttConfig.Items = new List<MqttConnectorItem>();
mqttConfig.Items.Add(new MqttConnectorItem()
{
    Name = "sharcEvents",
    Enabled = true,
    Address = "sharc/+/evt/#"
});

var mqtt = new ConsoleApp2.Connectors.Mqtt.Source();
mqtt.Initialize(mqttConfig);
mqtt.Create();
mqtt.Connect();
while (true)
{
    mqtt.Read();
}

mqtt.Disconnect();



/*
Console.WriteLine("Hello, World!");

var sources = new List<ISource>();
var sinks = new List<ISink>();
var config = new Config();
var yaml = config.Read(new[] { "config.yaml" });
var sourceBags = config.CreateSourceBags(yaml);
var sinkBags = config.CreateSinkBags(yaml);

foreach (var bag in sourceBags)
{
    var type = config.GetType().Assembly
        .GetTypes()
        .FirstOrDefault(t => t.FullName.EndsWith($"Connectors.Source.{bag.Connector.GetProperty<string>("connector")}"));

    var sourceInstance = Activator.CreateInstance(type) as ISource;
    sourceInstance.Initialize(bag.Connector, bag.Items);
    sourceInstance.Create();
    sourceInstance.Connect();
    sources.Add(sourceInstance);
}

foreach (var bag in sinkBags)
{
    var type = config.GetType().Assembly
        .GetTypes()
        .FirstOrDefault(t => t.FullName.EndsWith($"Connectors.Sink.{bag.Connector.GetProperty<string>("connector")}"));

    var sinkInstance = Activator.CreateInstance(type) as ISink;
    sinkInstance.Initialize(bag.Connector);
    sinkInstance.Create();
    sinkInstance.Connect();
    sinks.Add(sinkInstance);
}

while (true)
{
    foreach (var source in sources)
    {
        var results = source.Read();

        foreach (var sink in sinks)
        {
            sink.Write(source.ConnectorConfiguration, source.ItemsConfiguration, results);
        }
    }
    
    Thread.Sleep(1000);
}

//TODO: disconnect all sources
*/