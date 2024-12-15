
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
                { "http_server_uri", "http://127.0.0.1:9999/" },
                { "ws_server_uri", "ws://127.0.0.1:9998/" }
            };
        }
        
        var section = configuration["app"] as Dictionary<object, object>;
        
        Configuration.AppConfig config = new();
        config.RingBufferSize = Convert.ToInt32(section["ring_buffer"]);
        config.HttpServerUri = Convert.ToString(section["http_server_uri"]);
        config.WsServerUri = Convert.ToString(section["ws_server_uri"]);

        return config;
    }
}