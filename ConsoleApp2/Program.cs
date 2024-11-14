using ConsoleApp2;
using ConsoleApp2.Connectors.Source;

Console.WriteLine("Hello, World!");

var eipConfiguration = new PropertyBag();
eipConfiguration.SetProperty("type", 5);
eipConfiguration.SetProperty("address", "192.168.111.20");
eipConfiguration.SetProperty("path", "1,0");
eipConfiguration.SetProperty("log", 1);

var eipReadItems = new List<PropertyBag>();
var eipBool1 = new PropertyBag();
eipBool1.SetProperty("type", "bool");
eipBool1.SetProperty("address", "B3:0/2");
eipReadItems.Add(eipBool1);
var eipBool2 = new PropertyBag();
eipBool2.SetProperty("type", "bool");
eipBool2.SetProperty("address", "B3:0/3");
eipReadItems.Add(eipBool2);

var eipInstance = new EthernetIP();
eipInstance.Initialize(eipConfiguration, eipReadItems);
eipInstance.Create();
eipInstance.Connect();

var mqttConfiguration = new PropertyBag();
mqttConfiguration.SetProperty("address", "www.sharc.tech");
mqttConfiguration.SetProperty("port", 1883);

var mqttReadItems = new List<PropertyBag>();
var mqttTopic1 = new PropertyBag();
mqttTopic1.SetProperty("address", "sharc/+/evt/#");
mqttReadItems.Add(mqttTopic1);

var mqttInstance = new MQTT();
mqttInstance.Initialize(mqttConfiguration, mqttReadItems);
mqttInstance.Create();
mqttInstance.Connect();

while (true)
{
    var eipRead = eipInstance.Read();
    var mqttRead = mqttInstance.Read();
    
    Thread.Sleep(1000);
}


eipInstance.Disconnect();
mqttInstance.Disconnect();