namespace IDS.Transporter.Connectors;

public class BoxMessage
{
    public string Path { get; set; }
    public object Data { get; set; }
    public long Timestamp { get; set; }
}