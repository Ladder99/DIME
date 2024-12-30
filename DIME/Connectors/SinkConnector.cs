using System.Collections.Concurrent;
using DIME.Configuration;
using Newtonsoft.Json;
using Scriban;
using Scriban.Runtime;

namespace DIME.Connectors;

public abstract class SinkConnector<TConfig, TItem> : Connector<TConfig, TItem>, ISinkConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public List<MessageBoxMessage> Outbox { get; private set; } = new List<MessageBoxMessage>();
    public event Action<List<MessageBoxMessage>> OnOutboxReady;
    public event Action<List<MessageBoxMessage>, bool> OnOutboxSent;
    
    public bool IsWriting { get; set; }
    
    protected SinkConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor): base(configuration, disruptor)
    {
        IsWriting = false;
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:.ctor");
    }
    
    public override bool BeforeUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] SinkConnector:BeforeUpdate::ENTER");
        
        IsWriting = true;
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:BeforeUpdate::EXIT");
        
        return true;
    }
    
    protected abstract bool WriteImplementation();
    
    public virtual bool Write()
    {
        Logger.Trace($"[{Configuration.Name}] SinkConnector:Write::ENTER");
        
        FaultContext = FaultContextEnum.Write;

        bool result = false;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            result = false;
        }
        else if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            result = false;
        }
        else if (!IsConnected)
        {
            MarkFaulted(new Exception("Device not connected."));
            result = false;
        }
        else
        {
            try
            {
                OnOutboxReady?.Invoke(Outbox);
                
                EntireReadLoopStopwatch.Start();

                if (Outbox.Count > 0)
                {
                    if (Configuration.IncludeFilter.Count > 0)
                    {
                        Outbox = Outbox.Where(message =>
                                Configuration.IncludeFilter.Any(prefix =>
                                    message.Path.StartsWith(prefix))).ToList();
                    }
                    else
                    {
                        Outbox = Outbox.Where(message =>
                                !Configuration.ExcludeFilter.Any(prefix =>
                                    message.Path.StartsWith(prefix))).ToList();
                    }
                }

                result = WriteImplementation();
                
                EntireReadLoopStopwatch.Stop();
                
                OnOutboxSent?.Invoke(Outbox, result);

                if (result)
                {
                    ClearFault();
                }
                else
                {
                    MarkFaulted(new Exception("Device implementation write failed."));
                }
            }
            catch (Exception e)
            {
                MarkFaulted(e);
                Disconnect();
                result = false;
            }
        }
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:Write::EXIT");
        
        Logger.Debug($"[{Configuration.Name}] Loop Perf. " +
                    $"DeviceRead: {ReadFromDeviceSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"ExecuteScript: {ExecuteScriptSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"EntireLoop: {EntireReadLoopStopwatch.ElapsedMilliseconds}ms");
        
        base.InvokeOnLoopPerf(
            ReadFromDeviceSumStopwatch.ElapsedMilliseconds,
            ExecuteScriptSumStopwatch.ElapsedMilliseconds,
            EntireReadLoopStopwatch.ElapsedMilliseconds);
        
        EntireReadLoopStopwatch.Reset();

        return result;
    }

    public override bool AfterUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] SinkConnector:AfterUpdate::ENTER");
        
        Outbox.Clear();

        IsWriting = false;
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:AfterUpdate::EXIT");
        
        return true;
    }

    protected bool IsSystemPath(string itemPath)
    {
        var pathParts = itemPath.Split('/');
        if (pathParts.Length < 2) return false;
        return pathParts[1] == "$SYSTEM";
    }
    
    public string TransformAndSerializeMessage(MessageBoxMessage message)
    {
        try
        {
            var outMessage = TransformMessage(message);
            return (outMessage.GetType() == typeof(string) ? outMessage : JsonConvert.SerializeObject(outMessage)).ToString();
        }
        catch (Exception e)
        {
            //System.Console.WriteLine(JsonConvert.SerializeObject(message));
            //if (message.Data is null) return null;
            //return message.Data.GetType() == typeof(string) ? message.Data.ToString() : JsonConvert.SerializeObject(message.Data);
            return JsonConvert.SerializeObject(message);
        }
    }
    
    public object TransformAndSerializeMessageToObject(MessageBoxMessage message)
    {
        try
        {
            var outMessage = TransformMessage(message);
            return outMessage;
        }
        catch (Exception e)
        {
            //System.Console.WriteLine(JsonConvert.SerializeObject(message));
            //if (message.Data is null) return null;
            //return message.Data.GetType() == typeof(string) ? message.Data.ToString() : JsonConvert.SerializeObject(message.Data);
            return JsonConvert.SerializeObject(message);
        }
    }
    
    protected object TransformMessage(MessageBoxMessage message)
    {
        var itemTransformExists = message.ConnectorItemRef is not null && 
                              message.ConnectorItemRef.SinkMeta is not null &&
                              message.ConnectorItemRef.SinkMeta.ContainsKey("transform");
        
        var connectorTransformExists = message.ConnectorItemRef is not null && 
                                  message.ConnectorItemRef.Configuration is not null &&
                                  message.ConnectorItemRef.Configuration.SinkMeta is not null &&
                                  message.ConnectorItemRef.Configuration.SinkMeta.ContainsKey("transform");

        if (!itemTransformExists && !connectorTransformExists || IsSystemPath(message.Path))
        {
            return message;
        }
        else
        {
            var transformDict = itemTransformExists 
                ? message.ConnectorItemRef.SinkMeta["transform"] as Dictionary<object,object>
                : message.ConnectorItemRef.Configuration.SinkMeta["transform"] as Dictionary<object,object>;
            var transformType = transformDict?["type"] as string;
            var transformTemplate = transformDict?["template"] as string;

            switch (transformType.ToLower())
            {
                case "liquid":
                    var template1 = Template.ParseLiquid(transformTemplate);
                    return template1.Render(
                        new { Connector = this, Configuration = Configuration, Message = message }, 
                        member => member.Name);
                case "scriban":
                    var template2 = Template.Parse(transformTemplate);
                    return template2.Render(
                        new { Connector = this, Configuration = Configuration, Message = message }, 
                        member => member.Name);
                case "script":
                    var so = new ScriptObject();
                    so.SetValue("Connector", this, true);
                    so.SetValue("Configuration", Configuration, true);
                    so.SetValue("Message", message, true);
                    so.Import("print", new Action<object>(o => System.Console.WriteLine(o)));
                    so.Import("type", new Func<object, string>(o => o.GetType().ToString()));
                    var tc = new TemplateContext(so) { MemberRenamer = member => member.Name };
                    return Template.Evaluate(transformTemplate, tc);
                default:
                    return message;
            }
        }
    }
}