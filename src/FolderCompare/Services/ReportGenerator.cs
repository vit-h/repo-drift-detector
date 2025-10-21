// Made by Vitalii Honcharuk
using System.Text;
using System.Diagnostics;
using FolderCompare.Models;

namespace FolderCompare.Services;

public static class ReportGenerator
{
    // Helper method to get git branch and commit info
    private static (string branch, string commit, string commitShort) GetGitInfo(string repositoryPath)
    {
        try
        {
            if (!Directory.Exists(Path.Combine(repositoryPath, ".git")))
                return ("", "", "");

            // Get branch name
            var branchProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    WorkingDirectory = repositoryPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            branchProcess.Start();
            var branch = branchProcess.StandardOutput.ReadToEnd().Trim();
            branchProcess.WaitForExit();

            // Get commit hash (full)
            var commitProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = repositoryPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            commitProcess.Start();
            var commit = commitProcess.StandardOutput.ReadToEnd().Trim();
            commitProcess.WaitForExit();

            // Get short commit hash
            var commitShort = commit.Length >= 7 ? commit.Substring(0, 7) : commit;

            return (
                string.IsNullOrEmpty(branch) ? "" : branch,
                string.IsNullOrEmpty(commit) ? "" : commit,
                string.IsNullOrEmpty(commitShort) ? "" : commitShort
            );
        }
        catch
        {
            return ("", "", "");
        }
    }

    // Helper method to escape strings for JavaScript literals
    private static string EscapeJavaScript(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        return input
            .Replace("\\", "\\\\")  // Backslash must be first
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
    
    public static string GenerateReport(
        List<FileComparisonResult> results,
        string outputPath,
        string sourcePath,
        string targetPath,
        TimeSpan duration,
        IEnumerable<string>? diffFilters = null,
        List<DiffFilterRule>? filterRules = null,
        bool showFilteredDifferences = false,
        List<ExtensionStatistics>? extensionStats = null,
        List<FileInfo>? ignoredSourceFiles = null,
        List<FileInfo>? ignoredTargetFiles = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        // Extract folder names for report filename
        var sourceDir = new DirectoryInfo(sourcePath);
        var targetDir = new DirectoryInfo(targetPath);
        var sourceFolderName = sourceDir.Name;
        var targetFolderName = targetDir.Name;
        
        // If folder names are the same, use parent folder name for clarity
        if (sourceFolderName.Equals(targetFolderName, StringComparison.OrdinalIgnoreCase))
        {
            sourceFolderName = sourceDir.Parent?.Name ?? sourceFolderName;
            targetFolderName = targetDir.Parent?.Name ?? targetFolderName;
        }
        
        var reportFileName = $"{sourceFolderName}_vs_{targetFolderName}_{timestamp}.txt";
        var reportPath = Path.Combine(outputPath, reportFileName);

        using var writer = new StreamWriter(reportPath, false, Encoding.UTF8);

        // Use absolute paths for file URIs
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);

        WriteHeader(writer, sourcePath, targetPath, duration, diffFilters, filterRules);
        WriteSummary(writer, results, filterRules);
        WriteOnlyInSource(writer, results, absoluteSourcePath);
        WriteOnlyInTarget(writer, results, absoluteTargetPath);
        WriteDifferences(writer, results, absoluteSourcePath, absoluteTargetPath);
        WriteFilteredDifferences(writer, results, filterRules);
        WriteErrors(writer, results);
        WriteFooter(writer);

        // Generate analysis report with only unfiltered differences
        var analysisReportPath = GenerateAnalysisReport(results, outputPath, sourcePath, targetPath, 
            sourceFolderName, targetFolderName, timestamp, absoluteSourcePath, absoluteTargetPath);

        // Also generate HTML report with clickable diff links
        var htmlReportPath = GenerateHtmlReport(results, outputPath, sourcePath, targetPath, duration, diffFilters, 
            sourceFolderName, targetFolderName, timestamp, filterRules, showFilteredDifferences, extensionStats,
            ignoredSourceFiles, ignoredTargetFiles);

        return reportPath;
    }

    public static string GenerateHtmlReport(
        List<FileComparisonResult> results,
        string outputPath,
        string sourcePath,
        string targetPath,
        TimeSpan duration,
        IEnumerable<string>? diffFilters,
        string sourceFolderName,
        string targetFolderName,
        string timestamp,
        List<DiffFilterRule>? filterRules = null,
        bool showFilteredDifferences = false,
        List<ExtensionStatistics>? extensionStats = null,
        List<FileInfo>? ignoredSourceFiles = null,
        List<FileInfo>? ignoredTargetFiles = null)
    {
        var htmlFileName = $"{sourceFolderName}_vs_{targetFolderName}_{timestamp}.html";
        var htmlPath = Path.Combine(outputPath, htmlFileName);

        // Initialize template service once for the entire HTML report
        TemplateService.Initialize();

        using var writer = new StreamWriter(htmlPath, false, Encoding.UTF8);
        
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);

        WriteHtmlHeader(writer, sourcePath, targetPath, duration, diffFilters);
        WriteHtmlSummaryHandlebars(writer, results, filterRules, extensionStats, ignoredSourceFiles, ignoredTargetFiles);
        WriteHtmlIgnoredInSource(writer, ignoredSourceFiles, absoluteSourcePath);
        WriteHtmlIgnoredInTarget(writer, ignoredTargetFiles, absoluteTargetPath);
        WriteHtmlOnlyInSource(writer, results, absoluteSourcePath);
        WriteHtmlOnlyInTarget(writer, results, absoluteTargetPath);
        if (showFilteredDifferences)
        {
            WriteHtmlFilteredDifferences(writer, results, absoluteSourcePath, absoluteTargetPath, filterRules);
        }
        WriteHtmlDifferences(writer, results, absoluteSourcePath, absoluteTargetPath);
        WriteHtmlFooter(writer);

