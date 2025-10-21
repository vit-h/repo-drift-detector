using System.Text.RegularExpressions;
using FolderCompare.Models;

namespace FolderCompare.Services;

/// <summary>
/// Filters differences that are only comment changes
/// </summary>
public static class CommentFilter
{
    private static readonly Dictionary<string, Regex> SingleLineRegexCache = new();
    private static readonly Dictionary<string, Regex> MultiLineStartRegexCache = new();
    private static readonly Dictionary<string, Regex> MultiLineEndRegexCache = new();

    /// <summary>
    /// Removes comments from a line of code
    /// </summary>
    public static string StripComments(string line, CommentConfig config, ref bool inMultiLineComment)
    {
        if (string.IsNullOrEmpty(line) || config == null)
            return line;

        var result = line;

        // Get or create regex patterns
        var multiLineEndRegex = GetMultiLineEndRegex(config);
        var multiLineStartRegex = GetMultiLineStartRegex(config);
        var singleLineRegex = GetSingleLineRegex(config);

        // Handle multi-line comment continuation
        if (inMultiLineComment && multiLineEndRegex != null)
        {
            var endMatch = multiLineEndRegex.Match(result);
            if (endMatch.Success)
            {
                // End of multi-line comment found
                result = result.Substring(endMatch.Index + endMatch.Length);
                inMultiLineComment = false;
                // Continue processing rest of line in case there are more comments
                return StripComments(result, config, ref inMultiLineComment);
            }
            else
            {
                // Entire line is within multi-line comment
                return string.Empty;
            }
        }

        // Handle multi-line comment start
        if (multiLineStartRegex != null)
        {
            var startMatch = multiLineStartRegex.Match(result);
            if (startMatch.Success)
            {
                var beforeComment = result.Substring(0, startMatch.Index);
                var afterStart = result.Substring(startMatch.Index + startMatch.Length);
                
                if (multiLineEndRegex != null)
                {
                    var endMatch = multiLineEndRegex.Match(afterStart);
                    if (endMatch.Success)
                    {
                        // Complete multi-line comment on same line
                        var afterComment = afterStart.Substring(endMatch.Index + endMatch.Length);
                        result = beforeComment + afterComment;
                        // Continue processing in case there are more comments
                        return StripComments(result, config, ref inMultiLineComment);
                    }
                    else
                    {
                        // Multi-line comment starts but doesn't end on this line
                        inMultiLineComment = true;
                        return beforeComment;
                    }
                }
            }
        }

        // Handle single-line comment
        if (singleLineRegex != null)
        {
            var singleLineMatch = singleLineRegex.Match(result);
            if (singleLineMatch.Success)
            {
                result = result.Substring(0, singleLineMatch.Index);
            }
        }

        return result;
    }

    private static Regex? GetSingleLineRegex(CommentConfig config)
    {
        if (string.IsNullOrEmpty(config.SingleLinePattern))
            return null;

        if (!SingleLineRegexCache.TryGetValue(config.SingleLinePattern, out var regex))
        {
            regex = new Regex(Regex.Escape(config.SingleLinePattern) + ".*$", RegexOptions.Compiled);
            SingleLineRegexCache[config.SingleLinePattern] = regex;
        }
        return regex;
    }

    private static Regex? GetMultiLineStartRegex(CommentConfig config)
    {
        if (string.IsNullOrEmpty(config.MultiLineStartPattern))
            return null;

        if (!MultiLineStartRegexCache.TryGetValue(config.MultiLineStartPattern, out var regex))
        {
            regex = new Regex(Regex.Escape(config.MultiLineStartPattern), RegexOptions.Compiled);
            MultiLineStartRegexCache[config.MultiLineStartPattern] = regex;
        }
        return regex;
    }

    private static Regex? GetMultiLineEndRegex(CommentConfig config)
    {
        if (string.IsNullOrEmpty(config.MultiLineEndPattern))
            return null;

        if (!MultiLineEndRegexCache.TryGetValue(config.MultiLineEndPattern, out var regex))
        {
            regex = new Regex(Regex.Escape(config.MultiLineEndPattern), RegexOptions.Compiled);
            MultiLineEndRegexCache[config.MultiLineEndPattern] = regex;
        }
        return regex;
    }

