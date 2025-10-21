using Xunit;
using FolderCompare.Models;
using FolderCompare.Services;

namespace FolderCompare.Tests;

public class ConfigurationTests
{
    private readonly string _testDataPath;

    public ConfigurationTests()
    {
        _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
    }

    [Fact]
    public void LoadConfig_ValidConfiguration_LoadsSuccessfully()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            OutputPath = ".",
            IncludeExtensions = new List<string> { ".sql", ".cs" },
            IgnoreCase = true,
            SortInserts = true,
            MaxFileSizeMb = 50,
            MaxThreads = 4,
            BufferSize = 128
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("../Source", config.SourcePath);
        Assert.Equal("../Target", config.TargetPath);
        Assert.Equal(".", config.OutputPath);
        Assert.NotNull(config.IncludeExtensions);
        Assert.Equal(2, config.IncludeExtensions.Count);
        Assert.Contains(".sql", config.IncludeExtensions);
        Assert.Contains(".cs", config.IncludeExtensions);
        Assert.True(config.IgnoreCase);
        Assert.True(config.SortInserts);
        Assert.Equal(50, config.MaxFileSizeMb);
        Assert.Equal(4, config.MaxThreads);
        Assert.Equal(128, config.BufferSize);

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_MissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent-config.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ConfigurationService.LoadConfig(nonExistentPath));
    }

    [Fact]
    public void LoadConfig_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), $"invalid-config-{Guid.NewGuid()}.json");
        File.WriteAllText(configPath, "{ invalid json content }");

        try
        {
            // Act & Assert
            Assert.Throws<System.Text.Json.JsonException>(() => ConfigurationService.LoadConfig(configPath));
        }
        finally
        {
            // Cleanup
            File.Delete(configPath);
        }
    }

    [Fact]
    public void LoadConfig_WithAllowedSubstitutions_LoadsPatternsCorrectly()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            AllowedSubstitutions = new List<SubstitutionRule>
            {
                new SubstitutionRule
                {
                    Name = "TimeZone Conversion",
                    Source = "GETDATE()",
                    Target = "cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)",
                    IgnoreCase = true,
                    TrimWhitespaceAround = true,
                    ReportMatched = true
                },
                new SubstitutionRule
                {
                    Name = "Filegroup Removal",
                    Source = "ON [PRIMARY]",
                    Target = "",
                    IgnoreCase = true,
                    TrimWhitespaceAround = true,
                    ReportMatched = true
                }
            }
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config.AllowedSubstitutions);
        Assert.Equal(2, config.AllowedSubstitutions.Count);
        
        var tzPattern = config.AllowedSubstitutions[0];
        Assert.Equal("TimeZone Conversion", tzPattern.Name);
        Assert.Equal("GETDATE()", tzPattern.Source);
        Assert.Contains("AT TIME ZONE", tzPattern.Target);
        Assert.True(tzPattern.IgnoreCase);
        Assert.True(tzPattern.TrimWhitespaceAround);
        Assert.True(tzPattern.ReportMatched);

        var fgPattern = config.AllowedSubstitutions[1];
        Assert.Equal("Filegroup Removal", fgPattern.Name);
        Assert.Equal("ON [PRIMARY]", fgPattern.Source);
        Assert.Equal("", fgPattern.Target);

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_WithWhitelistLinePatterns_LoadsCorrectly()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            WhitelistLinePatterns = new List<WhitelistLinePattern>
            {
                new WhitelistLinePattern
                {
                    Name = "RowRevision does not exist in Azure",
                    Contains = "RowRevision]",
                    ExistsInSource = true,
                    ExistsInTarget = false
                }
            }
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config.WhitelistLinePatterns);
        Assert.Single(config.WhitelistLinePatterns);
        
        var pattern = config.WhitelistLinePatterns[0];
        Assert.Equal("RowRevision does not exist in Azure", pattern.Name);
        Assert.Equal("RowRevision]", pattern.Contains);
        Assert.True(pattern.ExistsInSource);
        Assert.False(pattern.ExistsInTarget);

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_WithWhitelistFilePatterns_LoadsCorrectly()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            WhitelistFilePatterns = new List<WhitelistFilePattern>
            {
                new WhitelistFilePattern
                {
                    Name = "Data files can have more lines in Azure",
                    Pattern = "*_Data.sql",
                    AllowLineMissingInSource = true,
                    AllowLineMissingInTarget = false,
                    AllowModified = true
                }
            }
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config.WhitelistFilePatterns);
        Assert.Single(config.WhitelistFilePatterns);
        
        var pattern = config.WhitelistFilePatterns[0];
        Assert.Equal("Data files can have more lines in Azure", pattern.Name);
        Assert.Equal("*_Data.sql", pattern.Pattern);
        Assert.True(pattern.AllowLineMissingInSource);
        Assert.False(pattern.AllowLineMissingInTarget);
        Assert.True(pattern.AllowModified);

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_WithIgnoreFolders_LoadsCorrectly()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            IgnoreFolders = new List<string> { "bin", "obj", ".git", "Misc" }
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config.IgnoreFolders);
        Assert.Equal(4, config.IgnoreFolders.Count);
        Assert.Contains("bin", config.IgnoreFolders);
        Assert.Contains("obj", config.IgnoreFolders);
        Assert.Contains(".git", config.IgnoreFolders);
        Assert.Contains("Misc", config.IgnoreFolders);

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_WithExcludeExtensions_LoadsCorrectly()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            ExcludeExtensions = new List<string> { ".dll", ".exe", ".bin" }
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config.ExcludeExtensions);
        Assert.Equal(3, config.ExcludeExtensions.Count);
        Assert.Contains(".dll", config.ExcludeExtensions);
        Assert.Contains(".exe", config.ExcludeExtensions);
        Assert.Contains(".bin", config.ExcludeExtensions);

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_DefaultValues_AreSetCorrectly()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target"
            // All other values should use defaults
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.Equal(".", config.OutputPath);
        Assert.False(config.IgnoreCase);
        Assert.False(config.SortInserts);
        Assert.Equal(100, config.MaxFileSizeMb);
        Assert.Equal(0, config.MaxThreads); // 0 means auto
        Assert.Equal(64, config.BufferSize);
        Assert.Equal("ConfigTemplates", config.SemanticConfigDirectory);

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_MultipleSubstitutionPatterns_AllLoaded()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            AllowedSubstitutions = new List<SubstitutionRule>
            {
                new SubstitutionRule { Name = "Pattern1", Source = "A", Target = "B" },
                new SubstitutionRule { Name = "Pattern2", Source = "C", Target = "D" },
                new SubstitutionRule { Name = "Pattern3", Source = "E", Target = "F" },
                new SubstitutionRule { Name = "Pattern4", Source = "G", Target = "H" },
                new SubstitutionRule { Name = "Pattern5", Source = "I", Target = "J" }
            }
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config.AllowedSubstitutions);
        Assert.Equal(5, config.AllowedSubstitutions.Count);
        Assert.All(config.AllowedSubstitutions, pattern => Assert.NotNull(pattern.Name));

        // Cleanup
        File.Delete(configPath);
    }

    [Fact]
    public void LoadConfig_EmptyLists_LoadedAsEmpty()
    {
        // Arrange
        var configPath = CreateTestConfig(new ComparisonConfig
        {
            SourcePath = "../Source",
            TargetPath = "../Target",
            IncludeExtensions = new List<string>(),
            ExcludeExtensions = new List<string>(),
            IgnoreFolders = new List<string>(),
            AllowedSubstitutions = new List<SubstitutionRule>()
        });

        // Act
        var config = ConfigurationService.LoadConfig(configPath);

        // Assert
        Assert.NotNull(config.IncludeExtensions);
        Assert.Empty(config.IncludeExtensions);
        Assert.NotNull(config.ExcludeExtensions);
        Assert.Empty(config.ExcludeExtensions);
        Assert.NotNull(config.IgnoreFolders);
        Assert.Empty(config.IgnoreFolders);
        Assert.NotNull(config.AllowedSubstitutions);
        Assert.Empty(config.AllowedSubstitutions);

        // Cleanup
        File.Delete(configPath);
    }

    // Helper method to create a temporary test config file
    private string CreateTestConfig(ComparisonConfig config)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.json");
        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        File.WriteAllText(tempPath, json);
        return tempPath;
    }
}
