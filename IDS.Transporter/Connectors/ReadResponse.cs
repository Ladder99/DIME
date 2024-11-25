namespace IDS.Transporter.Connectors;

public class ReadResponse
{
    public string Path { get; set; }
    public object Data { get; set; }
    public long Timestamp { get; set; }
}