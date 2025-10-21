using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FolderCompare.Models;

namespace FolderCompare.Services;

public static class DiffFilter
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public static List<Difference> ApplyFilters(
        List<Difference> differences,
        IEnumerable<string>? filterPatterns)
    {
        if (filterPatterns == null || !filterPatterns.Any())
            return differences;

        var compiledRegexes = filterPatterns
            .Select(p => RegexCache.GetOrAdd(p, pattern =>
                new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)))
            .ToList();

        return differences
            .Where(d => !ShouldFilter(d, compiledRegexes))
            .ToList();
    }

    public static (List<Difference> kept, List<Difference> filtered) ApplyFiltersWithTracking(
        List<Difference> differences,
        IEnumerable<string>? filterPatterns)
    {
        if (filterPatterns == null || !filterPatterns.Any())
            return (differences, new List<Difference>());

        var compiledRegexes = filterPatterns
            .Select(p => RegexCache.GetOrAdd(p, pattern =>
                new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)))
            .ToList();

        var kept = new List<Difference>();
        var filtered = new List<Difference>();

        foreach (var diff in differences)
        {
            if (ShouldFilter(diff, compiledRegexes))
                filtered.Add(diff);
            else
                kept.Add(diff);
        }

        return (kept, filtered);
    }

    public static (List<Difference> kept, List<Difference> filtered, DiffFilterStats stats) 
        ApplyRulesWithStats(
            List<Difference> differences,
            List<DiffFilterRule> rules)
    {
        if (rules == null || !rules.Any())
            return (differences, new List<Difference>(), new DiffFilterStats());

        var kept = new List<Difference>();
        var filtered = new List<Difference>();
        var stats = new DiffFilterStats();

        foreach (var diff in differences)
        {
            var matchedRule = FindMatchingRule(diff, rules);
            if (matchedRule != null)
            {
                // Set the matched rule name on the difference
                diff.MatchedRuleName = matchedRule.Name;
                filtered.Add(diff);
                
                // Track stats
                if (!stats.FilterCounts.ContainsKey(matchedRule.Name))
                    stats.FilterCounts[matchedRule.Name] = 0;
                
                stats.FilterCounts[matchedRule.Name]++;
                stats.TotalFiltered++;
            }
            else
            {
                kept.Add(diff);
            }
        }

        return (kept, filtered, stats);
    }

    private static DiffFilterRule? FindMatchingRule(Difference diff, List<DiffFilterRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.IsExactMatch)
            {
                // Exact string replacement matching
                if (IsExactReplacementMatch(diff, rule))
                    return rule;
            }
            else
            {
                // Regex matching
                var regex = RegexCache.GetOrAdd(rule.SourcePattern, pattern =>
                    new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                
                if (!string.IsNullOrEmpty(diff.SourceContent) && regex.IsMatch(diff.SourceContent))
                    return rule;
                
                if (!string.IsNullOrEmpty(diff.TargetContent) && regex.IsMatch(diff.TargetContent))
                    return rule;
            }
        }

        return null;
    }

    private static bool IsExactReplacementMatch(Difference diff, DiffFilterRule rule)
    {
        // For exact replacement: check if the only difference is the replacement pattern
        var source = diff.SourceContent ?? "";
        var target = diff.TargetContent ?? "";

        var comparison = rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Handle Modified type (both source and target have content)
        if (diff.Type == DifferenceType.Modified)
        {
            // Special case: Empty source pattern means we're checking if target was added to source
            // e.g., source="" target=";" means we want to match when source has no semicolon but target does
            if (string.IsNullOrEmpty(rule.SourcePattern) && !string.IsNullOrEmpty(rule.TargetPattern))
            {
                // Check if source + target pattern = target (i.e., target pattern was appended)
                var sourceWithTarget = source + rule.TargetPattern;
                if (rule.TrimWhitespaceAround)
                {
                    sourceWithTarget = NormalizeWhitespace(sourceWithTarget);
                    target = NormalizeWhitespace(target);
                }
                return sourceWithTarget.Equals(target, comparison);
            }
            
            // Special case: Empty target pattern means we're checking if source pattern was removed
            // e.g., source=";" target="" means we want to match when source has semicolon but target doesn't
            if (string.IsNullOrEmpty(rule.TargetPattern) && !string.IsNullOrEmpty(rule.SourcePattern))
            {
                // Check if source - source pattern = target (i.e., source pattern was removed)
                var sourceWithoutPattern = source.Replace(rule.SourcePattern, "", comparison);
                if (rule.TrimWhitespaceAround)
                {
                    sourceWithoutPattern = NormalizeWhitespace(sourceWithoutPattern);
                    target = NormalizeWhitespace(target);
                }
                return sourceWithoutPattern.Equals(target, comparison);
            }
            
            // Normal case: Both patterns are non-empty
            if (!string.IsNullOrEmpty(rule.SourcePattern) && !string.IsNullOrEmpty(rule.TargetPattern))
            {
                // Try replacing source pattern with target pattern in source
                var normalizedSource = source.Replace(rule.SourcePattern, rule.TargetPattern, comparison);
                
                // Also try the reverse (target pattern to source pattern in target)
                var normalizedTarget = target.Replace(rule.TargetPattern, rule.SourcePattern, comparison);

                // If trimWhitespaceAround is enabled, normalize whitespace after replacement
                if (rule.TrimWhitespaceAround)
                {
                    normalizedSource = NormalizeWhitespace(normalizedSource);
                    normalizedTarget = NormalizeWhitespace(normalizedTarget);
                    source = NormalizeWhitespace(source);
                    target = NormalizeWhitespace(target);
                }
                
                // If normalizeInternalWhitespace is enabled, normalize internal spaces
                if (rule.NormalizeInternalWhitespace)
                {
                    normalizedSource = NormalizeInternalWhitespace(normalizedSource);
                    normalizedTarget = NormalizeInternalWhitespace(normalizedTarget);
                    source = NormalizeInternalWhitespace(source);
                    target = NormalizeInternalWhitespace(target);
                }

                // If after replacement they match, this diff is just the allowed substitution
                return normalizedSource.Equals(target, comparison) || source.Equals(normalizedTarget, comparison);
            }
        }

        // Handle Removed type (only source has content) - check if it matches source after substitution
        if (diff.Type == DifferenceType.Removed && !string.IsNullOrEmpty(source))
        {
            // Only match if we have a non-empty source pattern (we're looking for removal of something)
            if (string.IsNullOrEmpty(rule.SourcePattern))
                return false;
                
            var checkSource = source;
            if (rule.TrimWhitespaceAround)
                checkSource = NormalizeWhitespace(checkSource);
            if (rule.NormalizeInternalWhitespace)
                checkSource = NormalizeInternalWhitespace(checkSource);
            return checkSource.Contains(rule.SourcePattern, comparison);
        }

        // Handle Added type (only target has content) - check if it matches target after substitution  
        if (diff.Type == DifferenceType.Added && !string.IsNullOrEmpty(target))
        {
            // Only match if we have a non-empty target pattern (we're looking for addition of something)
            if (string.IsNullOrEmpty(rule.TargetPattern))
                return false;
                
            var checkTarget = target;
            if (rule.TrimWhitespaceAround)
                checkTarget = NormalizeWhitespace(checkTarget);
            if (rule.NormalizeInternalWhitespace)
                checkTarget = NormalizeInternalWhitespace(checkTarget);
            return checkTarget.Contains(rule.TargetPattern, comparison);
        }

        return false;
    }

    private static bool ShouldFilter(Difference diff, List<Regex> regexes)
    {
        foreach (var regex in regexes)
        {
            if (!string.IsNullOrEmpty(diff.SourceContent) && regex.IsMatch(diff.SourceContent))
                return true;

            if (!string.IsNullOrEmpty(diff.TargetContent) && regex.IsMatch(diff.TargetContent))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Normalize internal whitespace by replacing multiple consecutive spaces with a single space
    /// </summary>
    private static string NormalizeInternalWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Replace multiple spaces with single space
        return System.Text.RegularExpressions.Regex.Replace(text, @"\s{2,}", " ");
    }

    /// <summary>
    /// Legacy method for comma-aware whitespace normalization (kept for backward compatibility)
    /// </summary>
    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Normalize whitespace around commas (space before comma should be removed)
        var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"\s+,", ",");
        
        // Normalize whitespace after commas (ensure single space)
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @",\s*", ", ");
        
        // Normalize whitespace around equals signs (remove spaces)
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*=\s*", "=");
        
        // Normalize whitespace before semicolons (remove spaces)
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+;", ";");
        
        // Normalize whitespace before closing parentheses/brackets
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+\)", ")");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+\]", "]");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+\}", "}");
        
        // Normalize whitespace after opening parentheses/brackets  
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\(\s+", "(");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\[\s+", "[");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\{\s+", "{");
        
        // Normalize multiple spaces to single space
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s{2,}", " ");
        
        // Trim the entire line
        normalized = normalized.Trim();
        
        return normalized;
    }
}
