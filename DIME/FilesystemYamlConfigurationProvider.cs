using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DIME;

public class FilesystemYamlConfigurationProvider: IConfigurationProvider
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
    public Dictionary<object, object> GetConfiguration()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        
        var configFiles = Directory.GetFiles("./Configs", "*.yaml");
        configFiles = configFiles.Where(x => !x.EndsWith("main.yaml"))
            .Concat(configFiles.Where(x => x.EndsWith("main.yaml")))
            .ToArray();
        
        return ReadFiles(configFiles);
    }
    
    public Dictionary<object,object> ReadFiles(string[] configurationFilenames)
    {
        Logger.Info("[ConfigProvider.Read] Reading files {0}", JsonConvert.SerializeObject(configurationFilenames));
        var yaml = "";
        foreach (var configFile in configurationFilenames)
        {
            try
            {
                yaml += File.ReadAllText(configFile);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"[ConfigProvider.Read] Problem with {configFile}");
            }
            
        }
        Logger.Debug("[ConfigProvider.Read] YAML \r\n{0}", yaml);
        File.WriteAllText("./running_configuration.yaml", yaml);
        var stringReader = new StringReader(yaml);
        var parser = new Parser(stringReader);
        var mergingParser = new MergingParser(parser);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        Dictionary<object, object> dictionary = new();
        try
        {
            dictionary = deserializer.Deserialize<Dictionary<object, object>>(mergingParser);
        }
        catch (SemanticErrorException e)
        {
            Logger.Error(e, "[ConfigProvider.Read] Error while parsing yaml.");
        }
        
        Logger.Debug("[ConfigProvider.Read] Dictionary \r\n{0}", JsonConvert.SerializeObject(dictionary));
        return dictionary;
    }
}