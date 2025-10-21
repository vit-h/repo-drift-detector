using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FolderCompare.Models;
using System.Text.RegularExpressions;

namespace FolderCompare.Services;

public static class DiffEngine
{
    private static readonly Differ Differ = new();
    
    // Default similarity threshold if not configured
    private const double DefaultSimilarityThreshold = 0.40;

    public static List<Difference> ComputeDifferences(
        List<string> normalizedA,
        Dictionary<int, int> lineMapA,
        List<string> normalizedB,
        Dictionary<int, int> lineMapB,
        SemanticSimilarityConfig? config = null)
    {
        var differences = new List<Difference>();
        
        // Use configured threshold or default
        double similarityThreshold = config?.Threshold ?? DefaultSimilarityThreshold;

        // Quick check for equality
        if (normalizedA.SequenceEqual(normalizedB))
            return differences;

        // Build full text for diff
        var textA = string.Join('\n', normalizedA);
        var textB = string.Join('\n', normalizedB);

        // Use SideBySideDiffBuilder for better line-by-line alignment
        var builder = new SideBySideDiffBuilder(Differ);
        var diff = builder.BuildDiffModel(textA, textB, ignoreWhitespace: false);

        // Process old (left/source) side
        var oldLines = diff.OldText.Lines;
        var newLines = diff.NewText.Lines;
        
        // Match up lines by index - SideBySide aligns them properly
        int maxLines = Math.Max(oldLines.Count, newLines.Count);
        
        for (int i = 0; i < maxLines; i++)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = i < newLines.Count ? newLines[i] : null;
            
            // Determine the type based on both sides
            if (oldLine?.Type == ChangeType.Deleted && newLine?.Type == ChangeType.Inserted)
            {
                // Check if these lines are actually related (similar enough to be a modification)
                var oldText = oldLine.Text ?? string.Empty;
                var newText = newLine.Text ?? string.Empty;
                
                double similarity = CalculateSimilarity(oldText, newText, config);
                
                if (similarity >= similarityThreshold)
                {
                    // Lines are similar enough - treat as a modification
                    differences.Add(new Difference
                    {
                        Type = DifferenceType.Modified,
                        SourceLineNumber = lineMapA.ContainsKey(i) ? lineMapA[i] : i + 1,
                        SourceContent = oldText,
                        TargetLineNumber = lineMapB.ContainsKey(i) ? lineMapB[i] : i + 1,
                        TargetContent = newText
                    });
                }
                else
                {
                    // Lines are too different - treat as separate delete and insert
                    differences.Add(new Difference
                    {
                        Type = DifferenceType.Removed,
                        SourceLineNumber = lineMapA.ContainsKey(i) ? lineMapA[i] : i + 1,
                        SourceContent = oldText,
                        TargetLineNumber = 0,
                        TargetContent = string.Empty
                    });
                    
                    differences.Add(new Difference
                    {
                        Type = DifferenceType.Added,
                        SourceLineNumber = 0,
                        SourceContent = string.Empty,
                        TargetLineNumber = lineMapB.ContainsKey(i) ? lineMapB[i] : i + 1,
                        TargetContent = newText
                    });
                }
            }
            else if (oldLine?.Type == ChangeType.Deleted)
            {
                // Line only in source (removed)
                differences.Add(new Difference
                {
                    Type = DifferenceType.Removed,
                    SourceLineNumber = lineMapA.ContainsKey(i) ? lineMapA[i] : i + 1,
                    SourceContent = oldLine.Text ?? string.Empty,
                    TargetLineNumber = 0,
                    TargetContent = string.Empty
                });
            }
            else if (newLine?.Type == ChangeType.Inserted)
            {
                // Line only in target (added)
                differences.Add(new Difference
                {
                    Type = DifferenceType.Added,
                    SourceLineNumber = 0,
                    SourceContent = string.Empty,
                    TargetLineNumber = lineMapB.ContainsKey(i) ? lineMapB[i] : i + 1,
                    TargetContent = newLine.Text ?? string.Empty
                });
            }
            else if (oldLine?.Type == ChangeType.Modified || newLine?.Type == ChangeType.Modified)
            {
                // Modified line (DiffPlex detected character-level changes)
                differences.Add(new Difference
                {
                    Type = DifferenceType.Modified,
                    SourceLineNumber = lineMapA.ContainsKey(i) ? lineMapA[i] : i + 1,
                    SourceContent = oldLine?.Text ?? string.Empty,
                    TargetLineNumber = lineMapB.ContainsKey(i) ? lineMapB[i] : i + 1,
                    TargetContent = newLine?.Text ?? string.Empty
                });
            }
            // Skip Unchanged lines
        }

