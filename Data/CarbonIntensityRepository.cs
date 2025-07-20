using McpEnergyCalculator.Models;

namespace McpEnergyCalculator.Data;

public interface ICarbonIntensityRepository
{
    Task<CarbonIntensityData?> GetCarbonIntensityAsync(string region, string provider = "aws");
    Task<List<CarbonIntensityData>> GetAllCarbonIntensitiesAsync();
    Task<List<string>> GetAvailableRegionsAsync(string provider = "aws");
}

public class CarbonIntensityRepository : ICarbonIntensityRepository
{
    private readonly List<CarbonIntensityData> _carbonIntensities;

    public CarbonIntensityRepository()
    {
        _carbonIntensities = InitializeCarbonIntensityData();
    }

    public async Task<CarbonIntensityData?> GetCarbonIntensityAsync(string region, string provider = "aws")
    {
        await Task.CompletedTask;
        return _carbonIntensities.FirstOrDefault(c => 
            string.Equals(c.Region, region, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<CarbonIntensityData>> GetAllCarbonIntensitiesAsync()
    {
        await Task.CompletedTask;
        return _carbonIntensities.ToList();
    }

    public async Task<List<string>> GetAvailableRegionsAsync(string provider = "aws")
    {
        await Task.CompletedTask;
        return _carbonIntensities
            .Where(c => string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Region)
            .Distinct()
            .OrderBy(r => r)
            .ToList();
    }

    private List<CarbonIntensityData> InitializeCarbonIntensityData()
    {
        var lastUpdated = DateTime.UtcNow;
        var carbonData = new List<CarbonIntensityData>();

        // AWS Regions - Carbon intensity data based on regional electricity grids
        // Source: AWS Customer Carbon Footprint Tool and EPA eGRID data
        carbonData.AddRange(new[]
        {
            // US Regions
            new CarbonIntensityData
            {
                Region = "us-east-1",
                Provider = "aws",
                GramsCo2PerKwh = 415.755, // Virginia - mixed grid with significant coal
                LastUpdated = lastUpdated,
                Source = "EPA eGRID 2021, AWS Customer Carbon Footprint Tool"
            },
            new CarbonIntensityData
            {
                Region = "us-east-2",
                Provider = "aws",
                GramsCo2PerKwh = 476.078, // Ohio - coal-heavy grid
                LastUpdated = lastUpdated,
                Source = "EPA eGRID 2021"
            },
            new CarbonIntensityData
            {
                Region = "us-west-1",
                Provider = "aws",
                GramsCo2PerKwh = 350.949, // N. California - cleaner grid with renewables
                LastUpdated = lastUpdated,
                Source = "EPA eGRID 2021"
            },
            new CarbonIntensityData
            {
                Region = "us-west-2",
                Provider = "aws",
                GramsCo2PerKwh = 367.209, // Oregon - hydro and wind heavy
                LastUpdated = lastUpdated,
                Source = "EPA eGRID 2021"
            },
            
            // Europe Regions
            new CarbonIntensityData
            {
                Region = "eu-west-1",
                Provider = "aws",
                GramsCo2PerKwh = 316.0, // Ireland - wind and gas
                LastUpdated = lastUpdated,
                Source = "EEA 2021, AWS Sustainability Report"
            },
            new CarbonIntensityData
            {
                Region = "eu-west-2",
                Provider = "aws",
                GramsCo2PerKwh = 233.0, // London - cleaner UK grid
                LastUpdated = lastUpdated,
                Source = "UK Government Carbon Factors 2021"
            },
            new CarbonIntensityData
            {
                Region = "eu-west-3",
                Provider = "aws",
                GramsCo2PerKwh = 57.0, // Paris - nuclear-heavy French grid
                LastUpdated = lastUpdated,
                Source = "RTE France, EEA 2021"
            },
            new CarbonIntensityData
            {
                Region = "eu-central-1",
                Provider = "aws",
                GramsCo2PerKwh = 338.0, // Frankfurt - German grid with coal
                LastUpdated = lastUpdated,
                Source = "German Federal Environment Agency 2021"
            },
            new CarbonIntensityData
            {
                Region = "eu-north-1",
                Provider = "aws",
                GramsCo2PerKwh = 34.0, // Stockholm - very clean Nordic hydro/nuclear
                LastUpdated = lastUpdated,
                Source = "Swedish Energy Agency 2021"
            },

            // Asia Pacific Regions
            new CarbonIntensityData
            {
                Region = "ap-northeast-1",
                Provider = "aws",
                GramsCo2PerKwh = 462.0, // Tokyo - mixed grid post-Fukushima
                LastUpdated = lastUpdated,
                Source = "Japan Ministry of Environment 2021"
            },
            new CarbonIntensityData
            {
                Region = "ap-northeast-2",
                Provider = "aws",
                GramsCo2PerKwh = 436.0, // Seoul - coal and nuclear mix
                LastUpdated = lastUpdated,
                Source = "Korea Energy Agency 2021"
            },
            new CarbonIntensityData
            {
                Region = "ap-southeast-1",
                Provider = "aws",
                GramsCo2PerKwh = 431.0, // Singapore - gas-heavy grid
                LastUpdated = lastUpdated,
                Source = "Singapore Energy Authority 2021"
            },
            new CarbonIntensityData
            {
                Region = "ap-southeast-2",
                Provider = "aws",
                GramsCo2PerKwh = 610.0, // Sydney - coal-heavy Australian grid
                LastUpdated = lastUpdated,
                Source = "Australian Energy Regulator 2021"
            },
            new CarbonIntensityData
            {
                Region = "ap-south-1",
                Provider = "aws",
                GramsCo2PerKwh = 708.0, // Mumbai - coal-dominated Indian grid
                LastUpdated = lastUpdated,
                Source = "Central Electricity Authority India 2021"
            },

            // Canada Region
            new CarbonIntensityData
            {
                Region = "ca-central-1",
                Provider = "aws",
                GramsCo2PerKwh = 120.0, // Central Canada - hydro-heavy
                LastUpdated = lastUpdated,
                Source = "Environment Canada 2021"
            },

            // South America
            new CarbonIntensityData
            {
                Region = "sa-east-1",
                Provider = "aws",
                GramsCo2PerKwh = 79.0, // São Paulo - hydro-dominant Brazilian grid
                LastUpdated = lastUpdated,
                Source = "Brazilian Ministry of Energy 2021"
            }
        });

        // Google Cloud Platform Regions
        carbonData.AddRange(new[]
        {
            new CarbonIntensityData
            {
                Region = "us-central1",
                Provider = "gcp",
                GramsCo2PerKwh = 428.0, // Iowa
                LastUpdated = lastUpdated,
                Source = "Google Cloud Carbon Footprint"
            },
            new CarbonIntensityData
            {
                Region = "us-east1",
                Provider = "gcp",
                GramsCo2PerKwh = 415.0, // South Carolina
                LastUpdated = lastUpdated,
                Source = "Google Cloud Carbon Footprint"
            },
            new CarbonIntensityData
            {
                Region = "europe-west1",
                Provider = "gcp",
                GramsCo2PerKwh = 393.0, // Belgium
                LastUpdated = lastUpdated,
                Source = "Google Cloud Carbon Footprint"
            },
            new CarbonIntensityData
            {
                Region = "asia-east1",
                Provider = "gcp",
                GramsCo2PerKwh = 554.0, // Taiwan
                LastUpdated = lastUpdated,
                Source = "Google Cloud Carbon Footprint"
            }
        });

        // Microsoft Azure Regions
        carbonData.AddRange(new[]
        {
            new CarbonIntensityData
            {
                Region = "eastus",
                Provider = "azure",
                GramsCo2PerKwh = 415.0, // Virginia
                LastUpdated = lastUpdated,
                Source = "Microsoft Sustainability Calculator"
            },
            new CarbonIntensityData
            {
                Region = "westus",
                Provider = "azure",
                GramsCo2PerKwh = 351.0, // Washington
                LastUpdated = lastUpdated,
                Source = "Microsoft Sustainability Calculator"
            },
            new CarbonIntensityData
            {
                Region = "northeurope",
                Provider = "azure",
                GramsCo2PerKwh = 316.0, // Ireland
                LastUpdated = lastUpdated,
                Source = "Microsoft Sustainability Calculator"
            },
            new CarbonIntensityData
            {
                Region = "westeurope",
                Provider = "azure",
                GramsCo2PerKwh = 393.0, // Netherlands
                LastUpdated = lastUpdated,
                Source = "Microsoft Sustainability Calculator"
            }
        });

        return carbonData;
    }
}
