namespace DIME.Configuration;

public sealed class AppConfig
{
    public int RingBufferSize { get; set; }
    public string HttpServerUri { get; set; }
    public string WsServerUri { get; set; }
}