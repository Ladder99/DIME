
namespace DIME.Configurator;

public static class AppConfig
{
    public static Configuration.AppConfig Create(Dictionary<object, object> configuration)
    {
        if (!configuration.ContainsKey("app"))
        {
            configuration["app"] = new Dictionary<object, object>()
            {
                { "ring_buffer", 4096 },
                { "server_uri", "http://localhost:9999" }
            };
        }
        
        var section = configuration["app"] as Dictionary<object, object>;
        
        Configuration.AppConfig config = new();
        config.RingBufferSize = Convert.ToInt32(section["ring_buffer"]);
        config.ServerUri = Convert.ToString(section["server_uri"]);

        return config;
    }
}