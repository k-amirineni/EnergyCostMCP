using McpEnergyCalculator.Models;

namespace McpEnergyCalculator.Data;

public interface IModelDataRepository
{
    Task<LlmModel?> GetModelAsync(string modelName, string? provider = null);
    Task<List<LlmModel>> GetModelsByProviderAsync(string provider);
    Task<List<LlmModel>> GetModelsByCategoryAsync(string category);
    Task<List<LlmModel>> GetAllModelsAsync();
    Task<List<string>> GetProvidersAsync();
    Task<List<string>> GetCategoriesAsync();
}

public class ModelDataRepository : IModelDataRepository
{
    private readonly List<LlmModel> _models;
    private readonly Dictionary<string, List<string>> _modelAliases;

    public ModelDataRepository()
    {
        _models = InitializeModelData();
        _modelAliases = InitializeModelAliases();
    }

    public async Task<LlmModel?> GetModelAsync(string modelName, string? provider = null)
    {
        await Task.CompletedTask;

        // Normalize model name
        var normalizedName = NormalizeModelName(modelName);
        
        // Try exact match first
        var model = _models.FirstOrDefault(m => 
            string.Equals(m.ModelName, normalizedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ModelId, normalizedName, StringComparison.OrdinalIgnoreCase));

        if (model != null && (provider == null || string.Equals(model.Provider, provider, StringComparison.OrdinalIgnoreCase)))
        {
            return model;
        }

        // Try alias matching
        foreach (var alias in _modelAliases)
        {
            if (alias.Value.Any(a => string.Equals(a, normalizedName, StringComparison.OrdinalIgnoreCase)))
            {
                model = _models.FirstOrDefault(m => string.Equals(m.ModelId, alias.Key, StringComparison.OrdinalIgnoreCase));
                if (model != null && (provider == null || string.Equals(model.Provider, provider, StringComparison.OrdinalIgnoreCase)))
                {
                    return model;
                }
            }
        }

        return null;
    }