        return differences;
    }
    
    /// <summary>
    /// Calculate similarity between two strings using a combination of:
    /// 1. Longest Common Subsequence (LCS) ratio for fast initial check
    /// 2. Semantic identifier similarity for meaningful names (SQL objects, variables, etc.)
    /// 3. Common token ratio for structural similarity
    /// Returns a value between 0.0 (completely different) and 1.0 (identical)
    /// </summary>
    private static double CalculateSimilarity(string text1, string text2, SemanticSimilarityConfig? config)
    {
        if (string.IsNullOrEmpty(text1) && string.IsNullOrEmpty(text2))
            return 1.0;
        
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;
        
        if (text1 == text2)
            return 1.0;
        
        // Special check: If the lines contain different named objects (indexes, procedures, etc.)
        // they should be treated as different lines, not modifications
        if (HasDifferentSemanticIdentifiers(text1, text2, config))
            return 0.0; // Force separate Add/Remove instead of Modified
        
        // Calculate LCS-based similarity (fast and effective)
        int lcsLength = LongestCommonSubsequenceLength(text1, text2);
        int maxLength = Math.Max(text1.Length, text2.Length);
        double lcsSimilarity = (double)lcsLength / maxLength;
        
        // Calculate token-based similarity (for structural similarity)
        // This helps identify lines that share key identifiers/keywords
        double tokenSimilarity = CalculateTokenSimilarity(text1, text2, config);
        
        // Use configured weights or defaults (70% LCS, 30% token)
        double lcsWeight = config?.LcsWeight ?? 0.70;
        double tokenWeight = config?.TokenWeight ?? 0.30;
        
        // Weighted average
        return (lcsSimilarity * lcsWeight) + (tokenSimilarity * tokenWeight);
    }
    
    /// <summary>
    /// Check if two lines have different semantic identifiers that make them fundamentally different.
    /// This handles cases like:
    /// - CREATE INDEX [IX_Table_ColumnA] vs CREATE INDEX [IX_Table_ColumnB]  
    /// - CREATE PROCEDURE dbo.ProcA vs CREATE PROCEDURE dbo.ProcB
    /// Returns true if the identifiers are different (should be separate Add/Remove, not Modified)
    /// </summary>
    private static bool HasDifferentSemanticIdentifiers(string text1, string text2, SemanticSimilarityConfig? config)
    {
        // If no config provided, semantic detection is disabled
        if (config == null)
            return false;

        // If no patterns defined, semantic detection is disabled
        var patterns = config.IdentifierPatterns?.OrderBy(p => p.Priority).ToList();
        if (patterns == null || patterns.Count == 0)
            return false;
        
        // Check each pattern in priority order
        foreach (var patternConfig in patterns)
        {
            try
            {
                var options = ParseRegexOptions(patternConfig.Options);
                var regex = new Regex(patternConfig.Pattern, options);
                
                var match1 = regex.Match(text1);
                var match2 = regex.Match(text2);
                
                // If both lines match this pattern, compare the captured identifiers
                if (match1.Success && match2.Success && match1.Groups.Count > 1 && match2.Groups.Count > 1)
                {
                    var identifier1 = match1.Groups[1].Value;
                    var identifier2 = match2.Groups[1].Value;
                    
                    // Check if identifiers are in the common names list
                    var commonIds = config.CommonIdentifiers ?? new List<string>();
                    
                    bool id1IsCommon = commonIds.Any(c => string.Equals(c, identifier1, StringComparison.OrdinalIgnoreCase));
                    bool id2IsCommon = commonIds.Any(c => string.Equals(c, identifier2, StringComparison.OrdinalIgnoreCase));
                    
                    // If both are common identifiers, skip this pattern (not distinctive)
                    if (id1IsCommon && id2IsCommon)
                        continue;
                    
                    // Compare identifiers (case-insensitive)
                    if (!string.Equals(identifier1, identifier2, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Different identifiers found
                    }
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern in config - skip it
                continue;
            }
        }
        
        return false; // No semantic difference detected
    }
    
    /// <summary>
    /// Parse regex options string into RegexOptions enum
    /// </summary>
    private static RegexOptions ParseRegexOptions(string optionsString)
    {
        if (string.IsNullOrWhiteSpace(optionsString))
            return RegexOptions.None;
        
        var options = RegexOptions.None;
        var parts = optionsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        foreach (var part in parts)
        {
            if (Enum.TryParse<RegexOptions>(part, true, out var option))
            {
                options |= option;
            }
        }
        
        return options;
    }
    
    /// <summary>
    /// Calculate the length of the Longest Common Subsequence using dynamic programming.
    /// This is efficient and gives good results for line comparison.
    /// Based on the Wagner-Fischer algorithm mentioned in research.
    /// </summary>
    private static int LongestCommonSubsequenceLength(string text1, string text2)
    {
        int m = text1.Length;
        int n = text2.Length;
        
        // Use two rows instead of full matrix to save memory (as per research best practices)
        int[] previousRow = new int[n + 1];
        int[] currentRow = new int[n + 1];
        
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (text1[i - 1] == text2[j - 1])
                {
                    currentRow[j] = previousRow[j - 1] + 1;
                }
                else
                {
                    currentRow[j] = Math.Max(previousRow[j], currentRow[j - 1]);
                }
            }
            
            // Swap rows for next iteration
            var temp = previousRow;
            previousRow = currentRow;
            currentRow = temp;
            Array.Clear(currentRow, 0, currentRow.Length);
        }
        
        return previousRow[n];
    }
    
    /// <summary>
    /// Calculate similarity based on common tokens (words/identifiers).
    /// This helps identify lines that share the same structure/keywords even if character positions differ.
    /// Uses Jaccard similarity coefficient on token sets.
    /// </summary>
    private static double CalculateTokenSimilarity(string text1, string text2, SemanticSimilarityConfig? config)
    {
        // Get delimiters from config or use defaults
        char[] delimiters = GetTokenDelimiters(config);
        
        var tokens1 = new HashSet<string>(
            text1.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0),
            StringComparer.OrdinalIgnoreCase
        );
        
        var tokens2 = new HashSet<string>(
            text2.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0),
            StringComparer.OrdinalIgnoreCase
        );
        
        if (tokens1.Count == 0 && tokens2.Count == 0)
            return 1.0;
        
        if (tokens1.Count == 0 || tokens2.Count == 0)
            return 0.0;
        
        // Calculate Jaccard similarity: |intersection| / |union|
        int intersectionCount = tokens1.Intersect(tokens2).Count();
        int unionCount = tokens1.Union(tokens2).Count();
        
        return (double)intersectionCount / unionCount;
    }
    
    /// <summary>
    /// Get token delimiters from config or return defaults
    /// </summary>
    private static char[] GetTokenDelimiters(SemanticSimilarityConfig? config)
    {
        if (config?.TokenDelimiters != null && config.TokenDelimiters.Count > 0)
        {
            // Convert strings to chars (take first char of each string)
            return config.TokenDelimiters
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s[0])
                .ToArray();
        }
        
        // If no config or no delimiters, return empty array (no token splitting)
        return Array.Empty<char>();
    }
}
