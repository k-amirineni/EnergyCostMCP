using McpEnergyCalculator.Data;
using McpEnergyCalculator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpEnergyCalculator;

class Program
{
    static async Task Main(string[] args)
    {
        // Create host builder
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Register services
        builder.Services.AddSingleton<IModelDataRepository, ModelDataRepository>();
        builder.Services.AddSingleton<ICarbonIntensityRepository, CarbonIntensityRepository>();
        builder.Services.AddScoped<IEnergyCalculationService, EnergyCalculationService>();
        builder.Services.AddScoped<IMcpServer, McpServer>();
        builder.Services.AddHostedService<McpServerHostedService>();

        // Build and run the host
        var host = builder.Build();
        
        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetService<ILogger<Program>>();
            logger?.LogCritical(ex, "Application terminated unexpectedly");
            throw;
        }
    }
}

public class McpServerHostedService : BackgroundService
{
    private readonly IMcpServer _mcpServer;
    private readonly ILogger<McpServerHostedService> _logger;

    public McpServerHostedService(IMcpServer mcpServer, ILogger<McpServerHostedService> logger)
    {
        _mcpServer = mcpServer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP Energy Calculator Server starting...");
        
        try
        {
            await _mcpServer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MCP Server stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP Server encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP Energy Calculator Server stopping...");
        await _mcpServer.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
