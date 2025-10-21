using FolderCompare.Models;
using FolderCompare.Utilities;

namespace FolderCompare.Services;

public class FileComparer
{
    private readonly bool _ignoreCase;
    private readonly bool _sortInserts;
    private readonly int _bufferSizeKb;
    private readonly IEnumerable<string>? _diffFilters;
    private readonly List<DiffFilterRule> _filterRules;
    private readonly long _maxFileSizeBytes;
    private readonly List<WhitelistLinePattern> _whitelistLinePatterns;
    private readonly List<WhitelistFilePattern> _whitelistFilePatterns;
    private readonly SemanticSimilarityConfig? _semanticSimilarityConfig;
    private readonly Dictionary<string, SemanticSimilarityConfig> _semanticConfigsByExtension;

    public FileComparer(
        bool ignoreCase,
        bool sortInserts,
        int bufferSizeKb,
        int maxFileSizeMb,
        IEnumerable<string>? diffFilters,
        List<DiffFilterRule> filterRules,
        List<WhitelistLinePattern>? whitelistLinePatterns = null,
        List<WhitelistFilePattern>? whitelistFilePatterns = null,
        SemanticSimilarityConfig? semanticSimilarityConfig = null,
        Dictionary<string, SemanticSimilarityConfig>? semanticConfigsByExtension = null)
    {
        _ignoreCase = ignoreCase;
        _sortInserts = sortInserts;
        _bufferSizeKb = bufferSizeKb;
        _maxFileSizeBytes = (long)maxFileSizeMb * 1024 * 1024;
        _diffFilters = diffFilters;
        _filterRules = filterRules;
        _whitelistLinePatterns = whitelistLinePatterns ?? new List<WhitelistLinePattern>();
        _whitelistFilePatterns = whitelistFilePatterns ?? new List<WhitelistFilePattern>();
        _semanticSimilarityConfig = semanticSimilarityConfig;
        _semanticConfigsByExtension = semanticConfigsByExtension ?? new Dictionary<string, SemanticSimilarityConfig>();
    }

