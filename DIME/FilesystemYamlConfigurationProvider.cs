using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DIME;

public class FilesystemYamlConfigurationProvider: IConfigurationProvider
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
    public (string, Dictionary<object, object>) ReadConfiguration()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        
        var configFiles = Directory.GetFiles("./Configs", "*.yaml");
        configFiles = configFiles.Where(x => !x.EndsWith("main.yaml"))
            .Concat(configFiles.Where(x => x.EndsWith("main.yaml")))
            .ToArray();
        
        return ReadFiles(configFiles);
    }
    
    public (bool, string, Dictionary<object, object>) WriteConfiguration(string yamlConfiguration)
    {
        if (!IsValid(yamlConfiguration))
        {
            return (false, string.Empty, new Dictionary<object, object>());
        }
        
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        // backup current configuration
        var (stringConfiguration, dictionaryConfiguration) = ReadConfiguration();
        File.WriteAllText($"./Configs/{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.bak", yamlConfiguration);
        // remove all yaml files
        Directory.GetFiles("./Configs", "*.yaml").ToList().ForEach(File.Delete);
        // write new configuration
        File.WriteAllText("./Configs/main.yaml", yamlConfiguration);
        // return new configuration
        (stringConfiguration, dictionaryConfiguration) = ReadConfiguration();
        return (true, stringConfiguration, dictionaryConfiguration);
    }
    
    private bool IsValid(string yamlString)
    {
        try
        {
            var deserializer = new Deserializer();
            deserializer.Deserialize(new StringReader(yamlString));
            return true;
        }
        catch (YamlException)
        {
            return false;
        }
    }
    
    private (string, Dictionary<object,object>) ReadFiles(string[] configurationFilenames)
    {
        Logger.Info("[ConfigProvider.Read] Reading files {0}", JsonConvert.SerializeObject(configurationFilenames));
        var yaml = "";
        foreach (var configFile in configurationFilenames)
        {
            try
            {
                yaml += File.ReadAllText(configFile) + "\r\n";
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
        return (yaml, dictionary);
    }
}