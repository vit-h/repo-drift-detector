namespace FolderCompare.Models;

public enum ComparisonStatus
{
    Identical,              // Byte-identical (hash match)
    IdenticalNormalized,    // Content identical after normalization
    Different,              // Content differs
    OnlyInSource,           // File exists only in source
    OnlyInTarget,           // File exists only in target
    Error                   // Error during processing
}

public enum DifferenceType
{
    Added,      // Line exists in target but not in source
    Removed,    // Line exists in source but not in target
    Modified    // Line exists in both but content differs
}

public class Difference
{
    public DifferenceType Type { get; set; }
    public int SourceLineNumber { get; set; }
    public int TargetLineNumber { get; set; }
    public string SourceContent { get; set; } = string.Empty;
    public string TargetContent { get; set; } = string.Empty;
    public string? MatchedRuleName { get; set; } // Name of the filter rule that matched this difference
}

public class FileComparisonResult
{
    public string RelativePath { get; set; } = string.Empty;
    public ComparisonStatus Status { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string? SourceHash { get; set; }
    public string? TargetHash { get; set; }
    public int DifferenceCount { get; set; }
    public List<Difference> Differences { get; set; } = new();
    public List<Difference> FilteredDifferences { get; set; } = new(); // Differences that were filtered out
    public Dictionary<string, int> FilterStats { get; set; } = new(); // Count per filter rule
    public string? ErrorMessage { get; set; }
}
