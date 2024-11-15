using ConsoleApp2;
using ConsoleApp2.Connectors;

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
