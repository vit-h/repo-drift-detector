using System.Text.Json.Serialization;

namespace FolderCompare.Models;

/// <summary>
/// Configuration for folder comparison that can be loaded from JSON
/// </summary>
public class ComparisonConfig
{
    [JsonPropertyName("sourcePath")]
    public string SourcePath { get; set; } = string.Empty;

    [JsonPropertyName("targetPath")]
    public string TargetPath { get; set; } = string.Empty;

    [JsonPropertyName("outputPath")]
    public string OutputPath { get; set; } = ".";

    [JsonPropertyName("includeExtensions")]
    public List<string>? IncludeExtensions { get; set; }

    [JsonPropertyName("excludeExtensions")]
    public List<string>? ExcludeExtensions { get; set; }

    [JsonPropertyName("ignoreFolders")]
    public List<string>? IgnoreFolders { get; set; }

    [JsonPropertyName("ignoreCase")]
    public bool IgnoreCase { get; set; } = false;

    [JsonPropertyName("sortInserts")]
    public bool SortInserts { get; set; } = false;

    [JsonPropertyName("maxFileSize")]
    [Obsolete("Use MaxFileSizeMb instead")]
    public int MaxFileSize { get; set; } = 100;

    [JsonPropertyName("maxFileSizeMb")]
    public int MaxFileSizeMb { get; set; } = 100;

    [JsonPropertyName("diffFilters")]
    public List<string>? DiffFilters { get; set; }

    [JsonPropertyName("substitutionRules")]
    [Obsolete("Use AllowedSubstitutions instead")]
    public List<SubstitutionRule>? SubstitutionRules { get; set; }

    [JsonPropertyName("allowedSubstitutions")]
    public List<SubstitutionRule>? AllowedSubstitutions { get; set; }

    [JsonPropertyName("showFilteredDifferences")]
    [Obsolete("Filtered differences are now always shown when reportMatched is true")]
    public bool ShowFilteredDifferences { get; set; } = false;

    [JsonPropertyName("maxThreads")]
    public int MaxThreads { get; set; } = 0;

    [JsonPropertyName("bufferSize")]
    public int BufferSize { get; set; } = 64;

    [JsonPropertyName("whitelistLinePatterns")]
    public List<WhitelistLinePattern>? WhitelistLinePatterns { get; set; }

    [JsonPropertyName("whitelistFilePatterns")]
    public List<WhitelistFilePattern>? WhitelistFilePatterns { get; set; }

    /// <summary>
    /// Inline semantic similarity configuration. 
    /// If specified, this overrides any convention-based configs loaded by file extension.
    /// </summary>
    [JsonPropertyName("semanticSimilarity")]
    public SemanticSimilarityConfig? SemanticSimilarity { get; set; }

    /// <summary>
    /// Directory containing semantic config files for automatic loading by extension.
    /// Default: "ConfigTemplates" 
    /// Convention: {extension}.semantic-config.json (e.g., sql.semantic-config.json for .sql files)
    /// </summary>
    [JsonPropertyName("semanticConfigDirectory")]
    public string? SemanticConfigDirectory { get; set; } = "ConfigTemplates";

    /// <summary>
    /// Internal: Loaded semantic configs per file extension (populated at runtime)
    /// Key: file extension (e.g., ".sql"), Value: config for that extension
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, SemanticSimilarityConfig> SemanticConfigsByExtension { get; set; } = new();
}

/// <summary>
/// Configuration for semantic similarity detection in diff comparison.
/// Controls how lines are compared to determine if they're modifications or separate add/remove operations.
/// </summary>
public class SemanticSimilarityConfig
{
    /// <summary>
    /// Minimum similarity threshold (0.0-1.0). Lines below this threshold are treated as separate operations.
    /// Default: 0.40 (40% similarity required to be considered a modification)
    /// </summary>
    [JsonPropertyName("threshold")]
    public double Threshold { get; set; } = 0.40;

    /// <summary>
    /// Regex patterns to extract semantic identifiers from lines.
    /// If identifiers differ, lines are treated as separate operations regardless of similarity.
    /// </summary>
    [JsonPropertyName("identifierPatterns")]
    public List<IdentifierPattern>? IdentifierPatterns { get; set; }

    /// <summary>
    /// Common names to ignore when comparing identifiers (schemas, keywords, etc.)
    /// </summary>
    [JsonPropertyName("commonIdentifiers")]
    public List<string>? CommonIdentifiers { get; set; }

