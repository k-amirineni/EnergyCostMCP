using McpEnergyCalculator.Models;
using McpEnergyCalculator.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace McpEnergyCalculator.Services;

public interface IMcpServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class McpServer : IMcpServer
{
    private readonly IEnergyCalculationService _energyService;
    private readonly ILogger<McpServer> _logger;
    private readonly JsonSerializerSettings _jsonSettings;
    private volatile bool _isRunning;

    public McpServer(IEnergyCalculationService energyService, ILogger<McpServer> logger)
    {
        _energyService = energyService;
        _logger = logger;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MCP Energy Calculator Server...");
        _isRunning = true;

        try
        {
            await ProcessStdinStdoutAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("MCP Server shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MCP Server");
            throw;
        }
        finally
        {
            _isRunning = false;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping MCP Energy Calculator Server...");
        _isRunning = false;
        return Task.CompletedTask;
    }

    private async Task ProcessStdinStdoutAsync(CancellationToken cancellationToken)
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin, Encoding.UTF8);
        using var writer = new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true };

        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    _logger.LogInformation("Stdin closed, shutting down");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var response = await ProcessMessageAsync(line);
                if (response != null)
                {
                    var responseJson = JsonConvert.SerializeObject(response, _jsonSettings);
                    await writer.WriteLineAsync(responseJson);
                    _logger.LogDebug("Sent response: {Response}", responseJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                
                var errorResponse = new McpMessage
                {
                    Error = new McpError
                    {
                        Code = -32603,
                        Message = "Internal error",
                        Data = ex.Message
                    }
                };
                
                var errorJson = JsonConvert.SerializeObject(errorResponse, _jsonSettings);
                await writer.WriteLineAsync(errorJson);
            }
        }
    }

    private async Task<McpMessage?> ProcessMessageAsync(string messageJson)
    {
        try
        {
            _logger.LogDebug("Received message: {Message}", messageJson);
            
            var message = JsonConvert.DeserializeObject<McpMessage>(messageJson);
            if (message == null)
            {
                return CreateErrorResponse(null, -32700, "Parse error");
            }

            return message.Method switch
            {
                "initialize" => await HandleInitializeAsync(message),
                "tools/list" => await HandleToolsListAsync(message),
                "tools/call" => await HandleToolCallAsync(message),
                "ping" => HandlePing(message),
                _ => CreateErrorResponse(message.Id, -32601, $"Method not found: {message.Method}")
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error");
            return CreateErrorResponse(null, -32700, "Parse error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return CreateErrorResponse(null, -32603, "Internal error");
        }
    }

    private async Task<McpMessage> HandleInitializeAsync(McpMessage message)
    {
        _logger.LogInformation("Handling initialize request");
        
        var result = new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            ServerInfo = new McpServerInfo
            {
                Name = "MCP Energy Calculator",
                Version = "1.0.0"
            },
            Capabilities = new McpServerCapabilities
            {
                Tools = new McpToolsCapability()
            }
        };

        return new McpMessage
        {
            Id = message.Id,
            Result = result
        };
    }

    private async Task<McpMessage> HandleToolsListAsync(McpMessage message)
    {
        _logger.LogInformation("Handling tools/list request");
        
        var tools = McpToolDefinitions.GetAllTools();
        var result = new McpToolsListResult
        {
            Tools = tools
        };

        return new McpMessage
        {
            Id = message.Id,
            Result = result
        };
    }

    private async Task<McpMessage> HandleToolCallAsync(McpMessage message)
    {
        try
        {
            _logger.LogInformation("Handling tools/call request");
            
            var requestData = message.Params as JObject;
            if (requestData == null)
            {
                return CreateErrorResponse(message.Id, -32602, "Invalid params");
            }

            var toolRequest = requestData.ToObject<McpToolCallRequest>();
            if (toolRequest == null || string.IsNullOrEmpty(toolRequest.Name))
            {
                return CreateErrorResponse(message.Id, -32602, "Invalid tool call request");
            }

            var result = await ExecuteToolAsync(toolRequest);
            
            return new McpMessage
            {
                Id = message.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool");
            return CreateErrorResponse(message.Id, -32603, $"Tool execution error: {ex.Message}");
        }
    }

    private McpMessage HandlePing(McpMessage message)
    {
        return new McpMessage
        {
            Id = message.Id,
            Result = new { status = "pong" }
        };
    }

    private async Task<McpToolCallResult> ExecuteToolAsync(McpToolCallRequest request)
    {
        var content = new List<McpContent>();
        
        try
        {
            switch (request.Name)
            {
                case "calculate-energy-cost":
                    var energyRequest = ParseToolArguments<EnergyCalculationRequest>(request.Arguments);
                    var energyResult = await _energyService.CalculateEnergyAsync(energyRequest);
                    content.Add(new McpContent
                    {
                        Type = "text",
                        Text = FormatEnergyResult(energyResult),
                        Data = energyResult
                    });
                    break;

                case "calculate-carbon-emissions":
                    var carbonRequest = ParseToolArguments<EnergyCalculationRequest>(request.Arguments);
                    var carbonResult = await _energyService.CalculateEnergyAsync(carbonRequest);
                    content.Add(new McpContent
                    {
                        Type = "text",
                        Text = FormatCarbonResult(carbonResult),
                        Data = carbonResult
                    });
                    break;

                case "get-model-info":
                    var modelRequest = ParseToolArguments<ModelInfoRequest>(request.Arguments);
                    var modelResult = await _energyService.GetModelInfoAsync(modelRequest);
                    content.Add(new McpContent
                    {
                        Type = "text",
                        Text = FormatModelInfoResult(modelResult),
                        Data = modelResult
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Unknown tool: {request.Name}");
            }
        }
        catch (Exception ex)
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"Error: {ex.Message}"
            });
            return new McpToolCallResult
            {
                Content = content,
                IsError = true
            };
        }

        return new McpToolCallResult
        {
            Content = content,
            IsError = false
        };
    }

    private T ParseToolArguments<T>(Dictionary<string, object>? arguments) where T : new()
    {
        if (arguments == null)
            return new T();

        var json = JsonConvert.SerializeObject(arguments, _jsonSettings);
        return JsonConvert.DeserializeObject<T>(json, _jsonSettings) ?? new T();
    }

    private string FormatEnergyResult(EnergyCalculationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"⚡ Energy Consumption Analysis for {result.Model} ({result.Provider})");
        sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"📊 Token Usage:");
        sb.AppendLine($"   • Input tokens: {result.InputTokens:N0}");
        sb.AppendLine($"   • Output tokens: {result.OutputTokens:N0}");
        sb.AppendLine($"   • Total tokens: {result.TotalTokens:N0}");
        sb.AppendLine();
        sb.AppendLine($"⚡ Energy Consumption:");
        sb.AppendLine($"   • Input processing: {result.Energy.InputTokensWh:F6} Wh");
        sb.AppendLine($"   • Output generation: {result.Energy.OutputTokensWh:F6} Wh");
        sb.AppendLine($"   • Total energy: {result.Energy.TotalWh:F6} Wh ({result.Energy.TotalKwh:F9} kWh)");
        sb.AppendLine();
        sb.AppendLine($"💰 Energy Cost (Region: {result.Region}):");
        sb.AppendLine($"   • Input cost: ${result.Costs.InputTokensCost:F6}");
        sb.AppendLine($"   • Output cost: ${result.Costs.OutputTokensCost:F6}");
        sb.AppendLine($"   • Total cost: ${result.Costs.TotalCost:F6}");
        sb.AppendLine($"   • Rate: ${result.Costs.CostPerKwh:F3}/kWh");
        sb.AppendLine();
        sb.AppendLine($"🌱 Carbon Footprint:");
        sb.AppendLine($"   • Input emissions: {result.Carbon.InputTokensGrams:F3} g CO₂e");
        sb.AppendLine($"   • Output emissions: {result.Carbon.OutputTokensGrams:F3} g CO₂e");
        sb.AppendLine($"   • Total emissions: {result.Carbon.TotalGrams:F3} g CO₂e ({result.Carbon.TotalKg:F6} kg CO₂e)");
        sb.AppendLine($"   • Grid intensity: {result.Carbon.CarbonIntensity:F1} g CO₂e/kWh");

        return sb.ToString();
    }

    private string FormatCarbonResult(EnergyCalculationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🌱 Carbon Emissions Analysis for {result.Model} ({result.Provider})");
        sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"📊 Token Usage: {result.TotalTokens:N0} tokens ({result.InputTokens:N0} input + {result.OutputTokens:N0} output)");
        sb.AppendLine($"⚡ Energy Consumption: {result.Energy.TotalWh:F6} Wh ({result.Energy.TotalKwh:F9} kWh)");
        sb.AppendLine();
        sb.AppendLine($"🌍 Carbon Footprint (Region: {result.Region}):");
        sb.AppendLine($"   • Input processing: {result.Carbon.InputTokensGrams:F3} g CO₂e");
        sb.AppendLine($"   • Output generation: {result.Carbon.OutputTokensGrams:F3} g CO₂e");
        sb.AppendLine($"   • Total emissions: {result.Carbon.TotalGrams:F3} g CO₂e");
        sb.AppendLine($"   • Equivalent kg CO₂e: {result.Carbon.TotalKg:F6} kg");
        sb.AppendLine();
        sb.AppendLine($"🏭 Grid Information:");
        sb.AppendLine($"   • Carbon intensity: {result.Carbon.CarbonIntensity:F1} g CO₂e/kWh");
        sb.AppendLine($"   • Region: {result.Region}");
        sb.AppendLine();
        sb.AppendLine($"📈 Environmental Context:");
        if (result.Carbon.TotalGrams < 1.0)
            sb.AppendLine($"   • Very low emissions - equivalent to a few seconds of a standard light bulb");
        else if (result.Carbon.TotalGrams < 10.0)
            sb.AppendLine($"   • Low emissions - equivalent to a few minutes of a standard light bulb");
        else if (result.Carbon.TotalGrams < 100.0)
            sb.AppendLine($"   • Moderate emissions - equivalent to charging a smartphone");
        else
            sb.AppendLine($"   • Higher emissions - consider using smaller models when possible");

        return sb.ToString();
    }

    private string FormatModelInfoResult(ModelInfoResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🤖 LLM Model Information Database");
        sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"📊 Summary: {result.TotalModels} models from {result.Providers.Count} providers");
        sb.AppendLine();
        
        if (result.Models.Any())
        {
            var groupedByProvider = result.Models.GroupBy(m => m.Provider).OrderBy(g => g.Key);
            
            foreach (var providerGroup in groupedByProvider)
            {
                sb.AppendLine($"🏢 {providerGroup.Key.ToUpper()} ({providerGroup.Count()} models)");
                
                foreach (var model in providerGroup.OrderBy(m => m.Category).ThenBy(m => m.ModelName))
                {
                    sb.AppendLine($"   • {model.ModelName} ({model.Category})");
                    sb.AppendLine($"     ├─ Parameters: {model.ParameterCount / 1_000_000_000:F1}B");
                    sb.AppendLine($"     ├─ Energy/token: {model.EnergyPerInputToken * 1_000_000:F2}μWh in, {model.EnergyPerOutputToken * 1_000_000:F2}μWh out");
                    sb.AppendLine($"     └─ Model ID: {model.ModelId}");
                }
                sb.AppendLine();
            }
        }
        
        sb.AppendLine($"🏷️  Available Categories: {string.Join(", ", result.Categories)}");
        sb.AppendLine($"🏢 Available Providers: {string.Join(", ", result.Providers)}");

        return sb.ToString();
    }

    private McpMessage CreateErrorResponse(object? id, int code, string message)
    {
        return new McpMessage
        {
            Id = id,
            Error = new McpError
            {
                Code = code,
                Message = message
            }
        };
    }
}
