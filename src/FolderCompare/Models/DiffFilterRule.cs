namespace FolderCompare.Models;

public class DiffFilterRule
{
    public string Name { get; set; } = string.Empty;
    public string SourcePattern { get; set; } = string.Empty;
    public string TargetPattern { get; set; } = string.Empty;
    public bool IsExactMatch { get; set; } = true; // true for exact string replacement, false for regex
    public bool IgnoreCase { get; set; } = false;
    public bool TrimWhitespaceAround { get; set; } = false; // Trim whitespace around the patterns before comparison
    public bool NormalizeInternalWhitespace { get; set; } = false; // Normalize internal whitespace (multiple spaces to single)
    public bool ReportMatched { get; set; } = false; // If true, show matched differences in report
    public bool AllowStructuralChanges { get; set; } = false; // If true, Added/Removed differences from this rule don't prevent "Identical" status
    public int FilteredCount { get; set; } = 0;
    public HashSet<string> FilteredFiles { get; set; } = new();
}

public class DiffFilterStats
{
    public Dictionary<string, int> FilterCounts { get; set; } = new();
    public int TotalFiltered { get; set; } = 0;
}