    /// <summary>
    /// Delimiters used for token-based similarity calculation.
    /// Default: space, tab, parentheses, brackets, punctuation
    /// </summary>
    [JsonPropertyName("tokenDelimiters")]
    public List<string>? TokenDelimiters { get; set; }

    /// <summary>
    /// Weight for LCS (Longest Common Subsequence) similarity (0.0-1.0).
    /// Default: 0.70 (70% weight for character-level similarity)
    /// </summary>
    [JsonPropertyName("lcsWeight")]
    public double LcsWeight { get; set; } = 0.70;

    /// <summary>
    /// Weight for token-based similarity (0.0-1.0).
    /// Default: 0.30 (30% weight for structural similarity)
    /// </summary>
    [JsonPropertyName("tokenWeight")]
    public double TokenWeight { get; set; } = 0.30;

    /// <summary>
    /// Configuration for comment filtering
    /// </summary>
    [JsonPropertyName("commentConfig")]
    public CommentConfig? CommentConfig { get; set; }
}

/// <summary>
/// Configuration for filtering comment-only differences
/// </summary>
public class CommentConfig
{
    /// <summary>
    /// Enable filtering of comment-only differences
    /// </summary>
    [JsonPropertyName("ignoreComments")]
    public bool IgnoreComments { get; set; } = false;

    /// <summary>
    /// Single-line comment pattern (e.g., "-- " for SQL, "// " for C#)
    /// </summary>
    [JsonPropertyName("singleLinePattern")]
    public string? SingleLinePattern { get; set; }

    /// <summary>
    /// Multi-line comment start pattern (e.g., "/*" for SQL/C#)
    /// </summary>
    [JsonPropertyName("multiLineStartPattern")]
    public string? MultiLineStartPattern { get; set; }

    /// <summary>
    /// Multi-line comment end pattern (e.g., "*/" for SQL/C#)
    /// </summary>
    [JsonPropertyName("multiLineEndPattern")]
    public string? MultiLineEndPattern { get; set; }

    /// <summary>
    /// When true, also ignores empty lines and whitespace-only lines during comparison
    /// </summary>
    [JsonPropertyName("ignoreEmptyLines")]
    public bool IgnoreEmptyLines { get; set; } = true;
}

/// <summary>
/// Regex pattern for extracting semantic identifiers from code lines.
/// Used to detect when two lines represent different objects (e.g., different SQL indexes).
/// </summary>
public class IdentifierPattern
{
    /// <summary>
    /// Name/description of this pattern (e.g., "SQL CREATE INDEX")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Regex pattern to match and extract identifier.
    /// Use capture group 1 for the primary identifier to compare.
    /// Example: CREATE\s+INDEX\s+\[([^\]]+)\] captures the index name
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Regex options (e.g., "IgnoreCase", "Multiline")
    /// </summary>
    [JsonPropertyName("options")]
    public string Options { get; set; } = "IgnoreCase";

    /// <summary>
    /// Priority order (lower number = higher priority). Patterns are checked in priority order.
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 100;
}

public class WhitelistLinePattern
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contains")]
    public string Contains { get; set; } = string.Empty;

    [JsonPropertyName("existsInSource")]
    public bool ExistsInSource { get; set; } = true;

    [JsonPropertyName("existsInTarget")]
    public bool ExistsInTarget { get; set; } = true;
}

public class WhitelistFilePattern
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    [JsonPropertyName("allowLineMissingInSource")]
    public bool AllowLineMissingInSource { get; set; } = false;

    [JsonPropertyName("allowLineMissingInTarget")]
    public bool AllowLineMissingInTarget { get; set; } = false;

    [JsonPropertyName("allowModified")]
    public bool AllowModified { get; set; } = false;
}

public class SubstitutionRule
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("ignoreCase")]
    public bool IgnoreCase { get; set; } = false;

    [JsonPropertyName("trimWhitespaceAround")]
    public bool TrimWhitespaceAround { get; set; } = false;

    [JsonPropertyName("normalizeInternalWhitespace")]
    public bool NormalizeInternalWhitespace { get; set; } = false;

    [JsonPropertyName("reportMatched")]
    public bool ReportMatched { get; set; } = false;

    [JsonPropertyName("allowStructuralChanges")]
    public bool AllowStructuralChanges { get; set; } = false;
}

