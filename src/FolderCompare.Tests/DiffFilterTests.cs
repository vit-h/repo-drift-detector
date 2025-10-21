using Xunit;
using FolderCompare.Models;
using FolderCompare.Services;

namespace FolderCompare.Tests;

public class DiffFilterTests
{
    private static Difference CreateDifference(
        DifferenceType type,
        string sourceContent,
        string targetContent,
        int sourceLine = 1,
        int targetLine = 1)
    {
        return new Difference
        {
            Type = type,
            SourceContent = sourceContent,
            TargetContent = targetContent,
            SourceLineNumber = sourceLine,
            TargetLineNumber = targetLine
        };
    }

    [Fact]
    public void ApplyRulesWithStats_ExactMatchRule_FiltersModifiedDifference()
    {
        var rule = new DiffFilterRule
        {
            Name = "GETDATE Timezone",
            SourcePattern = "GETDATE()",
            TargetPattern = "cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)",
            IsExactMatch = true,
            IgnoreCase = true,
            TrimWhitespaceAround = true
        };
        var differences = new List<Difference>
        {
            CreateDifference(
                DifferenceType.Modified,
                "WHERE CreatedDate > GETDATE()",
                "WHERE CreatedDate > cast(getdate() AT TIME ZONE 'UTC' AT TIME ZONE 'Pacific Standard Time' as datetime)")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, new List<DiffFilterRule> { rule });

        Assert.Empty(kept);
        Assert.Single(filtered);
        Assert.Equal(1, stats.TotalFiltered);
        Assert.Equal("GETDATE Timezone", filtered[0].MatchedRuleName);
    }

