using ConsoleApp2;
using ConsoleApp2.Connectors;

Console.WriteLine("Hello, World!");

var sources = new List<ISource>();
var config = new Config();
var yaml = config.Read(new[] { "config.yaml" });
var bags = config.CreateBags(yaml);

foreach (var bag in bags)
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

while (true)
{
    foreach (var source in sources)
    {
        var results = source.Read();
    }
    
    Thread.Sleep(1000);
}

//TODO: disconnect all sources
