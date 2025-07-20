using Newtonsoft.Json;

namespace McpEnergyCalculator.Models;

public class McpMessage
{
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonProperty("method")]
    public string? Method { get; set; }

    [JsonProperty("params")]
    public object? Params { get; set; }

    [JsonProperty("id")]
    public object? Id { get; set; }

    [JsonProperty("result")]
    public object? Result { get; set; }

    [JsonProperty("error")]
    public McpError? Error { get; set; }
}

public class McpError
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("data")]
    public object? Data { get; set; }
}

public class McpInitializeParams
{
    [JsonProperty("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonProperty("capabilities")]
    public McpClientCapabilities Capabilities { get; set; } = new();

    [JsonProperty("clientInfo")]
    public McpClientInfo ClientInfo { get; set; } = new();
}

public class McpClientCapabilities
{
    [JsonProperty("experimental")]
    public Dictionary<string, object>? Experimental { get; set; }

    [JsonProperty("sampling")]
    public object? Sampling { get; set; }
}

public class McpClientInfo
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;
}

public class McpInitializeResult
{
    [JsonProperty("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonProperty("capabilities")]
    public McpServerCapabilities Capabilities { get; set; } = new();

    [JsonProperty("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

public class McpServerCapabilities
{
    [JsonProperty("tools")]
    public McpToolsCapability? Tools { get; set; }

    [JsonProperty("resources")]
    public object? Resources { get; set; }

    [JsonProperty("prompts")]
    public object? Prompts { get; set; }

    [JsonProperty("experimental")]
    public Dictionary<string, object>? Experimental { get; set; }
}

public class McpToolsCapability
{
    [JsonProperty("listChanged")]
    public bool? ListChanged { get; set; }
}

public class McpServerInfo
{
    [JsonProperty("name")]
    public string Name { get; set; } = "MCP Energy Calculator";

    [JsonProperty("version")]
    public string Version { get; set; } = "1.0.0";
}
