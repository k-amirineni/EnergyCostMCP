using McpEnergyCalculator.Data;
using McpEnergyCalculator.Models;
using Microsoft.Extensions.Logging;

namespace McpEnergyCalculator.Services;

public interface IEnergyCalculationService
{
    Task<EnergyCalculationResult> CalculateEnergyAsync(EnergyCalculationRequest request);
    Task<ModelInfoResult> GetModelInfoAsync(ModelInfoRequest request);
}

public class EnergyCalculationService : IEnergyCalculationService
{
    private readonly IModelDataRepository _modelRepository;
    private readonly ICarbonIntensityRepository _carbonRepository;
    private readonly ILogger<EnergyCalculationService> _logger;

    // Default energy costs in USD per kWh for different regions (rough estimates)
    private readonly Dictionary<string, double> _energyCosts = new()
    {
        ["us-east-1"] = 0.10,    // Virginia
        ["us-east-2"] = 0.12,    // Ohio
        ["us-west-1"] = 0.18,    // N. California
        ["us-west-2"] = 0.08,    // Oregon
        ["eu-west-1"] = 0.22,    // Ireland
        ["eu-west-2"] = 0.20,    // London
        ["eu-west-3"] = 0.18,    // Paris
        ["eu-central-1"] = 0.25, // Frankfurt
        ["eu-north-1"] = 0.15,   // Stockholm
        ["ap-northeast-1"] = 0.23, // Tokyo
        ["ap-northeast-2"] = 0.16, // Seoul
        ["ap-southeast-1"] = 0.19, // Singapore
        ["ap-southeast-2"] = 0.21, // Sydney
        ["ap-south-1"] = 0.08,   // Mumbai
        ["ca-central-1"] = 0.11, // Canada Central
        ["sa-east-1"] = 0.12,    // São Paulo
        ["default"] = 0.12       // Global average
    };

    public EnergyCalculationService(
        IModelDataRepository modelRepository,
        ICarbonIntensityRepository carbonRepository,
        ILogger<EnergyCalculationService> logger)
    {
        _modelRepository = modelRepository;
        _carbonRepository = carbonRepository;
        _logger = logger;
    }

    public async Task<EnergyCalculationResult> CalculateEnergyAsync(EnergyCalculationRequest request)
    {
        try
        {
            _logger.LogInformation("Calculating energy for model: {Model}, Input: {Input}, Output: {Output}",
                request.Model, request.InputTokens, request.OutputTokens);

            // Get model data
            var model = await _modelRepository.GetModelAsync(request.Model, request.Provider);
            if (model == null)
            {
                _logger.LogWarning("Model not found: {Model}", request.Model);
                throw new InvalidOperationException($"Model '{request.Model}' not found in the database. Use the get-model-info tool to see available models.");
            }

            // Get carbon intensity data
            var region = request.Region ?? "us-east-1";
            var carbonIntensity = await _carbonRepository.GetCarbonIntensityAsync(region, "aws");
            if (carbonIntensity == null)
            {
                _logger.LogWarning("Carbon intensity data not found for region: {Region}", region);
                // Use default carbon intensity for us-east-1
                carbonIntensity = await _carbonRepository.GetCarbonIntensityAsync("us-east-1", "aws");
                carbonIntensity ??= new CarbonIntensityData
                {
                    Region = region,
                    Provider = "aws",
                    GramsCo2PerKwh = 415.755, // US average
                    LastUpdated = DateTime.UtcNow,
                    Source = "Default fallback"
                };
            }

            // Calculate energy consumption
            var inputEnergyWh = request.InputTokens * model.EnergyPerInputToken;
            var outputEnergyWh = request.OutputTokens * model.EnergyPerOutputToken;
            var totalEnergyWh = inputEnergyWh + outputEnergyWh;
            var totalEnergyKwh = totalEnergyWh / 1000.0;

            // Calculate carbon emissions
            var inputCarbonGrams = inputEnergyWh / 1000.0 * carbonIntensity.GramsCo2PerKwh;
            var outputCarbonGrams = outputEnergyWh / 1000.0 * carbonIntensity.GramsCo2PerKwh;

            // Calculate energy costs
            var costPerKwh = _energyCosts.GetValueOrDefault(region, _energyCosts["default"]);
            var inputCost = (inputEnergyWh / 1000.0) * costPerKwh;
            var outputCost = (outputEnergyWh / 1000.0) * costPerKwh;

            var result = new EnergyCalculationResult
            {
                Model = model.ModelName,
                Provider = model.Provider,
                InputTokens = request.InputTokens,
                OutputTokens = request.OutputTokens,
                Region = region,
                Energy = new EnergyConsumption
                {
                    InputTokensWh = inputEnergyWh,
                    OutputTokensWh = outputEnergyWh
                },
                Carbon = new CarbonEmissions
                {
                    InputTokensGrams = inputCarbonGrams,
                    OutputTokensGrams = outputCarbonGrams,
                    CarbonIntensity = carbonIntensity.GramsCo2PerKwh
                },
                Costs = new EnergyCosts
                {
                    InputTokensCost = inputCost,
                    OutputTokensCost = outputCost,
                    CostPerKwh = costPerKwh
                },
                CalculatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Energy calculation completed. Total energy: {Energy:F6} Wh, Carbon: {Carbon:F3} g CO2e, Cost: ${Cost:F6}",
                result.Energy.TotalWh, result.Carbon.TotalGrams, result.Costs.TotalCost);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating energy for model: {Model}", request.Model);
            throw;
        }
    }

    public async Task<ModelInfoResult> GetModelInfoAsync(ModelInfoRequest request)
    {
        try
        {
            _logger.LogInformation("Getting model info. Provider: {Provider}, Category: {Category}, Model: {Model}",
                request.Provider, request.Category, request.ModelName);

            List<LlmModel> models;

            if (!string.IsNullOrEmpty(request.ModelName))
            {
                var model = await _modelRepository.GetModelAsync(request.ModelName, request.Provider);
                models = model != null ? new List<LlmModel> { model } : new List<LlmModel>();
            }
            else if (!string.IsNullOrEmpty(request.Provider))
            {
                models = await _modelRepository.GetModelsByProviderAsync(request.Provider);
                if (!string.IsNullOrEmpty(request.Category))
                {
                    models = models.Where(m => string.Equals(m.Category, request.Category, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            else if (!string.IsNullOrEmpty(request.Category))
            {
                models = await _modelRepository.GetModelsByCategoryAsync(request.Category);
            }
            else
            {
                models = await _modelRepository.GetAllModelsAsync();
            }

            var providers = await _modelRepository.GetProvidersAsync();
            var categories = await _modelRepository.GetCategoriesAsync();

            var result = new ModelInfoResult
            {
                Models = models,
                Providers = providers,
                Categories = categories
            };

            _logger.LogInformation("Retrieved {Count} models", result.TotalModels);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model info");
            throw;
        }
    }
}
