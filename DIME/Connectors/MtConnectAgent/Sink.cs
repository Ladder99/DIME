using DIME.Configuration.MtConnectAgent;
using MTConnect.Agents;
using MTConnect.Applications;
using MTConnect.Configurations;
using MTConnect.Devices;
using MTConnect.Devices.Components;
using MTConnect.Devices.DataItems;
using MTConnect.Observations.Events;

namespace DIME.Connectors.MtConnectAgent;

public class Sink: SinkConnector<ConnectorConfiguration, Configuration.ConnectorItem>
{
    private MTConnectAgentApplication _client = null;
    private Module _module = null;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        _client = new MTConnectAgentApplication();
        return true;
    }

    protected override bool ConnectImplementation()
    {
        //_client.StartAgent(new AgentApplicationConfiguration());
        _client.Run(["run", "agent.config.yaml"]);
        _module = new Module(_client.Agent, this);
        _module.Start();
        IsConnected = true;
        return true;
    }

    protected override bool WriteImplementation()
    {
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _module.Stop();
        _client.StopAgent();
        IsConnected = false;
        return true;
    }

    public override bool AfterUpdate()
    {
        // do not allow base to clear outbox
        return true;
    }
}

public class Module : MTConnectInputAgentModule
{
    Sink _connector = null;
    private Device _device = null;
    
    public Module(IMTConnectAgentBroker agent, Sink connector) : base(agent)
    {
        _connector = connector;
    }
    
    protected override IDevice OnAddDevice()
    {
        var device = new Device();
        device.Uuid = _connector.Configuration.DeviceUuid;
        device.Id = _connector.Configuration.DeviceId;
        device.Name = _connector.Configuration.DeviceName;
        device.Description = new Description()
        {
            //Manufacturer = _connector.Configuration.DeviceManufacturer,
            //Model = _connector.Configuration.DeviceModel,
            //SerialNumber = _connector.Configuration.DeviceSerialNumber
        };

        _device = device;
        return device;
    }
    
    protected override void OnRead()
    {
        foreach (var message in _connector.Outbox)
        {
            //_device.AddComponent();
            //AddValueObservation();
        }
        
        _connector.Outbox.Clear();
        
        /*
        Log(MTConnect.Logging.MTConnectLogLevel.Information, "Read PLC Data");

        AddValueObservation<AvailabilityDataItem>(Availability.AVAILABLE);
        AddValueObservation<ControllerComponent, EmergencyStopDataItem>(EmergencyStop.ARMED);
        AddValueObservation<PathComponent, ProgramDataItem>("BRACKET.NC");
        AddValueObservation<PathComponent, DateCodeDataItem>(DateTime.Now.ToString("o"));
        AddConditionObservation<PathComponent, SystemDataItem>(MTConnect.Observations.ConditionLevel.WARNING, "404", "This is an Alarm");


        AddValueObservation<LinearComponent, PositionDataItem>(0.0000, "X", PositionDataItem.SubTypes.PROGRAMMED);
        AddValueObservation<LinearComponent, PositionDataItem>(0.0002, "X", PositionDataItem.SubTypes.ACTUAL);
        AddValueObservation<LinearComponent, LoadDataItem>(2, "X");

        AddValueObservation<LinearComponent, PositionDataItem>(150.0000, "Y", PositionDataItem.SubTypes.PROGRAMMED);
        AddValueObservation<LinearComponent, PositionDataItem>(150.0001, "Y", PositionDataItem.SubTypes.ACTUAL);
        AddValueObservation<LinearComponent, LoadDataItem>(1.5, "Y");

        AddValueObservation<LinearComponent, PositionDataItem>(200.0000, "Z", PositionDataItem.SubTypes.PROGRAMMED);
        AddValueObservation<LinearComponent, PositionDataItem>(200.0003, "Z", PositionDataItem.SubTypes.ACTUAL);
        AddValueObservation<LinearComponent, LoadDataItem>(6.3, "Z");
        */
    }


    private void AddController(Device device)
    {
        // Create a Controller Component
        var controller = new ControllerComponent();

        // Add an EmergencyStop DataItem to the controller component
        controller.AddDataItem<EmergencyStopDataItem>();

        // Create a Path Component
        var path = new PathComponent();

        // Add Path DataItems
        path.AddDataItem<ControllerModeDataItem>();
        path.AddDataItem<ExecutionDataItem>();
        path.AddDataItem<ProgramDataItem>();
        path.AddDataItem<DateCodeDataItem>();
        path.AddDataItem<SystemDataItem>();

        // Add the Path Component as a child of the Controller Component
        controller.AddComponent(path);

        // Add the Controller Component to the Device
        device.AddComponent(controller);
    }

    private void AddAxes(Device device)
    {
        // Create a Axes Component
        var axes = new AxesComponent();

        AddLinearAxis(axes, "X");
        AddLinearAxis(axes, "Y");
        AddLinearAxis(axes, "Z");

        // Add the Component to the Device
        device.AddComponent(axes);
    }

    private void AddLinearAxis(AxesComponent axesComponent, string name)
    {
        // Create a Linear Component
        var axis = new LinearComponent();
        axis.Name = name;

        axis.AddDataItem<PositionDataItem>(PositionDataItem.SubTypes.PROGRAMMED);
        axis.AddDataItem<PositionDataItem>(PositionDataItem.SubTypes.ACTUAL);
        axis.AddDataItem<LoadDataItem>();

        // Add the Component to the AxesComponent
        axesComponent.AddComponent(axis);
    }
}