    public FileComparisonResult CompareFiles(
        string sourceFilePath,
        string targetFilePath,
        string relativePath)
    {
        var result = new FileComparisonResult
        {
            RelativePath = relativePath,
            SourcePath = sourceFilePath,
            TargetPath = targetFilePath
        };

        try
        {
            // Check file sizes
            var sourceInfo = new FileInfo(sourceFilePath);
            var targetInfo = new FileInfo(targetFilePath);

            if (sourceInfo.Length > _maxFileSizeBytes || targetInfo.Length > _maxFileSizeBytes)
            {
                result.Status = ComparisonStatus.Error;
                result.ErrorMessage = $"File too large (max: {_maxFileSizeBytes / (1024 * 1024)} MB)";
                return result;
            }

            // Compute hashes first
            result.SourceHash = HashComparer.ComputeFileHash(sourceFilePath, _bufferSizeKb);
            result.TargetHash = HashComparer.ComputeFileHash(targetFilePath, _bufferSizeKb);

            // If hashes match, files are identical
            if (result.SourceHash == result.TargetHash)
            {
                result.Status = ComparisonStatus.Identical;
                return result;
            }

            // Check if it's a text file
            var extension = Path.GetExtension(sourceFilePath);
            if (!FileTypeDetector.IsTextFile(extension))
            {
                // Binary file with different hash
                result.Status = ComparisonStatus.Different;
                result.DifferenceCount = 1;
                return result;
            }

            // Perform line-by-line comparison for text files
            var (linesA, mapA) = TextNormalizer.NormalizeTextLinesWithMap(
                sourceFilePath, _ignoreCase, _sortInserts, _bufferSizeKb);
            var (linesB, mapB) = TextNormalizer.NormalizeTextLinesWithMap(
                targetFilePath, _ignoreCase, _sortInserts, _bufferSizeKb);

            // Check if normalized content is identical
            if (linesA.SequenceEqual(linesB))
            {
                result.Status = ComparisonStatus.IdenticalNormalized;
                return result;
            }

            // Get semantic similarity config for this file extension
            var fileExtension = Path.GetExtension(relativePath);
            var semanticConfig = GetSemanticConfigForFile(fileExtension);

            // Compute differences
            var differences = DiffEngine.ComputeDifferences(linesA, mapA, linesB, mapB, semanticConfig);

            // Apply filter rules first (exact replacements with stats)
            var (afterRules, filteredByRules, ruleStats) = DiffFilter.ApplyRulesWithStats(differences, _filterRules);
            
            // Then apply regex filters
            var (keptDifferences, filteredByRegex) = DiffFilter.ApplyFiltersWithTracking(afterRules, _diffFilters);

            // Apply whitelist patterns (line and file-level)
            var (afterWhitelist, filteredByWhitelist) = ApplyWhitelistPatterns(keptDifferences, relativePath);

            // Apply comment filtering based on file extension's semantic config
            var filteredByComments = new List<Difference>();
            var commentConfig = semanticConfig?.CommentConfig;
            if (commentConfig != null && commentConfig.IgnoreComments)
            {
                var (afterComments, commentDiffs) = CommentFilter.FilterComments(afterWhitelist, commentConfig, linesA, linesB);
                afterWhitelist = afterComments;
                filteredByComments = commentDiffs;
            }

            // Combine all filtered diffs
            var allFiltered = new List<Difference>();
            allFiltered.AddRange(filteredByRules);
            allFiltered.AddRange(filteredByRegex);
            allFiltered.AddRange(filteredByWhitelist);
            allFiltered.AddRange(filteredByComments);

            // Check for structural changes (Added/Removed lines)
            var hasUnfilteredStructuralChanges = afterWhitelist.Any(d => 
                d.Type == DifferenceType.Added || d.Type == DifferenceType.Removed);
            
            // Check if filtered structural changes are from rules that DON'T allow them
            // Note: Rules with empty source or target automatically allow structural changes
            var hasDisallowedStructuralChanges = filteredByRules.Any(d => 
                (d.Type == DifferenceType.Added || d.Type == DifferenceType.Removed) &&
                d.MatchedRuleName != null &&
                !_filterRules.Any(r => r.Name == d.MatchedRuleName && 
                    (r.AllowStructuralChanges || 
                     string.IsNullOrEmpty(r.SourcePattern) || 
                     string.IsNullOrEmpty(r.TargetPattern))));

            result.Differences = afterWhitelist;
            result.FilteredDifferences = allFiltered;
            
            // Combine rule stats with whitelist stats
            var combinedStats = new Dictionary<string, int>(ruleStats.FilterCounts);
            foreach (var diff in filteredByWhitelist.Where(d => d.MatchedRuleName != null))
            {
                var ruleName = diff.MatchedRuleName!;
                combinedStats[ruleName] = combinedStats.ContainsKey(ruleName) 
                    ? combinedStats[ruleName] + 1 
                    : 1;
            }
            
            result.FilterStats = combinedStats;
            result.DifferenceCount = afterWhitelist.Count;
            
            // Mark as Different if:
            // 1. Has unfiltered differences, OR
            // 2. Has filtered structural changes from rules that don't allow them
            if (afterWhitelist.Count > 0)
            {
                result.Status = ComparisonStatus.Different;
            }
            else if (hasDisallowedStructuralChanges)
            {
                // Filtered structural changes from non-safe rules - still mark as Different
                result.Status = ComparisonStatus.Different;
                result.Differences = filteredByRules.Where(d => 
                    d.Type == DifferenceType.Added || d.Type == DifferenceType.Removed).ToList();
                result.FilteredDifferences = allFiltered.Where(d => d.Type == DifferenceType.Modified).ToList();
                result.DifferenceCount = result.Differences.Count;
            }
            else
            {
                // All differences were filtered by safe rules - file is identical after normalization
                result.Status = ComparisonStatus.IdenticalNormalized;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Status = ComparisonStatus.Error;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private (List<Difference> kept, List<Difference> filtered) ApplyWhitelistPatterns(
        List<Difference> differences, 
        string relativePath)
    {
        if (!_whitelistLinePatterns.Any() && !_whitelistFilePatterns.Any())
            return (differences, new List<Difference>());

        var kept = new List<Difference>();
        var filtered = new List<Difference>();

        // Check if file matches any file-level whitelist patterns
        var matchingFilePattern = _whitelistFilePatterns.FirstOrDefault(p => 
            MatchesGlobPattern(relativePath, p.Pattern));

        foreach (var diff in differences)
        {
            bool shouldFilter = false;
            string? matchedWhitelistName = null;

            // Check line-level whitelist patterns
            foreach (var pattern in _whitelistLinePatterns)
            {
                bool sourceMatches = diff.SourceContent?.Contains(pattern.Contains, StringComparison.OrdinalIgnoreCase) == true;
                bool targetMatches = diff.TargetContent?.Contains(pattern.Contains, StringComparison.OrdinalIgnoreCase) == true;

                // Check if the pattern applies based on existence rules
                if ((pattern.ExistsInSource && sourceMatches && !pattern.ExistsInTarget && !targetMatches) ||
                    (!pattern.ExistsInSource && !sourceMatches && pattern.ExistsInTarget && targetMatches))
                {
                    shouldFilter = true;
                    matchedWhitelistName = $"[Whitelist] {pattern.Name}";
                    break;
                }
            }

            // Check file-level whitelist patterns
            if (!shouldFilter && matchingFilePattern != null)
            {
                // File pattern applies - check if this difference type is allowed
                if ((diff.Type == DifferenceType.Removed && matchingFilePattern.AllowLineMissingInSource) ||
                    (diff.Type == DifferenceType.Added && matchingFilePattern.AllowLineMissingInTarget) ||
                    (diff.Type == DifferenceType.Modified && matchingFilePattern.AllowModified))
                {
                    shouldFilter = true;
                    matchedWhitelistName = $"[Whitelist] {matchingFilePattern.Name}";
                }
            }

            if (shouldFilter)
            {
                diff.MatchedRuleName = matchedWhitelistName;
                filtered.Add(diff);
            }
            else
            {
                kept.Add(diff);
            }
        }

        return (kept, filtered);
    }

    /// <summary>
    /// Get the semantic similarity config for a given file extension.
    /// Priority: 1) Inline config (override all), 2) Extension-specific config
    /// </summary>
    private SemanticSimilarityConfig? GetSemanticConfigForFile(string fileExtension)
    {
        // Inline config takes highest priority (overrides everything)
        if (_semanticSimilarityConfig != null)
        {
            return _semanticSimilarityConfig;
        }

        // Check if we have a config for this extension
        if (_semanticConfigsByExtension.TryGetValue(fileExtension, out var config))
        {
            return config;
        }

        // No config found for this extension
        return null;
    }

    private static bool MatchesGlobPattern(string path, string pattern)
    {
        // Simple glob pattern matching (* = wildcard)
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

