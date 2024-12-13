namespace DIME.Connectors.SplunkEhSdk;

using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;


/// <summary>
/// Represents the EdgeHub service contract for data operations.
/// </summary>
public interface IEdgeHubService
{
    /// <summary>
    /// Retrieves discovery information about available topics.
    /// </summary>
    GetDiscoveryResponse GetDiscovery();

    /// <summary>
    /// Retrieves a single reading for a specific topic.
    /// </summary>
    GetReadingResponse GetReading(GetReadingRequest request);

    /// <summary>
    /// Retrieves a stream of readings for a specific topic.
    /// </summary>
    IAsyncEnumerable<GetReadingResponse> GetReadingStream(GetReadingRequest request);

    /// <summary>
    /// Sends a single event data payload.
    /// </summary>
    SendEventDataResponse SendEventData(SendEventDataRequest request);

    /// <summary>
    /// Sends a stream of event data payloads.
    /// </summary>
    IAsyncEnumerable<SendEventDataResponse> SendEventDataStream(IAsyncEnumerable<SendEventDataRequest> requests);

    /// <summary>
    /// Sends a single metric data payload.
    /// </summary>
    SendMetricDataResponse SendMetricData(SendMetricDataRequest request);

    /// <summary>
    /// Sends a stream of metric data payloads.
    /// </summary>
    IAsyncEnumerable<SendMetricDataResponse> SendMetricDataStream(IAsyncEnumerable<SendMetricDataRequest> requests);
}

/// <summary>
/// Represents discovery information for a topic.
/// </summary>
public class DiscoveryInfo
{
    /// <summary>
    /// The name of the topic.
    /// </summary>
    public string TopicName { get; set; }

    /// <summary>
    /// The type of the topic.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Additional information about the topic.
    /// </summary>
    public Struct AdditionalInformation { get; set; }
}

/// <summary>
/// Request for retrieving a reading from a specific topic.
/// </summary>
public class GetReadingRequest
{
    /// <summary>
    /// The name of the topic to retrieve reading from.
    /// </summary>
    public string TopicName { get; set; }
}

/// <summary>
/// Response containing a reading from a topic.
/// </summary>
public class GetReadingResponse
{
    /// <summary>
    /// The name of the topic the reading is from.
    /// </summary>
    public string TopicName { get; set; }

    /// <summary>
    /// The timestamp of the reading.
    /// </summary>
    public Timestamp Timestamp { get; set; }

    /// <summary>
    /// Fields associated with the reading.
    /// </summary>
    public Struct Fields { get; set; }
}

/// <summary>
/// Request for sending event data.
/// </summary>
public class SendEventDataRequest
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public Timestamp CreateTime { get; set; }

    /// <summary>
    /// Additional fields for the event.
    /// </summary>
    public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Response from sending event data.
/// </summary>
public class SendEventDataResponse
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Optional error information.
    /// </summary>
    public Error Error { get; set; }
}

/// <summary>
/// Request for sending metric data.
/// </summary>
public class SendMetricDataRequest
{
    /// <summary>
    /// Unique identifier for the metric data.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Timestamp when the metric data was created.
    /// </summary>
    public Timestamp CreateTime { get; set; }

    /// <summary>
    /// Dimensions associated with the metric data.
    /// </summary>
    public Dictionary<string, string> Dimensions { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// List of metrics.
    /// </summary>
    public List<Metric> Metrics { get; set; } = new List<Metric>();

    /// <summary>
    /// Optional additional details.
    /// </summary>
    public byte[] AdditionalDetails { get; set; }
}

/// <summary>
/// Response from sending metric data.
/// </summary>
public class SendMetricDataResponse
{
    /// <summary>
    /// Unique identifier for the metric data.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Optional error information.
    /// </summary>
    public Error Error { get; set; }
}

/// <summary>
/// Represents a single metric with its details.
/// </summary>
public class Metric
{
    /// <summary>
    /// Name of the metric.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Value of the metric.
    /// </summary>
    public float Value { get; set; }

    /// <summary>
    /// Unit of the metric.
    /// </summary>
    public string Unit { get; set; }
}

/// <summary>
/// Represents an error with an error message.
/// </summary>
public class Error
{
    /// <summary>
    /// Error message describing the issue.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// Response for discovery information.
/// </summary>
public class GetDiscoveryResponse
{
    /// <summary>
    /// List of discovered topics and their information.
    /// </summary>
    public List<DiscoveryInfo> DiscoveryInfo { get; set; } = new List<DiscoveryInfo>();
}