using Newtonsoft.Json;

namespace McpEnergyCalculator.Models;

public class LlmModel
{
    public string Provider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public double EnergyPerInputToken { get; set; } // Watt-hours per token
    public double EnergyPerOutputToken { get; set; } // Watt-hours per token
    public string Category { get; set; } = string.Empty; // e.g., "small", "medium", "large"
    public long ParameterCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? Notes { get; set; }
}

public class EnergyCalculationRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("inputTokens")]
    public int InputTokens { get; set; }

    [JsonProperty("outputTokens")]
    public int OutputTokens { get; set; }

    [JsonProperty("region")]
    public string? Region { get; set; } = "us-east-1"; // AWS region for carbon intensity

    [JsonProperty("provider")]
    public string? Provider { get; set; }
}

public class EnergyCalculationResult
{
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonProperty("inputTokens")]
    public int InputTokens { get; set; }

    [JsonProperty("outputTokens")]
    public int OutputTokens { get; set; }

    [JsonProperty("totalTokens")]
    public int TotalTokens => InputTokens + OutputTokens;

    [JsonProperty("energyConsumption")]
    public EnergyConsumption Energy { get; set; } = new();

    [JsonProperty("carbonEmissions")]
    public CarbonEmissions Carbon { get; set; } = new();

    [JsonProperty("costs")]
    public EnergyCosts Costs { get; set; } = new();

    [JsonProperty("region")]
    public string Region { get; set; } = string.Empty;

    [JsonProperty("calculatedAt")]
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class EnergyConsumption
{
    [JsonProperty("inputTokensWh")]
    public double InputTokensWh { get; set; } // Watt-hours for input tokens

    [JsonProperty("outputTokensWh")]
    public double OutputTokensWh { get; set; } // Watt-hours for output tokens

    [JsonProperty("totalWh")]
    public double TotalWh => InputTokensWh + OutputTokensWh;

    [JsonProperty("totalKwh")]
    public double TotalKwh => TotalWh / 1000.0;
}

public class CarbonEmissions
{
    [JsonProperty("inputTokensGrams")]
    public double InputTokensGrams { get; set; } // grams CO2e for input tokens

    [JsonProperty("outputTokensGrams")]
    public double OutputTokensGrams { get; set; } // grams CO2e for output tokens

    [JsonProperty("totalGrams")]
    public double TotalGrams => InputTokensGrams + OutputTokensGrams;

    [JsonProperty("totalKg")]
    public double TotalKg => TotalGrams / 1000.0;

    [JsonProperty("carbonIntensity")]
    public double CarbonIntensity { get; set; } // grams CO2e per kWh
}

public class EnergyCosts
{
    [JsonProperty("inputTokensCost")]
    public double InputTokensCost { get; set; } // USD cost for input tokens energy

    [JsonProperty("outputTokensCost")]
    public double OutputTokensCost { get; set; } // USD cost for output tokens energy

    [JsonProperty("totalCost")]
    public double TotalCost => InputTokensCost + OutputTokensCost;

    [JsonProperty("costPerKwh")]
    public double CostPerKwh { get; set; } // USD per kWh
}

public class CarbonIntensityData
{
    public string Region { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // AWS, Azure, GCP
    public double GramsCo2PerKwh { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? Source { get; set; }
}

public class ModelInfoRequest
{
    [JsonProperty("provider")]
    public string? Provider { get; set; }

    [JsonProperty("category")]
    public string? Category { get; set; }

    [JsonProperty("modelName")]
    public string? ModelName { get; set; }
}

public class ModelInfoResult
{
    [JsonProperty("models")]
    public List<LlmModel> Models { get; set; } = new();

    [JsonProperty("totalModels")]
    public int TotalModels => Models.Count;

    [JsonProperty("providers")]
    public List<string> Providers { get; set; } = new();

    [JsonProperty("categories")]
    public List<string> Categories { get; set; } = new();
}
