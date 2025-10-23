using System.Text.Json;
using FolderCompare.Models;

namespace FolderCompare.Services;

public static class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Load configuration from a JSON file
    /// </summary>
    public static ComparisonConfig LoadConfig(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<ComparisonConfig>(json, JsonOptions);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to parse configuration file: {configPath}");
        }

        // Auto-load semantic configs by file extension convention
        LoadSemanticConfigsByExtension(config, Path.GetDirectoryName(configPath) ?? ".");

        return config;
    }

    /// <summary>
    /// Load semantic similarity configurations automatically based on file extensions.
    /// Convention: {extension}.semantic-config.json (e.g., sql.semantic-config.json for .sql files)
    /// </summary>
    private static void LoadSemanticConfigsByExtension(ComparisonConfig config, string baseDirectory)
    {
        // If inline semanticSimilarity is provided, it takes precedence - don't auto-load
        if (config.SemanticSimilarity != null)
        {
            return;
        }

        // Get the semantic config directory
        var semanticConfigDir = config.SemanticConfigDirectory;
        Console.WriteLine($"Semantic config directory from config: '{semanticConfigDir}' (null/empty = use default)");
        
        if (string.IsNullOrEmpty(semanticConfigDir))
        {
            Console.WriteLine("No semantic config directory specified, skipping auto-loading");
            return; // No directory specified, skip auto-loading
        }

        var fullConfigDir = Path.IsPathRooted(semanticConfigDir)
            ? semanticConfigDir
            : Path.Combine(baseDirectory, semanticConfigDir);
            
        Console.WriteLine($"Looking for semantic configs in: {fullConfigDir}");

        if (!Directory.Exists(fullConfigDir))
        {
            Console.WriteLine($"Semantic config directory does not exist: {fullConfigDir}");
            return; // Directory doesn't exist, skip auto-loading
        }

        // Get all extensions we'll be processing
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (config.IncludeExtensions?.Any() == true)
        {
            foreach (var ext in config.IncludeExtensions)
            {
                extensions.Add(ext.StartsWith(".") ? ext : "." + ext);
            }
        }

        // Load semantic config for each extension
        foreach (var extension in extensions)
        {
            // Convention: sql.semantic-config.json for .sql files
            var extWithoutDot = extension.TrimStart('.');
            var configFileName = $"{extWithoutDot}.semantic-config.json";
            var configFilePath = Path.Combine(fullConfigDir, configFileName);

            if (File.Exists(configFilePath))
            {
                try
                {
                    Console.WriteLine($"Loading semantic config: {configFilePath}");
                    var semanticConfig = LoadSemanticSimilarityConfig(configFilePath);
                    config.SemanticConfigsByExtension[extension] = semanticConfig;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load semantic config for {extension}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Semantic config not found: {configFilePath}");
            }
        }

        // If we loaded any configs, report it
        if (config.SemanticConfigsByExtension.Any())
        {
            Console.WriteLine($"Loaded semantic configs for: {string.Join(", ", config.SemanticConfigsByExtension.Keys)}");
        }
    }

    /// <summary>
    /// Load a semantic similarity config from a JSON file
    /// </summary>
    private static SemanticSimilarityConfig LoadSemanticSimilarityConfig(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Semantic similarity config file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<SemanticSimilarityConfig>(json, JsonOptions);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to parse semantic similarity config: {path}");
        }

        return config;
    }

    /// <summary>
    /// Save configuration to a JSON file
    /// </summary>
    public static void SaveConfig(ComparisonConfig config, string configPath)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    /// <summary>
    /// Create a sample configuration file
    /// </summary>
    public static void CreateSampleConfig(string configPath)
    {
        var sampleConfig = new ComparisonConfig
        {
            SourcePath = @"C:\Path\To\Source",
            TargetPath = @"C:\Path\To\Target",
            OutputPath = ".",
            IncludeExtensions = new List<string> { ".sql", ".cs", ".txt" },
            ExcludeExtensions = new List<string> { ".dll", ".exe", ".bin" },
            IgnoreFolders = new List<string> { "bin", "obj", "node_modules", ".git" },
            IgnoreCase = false,
            SortInserts = true,
            MaxFileSizeMb = 100,
            DiffFilters = new List<string> { @"^\s*--", @"^\s*/\*" },
            AllowedSubstitutions = new List<SubstitutionRule>
            {
                new SubstitutionRule
                {
                    Name = "TimeZone Conversion",
                    Source = "getdate()",
                    Target = "cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)",
                    IgnoreCase = true,
                    TrimWhitespaceAround = true,
                    ReportMatched = true
                },
                new SubstitutionRule
                {
                    Name = "Quote Prefix",
                    Source = "N'NEXTESTATE",
                    Target = "'NEXTESTATE",
                    ReportMatched = true
                }
            },
            MaxThreads = 0,
            BufferSize = 64
        };

        SaveConfig(sampleConfig, configPath);
    }

    /// <summary>
    /// Convert CommandLineOptions to ComparisonConfig
    /// </summary>
    public static ComparisonConfig FromCommandLineOptions(CommandLineOptions options)
    {
        var config = new ComparisonConfig
        {
            SourcePath = options.SourcePath,
            TargetPath = options.TargetPath,
            OutputPath = options.OutputPath,
            IncludeExtensions = options.IncludeExtensions?.ToList(),
            ExcludeExtensions = options.ExcludeExtensions?.ToList(),
            IgnoreCase = options.IgnoreCase,
            SortInserts = options.SortInserts,
            MaxFileSizeMb = options.MaxFileSize,
            DiffFilters = options.DiffFilters?.ToList(),
            MaxThreads = options.MaxThreads,
            BufferSize = options.BufferSize
        };

        // Parse allowed substitutions
        if (options.AllowedSubstitutions != null && options.AllowedSubstitutions.Any())
        {
            config.AllowedSubstitutions = new List<SubstitutionRule>();
            foreach (var sub in options.AllowedSubstitutions)
            {
                var parts = sub.Split("->", 2);
                if (parts.Length == 2)
                {
                    config.AllowedSubstitutions.Add(new SubstitutionRule
                    {
                        Name = $"{parts[0]} → {parts[1]}",
                        Source = parts[0],
                        Target = parts[1]
                    });
                }
            }
        }

        return config;
    }
}
