using MTConnect.Devices;

namespace DIME.ConnectorSupport.MtConnect.DeviceBuilder;

static class Builder
{
    static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
    public static (bool, Device, IDataItem) Build(Dictionary<string, Device> devices, string mtConnectPath, string mtConnectSource)
    {
        Logger.Debug($"Processing Path: {mtConnectPath}");
    
        var parts = new PathParts(mtConnectPath);
        bool wasModified = false;
        IDataItem dataItem = null;

        Device device = null;
        IComponent nextComponent = null;
        
        for(int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            
            Logger.Debug($"\tProcessing Part: {part.Name}");
            
            if(i == 0) // Device expected
            {
                if (part.Name != "Device") Logger.Warn($"\tExpecting 'Device' but found '{part.Name}'");
                part.Attributes.TryGetValue("Name", out var deviceName);
                if (deviceName is null) deviceName = "undefined";
                var deviceExists = devices.TryGetValue(deviceName, out device);
                if (!deviceExists)
                {
                    device = new Device();
                    devices[deviceName] = device;
                    
                    part.Attributes.TryAdd("Id", Guid.NewGuid().ToString());
                    part.Attributes.TryAdd("Name", string.Empty);
                    part.Attributes.TryAdd("Type", part.Name);
                    
                    foreach (var attribute in part.Attributes)
                    {
                        Logger.Debug($"\t\tProcessing Attribute: {attribute.Key}={attribute.Value}");
                        if (!SetPropertyFromAttribute(device, attribute.Key, attribute.Value))
                        {
                            Logger.Warn($"\t\tFailed to set property '{attribute.Key}'={attribute.Value}");
                        }
                    }
                    
                    wasModified = true;
                }
                nextComponent = device;
            }
            else if (i == parts.Count - 1) // DataItem expected
            {
                dataItem = nextComponent.GetDataItemByType($"{part.Name}", SearchType.Child);
                if (dataItem is null)
                {
                    dataItem = DataItem.Create($"{part.Name}");
                    ((DataItem)dataItem).Source = new Source()
                    {
                        Value = mtConnectSource
                    };
                    
                    part.Attributes.TryAdd("Id", Guid.NewGuid().ToString());
                    part.Attributes.TryAdd("Name", string.Empty);
                    part.Attributes.TryAdd("Type", part.Name);
                    
                    foreach (var attribute in part.Attributes)
                    {
                        Logger.Debug($"\t\tProcessing Attribute: {attribute.Key}={attribute.Value}");
                        if(!SetPropertyFromAttribute(dataItem, attribute.Key, attribute.Value))
                        {
                            Logger.Warn($"\t\tFailed to set property '{attribute.Key}'={attribute.Value}");
                        }
                    }
                    
                    try
                    {
                        ((Component)nextComponent).AddDataItem(dataItem);
                    }
                    catch
                    {
                        ((Device)nextComponent).AddDataItem(dataItem);
                    }
                    
                    wasModified = true;
                }
            }
            else // Component expected
            {
                var childComponent = nextComponent.GetComponent($"{part.Name}", searchType: SearchType.Child);
                if (childComponent is null)
                {
                    childComponent = Component.Create($"{part.Name}");

                    part.Attributes.TryAdd("Id", Guid.NewGuid().ToString());
                    part.Attributes.TryAdd("Name", string.Empty);
                    part.Attributes.TryAdd("Type", part.Name);

                    foreach (var attribute in part.Attributes)
                    {
                        Logger.Debug($"\t\tProcessing Attribute: {attribute.Key}={attribute.Value}");
                        if(!SetPropertyFromAttribute(childComponent, attribute.Key, attribute.Value))
                        {
                            Logger.Warn($"\t\tFailed to set property '{attribute.Key}'={attribute.Value}");
                        }
                    }

                    try
                    {
                        ((Component)nextComponent).AddComponent(childComponent);
                    }
                    catch
                    {
                        ((Device)nextComponent).AddComponent(childComponent);
                    }
                    
                    wasModified = true;
                }
                nextComponent = childComponent;
            }
        }

        return (wasModified, device, dataItem);
    }
    
    private static bool SetPropertyFromAttribute(object obj, string propertyName, object attributeValue)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property is null) return false;
        if (property.PropertyType == typeof(DataItemCategory))
        {
            property.SetValue(obj, Enum.Parse(typeof(DataItemCategory), attributeValue.ToString().ToUpper()));
        }
        else
        {
            property.SetValue(obj, attributeValue);
        }

        return true;
    }
}