    /// <summary>
    /// Checks if a difference is only in comments
    /// </summary>
    public static bool IsCommentOnlyDifference(Difference diff, CommentConfig config)
    {
        if (config == null || !config.IgnoreComments)
            return false;

        var sourceWithoutComments = string.Empty;
        var targetWithoutComments = string.Empty;

        bool inMultiLineSource = false;
        bool inMultiLineTarget = false;

        if (!string.IsNullOrEmpty(diff.SourceContent))
        {
            sourceWithoutComments = StripComments(diff.SourceContent, config, ref inMultiLineSource).Trim();
        }

        if (!string.IsNullOrEmpty(diff.TargetContent))
        {
            targetWithoutComments = StripComments(diff.TargetContent, config, ref inMultiLineTarget).Trim();
        }

        // If both are empty after stripping comments, it's a comment-only difference
        if (string.IsNullOrEmpty(sourceWithoutComments) && string.IsNullOrEmpty(targetWithoutComments))
        {
            return true;
        }

        // If the non-comment parts are identical, it's a comment-only difference
        if (sourceWithoutComments == targetWithoutComments)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Filters differences to separate comment-only changes
    /// Handles multi-line comments that span across multiple Difference objects
    /// </summary>
    public static (List<Difference> kept, List<Difference> commentOnly) FilterComments(
        List<Difference> differences,
        CommentConfig? config,
        List<string> sourceLines,
        List<string> targetLines)
    {
        var kept = new List<Difference>();
        var commentOnly = new List<Difference>();

        if (config == null || !config.IgnoreComments)
            return (differences, commentOnly);

        // Track the last processed line for each side
        int lastProcessedSourceLine = 0;
        int lastProcessedTargetLine = 0;

        // Track multiline comment state
        bool sourceInMultiLine = false;
        bool targetInMultiLine = false;

        for (int i = 0; i < differences.Count; i++)
        {
            var diff = differences[i];
            
            // Process all lines from last processed line to current difference line
            // to properly track multiline comment state
            for (int srcLine = lastProcessedSourceLine + 1; srcLine < diff.SourceLineNumber && srcLine <= sourceLines.Count; srcLine++)
            {
                StripComments(sourceLines[srcLine - 1], config, ref sourceInMultiLine);
            }
            
            for (int tgtLine = lastProcessedTargetLine + 1; tgtLine < diff.TargetLineNumber && tgtLine <= targetLines.Count; tgtLine++)
            {
                StripComments(targetLines[tgtLine - 1], config, ref targetInMultiLine);
            }
            
            // Update last processed line (only if line number is > 0)
            if (diff.SourceLineNumber > 0)
                lastProcessedSourceLine = diff.SourceLineNumber;
            if (diff.TargetLineNumber > 0)
                lastProcessedTargetLine = diff.TargetLineNumber;

            // Now process the actual difference lines with correct multiline state
            var sourceWithoutComments = string.Empty;
            var targetWithoutComments = string.Empty;

            // Track state before stripping (for debugging if needed)
            bool sourceWasInMultiLine = sourceInMultiLine;
            bool targetWasInMultiLine = targetInMultiLine;

            if (!string.IsNullOrEmpty(diff.SourceContent))
            {
                sourceWithoutComments = StripComments(diff.SourceContent, config, ref sourceInMultiLine).Trim();
            }

            if (!string.IsNullOrEmpty(diff.TargetContent))
            {
                targetWithoutComments = StripComments(diff.TargetContent, config, ref targetInMultiLine).Trim();
            }

            // Check if this difference is comment-only
            bool isCommentOnly = false;

            // If both are empty after stripping comments, it's a comment-only difference
            if (string.IsNullOrEmpty(sourceWithoutComments) && string.IsNullOrEmpty(targetWithoutComments))
            {
                isCommentOnly = true;
            }
            // If the non-comment parts are identical, it's a comment-only difference
            else if (sourceWithoutComments == targetWithoutComments)
            {
                isCommentOnly = true;
            }

            if (isCommentOnly)
            {
                diff.MatchedRuleName = "Comment-Only Change";
                commentOnly.Add(diff);
            }
            else
            {
                kept.Add(diff);
            }
        }

        return (kept, commentOnly);
    }
}
