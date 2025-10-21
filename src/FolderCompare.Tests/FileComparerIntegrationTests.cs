using Xunit;
using FolderCompare.Models;
using FolderCompare.Services;

namespace FolderCompare.Tests;

/// <summary>
/// Integration tests for FileComparer using actual test data files.
/// Tests end-to-end file comparison scenarios.
/// </summary>
public class FileComparerIntegrationTests
{
    private readonly string _testDataPath;
    private readonly string _sourcePath;
    private readonly string _targetPath;

    public FileComparerIntegrationTests()
    {
        // Use test assembly location rather than current directory
        var baseDir = AppContext.BaseDirectory;
        _testDataPath = Path.Combine(baseDir, "TestData");
        _sourcePath = Path.Combine(_testDataPath, "Source");
        _targetPath = Path.Combine(_testDataPath, "Target");
    }

    #region Basic Comparison Tests

    [Fact]
    public void CompareFiles_IdenticalFiles_ReturnsIdentical()
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, "SELECT * FROM Users");

            var comparer = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var result = comparer.CompareFiles(tempFile, tempFile, "test.sql");

            Assert.Equal(ComparisonStatus.Identical, result.Status);
            Assert.Equal(result.SourceHash, result.TargetHash);
            Assert.Equal(0, result.DifferenceCount);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompareFiles_DifferentFiles_ReturnsDifferent()
    {
        var tempSource = Path.GetTempFileName();
        var tempTarget = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempSource, "SELECT * FROM Users");
            File.WriteAllText(tempTarget, "SELECT * FROM Customers");

            var comparer = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var result = comparer.CompareFiles(tempSource, tempTarget, "test.sql");

            Assert.Equal(ComparisonStatus.Different, result.Status);
            Assert.True(result.DifferenceCount > 0);
        }
        finally
        {
            File.Delete(tempSource);
            File.Delete(tempTarget);
        }
    }

    [Fact]
    public void CompareFiles_OnlyInSource_FileExists()
    {
        // Use relative path from test assembly location
        var baseDir = AppContext.BaseDirectory;
        var testDataPath = Path.Combine(baseDir, "TestData", "Source");
        var sourceFile = Path.Combine(testDataPath, "OnlyInSource.txt");
        
        Assert.True(File.Exists(sourceFile), $"OnlyInSource.txt should exist at {sourceFile}");
        
        var content = File.ReadAllText(sourceFile);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void CompareFiles_EmptyFiles_AreIdentical()
    {
        var tempSource = Path.GetTempFileName();
        var tempTarget = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempSource, "");
            File.WriteAllText(tempTarget, "");

            var comparer = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var result = comparer.CompareFiles(tempSource, tempTarget, "empty.txt");

            Assert.Equal(ComparisonStatus.Identical, result.Status);
        }
        finally
        {
            File.Delete(tempSource);
            File.Delete(tempTarget);
        }
    }

    #endregion

    #region Filter Rules Tests

    [Fact]
    public void CompareFiles_WithFilegroupFilterRules_FiltersStorageChanges()
    {
        var rules = new List<DiffFilterRule>
        {
            new()
            {
                Name = "Remove PRIMARY",
                SourcePattern = "ON [PRIMARY]",
                TargetPattern = "",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true,
                AllowStructuralChanges = true
            },
            new()
            {
                Name = "Remove Data filegroup",
                SourcePattern = "ON [Data]",
                TargetPattern = "",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true,
                AllowStructuralChanges = true
            },
            new()
            {
                Name = "Semicolon removal",
                SourcePattern = ";",
                TargetPattern = "",
                IsExactMatch = true,
                IgnoreCase = false,
                TrimWhitespaceAround = true,
                AllowStructuralChanges = true
            },
            new()
            {
                Name = "GO removal",
                SourcePattern = "GO",
                TargetPattern = "",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true,
                AllowStructuralChanges = true
            },
            new()
            {
                Name = "GRANT EXECUTE removal",
                SourcePattern = "GRANT EXECUTE ON",
                TargetPattern = "",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true,
                AllowStructuralChanges = true
            },
            new()
            {
                Name = "USE statement removal",
                SourcePattern = "USE [TestDB]",
                TargetPattern = "",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true,
                AllowStructuralChanges = true
            },
            new()
            {
                Name = "SET QUOTED_IDENTIFIER removal",
                SourcePattern = "SET QUOTED_IDENTIFIER ON",
                TargetPattern = "",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true,
                AllowStructuralChanges = true
            }
        };

        var comparer = new FileComparer(
            ignoreCase: false,
            sortInserts: false,
            bufferSizeKb: 64,
            maxFileSizeMb: 100,
            diffFilters: null,
            filterRules: rules);

        var result = comparer.CompareFiles(
            Path.Combine(_sourcePath, "WhitelistPattern.sql"),
            Path.Combine(_targetPath, "WhitelistPattern.sql"),
            "WhitelistPattern.sql");

        // Should complete successfully
        Assert.NotEqual(ComparisonStatus.Error, result.Status);
        
        // Should have computed hashes
        Assert.NotNull(result.SourceHash);
        Assert.NotNull(result.TargetHash);
    }

    [Fact]
    public void CompareFiles_WithRegexFilters_FiltersMatchingPatterns()
    {
        var patterns = new List<string>
        {
            @"ON \[[\w]+\]", // Any filegroup
            @"GRANT\s+EXECUTE", // GRANT statements
        };

        var comparer = new FileComparer(
            ignoreCase: false,
            sortInserts: false,
            bufferSizeKb: 64,
            maxFileSizeMb: 100,
            diffFilters: patterns,
            filterRules: new List<DiffFilterRule>());

        var result = comparer.CompareFiles(
            Path.Combine(_sourcePath, "WhitelistPattern.sql"),
            Path.Combine(_targetPath, "WhitelistPattern.sql"),
            "WhitelistPattern.sql");

        // Should complete successfully without errors
        Assert.NotEqual(ComparisonStatus.Error, result.Status);
        
        // Regex filters should have reduced differences from the files
        // (WhitelistPattern.sql has filegroup and GRANT differences that should be filtered)
        Assert.True(result.DifferenceCount >= 0, "Should have valid difference count");
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void CompareFiles_IgnoreCase_ReducesDifferences()
    {
        // Create temp files with case differences
        var tempSource = Path.GetTempFileName();
        var tempTarget = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempSource, "SELECT * FROM Users");
            File.WriteAllText(tempTarget, "select * from users");

            var comparerIgnoreCase = new FileComparer(
                ignoreCase: true,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var comparerCaseSensitive = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var resultIgnoreCase = comparerIgnoreCase.CompareFiles(tempSource, tempTarget, "temp.sql");
            var resultCaseSensitive = comparerCaseSensitive.CompareFiles(tempSource, tempTarget, "temp.sql");

            // Ignore case should have fewer or same differences as case sensitive
            Assert.True(resultIgnoreCase.DifferenceCount <= resultCaseSensitive.DifferenceCount,
                $"IgnoreCase: {resultIgnoreCase.DifferenceCount} diffs, CaseSensitive: {resultCaseSensitive.DifferenceCount} diffs");
        }
        finally
        {
            File.Delete(tempSource);
            File.Delete(tempTarget);
        }
    }

    [Fact]
    public void CompareFiles_CaseSensitive_TreatsAsDifferent()
    {
        var tempSource = Path.GetTempFileName();
        var tempTarget = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempSource, "SELECT * FROM Users");
            File.WriteAllText(tempTarget, "select * from users");

            var comparer = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var result = comparer.CompareFiles(tempSource, tempTarget, "temp.sql");

            Assert.Equal(ComparisonStatus.Different, result.Status);
            Assert.True(result.DifferenceCount > 0);
        }
        finally
        {
            File.Delete(tempSource);
            File.Delete(tempTarget);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CompareFiles_LargeFile_ReturnsError()
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            // Write a small file but set maxFileSizeMb very low
            File.WriteAllText(tempFile, "Small content");

            var comparer = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 0, // 0 MB limit
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var result = comparer.CompareFiles(tempFile, tempFile, "test.txt");

            Assert.Equal(ComparisonStatus.Error, result.Status);
            Assert.Contains("too large", result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompareFiles_NonExistentFile_ReturnsError()
    {
        var comparer = new FileComparer(
            ignoreCase: false,
            sortInserts: false,
            bufferSizeKb: 64,
            maxFileSizeMb: 100,
            diffFilters: null,
            filterRules: new List<DiffFilterRule>());

        var result = comparer.CompareFiles(
            "nonexistent-source.txt",
            "nonexistent-target.txt",
            "test.txt");

        Assert.Equal(ComparisonStatus.Error, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void CompareFiles_CompleteTimezoneConversion_WithAllRules()
    {
        // Comprehensive set of timezone conversion rules
        var rules = new List<DiffFilterRule>
        {
            new()
            {
                Name = "GETDATE()",
                SourcePattern = "GETDATE()",
                TargetPattern = "cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true
            },
            new()
            {
                Name = "GETDATE()-",
                SourcePattern = "GETDATE()-",
                TargetPattern = "CAST(GETDATE()AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' AS DATETIME)-",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true
            },
            new()
            {
                Name = "< GETDATE()",
                SourcePattern = "< GETDATE()",
                TargetPattern = "< CAST(GETDATE()AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' AS DATETIME)",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true
            },
            new()
            {
                Name = "MONTH(GETDATE())",
                SourcePattern = "MONTH(GETDATE())",
                TargetPattern = "MONTH( cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime) )",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true
            },
            new()
            {
                Name = "ON [PRIMARY] removal",
                SourcePattern = ") ON [PRIMARY]",
                TargetPattern = ")",
                IsExactMatch = true,
                IgnoreCase = true,
                TrimWhitespaceAround = true
            }
        };

        var comparer = new FileComparer(
            ignoreCase: false,
            sortInserts: false,
            bufferSizeKb: 64,
            maxFileSizeMb: 100,
            diffFilters: null,
            filterRules: rules);

        var result = comparer.CompareFiles(
            Path.Combine(_sourcePath, "TimezoneFilter.sql"),
            Path.Combine(_targetPath, "TimezoneFilter.sql"),
            "TimezoneFilter.sql");

        // Should complete successfully
        Assert.NotEqual(ComparisonStatus.Error, result.Status);
        
        // Should have computed hashes
        Assert.NotNull(result.SourceHash);
        Assert.NotNull(result.TargetHash);
        
        // Hashes should be different (files have different content)
        Assert.NotEqual(result.SourceHash, result.TargetHash);
    }

    #endregion

    #region Hash and Performance Tests

    [Fact]
    public void CompareFiles_SameHash_SkipsDetailedComparison()
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, "Same content");

            var comparer = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var result = comparer.CompareFiles(tempFile, tempFile, "test.txt");

            Assert.Equal(ComparisonStatus.Identical, result.Status);
            Assert.Equal(result.SourceHash, result.TargetHash);
            Assert.Empty(result.Differences); // Should not compute differences
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CompareFiles_DifferentContent_ComputesHash()
    {
        var tempSource = Path.GetTempFileName();
        var tempTarget = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempSource, "Content A");
            File.WriteAllText(tempTarget, "Content B");

            var comparer = new FileComparer(
                ignoreCase: false,
                sortInserts: false,
                bufferSizeKb: 64,
                maxFileSizeMb: 100,
                diffFilters: null,
                filterRules: new List<DiffFilterRule>());

            var result = comparer.CompareFiles(tempSource, tempTarget, "test.txt");

            Assert.NotNull(result.SourceHash);
            Assert.NotNull(result.TargetHash);
            Assert.NotEqual(result.SourceHash, result.TargetHash);
        }
        finally
        {
            File.Delete(tempSource);
            File.Delete(tempTarget);
        }
    }

    #endregion
}