        return htmlPath;
    }

    public static string GenerateAnalysisReport(
        List<FileComparisonResult> results,
        string outputPath,
        string sourcePath,
        string targetPath,
        string sourceFolderName,
        string targetFolderName,
        string timestamp,
        string absoluteSourcePath,
        string absoluteTargetPath)
    {
        var analysisFileName = $"{sourceFolderName}_vs_{targetFolderName}_{timestamp}_ANALYSIS.txt";
        var analysisPath = Path.Combine(outputPath, analysisFileName);

        using var writer = new StreamWriter(analysisPath, false, Encoding.UTF8);

        writer.WriteLine("=============================================================================");
        writer.WriteLine("UNFILTERED DIFFERENCES ANALYSIS REPORT");
        writer.WriteLine("=============================================================================");
        writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"Source: {sourcePath}");
        writer.WriteLine($"Target: {targetPath}");
        writer.WriteLine();
        writer.WriteLine("This report contains ONLY files with differences that were NOT filtered out.");
        writer.WriteLine("Use this to analyze patterns and identify new allowed substitutions for");
        writer.WriteLine("AWS SQL Server to Azure SQL migration.");
        writer.WriteLine();

        // Get only files with unfiltered differences
        var withDifferences = results
            .Where(r => r.Status == ComparisonStatus.Different && r.DifferenceCount > 0)
            .OrderBy(r => r.RelativePath)
            .ToList();

        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine("SUMMARY");
        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine($"Files with unfiltered differences: {withDifferences.Count:N0}");
        writer.WriteLine($"Total unfiltered line differences: {withDifferences.Sum(r => r.DifferenceCount):N0}");
        writer.WriteLine();

        if (!withDifferences.Any())
        {
            writer.WriteLine("🎉 NO UNFILTERED DIFFERENCES FOUND!");
            writer.WriteLine("All differences have been successfully filtered as allowed migrations.");
            writer.WriteLine("=============================================================================");
            return analysisPath;
        }

        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine("FILES WITH UNFILTERED DIFFERENCES");
        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine();

        int fileIndex = 1;
        foreach (var result in withDifferences)
        {
            var sourceFullPath = Path.Combine(absoluteSourcePath, result.RelativePath);
            var targetFullPath = Path.Combine(absoluteTargetPath, result.RelativePath);
            
            writer.WriteLine($"[{fileIndex}/{withDifferences.Count}] {result.RelativePath}");
            writer.WriteLine($"  Differences: {result.DifferenceCount:N0} lines");
            writer.WriteLine($"  Source: file:///{sourceFullPath.Replace('\\', '/')}");
            writer.WriteLine($"  Target: file:///{targetFullPath.Replace('\\', '/')}");
            writer.WriteLine($"  Diff Command: code --diff \"{sourceFullPath}\" \"{targetFullPath}\"");
            writer.WriteLine();
            writer.WriteLine("  LINE DIFFERENCES:");
            writer.WriteLine("  " + new string('-', 75));

            // Show actual line differences
            var diffsToShow = result.Differences.Take(50).ToList(); // Limit to first 50 per file
            foreach (var diff in diffsToShow)
            {
                switch (diff.Type)
                {
                    case DifferenceType.Modified:
                        writer.WriteLine($"  Line {diff.SourceLineNumber} → {diff.TargetLineNumber} (MODIFIED)");
                        writer.WriteLine($"    SOURCE:  {diff.SourceContent?.Trim()}");
                        writer.WriteLine($"    TARGET:  {diff.TargetContent?.Trim()}");
                        break;
                    case DifferenceType.Removed:
                        writer.WriteLine($"  Line {diff.SourceLineNumber} (REMOVED)");
                        writer.WriteLine($"    < {diff.SourceContent?.Trim()}");
                        break;
                    case DifferenceType.Added:
                        writer.WriteLine($"  Line {diff.TargetLineNumber} (ADDED)");
                        writer.WriteLine($"    > {diff.TargetContent?.Trim()}");
                        break;
                }
                writer.WriteLine();
            }

            if (result.Differences.Count > 50)
            {
                writer.WriteLine($"  ... and {result.Differences.Count - 50} more differences");
                writer.WriteLine();
            }

            writer.WriteLine("  " + new string('=', 75));
            writer.WriteLine();
            fileIndex++;
        }

        writer.WriteLine("=============================================================================");
        writer.WriteLine("END OF ANALYSIS REPORT");
        writer.WriteLine("=============================================================================");

        return analysisPath;
    }

    private static string ToFileUri(string absolutePath, int? lineNumber = null)
    {
        // Convert Windows path to proper file:// URI
        // C:\Path\To\File.txt -> file:///C:/Path/To/File.txt
        // With line number: file:///C:/Path/To/File.txt#L123
        var uri = new Uri(absolutePath).AbsoluteUri;
        
        if (lineNumber.HasValue)
        {
            uri += $"#L{lineNumber.Value}";
        }
        
        return uri;
    }
    
    private static string ToVSCodeDiffCommand(string sourceAbsolutePath, string targetAbsolutePath, string relativePath)
    {
        // Create a command that can be run to open diff in VS Code
        // Format: code --diff "source" "target"
        var escapedSource = sourceAbsolutePath.Replace("\"", "\"\"");
        var escapedTarget = targetAbsolutePath.Replace("\"", "\"\"");
        
        return $"code --diff \"{escapedSource}\" \"{escapedTarget}\"";
    }

    private static void WriteHeader(StreamWriter writer, string sourcePath, string targetPath, TimeSpan duration, IEnumerable<string>? diffFilters, List<DiffFilterRule>? filterRules)
    {
        writer.WriteLine("=============================================================================");
        writer.WriteLine("FOLDER COMPARISON REPORT");
        writer.WriteLine("=============================================================================");
        writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"Source: {sourcePath}");
        writer.WriteLine($"Target: {targetPath}");
        writer.WriteLine($"Duration: {duration:hh\\:mm\\:ss}");
        
        if (filterRules != null && filterRules.Any())
        {
            writer.WriteLine();
            writer.WriteLine("Allowed Substitutions:");
            foreach (var rule in filterRules)
            {
                writer.WriteLine($"  - {rule.Name}");
            }
        }
        
        if (diffFilters != null && diffFilters.Any())
        {
            writer.WriteLine();
            writer.WriteLine("Diff Filters Applied:");
            foreach (var filter in diffFilters)
            {
                writer.WriteLine($"  - {filter}");
            }
        }
        
        writer.WriteLine();
        writer.WriteLine("HOW TO USE:");
        writer.WriteLine("  - File links (file:///) - Ctrl+Click to open in VS Code");
        writer.WriteLine("  - Line links (#L123) - Ctrl+Click to jump to specific line");
        writer.WriteLine("  - Diff commands - Copy and paste into terminal, or Ctrl+Click in terminal");
        writer.WriteLine("    Example: code --diff \"source\" \"target\"");
        writer.WriteLine();
    }

    private static void WriteSummary(StreamWriter writer, List<FileComparisonResult> results, List<DiffFilterRule>? filterRules)
    {
        var identical = results.Count(r => r.Status == ComparisonStatus.Identical);
        var identicalNormalized = results.Count(r => r.Status == ComparisonStatus.IdenticalNormalized);
        var different = results.Count(r => r.Status == ComparisonStatus.Different);
        var onlyInSource = results.Count(r => r.Status == ComparisonStatus.OnlyInSource);
        var onlyInTarget = results.Count(r => r.Status == ComparisonStatus.OnlyInTarget);
        var errors = results.Count(r => r.Status == ComparisonStatus.Error);

        var totalLines = results.Where(r => r.Differences.Any()).Sum(r => r.Differences.Count);
        var totalFiltered = results.Sum(r => r.FilteredDifferences.Count);

        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine("SUMMARY");
        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine($"Total Files Processed: {results.Count:N0}");
        writer.WriteLine($"  Identical (byte-level): {identical:N0}");
        writer.WriteLine($"  Identical (normalized): {identicalNormalized:N0}");
        writer.WriteLine($"  Different: {different:N0}");
        writer.WriteLine($"  Only in Source: {onlyInSource:N0}");
        writer.WriteLine($"  Only in Target: {onlyInTarget:N0}");
        writer.WriteLine($"  Errors: {errors:N0}");
        writer.WriteLine();
        writer.WriteLine($"Total Lines Different: {totalLines:N0}");
        writer.WriteLine($"Total Lines Filtered Out: {totalFiltered:N0}");
        
        // Show per-filter statistics
        if (filterRules != null && filterRules.Any())
        {
            writer.WriteLine();
            writer.WriteLine("Differences Filtered By Rule and Whitelist Patterns:");
            
            var allFilterStats = new Dictionary<string, int>();
            foreach (var result in results)
            {
                foreach (var kvp in result.FilterStats)
                {
                    if (!allFilterStats.ContainsKey(kvp.Key))
                        allFilterStats[kvp.Key] = 0;
                    allFilterStats[kvp.Key] += kvp.Value;
                }
            }
            
            foreach (var kvp in allFilterStats.OrderByDescending(x => x.Value))
            {
                writer.WriteLine($"  {kvp.Key}: {kvp.Value:N0} differences");
            }
        }
        
        writer.WriteLine();
    }

    private static void WriteOnlyInSource(StreamWriter writer, List<FileComparisonResult> results, string absoluteSourcePath)
    {
        var onlyInSource = results.Where(r => r.Status == ComparisonStatus.OnlyInSource).ToList();

        if (onlyInSource.Count == 0)
            return;

        writer.WriteLine("=============================================================================");
        writer.WriteLine($"FILES ONLY IN SOURCE ({onlyInSource.Count})");
        writer.WriteLine("=============================================================================");

        foreach (var result in onlyInSource.OrderBy(r => r.RelativePath))
        {
            var fullPath = Path.Combine(absoluteSourcePath, result.RelativePath);
            var uri = ToFileUri(fullPath);
            writer.WriteLine($"{uri}");
        }

        writer.WriteLine();
    }

    private static void WriteOnlyInTarget(StreamWriter writer, List<FileComparisonResult> results, string absoluteTargetPath)
    {
        var onlyInTarget = results.Where(r => r.Status == ComparisonStatus.OnlyInTarget).ToList();

        if (onlyInTarget.Count == 0)
            return;

        writer.WriteLine("=============================================================================");
        writer.WriteLine($"FILES ONLY IN TARGET ({onlyInTarget.Count})");
        writer.WriteLine("=============================================================================");

        foreach (var result in onlyInTarget.OrderBy(r => r.RelativePath))
        {
            var fullPath = Path.Combine(absoluteTargetPath, result.RelativePath);
            var uri = ToFileUri(fullPath);
            writer.WriteLine($"{uri}");
        }

        writer.WriteLine();
    }

    private static void WriteDifferences(
        StreamWriter writer,
        List<FileComparisonResult> results,
        string absoluteSourcePath,
        string absoluteTargetPath)
    {
        var withDifferences = results
            .Where(r => r.Status == ComparisonStatus.Different && r.Differences.Any())
            .OrderBy(r => r.RelativePath)
            .ToList();

        if (withDifferences.Count == 0)
            return;

        writer.WriteLine("=============================================================================");
        writer.WriteLine($"FILES WITH DIFFERENCES ({withDifferences.Count})");
        writer.WriteLine("=============================================================================");
        writer.WriteLine();

        foreach (var result in withDifferences)
        {
            var relativePath = result.RelativePath.Replace('\\', '/');
            var sourceFullPath = Path.Combine(absoluteSourcePath, result.RelativePath);
            var targetFullPath = Path.Combine(absoluteTargetPath, result.RelativePath);
            var sourceUri = ToFileUri(sourceFullPath);
            var targetUri = ToFileUri(targetFullPath);
            var diffCommand = ToVSCodeDiffCommand(sourceFullPath, targetFullPath, relativePath);

            writer.WriteLine($"File: {relativePath}");
            writer.WriteLine($"Source: {sourceUri}");
            writer.WriteLine($"Target: {targetUri}");
            writer.WriteLine($"Diff: {diffCommand}");
            writer.WriteLine($"Lines Different: {result.DifferenceCount}");
            writer.WriteLine();

            foreach (var diff in result.Differences.Take(50)) // Limit to first 50 differences per file
            {
                switch (diff.Type)
                {
                    case DifferenceType.Removed:
                        writer.WriteLine($"  Line {diff.SourceLineNumber} [REMOVED]:");
                        writer.WriteLine($"    {ToFileUri(sourceFullPath, diff.SourceLineNumber)}");
                        writer.WriteLine($"    {diff.SourceContent}");
                        writer.WriteLine();
                        break;

                    case DifferenceType.Added:
                        writer.WriteLine($"  Line {diff.TargetLineNumber} [ADDED]:");
                        writer.WriteLine($"    {ToFileUri(targetFullPath, diff.TargetLineNumber)}");
                        writer.WriteLine($"    {diff.TargetContent}");
                        writer.WriteLine();
                        break;

                    case DifferenceType.Modified:
                        writer.WriteLine($"  Line {diff.SourceLineNumber}/{diff.TargetLineNumber} [MODIFIED]:");
                        writer.WriteLine($"    Source: {ToFileUri(sourceFullPath, diff.SourceLineNumber)}");
                        writer.WriteLine($"      {diff.SourceContent}");
                        writer.WriteLine($"    Target: {ToFileUri(targetFullPath, diff.TargetLineNumber)}");
                        writer.WriteLine($"      {diff.TargetContent}");
                        writer.WriteLine();
                        break;
                }
            }

            if (result.Differences.Count > 50)
            {
                writer.WriteLine($"  ... and {result.Differences.Count - 50} more differences");
                writer.WriteLine();
            }

            writer.WriteLine("-----------------------------------------------------------------------------");
            writer.WriteLine();
        }
    }

    private static void WriteErrors(StreamWriter writer, List<FileComparisonResult> results)
    {
        var errors = results.Where(r => r.Status == ComparisonStatus.Error).ToList();

        if (errors.Count == 0)
            return;

        writer.WriteLine("=============================================================================");
        writer.WriteLine("ERRORS AND WARNINGS");
        writer.WriteLine("=============================================================================");

        foreach (var error in errors.OrderBy(r => r.RelativePath))
        {
            writer.WriteLine($"[ERROR] {error.RelativePath}: {error.ErrorMessage}");
        }

        writer.WriteLine();
    }

    private static void WriteFilteredDifferences(StreamWriter writer, List<FileComparisonResult> results, List<DiffFilterRule>? filterRules)
    {
        var filesWithFilteredDiffs = results
            .Where(r => r.FilteredDifferences != null && r.FilteredDifferences.Count > 0)
            .ToList();

        if (filesWithFilteredDiffs.Count == 0)
            return;

        writer.WriteLine("=============================================================================");
        writer.WriteLine("FILTERED DIFFERENCES (Excluded by Custom Filters)");
        writer.WriteLine("=============================================================================");
        writer.WriteLine();

        int totalFilteredCount = filesWithFilteredDiffs.Sum(r => r.FilteredDifferences.Count);
        writer.WriteLine($"Total: {totalFilteredCount:N0} filtered differences across {filesWithFilteredDiffs.Count:N0} files");
        writer.WriteLine();
        
        // Show per-filter statistics
        if (filterRules != null && filterRules.Any())
        {
            var allFilterStats = new Dictionary<string, int>();
            foreach (var result in results)
            {
                foreach (var kvp in result.FilterStats)
                {
                    if (!allFilterStats.ContainsKey(kvp.Key))
                        allFilterStats[kvp.Key] = 0;
                    allFilterStats[kvp.Key] += kvp.Value;
                }
            }
            
            if (allFilterStats.Any())
            {
                writer.WriteLine("Breakdown by Filter Rule:");
                foreach (var kvp in allFilterStats.OrderByDescending(x => x.Value))
                {
                    writer.WriteLine($"  {kvp.Key}: {kvp.Value:N0} differences");
                }
                writer.WriteLine();
            }
        }

        foreach (var result in filesWithFilteredDiffs.OrderBy(r => r.RelativePath))
        {
            writer.WriteLine($"File: {result.RelativePath}");
            writer.WriteLine($"  Source: {ToFileUri(result.SourcePath)}");
            writer.WriteLine($"  Target: {ToFileUri(result.TargetPath)}");
            writer.WriteLine($"  Filtered Differences: {result.FilteredDifferences.Count}");
            writer.WriteLine();

            // Show up to 20 filtered differences per file
            foreach (var diff in result.FilteredDifferences.Take(20))
            {
                string lineInfo = diff.Type switch
                {
                    DifferenceType.Added => $"Line {diff.TargetLineNumber} (Target): +",
                    DifferenceType.Removed => $"Line {diff.SourceLineNumber} (Source): -",
                    DifferenceType.Modified => $"Lines {diff.SourceLineNumber}/{diff.TargetLineNumber}: ~",
                    _ => "?"
                };

                string content = diff.Type == DifferenceType.Added ? diff.TargetContent :
                                 diff.Type == DifferenceType.Removed ? diff.SourceContent :
                                 $"{diff.SourceContent} => {diff.TargetContent}";

                writer.WriteLine($"  {lineInfo} {content}");
            }

            if (result.FilteredDifferences.Count > 20)
            {
                writer.WriteLine($"  ... and {result.FilteredDifferences.Count - 20} more filtered differences");
            }

            writer.WriteLine();
        }

        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine();
    }

    private static void WriteFooter(StreamWriter writer)
    {
        writer.WriteLine("=============================================================================");
        writer.WriteLine("END OF REPORT");
        writer.WriteLine("=============================================================================");
    }

    // ========================== HTML REPORT GENERATION ==========================

    private static void WriteHtmlHeader(StreamWriter writer, string sourcePath, string targetPath, TimeSpan duration, IEnumerable<string>? diffFilters)
    {
        // Render the main header template (HTML structure, CSS, JavaScript)
        // Extract folder name for unique localStorage keys per database
        var sourceFolderName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var storageKeyPrefix = sourceFolderName.Replace(" ", "-").Replace(".", "-");
        
        var headerTemplateData = new
        {
            JsEscapedSourcePath = EscapeJavaScript(sourcePath),
            JsEscapedTargetPath = EscapeJavaScript(targetPath),
            StorageKeyPrefix = storageKeyPrefix
        };
        var headerHtml = TemplateService.RenderTemplate("header", headerTemplateData);
        writer.Write(headerHtml);
        
        // Render the dynamic header-info section
        var filters = diffFilters?.Select(f => System.Security.SecurityElement.Escape(f) ?? f).ToList() ?? new List<string>();
        
        // Get git information for both repositories
        var (sourceBranch, sourceCommit, sourceCommitShort) = GetGitInfo(sourcePath);
        var (targetBranch, targetCommit, targetCommitShort) = GetGitInfo(targetPath);
        
        var headerData = new HeaderInfoData
        {
            GeneratedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            EscapedSourcePath = System.Security.SecurityElement.Escape(sourcePath) ?? sourcePath,
            EscapedTargetPath = System.Security.SecurityElement.Escape(targetPath) ?? targetPath,
            JsEscapedSourcePath = EscapeJavaScript(sourcePath),
            JsEscapedTargetPath = EscapeJavaScript(targetPath),
            Duration = duration.ToString("hh\\:mm\\:ss"),
            HasFilters = filters.Any(),
            Filters = filters,
            SourceBranch = sourceBranch,
            SourceCommit = sourceCommit,
            SourceCommitShort = sourceCommitShort,
            TargetBranch = targetBranch,
            TargetCommit = targetCommit,
            TargetCommitShort = targetCommitShort
        };
        
        var headerInfoHtml = TemplateService.RenderTemplate("header-info", headerData);
        writer.Write(headerInfoHtml);
    }

    private static void WriteHtmlSummaryHandlebars(StreamWriter writer, List<FileComparisonResult> results, List<DiffFilterRule>? filterRules, List<ExtensionStatistics>? extensionStats = null, List<FileInfo>? ignoredSourceFiles = null, List<FileInfo>? ignoredTargetFiles = null)
    {
        // Prepare data
        var identical = results.Count(r => r.Status == ComparisonStatus.Identical);
        var identicalNormalized = results.Count(r => r.Status == ComparisonStatus.IdenticalNormalized);
        var different = results.Count(r => r.Status == ComparisonStatus.Different);
        var onlyInSource = results.Count(r => r.Status == ComparisonStatus.OnlyInSource);
        var onlyInTarget = results.Count(r => r.Status == ComparisonStatus.OnlyInTarget);
        var errors = results.Count(r => r.Status == ComparisonStatus.Error);
        var totalDifferences = results.Where(r => r.Status == ComparisonStatus.Different).Sum(r => r.DifferenceCount);

        var data = new HtmlReportData
        {
            Summary = new SummaryData
            {
                TotalFiles = results.Count,
                Identical = identical,
                IdenticalNormalized = identicalNormalized,
                Different = different,
                OnlyInSource = onlyInSource,
                OnlyInTarget = onlyInTarget,
                Errors = errors,
                TotalLines = totalDifferences,
                TotalFiltered = results.Sum(r => r.FilteredDifferences.Count),
                IgnoredInSource = ignoredSourceFiles?.Count ?? 0,
                IgnoredInTarget = ignoredTargetFiles?.Count ?? 0
            }
        };

        // Render template
        var html = TemplateService.RenderTemplate("summary", data);
        writer.Write(html);
        
        // For now, still use old method for extension stats
        if (extensionStats != null && extensionStats.Any())
        {
            WriteAllExtensionStatistics(writer, extensionStats);
        }
        else
        {
            WriteFileExtensionStatistics(writer, results);
        }
        
        // And for filter stats
        if (filterRules != null && filterRules.Any())
        {
            WriteHtmlFilterStatistics(writer, results, filterRules);
        }
    }

    private static void WriteHtmlFilterStatistics(StreamWriter writer, List<FileComparisonResult> results, List<DiffFilterRule> filterRules)
    {
        // Aggregate filter statistics from all files
        foreach (var rule in filterRules)
        {
            rule.FilteredCount = 0;
            rule.FilteredFiles.Clear();
        }

        foreach (var result in results)
        {
            foreach (var kvp in result.FilterStats)
            {
                var rule = filterRules.FirstOrDefault(r => r.Name == kvp.Key);
                if (rule != null)
                {
                    rule.FilteredCount += kvp.Value;
                    rule.FilteredFiles.Add(result.RelativePath);
                }
            }
        }

        // Build mapping of rule names to section IDs
        var ruleNameToId = new Dictionary<string, string>();
        var filesWithFilteredDiffs = results.Where(r => r.FilteredDifferences.Any()).ToList();
        
        if (filesWithFilteredDiffs.Count > 0)
        {
            var diffsByRule = new Dictionary<string, List<(FileComparisonResult file, List<Difference> diffs)>>();
            
            foreach (var result in filesWithFilteredDiffs)
            {
                var groupedByRule = result.FilteredDifferences
                    .GroupBy(d => d.MatchedRuleName ?? "Unknown")
                    .ToList();
                
                foreach (var group in groupedByRule)
                {
                    if (!diffsByRule.ContainsKey(group.Key))
                        diffsByRule[group.Key] = new List<(FileComparisonResult, List<Difference>)>();
                    
                    diffsByRule[group.Key].Add((result, group.ToList()));
                }
            }

            int ruleMappingIndex = 0;
            foreach (var ruleGroup in diffsByRule.OrderBy(kvp => kvp.Key))
            {
                ruleNameToId[ruleGroup.Key] = $"rule{ruleMappingIndex}";
                ruleMappingIndex++;
            }
        }
        
        // Prepare data for template
        var rulesWithMatches = new List<FilterRuleStats>();
        var totalFiltered = 0;
        var totalFilesAffected = new HashSet<string>();
        
        foreach (var rule in filterRules.Where(r => r.FilteredCount > 0).OrderByDescending(r => r.FilteredCount))
        {
            var escapedName = System.Security.SecurityElement.Escape(rule.Name);
            var anchorId = ruleNameToId.ContainsKey(rule.Name) ? $"{ruleNameToId[rule.Name]}-header" : "";
            
            rulesWithMatches.Add(new FilterRuleStats
            {
                Name = rule.Name,
                EscapedName = escapedName,
                FilteredCount = rule.FilteredCount,
                FilteredFilesCount = rule.FilteredFiles.Count,
                AnchorId = anchorId,
                IsWhitelist = rule.Name.StartsWith("[Whitelist]")
            });
            
            totalFiltered += rule.FilteredCount;
            foreach (var file in rule.FilteredFiles)
                totalFilesAffected.Add(file);
        }
        
        var whitelistZeroMatches = new List<FilterRuleStats>();
        foreach (var rule in filterRules.Where(r => r.FilteredCount == 0 && r.Name.StartsWith("[Whitelist]")).OrderBy(r => r.Name))
        {
            var escapedName = System.Security.SecurityElement.Escape(rule.Name);
            whitelistZeroMatches.Add(new FilterRuleStats
            {
                Name = rule.Name,
                EscapedName = escapedName,
                FilteredCount = 0,
                FilteredFilesCount = 0,
                IsWhitelist = true
            });
        }
        
        var filterStatsData = new FilterStatisticsData
        {
            FilterRulesWithMatches = rulesWithMatches,
            WhitelistRulesWithZeroMatches = whitelistZeroMatches,
            TotalFilteredCount = totalFiltered,
            TotalFilteredFilesCount = totalFilesAffected.Count
        };
        
        // Render using Handlebars
        var html = TemplateService.RenderTemplate("filter-statistics", filterStatsData);
        writer.Write(html);
    }

    private static void WriteAllExtensionStatistics(StreamWriter writer, List<ExtensionStatistics> extensionStats)
    {
        if (extensionStats == null || !extensionStats.Any())
            return;

        // Prepare headers
        var headers = new List<TableHeader>
        {
            new TableHeader { Name = "Extension", TextAlign = "left" },
            new TableHeader { Name = "Source Count", TextAlign = "right" },
            new TableHeader { Name = "Target Count", TextAlign = "right" },
            new TableHeader { Name = "Status", TextAlign = "center" }
        };

        // Prepare extension rows
        var extensionRows = new List<ExtensionRow>();
        foreach (var stat in extensionStats.OrderByDescending(s => s.SourceCount + s.TargetCount))
        {
            var status = stat.Excluded ? "❌ Excluded" : (stat.Included ? "✅ Processed" : "⚠️ Ignored");
            var statusColor = stat.Excluded ? "#ff6b6b" : (stat.Included ? "#51cf66" : "#ffd43b");

            extensionRows.Add(new ExtensionRow
            {
                Extension = stat.Extension,
                EscapedExtension = System.Security.SecurityElement.Escape(stat.Extension),
                Values = new List<CellValue>
                {
                    new CellValue { Value = stat.SourceCount.ToString("N0") },
                    new CellValue { Value = stat.TargetCount.ToString("N0") },
                    new CellValue { Value = status, Style = $"text-align: center; color: {statusColor};" }
                }
            });
        }

        // Calculate totals
        var totalSource = extensionStats.Sum(x => x.SourceCount);
        var totalTarget = extensionStats.Sum(x => x.TargetCount);
        var processedSource = extensionStats.Where(x => x.Included && !x.Excluded).Sum(x => x.SourceCount);
        var processedTarget = extensionStats.Where(x => x.Included && !x.Excluded).Sum(x => x.TargetCount);

        // Prepare total rows
        var totalRows = new List<TotalRow>
        {
            new TotalRow
            {
                Label = "Total Files",
                BorderTop = true,
                Values = new List<CellValue>
                {
                    new CellValue { Value = totalSource.ToString("N0"), IsBold = true },
                    new CellValue { Value = totalTarget.ToString("N0"), IsBold = true },
                    new CellValue { Value = "" }
                }
            },
            new TotalRow
            {
                Label = "Processed",
                BorderTop = false,
                Values = new List<CellValue>
                {
                    new CellValue { Value = processedSource.ToString("N0"), IsBold = true },
                    new CellValue { Value = processedTarget.ToString("N0"), IsBold = true },
                    new CellValue { Value = "✅ Processed", Style = "text-align: center; color: #51cf66;" }
                }
            }
        };

        var data = new ExtensionStatisticsData
        {
            Description = "All file extensions found in source and target folders:",
            Headers = headers,
            Extensions = extensionRows,
            TotalRows = totalRows
        };

        // Render template
        var html = TemplateService.RenderTemplate("extension-statistics", data);
        writer.Write(html);
    }

    private static void WriteFileExtensionStatistics(StreamWriter writer, List<FileComparisonResult> results)
    {
        // Group files by extension
        var allFiles = results.Where(r => !string.IsNullOrEmpty(r.RelativePath)).ToList();
        
        var extensionStats = allFiles
            .GroupBy(r => Path.GetExtension(r.RelativePath).ToLowerInvariant())
            .Select(g => new
            {
                Extension = string.IsNullOrEmpty(g.Key) ? "(no extension)" : g.Key,
                Count = g.Count(),
                Identical = g.Count(f => f.Status == ComparisonStatus.Identical),
                IdenticalNormalized = g.Count(f => f.Status == ComparisonStatus.IdenticalNormalized),
                Different = g.Count(f => f.Status == ComparisonStatus.Different),
                OnlyInSource = g.Count(f => f.Status == ComparisonStatus.OnlyInSource),
                OnlyInTarget = g.Count(f => f.Status == ComparisonStatus.OnlyInTarget),
                Errors = g.Count(f => f.Status == ComparisonStatus.Error)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (!extensionStats.Any())
            return;

        // Prepare headers
        var headers = new List<TableHeader>
        {
            new TableHeader { Name = "Extension", TextAlign = "left" },
            new TableHeader { Name = "Total", TextAlign = "right" },
            new TableHeader { Name = "Identical", TextAlign = "right" },
            new TableHeader { Name = "Normalized", TextAlign = "right" },
            new TableHeader { Name = "Different", TextAlign = "right" },
            new TableHeader { Name = "Source Only", TextAlign = "right" },
            new TableHeader { Name = "Target Only", TextAlign = "right" },
            new TableHeader { Name = "Errors", TextAlign = "right" }
        };

        // Prepare extension rows
        var extensionRows = new List<ExtensionRow>();
        foreach (var stat in extensionStats)
        {
            extensionRows.Add(new ExtensionRow
            {
                Extension = stat.Extension,
                EscapedExtension = System.Security.SecurityElement.Escape(stat.Extension),
                Values = new List<CellValue>
                {
                    new CellValue { Value = stat.Count.ToString("N0") },
                    new CellValue { Value = stat.Identical.ToString("N0") },
                    new CellValue { Value = stat.IdenticalNormalized.ToString("N0") },
                    new CellValue { Value = stat.Different.ToString("N0") },
                    new CellValue { Value = stat.OnlyInSource.ToString("N0") },
                    new CellValue { Value = stat.OnlyInTarget.ToString("N0") },
                    new CellValue { Value = stat.Errors.ToString("N0") }
                }
            });
        }

        // Calculate totals
        var totalCount = extensionStats.Sum(x => x.Count);
        var totalIdentical = extensionStats.Sum(x => x.Identical);
        var totalIdenticalNormalized = extensionStats.Sum(x => x.IdenticalNormalized);
        var totalDifferent = extensionStats.Sum(x => x.Different);
        var totalOnlyInSource = extensionStats.Sum(x => x.OnlyInSource);
        var totalOnlyInTarget = extensionStats.Sum(x => x.OnlyInTarget);
        var totalErrors = extensionStats.Sum(x => x.Errors);

        // Prepare total rows
        var totalRows = new List<TotalRow>
        {
            new TotalRow
            {
                Label = "Total",
                BorderTop = true,
                Values = new List<CellValue>
                {
                    new CellValue { Value = totalCount.ToString("N0"), IsBold = true },
                    new CellValue { Value = totalIdentical.ToString("N0"), IsBold = true },
                    new CellValue { Value = totalIdenticalNormalized.ToString("N0"), IsBold = true },
                    new CellValue { Value = totalDifferent.ToString("N0"), IsBold = true },
                    new CellValue { Value = totalOnlyInSource.ToString("N0"), IsBold = true },
                    new CellValue { Value = totalOnlyInTarget.ToString("N0"), IsBold = true },
                    new CellValue { Value = totalErrors.ToString("N0"), IsBold = true }
                }
            }
        };

        var data = new ExtensionStatisticsData
        {
            Description = "Files processed by extension:",
            Headers = headers,
            Extensions = extensionRows,
            TotalRows = totalRows
        };

        // Render template
        var html = TemplateService.RenderTemplate("extension-statistics", data);
        writer.Write(html);
    }

    private static void WriteHtmlOnlyInSource(StreamWriter writer, List<FileComparisonResult> results, string absoluteSourcePath)
    {
        var onlyInSource = results
            .Where(r => r.Status == ComparisonStatus.OnlyInSource)
            .OrderBy(r => r.RelativePath)
            .ToList();

        if (onlyInSource.Count == 0)
            return;

        // Prepare data
        var fileItems = new List<FileItemData>();
        foreach (var result in onlyInSource)
        {
            var relativePath = result.RelativePath.Replace('\\', '/');
            var fullPath = Path.Combine(absoluteSourcePath, result.RelativePath);

            fileItems.Add(new FileItemData
            {
                RelativePath = relativePath,
                EscapedRelativePath = System.Security.SecurityElement.Escape(relativePath),
                SourceUri = ToFileUri(fullPath),
                TargetUri = string.Empty
            });
        }

        var data = new FileListData
        {
            SectionId = "only-source",
            Title = "📁 Files Only in Source",
            Files = fileItems,
            FileCount = fileItems.Count,
            IsSource = true
        };

        // Render template
        var html = TemplateService.RenderTemplate("file-list", data);
        writer.Write(html);
    }

    private static void WriteHtmlOnlyInTarget(StreamWriter writer, List<FileComparisonResult> results, string absoluteTargetPath)
    {
        var onlyInTarget = results
            .Where(r => r.Status == ComparisonStatus.OnlyInTarget)
            .OrderBy(r => r.RelativePath)
            .ToList();

        if (onlyInTarget.Count == 0)
            return;

        // Prepare data
        var fileItems = new List<FileItemData>();
        foreach (var result in onlyInTarget)
        {
            var relativePath = result.RelativePath.Replace('\\', '/');
            var fullPath = Path.Combine(absoluteTargetPath, result.RelativePath);

            fileItems.Add(new FileItemData
            {
                RelativePath = relativePath,
                EscapedRelativePath = System.Security.SecurityElement.Escape(relativePath),
                SourceUri = string.Empty,
                TargetUri = ToFileUri(fullPath)
            });
        }

        var data = new FileListData
        {
            SectionId = "only-target",
            Title = "📁 Files Only in Target",
            Files = fileItems,
            FileCount = fileItems.Count,
            IsSource = false
        };

        // Render template
        var html = TemplateService.RenderTemplate("file-list", data);
        writer.Write(html);
    }

    private static void WriteHtmlIgnoredInSource(StreamWriter writer, List<FileInfo>? ignoredFiles, string absoluteSourcePath)
    {
        if (ignoredFiles == null || ignoredFiles.Count == 0)
            return;

        // Prepare data
        var fileItems = new List<FileItemData>();
        foreach (var file in ignoredFiles.OrderBy(f => f.FullName))
        {
            var relativePath = Path.GetRelativePath(absoluteSourcePath, file.FullName).Replace('\\', '/');

            fileItems.Add(new FileItemData
            {
                RelativePath = relativePath,
                EscapedRelativePath = System.Security.SecurityElement.Escape(relativePath),
                SourceUri = ToFileUri(file.FullName),
                TargetUri = string.Empty
            });
        }

        var data = new FileListData
        {
            SectionId = "ignored-source",
            Title = "🚫 Ignored Files in Source",
            Files = fileItems,
            FileCount = fileItems.Count,
            IsSource = true
        };

        // Render template
        var html = TemplateService.RenderTemplate("file-list", data);
        writer.Write(html);
    }

    private static void WriteHtmlIgnoredInTarget(StreamWriter writer, List<FileInfo>? ignoredFiles, string absoluteTargetPath)
    {
        if (ignoredFiles == null || ignoredFiles.Count == 0)
            return;

        // Prepare data
        var fileItems = new List<FileItemData>();
        foreach (var file in ignoredFiles.OrderBy(f => f.FullName))
        {
            var relativePath = Path.GetRelativePath(absoluteTargetPath, file.FullName).Replace('\\', '/');

            fileItems.Add(new FileItemData
            {
                RelativePath = relativePath,
                EscapedRelativePath = System.Security.SecurityElement.Escape(relativePath),
                SourceUri = string.Empty,
                TargetUri = ToFileUri(file.FullName)
            });
        }

        var data = new FileListData
        {
            SectionId = "ignored-target",
            Title = "🚫 Ignored Files in Target",
            Files = fileItems,
            FileCount = fileItems.Count,
            IsSource = false
        };

        // Render template
        var html = TemplateService.RenderTemplate("file-list", data);
        writer.Write(html);
    }

    private static void WriteHtmlFilteredDifferences(StreamWriter writer, List<FileComparisonResult> results, string absoluteSourcePath, string absoluteTargetPath, List<DiffFilterRule>? filterRules)
    {
        var filesWithFilteredDiffs = results
            .Where(r => r.FilteredDifferences.Any())
            .ToList();

        if (filesWithFilteredDiffs.Count == 0)
            return;

        // Group all filtered differences by rule name
        var diffsByRule = new Dictionary<string, List<(FileComparisonResult file, List<Difference> diffs)>>();
        
        foreach (var result in filesWithFilteredDiffs)
        {
            var groupedByRule = result.FilteredDifferences
                .GroupBy(d => d.MatchedRuleName ?? "Unknown")
                .ToList();
            
            foreach (var group in groupedByRule)
            {
                if (!diffsByRule.ContainsKey(group.Key))
                    diffsByRule[group.Key] = new List<(FileComparisonResult, List<Difference>)>();
                
                diffsByRule[group.Key].Add((result, group.ToList()));
            }
        }

        var totalFilteredCount = filesWithFilteredDiffs.Sum(r => r.FilteredDifferences.Count);

        // Build data for template
        var ruleGroups = new List<FilterRuleGroup>();
        int ruleIndex = 0;
        
        foreach (var ruleGroup in diffsByRule.OrderBy(kvp => kvp.Key))
        {
            var ruleName = ruleGroup.Key;
            var filesForRule = ruleGroup.Value.OrderBy(f => f.file.RelativePath).ToList();
            var totalDiffsForRule = filesForRule.Sum(f => f.diffs.Count);
            
            var files = new List<FileDiffData>();
            int fileIndex = 0;
            
            foreach (var (file, diffs) in filesForRule)
            {
                var relativePath = file.RelativePath.Replace('\\', '/');
                
                // Build diff data for inline viewer
                var diffData = new
                {
                    source = relativePath + " (Source)",
                    target = relativePath + " (Target)",
                    diffs = diffs.Select(d => new
                    {
                        type = d.Type.ToString(),
                        source = d.SourceLineNumber.ToString(),
                        target = d.TargetLineNumber.ToString(),
                        content = d.Type == DifferenceType.Removed ? d.SourceContent : d.TargetContent,
                        sourceContent = d.SourceContent,
                        targetContent = d.TargetContent,
                        matchedRule = d.MatchedRuleName // Include rule name for whitelisted diffs
                    }).ToList()
                };
                var diffJson = System.Text.Json.JsonSerializer.Serialize(diffData);
                var encodedJson = System.Net.WebUtility.HtmlEncode(diffJson);
                
                files.Add(new FileDiffData
                {
                    FileId = $"filtered_{ruleIndex}_{fileIndex}",
                    RelativePath = relativePath,
                    EscapedRelativePath = System.Security.SecurityElement.Escape(relativePath) ?? relativePath,
                    DiffJson = diffJson,
                    EncodedDiffJson = encodedJson,
                    DiffCount = diffs.Count.ToString("N0")
                });
                
                fileIndex++;
            }
            
            ruleGroups.Add(new FilterRuleGroup
            {
                RuleId = $"rule{ruleIndex}",
                RuleName = ruleName,
                EscapedRuleName = System.Security.SecurityElement.Escape(ruleName) ?? ruleName,
                FileCount = filesForRule.Count.ToString("N0"),
                TotalDiffs = totalDiffsForRule.ToString("N0"),
                Files = files
            });
            
            ruleIndex++;
        }

        var data = new FilteredDifferencesData
        {
            TotalFiles = filesWithFilteredDiffs.Count.ToString("N0"),
            TotalFilteredCount = totalFilteredCount.ToString("N0"),
            RuleGroups = ruleGroups
        };

        // Render template
        var html = TemplateService.RenderTemplate("filtered-differences", data);
        writer.Write(html);
    }

    private static void WriteHtmlDifferences(StreamWriter writer, List<FileComparisonResult> results, string absoluteSourcePath, string absoluteTargetPath)
    {
        var withDifferences = results
            .Where(r => r.Status == ComparisonStatus.Different && r.Differences.Any())
            .OrderBy(r => r.RelativePath)
            .ToList();

        if (withDifferences.Count == 0)
            return;

        var totalDiffLines = withDifferences.Sum(r => r.Differences.Count);

        // Build data for template
        var files = new List<FileDiffData>();
        int fileIndex = 0;

        foreach (var result in withDifferences)
        {
            var relativePath = result.RelativePath.Replace('\\', '/');

            // Build diff data for inline viewer
            var diffData = new
            {
                source = relativePath + " (Source)",
                target = relativePath + " (Target)",
                diffs = result.Differences.Select(d => new
                {
                    type = d.Type.ToString(),
                    source = d.SourceLineNumber.ToString(),
                    target = d.TargetLineNumber.ToString(),
                    content = d.Type == DifferenceType.Removed ? d.SourceContent : d.TargetContent,
                    sourceContent = d.SourceContent,
                    targetContent = d.TargetContent,
                    matchedRule = d.MatchedRuleName // Include rule name for whitelisted diffs
                }).ToList()
            };
            var diffJson = System.Text.Json.JsonSerializer.Serialize(diffData);
            var encodedJson = System.Net.WebUtility.HtmlEncode(diffJson);

            files.Add(new FileDiffData
            {
                FileId = $"file{fileIndex}",
                RelativePath = relativePath,
                EscapedRelativePath = System.Security.SecurityElement.Escape(relativePath) ?? relativePath,
                DiffJson = diffJson,
                EncodedDiffJson = encodedJson,
                DiffCount = result.DifferenceCount.ToString("N0")
            });

            fileIndex++;
        }

        var data = new DifferencesData
        {
            TotalFiles = withDifferences.Count.ToString("N0"),
            TotalDiffLines = totalDiffLines.ToString("N0"),
            Files = files
        };

        // Render template
        var html = TemplateService.RenderTemplate("differences", data);
        writer.Write(html);
    }

    private static void WriteHtmlFooter(StreamWriter writer)
    {
        // Footer has no dynamic data, so pass empty object
        var html = TemplateService.RenderTemplate("footer", new { });
        writer.Write(html);
    }
}
