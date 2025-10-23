using CommandLine;

namespace FolderCompare.Models;

public class CommandLineOptions
{
    [Option('c', "config", Required = false, HelpText = "Path to configuration JSON file. If specified, other options are ignored unless overridden.")]
    public string? ConfigPath { get; set; }

    [Option("configs", Required = false, Separator = ',', HelpText = "Multiple configuration JSON files (comma-separated).")]
    public IEnumerable<string>? ConfigPaths { get; set; }

    [Option('d', "databases", Required = false, Separator = ',', HelpText = "Database names to compare (e.g., 'vip,care'). Looks for {name}-comparison.json files.")]
    public IEnumerable<string>? DatabaseNames { get; set; }

    [Option("create-config", Required = false, HelpText = "Create a sample configuration file at the specified path and exit.")]
    public string? CreateConfigPath { get; set; }

    [Option('s', "source", Required = false, HelpText = "Source folder path to compare.")]
    public string SourcePath { get; set; } = string.Empty;

    [Option('t', "target", Required = false, HelpText = "Target folder path to compare.")]
    public string TargetPath { get; set; } = string.Empty;

    [Option('o', "output", Required = false, Default = ".", HelpText = "Output directory for comparison report.")]
    public string OutputPath { get; set; } = ".";

    [Option("include-ext", Required = false, Separator = ',', HelpText = "File extensions to include (comma-separated). Default: common text files.")]
    public IEnumerable<string>? IncludeExtensions { get; set; }

    [Option("exclude-ext", Required = false, Separator = ',', HelpText = "File extensions to exclude (comma-separated). Default: common binary files.")]
    public IEnumerable<string>? ExcludeExtensions { get; set; }

    [Option("ignore-case", Required = false, Default = false, HelpText = "Perform case-insensitive comparison.")]
    public bool IgnoreCase { get; set; }

    [Option("sort-inserts", Required = false, Default = false, HelpText = "Sort INSERT statements to ignore line order differences.")]
    public bool SortInserts { get; set; }

    [Option("max-file-size", Required = false, Default = 100, HelpText = "Maximum file size in MB to process.")]
    public int MaxFileSize { get; set; }

    [Option("diff-filters", Required = false, Separator = ',', HelpText = "Regex patterns to exclude from diff output (comma-separated).")]
    public IEnumerable<string>? DiffFilters { get; set; }

    [Option("allow-substitutions", Required = false, Separator = '|', HelpText = "Allowed exact string substitutions in format 'source->target' separated by | (e.g., \"getdate()->GETDATE()|'NEXTESTATE->N'NEXTESTATE\").")]
    public IEnumerable<string>? AllowedSubstitutions { get; set; }

    [Option("max-threads", Required = false, Default = 0, HelpText = "Maximum parallel threads. 0 = CPU core count.")]
    public int MaxThreads { get; set; }

    [Option("buffer-size", Required = false, Default = 64, HelpText = "File read buffer size in KB.")]
    public int BufferSize { get; set; }
}