    public async Task<List<LlmModel>> GetModelsByProviderAsync(string provider)
    {
        await Task.CompletedTask;
        return _models.Where(m => string.Equals(m.Provider, provider, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<LlmModel>> GetModelsByCategoryAsync(string category)
    {
        await Task.CompletedTask;
        return _models.Where(m => string.Equals(m.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<LlmModel>> GetAllModelsAsync()
    {
        await Task.CompletedTask;
        return _models.ToList();
    }

    public async Task<List<string>> GetProvidersAsync()
    {
        await Task.CompletedTask;
        return _models.Select(m => m.Provider).Distinct().OrderBy(p => p).ToList();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await Task.CompletedTask;
        return _models.Select(m => m.Category).Distinct().OrderBy(c => c).ToList();
    }

    private string NormalizeModelName(string modelName)
    {
        return modelName.ToLowerInvariant()
            .Replace("-", "")
            .Replace("_", "")
            .Replace(" ", "");
    }

    private List<LlmModel> InitializeModelData()
    {
        var models = new List<LlmModel>();
        var lastUpdated = DateTime.UtcNow;

        // OpenAI Models
        models.AddRange(new[]
        {
            new LlmModel
            {
                Provider = "openai",
                ModelName = "GPT-4o",
                ModelId = "gpt-4o",
                EnergyPerInputToken = 0.0000045, // Estimated: 4.5 μWh per input token
                EnergyPerOutputToken = 0.0000090, // Estimated: 9.0 μWh per output token
                Category = "large",
                ParameterCount = 1760000000000, // ~1.76T parameters (estimated)
                LastUpdated = lastUpdated,
                Notes = "Latest GPT-4 Omni model, high efficiency optimizations"
            },
            new LlmModel
            {
                Provider = "openai",
                ModelName = "GPT-4 Turbo",
                ModelId = "gpt-4-turbo",
                EnergyPerInputToken = 0.0000050,
                EnergyPerOutputToken = 0.0000100,
                Category = "large",
                ParameterCount = 1760000000000,
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "openai",
                ModelName = "GPT-4",
                ModelId = "gpt-4",
                EnergyPerInputToken = 0.0000055,
                EnergyPerOutputToken = 0.0000110,
                Category = "large",
                ParameterCount = 1760000000000,
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "openai",
                ModelName = "GPT-3.5 Turbo",
                ModelId = "gpt-3.5-turbo",
                EnergyPerInputToken = 0.0000020,
                EnergyPerOutputToken = 0.0000040,
                Category = "medium",
                ParameterCount = 175000000000, // 175B parameters
                LastUpdated = lastUpdated
            }
        });

        // Anthropic Models
        models.AddRange(new[]
        {
            new LlmModel
            {
                Provider = "anthropic",
                ModelName = "Claude 3.5 Sonnet",
                ModelId = "claude-3-5-sonnet-20241022",
                EnergyPerInputToken = 0.0000042,
                EnergyPerOutputToken = 0.0000084,
                Category = "large",
                ParameterCount = 600000000000, // Estimated ~600B parameters
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "anthropic",
                ModelName = "Claude 3 Opus",
                ModelId = "claude-3-opus-20240229",
                EnergyPerInputToken = 0.0000048,
                EnergyPerOutputToken = 0.0000096,
                Category = "extra-large",
                ParameterCount = 1000000000000, // Estimated ~1T parameters
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "anthropic",
                ModelName = "Claude 3 Sonnet",
                ModelId = "claude-3-sonnet-20240229",
                EnergyPerInputToken = 0.0000035,
                EnergyPerOutputToken = 0.0000070,
                Category = "large",
                ParameterCount = 400000000000, // Estimated ~400B parameters
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "anthropic",
                ModelName = "Claude 3 Haiku",
                ModelId = "claude-3-haiku-20240307",
                EnergyPerInputToken = 0.0000015,
                EnergyPerOutputToken = 0.0000030,
                Category = "small",
                ParameterCount = 50000000000, // Estimated ~50B parameters
                LastUpdated = lastUpdated
            }
        });

        // Google Models
        models.AddRange(new[]
        {
            new LlmModel
            {
                Provider = "google",
                ModelName = "Gemini Pro 1.5",
                ModelId = "gemini-1.5-pro",
                EnergyPerInputToken = 0.0000040,
                EnergyPerOutputToken = 0.0000080,
                Category = "large",
                ParameterCount = 500000000000, // Estimated ~500B parameters
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "google",
                ModelName = "Gemini Flash 1.5",
                ModelId = "gemini-1.5-flash",
                EnergyPerInputToken = 0.0000020,
                EnergyPerOutputToken = 0.0000040,
                Category = "medium",
                ParameterCount = 100000000000, // Estimated ~100B parameters
                LastUpdated = lastUpdated
            }
        });

        // Meta (Llama) Models
        models.AddRange(new[]
        {
            new LlmModel
            {
                Provider = "meta",
                ModelName = "Llama 3.1 405B",
                ModelId = "llama-3.1-405b",
                EnergyPerInputToken = 0.0000060,
                EnergyPerOutputToken = 0.0000120,
                Category = "extra-large",
                ParameterCount = 405000000000,
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "meta",
                ModelName = "Llama 3.1 70B",
                ModelId = "llama-3.1-70b",
                EnergyPerInputToken = 0.0000030,
                EnergyPerOutputToken = 0.0000060,
                Category = "large",
                ParameterCount = 70000000000,
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "meta",
                ModelName = "Llama 3.1 8B",
                ModelId = "llama-3.1-8b",
                EnergyPerInputToken = 0.0000010,
                EnergyPerOutputToken = 0.0000020,
                Category = "small",
                ParameterCount = 8000000000,
                LastUpdated = lastUpdated
            }
        });

        // DeepSeek Models
        models.AddRange(new[]
        {
            new LlmModel
            {
                Provider = "deepseek",
                ModelName = "DeepSeek V3",
                ModelId = "deepseek-v3",
                EnergyPerInputToken = 0.0000025,
                EnergyPerOutputToken = 0.0000050,
                Category = "large",
                ParameterCount = 671000000000, // 671B parameters
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "deepseek",
                ModelName = "DeepSeek Chat",
                ModelId = "deepseek-chat",
                EnergyPerInputToken = 0.0000018,
                EnergyPerOutputToken = 0.0000036,
                Category = "medium",
                ParameterCount = 67000000000,
                LastUpdated = lastUpdated
            }
        });

        // Cohere Models
        models.AddRange(new[]
        {
            new LlmModel
            {
                Provider = "cohere",
                ModelName = "Command R+",
                ModelId = "command-r-plus",
                EnergyPerInputToken = 0.0000038,
                EnergyPerOutputToken = 0.0000076,
                Category = "large",
                ParameterCount = 104000000000,
                LastUpdated = lastUpdated
            },
            new LlmModel
            {
                Provider = "cohere",
                ModelName = "Command R",
                ModelId = "command-r",
                EnergyPerInputToken = 0.0000028,
                EnergyPerOutputToken = 0.0000056,
                Category = "medium",
                ParameterCount = 35000000000,
                LastUpdated = lastUpdated
            }
        });

        return models;
    }

    private Dictionary<string, List<string>> InitializeModelAliases()
    {
        return new Dictionary<string, List<string>>
        {
            ["gpt-4o"] = new() { "gpt4o", "gpt-4-omni", "gpt4omni" },
            ["gpt-4-turbo"] = new() { "gpt4turbo", "gpt-4-turbo-preview" },
            ["gpt-4"] = new() { "gpt4" },
            ["gpt-3.5-turbo"] = new() { "gpt3.5turbo", "gpt35turbo", "chatgpt" },
            ["claude-3-5-sonnet-20241022"] = new() { "claude-3.5-sonnet", "claude3.5sonnet", "claude35sonnet" },
            ["claude-3-opus-20240229"] = new() { "claude-3-opus", "claude3opus", "opus" },
            ["claude-3-sonnet-20240229"] = new() { "claude-3-sonnet", "claude3sonnet", "sonnet" },
            ["claude-3-haiku-20240307"] = new() { "claude-3-haiku", "claude3haiku", "haiku" },
            ["gemini-1.5-pro"] = new() { "gemini15pro", "gemini-pro", "geminipro" },
            ["gemini-1.5-flash"] = new() { "gemini15flash", "gemini-flash", "geminiflash" },
            ["llama-3.1-405b"] = new() { "llama31405b", "llama405b", "llama-405b" },
            ["llama-3.1-70b"] = new() { "llama3170b", "llama70b", "llama-70b" },
            ["llama-3.1-8b"] = new() { "llama318b", "llama8b", "llama-8b" },
            ["deepseek-v3"] = new() { "deepseekv3", "deepseek-v3-base" },
            ["deepseek-chat"] = new() { "deepseekchat", "deepseek-coder" },
            ["command-r-plus"] = new() { "commandrplus", "commandr+" },
            ["command-r"] = new() { "commandr" }
        };
    }
}
