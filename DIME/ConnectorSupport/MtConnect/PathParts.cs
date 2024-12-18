namespace DIME.ConnectorSupport.MtConnect.DeviceBuilder;

public class PathParts : List<PathPart>
{
    public PathParts() : base() { }

    public PathParts(IEnumerable<PathPart> collection) : base(collection) { }

    // Find first part by name
    public PathPart FindByName(string name)
    {
        return this.FirstOrDefault(p => p.Name == name);
    }

    // Find all parts by name
    public PathParts FindAllByName(string name)
    {
        return new PathParts(this.Where(p => p.Name == name));
    }

    // Find parts with specific attribute
    public PathParts FindByAttribute(string key, string value)
    {
        return new PathParts(this.Where(p => 
            p.Attributes != null && 
            p.Attributes.TryGetValue(key, out var attributeValue) && 
            attributeValue == value));
    }

    // Parse device path directly in constructor
    public PathParts(string devicePath) : base()
    {
        // Split the path into segments
        var segments = devicePath.Split('/');
        
        // Parse each segment into a Part
        foreach (var segment in segments)
        {
            Add(ParsePartSegment(segment));
        }
    }

    // Keep the parsing method from previous example
    private PathPart ParsePartSegment(string segment)
    {
        int bracketStart = segment.IndexOf('[');
        
        PathPart part = new PathPart();
        
        if (bracketStart == -1)
        {
            part.Name = segment;
        }
        else
        {
            part.Name = segment.Substring(0, bracketStart);
            
            string attributeString = segment.Substring(bracketStart + 1, 
                segment.Length - bracketStart - 2);
            
            part.Attributes = attributeString.Split(',')
                .Select(attr => attr.Split('='))
                .ToDictionary(
                    attr => attr[0].Trim(), 
                    attr => attr[1].Trim()
                );
        }
        
        return part;
    }

    // Convenience method to get parts at a specific index with null check
    public PathPart GetPartOrDefault(int index)
    {
        return index >= 0 && index < Count ? this[index] : null;
    }

    // Pretty print all parts
    public void PrintParts()
    {
        foreach (var part in this)
        {
            System.Console.WriteLine($"Name: {part.Name}");
            if (part.Attributes != null)
            {
                foreach (var attr in part.Attributes)
                {
                    System.Console.WriteLine($"  {attr.Key}: {attr.Value}");
                }
            }
        }
    }
}