    [Fact]
    public void ApplyRulesWithStats_ExactMatchRule_FiltersRemovedDifference()
    {
        var rule = new DiffFilterRule
        {
            Name = "Filegroup Removal",
            SourcePattern = "ON [PRIMARY]",
            TargetPattern = "",
            IsExactMatch = true,
            IgnoreCase = true,
            TrimWhitespaceAround = true
        };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Removed, "ON [PRIMARY]", "")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, new List<DiffFilterRule> { rule });

        Assert.Empty(kept);
        Assert.Single(filtered);
        Assert.Equal("Filegroup Removal", filtered[0].MatchedRuleName);
    }

    [Fact]
    public void ApplyRulesWithStats_RegexRule_MatchesPattern()
    {
        var rule = new DiffFilterRule
        {
            Name = "Any Filegroup",
            SourcePattern = @"ON \[[\w]+\]",
            TargetPattern = "",
            IsExactMatch = false,
            IgnoreCase = true
        };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Removed, "ON [PRIMARY]", ""),
            CreateDifference(DifferenceType.Removed, "ON [Data]", ""),
            CreateDifference(DifferenceType.Removed, "ON [Index]", ""),
            CreateDifference(DifferenceType.Modified, "Different content", "Also different")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, new List<DiffFilterRule> { rule });

        Assert.Single(kept);
        Assert.Equal(3, filtered.Count);
        Assert.Equal(3, stats.TotalFiltered);
    }

    [Fact]
    public void ApplyRulesWithStats_CaseSensitiveExactMatch_OnlyMatchesExactCase()
    {
        var rule = new DiffFilterRule
        {
            Name = "Case Sensitive",
            SourcePattern = "GETDATE()",
            TargetPattern = "NOW()",
            IsExactMatch = true,
            IgnoreCase = false
        };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "GETDATE()", "NOW()"),
            CreateDifference(DifferenceType.Modified, "getdate()", "now()"),
            CreateDifference(DifferenceType.Modified, "GetDate()", "Now()")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, new List<DiffFilterRule> { rule });

        Assert.Equal(2, kept.Count);
        Assert.Single(filtered);
    }

    [Fact]
    public void ApplyRulesWithStats_CaseInsensitiveExactMatch_MatchesAllCases()
    {
        var rule = new DiffFilterRule
        {
            Name = "Case Insensitive",
            SourcePattern = "GETDATE()",
            TargetPattern = "NOW()",
            IsExactMatch = true,
            IgnoreCase = true
        };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "GETDATE()", "NOW()"),
            CreateDifference(DifferenceType.Modified, "getdate()", "now()"),
            CreateDifference(DifferenceType.Modified, "GetDate()", "Now()")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, new List<DiffFilterRule> { rule });

        Assert.Empty(kept);
        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void ApplyRulesWithStats_TrimWhitespaceAround_NormalizesWhitespace()
    {
        var rule = new DiffFilterRule
        {
            Name = "Whitespace Trim",
            SourcePattern = ") ON [PRIMARY]",
            TargetPattern = ")",
            IsExactMatch = true,
            IgnoreCase = true,
            TrimWhitespaceAround = true
        };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "  )  ON  [PRIMARY]  ", "  )  ")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, new List<DiffFilterRule> { rule });

        Assert.Empty(kept);
        Assert.Single(filtered);
    }

    [Fact]
    public void ApplyRulesWithStats_MultipleRules_TracksStatsPerRule()
    {
        var rules = new List<DiffFilterRule>
        {
            new() { Name = "Timezone", SourcePattern = "GETDATE()", TargetPattern = "CONVERTED", IsExactMatch = true, IgnoreCase = true },
            new() { Name = "Filegroup", SourcePattern = "ON [PRIMARY]", TargetPattern = "", IsExactMatch = true, IgnoreCase = true },
            new() { Name = "Semicolon", SourcePattern = ";", TargetPattern = "", IsExactMatch = true, IgnoreCase = false }
        };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "GETDATE()", "CONVERTED"),
            CreateDifference(DifferenceType.Modified, "GETDATE()", "CONVERTED"),
            CreateDifference(DifferenceType.Removed, "ON [PRIMARY]", ""),
            CreateDifference(DifferenceType.Removed, ";", ""),
            CreateDifference(DifferenceType.Modified, "Different", "Content")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, rules);

        Assert.Single(kept);
        Assert.Equal(4, filtered.Count);
        Assert.Equal(4, stats.TotalFiltered);
        Assert.Equal(2, stats.FilterCounts["Timezone"]);
        Assert.Equal(1, stats.FilterCounts["Filegroup"]);
        Assert.Equal(1, stats.FilterCounts["Semicolon"]);
    }

    [Fact]
    public void ApplyRulesWithStats_NoRules_ReturnsAllDifferences()
    {
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "Source", "Target")
        };

        var (kept, filtered, stats) = DiffFilter.ApplyRulesWithStats(differences, new List<DiffFilterRule>());

        Assert.Single(kept);
        Assert.Empty(filtered);
        Assert.Equal(0, stats.TotalFiltered);
    }

    [Fact]
    public void ApplyFilters_WithRegexPatterns_FiltersMatchingDifferences()
    {
        var patterns = new List<string> { @"GETDATE\(\)", @"ON \[PRIMARY\]" };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "SELECT GETDATE()", "SELECT NOW()"),
            CreateDifference(DifferenceType.Removed, "ON [PRIMARY]", ""),
            CreateDifference(DifferenceType.Modified, "Different content", "Also different")
        };

        var result = DiffFilter.ApplyFilters(differences, patterns);

        Assert.Single(result);
    }

    [Fact]
    public void ApplyFiltersWithTracking_TracksFilteredSeparately()
    {
        var patterns = new List<string> { @"GETDATE\(\)" };
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "SELECT GETDATE()", "SELECT NOW()"),
            CreateDifference(DifferenceType.Modified, "Different", "Content")
        };

        var (kept, filtered) = DiffFilter.ApplyFiltersWithTracking(differences, patterns);

        Assert.Single(kept);
        Assert.Single(filtered);
    }
}
