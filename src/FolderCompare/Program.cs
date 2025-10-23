// Made by Vitalii Honcharuk
using System.Collections.Concurrent;
using System.Diagnostics;
using CommandLine;
using FolderCompare.Models;
using FolderCompare.Services;
using FolderCompare.Utilities;

namespace FolderCompare;

class Program
{
    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(
                options => RunComparison(options),
                _ => 2); // Invalid parameters
    }

    static int RunComparison(CommandLineOptions options)
    {
        try
        {
            // Handle --create-config
            if (!string.IsNullOrEmpty(options.CreateConfigPath))
            {
                ConfigurationService.CreateSampleConfig(options.CreateConfigPath);
                Console.WriteLine($"Sample configuration file created: {Path.GetFullPath(options.CreateConfigPath)}");
                Console.WriteLine("Edit this file and run with --config option to use it.");
                return 0;
            }

            // Determine config files to process
            var configFiles = new List<string>();
            
            if (!string.IsNullOrEmpty(options.ConfigPath))
            {
                configFiles.Add(options.ConfigPath);
            }
            else if (options.ConfigPaths?.Any() == true)
            {
                configFiles.AddRange(options.ConfigPaths);
            }
            else if (options.DatabaseNames?.Any() == true)
            {
                // Convert database names to config file paths
                foreach (var dbName in options.DatabaseNames)
                {
                    var configFile = $"{dbName.Trim().ToLower()}-comparison.json";
                    configFiles.Add(configFile);
                }
            }
            else
            {
                // Use command-line options for single comparison
                return RunSingleComparison(options);
            }

            // Run multiple comparisons
            var results = new List<(string Database, int ExitCode, string Status)>();
            var hasFailures = false;

            Console.WriteLine($"Running comparisons for: {string.Join(", ", configFiles.Select(Path.GetFileNameWithoutExtension))}");
            Console.WriteLine();

            foreach (var configFile in configFiles)
            {
                var dbName = Path.GetFileNameWithoutExtension(configFile).Replace("-comparison", "");
                Console.WriteLine($"Processing {dbName}...");
                
                var exitCode = RunSingleComparison(new CommandLineOptions { ConfigPath = configFile });
                
                var status = exitCode switch
                {
                    0 => "✅ Identical",
                    1 => "⚠️  Differences Found",
                    _ => "❌ Failed"
                };
                
                results.Add((dbName, exitCode, status));
                Console.WriteLine($"  {dbName}: {status}");
                Console.WriteLine();
                
                if (exitCode > 1) hasFailures = true;
            }

            // Show summary
            Console.WriteLine("Summary:");
            foreach (var (database, exitCode, status) in results)
            {
                var color = exitCode <= 1 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = color;
                Console.WriteLine($"  {database}: {status}");
                Console.ResetColor();
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ All comparisons completed");

            // Return appropriate exit code
            if (hasFailures) return 2;
            if (results.Any(r => r.ExitCode == 1)) return 1;
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 4;
        }
    }

    static int RunSingleComparison(CommandLineOptions options)
    {
        try
        {
            // Load configuration from file or command line
            ComparisonConfig config;
            if (!string.IsNullOrEmpty(options.ConfigPath))
            {
                Console.WriteLine($"Loading configuration from: {options.ConfigPath}");
                config = ConfigurationService.LoadConfig(options.ConfigPath);
                
                // Resolve relative paths based on config file location
                var configDir = Path.GetDirectoryName(Path.GetFullPath(options.ConfigPath))!;
                
                if (!string.IsNullOrEmpty(config.SourcePath) && !Path.IsPathRooted(config.SourcePath))
                {
                    config.SourcePath = Path.GetFullPath(Path.Combine(configDir, config.SourcePath));
                }
                
                if (!string.IsNullOrEmpty(config.TargetPath) && !Path.IsPathRooted(config.TargetPath))
                {
                    config.TargetPath = Path.GetFullPath(Path.Combine(configDir, config.TargetPath));
                }
                
                if (!string.IsNullOrEmpty(config.OutputPath) && !Path.IsPathRooted(config.OutputPath))
                {
                    config.OutputPath = Path.GetFullPath(Path.Combine(configDir, config.OutputPath));
                }
                
                Console.WriteLine("Configuration loaded successfully.");
                Console.WriteLine();
            }
            else
            {
                // Use command-line options
                config = ConfigurationService.FromCommandLineOptions(options);
            }

            // Validate paths
            if (string.IsNullOrEmpty(config.SourcePath))
            {
                Console.Error.WriteLine("Error: Source path is required (use --source or specify in config file)");
                return 3;
            }

            if (string.IsNullOrEmpty(config.TargetPath))
            {
                Console.Error.WriteLine("Error: Target path is required (use --target or specify in config file)");
                return 3;
            }

            if (!Directory.Exists(config.SourcePath))
            {
                Console.Error.WriteLine($"Error: Source path does not exist: {config.SourcePath}");
                return 3;
            }

            if (!Directory.Exists(config.TargetPath))
            {
                Console.Error.WriteLine($"Error: Target path does not exist: {config.TargetPath}");
                return 3;
            }

            // Ensure output directory exists
            Directory.CreateDirectory(config.OutputPath);

            Console.WriteLine("Folder Comparison Tool");
            Console.WriteLine("======================");
            Console.WriteLine($"Source: {Path.GetFullPath(config.SourcePath)}");
            Console.WriteLine($"Target: {Path.GetFullPath(config.TargetPath)}");
            Console.WriteLine($"Output: {Path.GetFullPath(config.OutputPath)}");
            Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();

            // Build extension filters
            var includeExtensions = config.IncludeExtensions?.Any() == true
                ? new HashSet<string>(config.IncludeExtensions, StringComparer.OrdinalIgnoreCase)
                : FileTypeDetector.GetDefaultTextExtensions();

            var excludeExtensions = config.ExcludeExtensions?.Any() == true
                ? new HashSet<string>(config.ExcludeExtensions, StringComparer.OrdinalIgnoreCase)
                : FileTypeDetector.GetDefaultBinaryExtensions();

            // Discover files
            Console.WriteLine("Discovering files...");
            
            // Get ALL files and separate ignored ones
            var allSourceFilesRaw = Directory.EnumerateFiles(config.SourcePath, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .ToList();
            var allTargetFilesRaw = Directory.EnumerateFiles(config.TargetPath, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .ToList();
            
            var ignoredSourceFiles = allSourceFilesRaw
                .Where(f => IsInIgnoredFolder(f, config.SourcePath, config.IgnoreFolders))
                .ToList();
            var ignoredTargetFiles = allTargetFilesRaw
                .Where(f => IsInIgnoredFolder(f, config.TargetPath, config.IgnoreFolders))
                .ToList();
            
            var allSourceFiles = allSourceFilesRaw.Except(ignoredSourceFiles).ToList();
            var allTargetFiles = allTargetFilesRaw.Except(ignoredTargetFiles).ToList();
            
            if (config.IgnoreFolders?.Any() == true)
            {
                Console.WriteLine($"Ignored {ignoredSourceFiles.Count:N0} files in source (folders: {string.Join(", ", config.IgnoreFolders)})");
                Console.WriteLine($"Ignored {ignoredTargetFiles.Count:N0} files in target (folders: {string.Join(", ", config.IgnoreFolders)})");
            }
            
            // Gather extension statistics from all files
            var allExtensions = allSourceFiles.Select(f => f.Extension.ToLowerInvariant())
                .Concat(allTargetFiles.Select(f => f.Extension.ToLowerInvariant()))
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Distinct()
                .OrderBy(ext => ext)
                .ToList();
            
            var extensionStats = allExtensions.Select(ext => new ExtensionStatistics
            {
                Extension = ext,
                SourceCount = allSourceFiles.Count(f => f.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)),
                TargetCount = allTargetFiles.Count(f => f.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)),
                Included = includeExtensions?.Count == 0 || includeExtensions?.Contains(ext) == true,
                Excluded = excludeExtensions.Contains(ext)
            }).ToList();
            
            // Now filter to get files to process
            var sourceFiles = FileDiscovery.DiscoverFiles(
                config.SourcePath, includeExtensions, excludeExtensions, config.IgnoreFolders).ToList();
            var targetFiles = FileDiscovery.DiscoverFiles(
                config.TargetPath, includeExtensions, excludeExtensions, config.IgnoreFolders).ToList();

            Console.WriteLine($"Found {sourceFiles.Count:N0} files in source");
            Console.WriteLine($"Found {targetFiles.Count:N0} files in target");
            Console.WriteLine();

            // Build file maps
            var sourceMap = FileDiscovery.BuildFileMap(sourceFiles, config.SourcePath);
            var targetMap = FileDiscovery.BuildFileMap(targetFiles, config.TargetPath);

            // Categorize files
            var (onlyInSource, onlyInTarget, inBoth) = FileDiscovery.CategorizeFiles(sourceMap, targetMap);

            var results = new ConcurrentBag<FileComparisonResult>();

            // Add files only in source
            foreach (var relativePath in onlyInSource)
            {
                results.Add(new FileComparisonResult
                {
                    RelativePath = relativePath,
                    Status = ComparisonStatus.OnlyInSource,
                    SourcePath = sourceMap[relativePath].FullName
                });
            }

            // Add files only in target
            foreach (var relativePath in onlyInTarget)
            {
                results.Add(new FileComparisonResult
                {
                    RelativePath = relativePath,
                    Status = ComparisonStatus.OnlyInTarget,
                    TargetPath = targetMap[relativePath].FullName
                });
            }

            // Compare files that exist in both
            Console.WriteLine($"Comparing {inBoth.Count:N0} common files...");
            Console.WriteLine();

            // Build filter rules from config (support both old and new property names)
#pragma warning disable CS0618 // Type or member is obsolete
            var substitutionRules = config.AllowedSubstitutions ?? config.SubstitutionRules;
#pragma warning restore CS0618
            var filterRules = new List<DiffFilterRule>();
            if (substitutionRules != null && substitutionRules.Any())
            {
                foreach (var rule in substitutionRules)
                {
                    filterRules.Add(new DiffFilterRule
                    {
                        Name = string.IsNullOrEmpty(rule.Name) ? $"{rule.Source} → {rule.Target}" : rule.Name,
                        SourcePattern = rule.Source,
                        TargetPattern = rule.Target,
                        IsExactMatch = true,
                        IgnoreCase = rule.IgnoreCase,
                        TrimWhitespaceAround = rule.TrimWhitespaceAround,
                        NormalizeInternalWhitespace = rule.NormalizeInternalWhitespace,
                        ReportMatched = rule.ReportMatched,
                        AllowStructuralChanges = rule.AllowStructuralChanges
                    });
                }
            }

            // Support both old and new property names for max file size
#pragma warning disable CS0618
            int maxFileSizeMb = config.MaxFileSizeMb > 0 ? config.MaxFileSizeMb : config.MaxFileSize;
#pragma warning restore CS0618

            var comparer = new FileComparer(
                config.IgnoreCase,
                config.SortInserts,
                config.BufferSize,
                maxFileSizeMb,
                config.DiffFilters,
                filterRules,
                config.WhitelistLinePatterns,
                config.WhitelistFilePatterns,
                config.SemanticSimilarity,
                config.SemanticConfigsByExtension);

            int maxThreads = config.MaxThreads > 0
                ? config.MaxThreads
                : Environment.ProcessorCount;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreads
            };

            int processedCount = 0;
            int identicalCount = 0;
            int differentCount = 0;
            object lockObj = new object();

            Parallel.ForEach(inBoth, parallelOptions, relativePath =>
            {
                var sourceFilePath = sourceMap[relativePath].FullName;
                var targetFilePath = targetMap[relativePath].FullName;

                var result = comparer.CompareFiles(sourceFilePath, targetFilePath, relativePath);
                results.Add(result);

                int currentCount;
                lock (lockObj)
                {
                    processedCount++;
                    currentCount = processedCount;

                    if (result.Status == ComparisonStatus.Identical ||
                        result.Status == ComparisonStatus.IdenticalNormalized)
                    {
                        identicalCount++;
                    }
                    else if (result.Status == ComparisonStatus.Different)
                    {
                        differentCount++;
                    }

                    // Update console every 100 files
                    if (currentCount % 100 == 0)
                    {
                        Console.WriteLine(
                            $"[{currentCount:N0}/{inBoth.Count:N0}] Processed: {currentCount:N0} files " +
                            $"({identicalCount:N0} identical, {differentCount:N0} different)");
                    }
                }
            });

            // Final progress update
            Console.WriteLine(
                $"[{processedCount:N0}/{inBoth.Count:N0}] Complete");
            Console.WriteLine();

            stopwatch.Stop();

            // Aggregate filter statistics from all comparison results
            var aggregatedStats = new Dictionary<string, HashSet<string>>();
            foreach (var result in results)
            {
                if (result.FilterStats != null)
                {
                    foreach (var stat in result.FilterStats)
                    {
                        if (!aggregatedStats.ContainsKey(stat.Key))
                        {
                            aggregatedStats[stat.Key] = new HashSet<string>();
                        }
                        aggregatedStats[stat.Key].Add(result.RelativePath);
                    }
                }
            }

            // Update filterRules with aggregated statistics (including whitelist patterns)
            foreach (var stat in aggregatedStats)
            {
                var existingRule = filterRules.FirstOrDefault(r => r.Name == stat.Key);
                if (existingRule != null)
                {
                    // Update existing rule with aggregated stats
                    foreach (var file in stat.Value)
                    {
                        existingRule.FilteredFiles.Add(file);
                    }
                    existingRule.FilteredCount = existingRule.FilteredFiles.Count;
                }
                else
                {
                    // Add new rule for whitelist patterns
                    filterRules.Add(new DiffFilterRule
                    {
                        Name = stat.Key,
                        FilteredFiles = stat.Value,
                        FilteredCount = stat.Value.Count,
                        ReportMatched = true // Whitelist patterns should always be reported
                    });
                }
            }

            // Add whitelist patterns with 0 matches to ensure they appear in statistics
            if (config.WhitelistLinePatterns != null)
            {
                foreach (var pattern in config.WhitelistLinePatterns)
                {
                    var whitelistName = $"[Whitelist] {pattern.Name}";
                    if (!filterRules.Any(r => r.Name == whitelistName))
                    {
                        filterRules.Add(new DiffFilterRule
                        {
                            Name = whitelistName,
                            FilteredFiles = new HashSet<string>(),
                            FilteredCount = 0,
                            ReportMatched = true
                        });
                    }
                }
            }

            if (config.WhitelistFilePatterns != null)
            {
                foreach (var pattern in config.WhitelistFilePatterns)
                {
                    var whitelistName = $"[Whitelist] {pattern.Name}";
                    if (!filterRules.Any(r => r.Name == whitelistName))
                    {
                        filterRules.Add(new DiffFilterRule
                        {
                            Name = whitelistName,
                            FilteredFiles = new HashSet<string>(),
                            FilteredCount = 0,
                            ReportMatched = true
                        });
                    }
                }
            }

            // Generate report
            Console.WriteLine("Generating reports...");
            var sortedResults = results.OrderBy(r => r.RelativePath).ToList();
            
            // Check if any rule has reportMatched enabled
#pragma warning disable CS0618
            bool showFilteredDiffs = filterRules.Any(r => r.ReportMatched) || config.ShowFilteredDifferences;
#pragma warning restore CS0618
            
            var reportPath = ReportGenerator.GenerateReport(
                sortedResults,
                config.OutputPath,
                Path.GetFullPath(config.SourcePath),
                Path.GetFullPath(config.TargetPath),
                stopwatch.Elapsed,
                config.DiffFilters,
                filterRules,
                showFilteredDiffs,
                extensionStats,
                ignoredSourceFiles,
                ignoredTargetFiles);

            Console.WriteLine($"Text report: {reportPath}");
            
            // Get the HTML report path (it was generated inside GenerateReport)
            var htmlReportPath = reportPath.Replace(".txt", ".html");
            Console.WriteLine($"HTML report: {htmlReportPath}");
            Console.WriteLine($"  → Open in browser for clickable diff links!");
            
            // Get the analysis report path
            var analysisReportPath = reportPath.Replace(".txt", "_Not_Allowed_Differences.txt");
            if (File.Exists(analysisReportPath))
            {
                Console.WriteLine($"Not Allowed Differences report: {analysisReportPath}");
                Console.WriteLine($"  → Contains ONLY unfiltered differences that need attention");
            }
            
            Console.WriteLine($"Processing complete in {stopwatch.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine();

            // Determine exit code
            var hasDifferences = sortedResults.Any(r =>
                r.Status == ComparisonStatus.Different ||
                r.Status == ComparisonStatus.OnlyInSource ||
                r.Status == ComparisonStatus.OnlyInTarget);

            return hasDifferences ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 4;
        }
    }

    private static bool IsInIgnoredFolder(FileInfo file, string basePath, List<string>? ignoreFolders)
    {
        if (ignoreFolders == null || ignoreFolders.Count == 0)
            return false;

        var relativePath = Path.GetRelativePath(basePath, file.FullName);
        var pathSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var ignorePattern in ignoreFolders)
        {
            foreach (var segment in pathSegments)
            {
                if (MatchesFolderPattern(segment, ignorePattern))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesFolderPattern(string segment, string pattern)
    {
        // Support simple wildcards
        if (pattern.Contains('*'))
        {
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(segment, regex,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Exact match (case-insensitive)
        return segment.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
