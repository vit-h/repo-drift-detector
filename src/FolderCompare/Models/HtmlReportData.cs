using System.Collections.Generic;

namespace FolderCompare.Models;

public class HeaderInfoData
{
    public string GeneratedTime { get; set; } = string.Empty;
    public string EscapedSourcePath { get; set; } = string.Empty;
    public string EscapedTargetPath { get; set; } = string.Empty;
    public string JsEscapedSourcePath { get; set; } = string.Empty;
    public string JsEscapedTargetPath { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public bool HasFilters { get; set; }
    public List<string> Filters { get; set; } = new();
    public string SourceBranch { get; set; } = string.Empty;
    public string SourceCommit { get; set; } = string.Empty;
    public string SourceCommitShort { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string TargetCommit { get; set; } = string.Empty;
    public string TargetCommitShort { get; set; } = string.Empty;
}

public class HtmlReportData
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public List<string> DiffFilters { get; set; } = new();
    public SummaryData Summary { get; set; } = new();
    public List<FilterRuleStats> FilterRules { get; set; } = new();
    public List<ExtensionStats> ExtensionStats { get; set; } = new();
}

public class SummaryData
{
    public int TotalFiles { get; set; }
    public int Identical { get; set; }
    public int IdenticalNormalized { get; set; }
    public int Different { get; set; }
    public int OnlyInSource { get; set; }
    public int OnlyInTarget { get; set; }
    public int Errors { get; set; }
    public int TotalLines { get; set; }
    public int TotalFiltered { get; set; }
    public int IgnoredInSource { get; set; }
    public int IgnoredInTarget { get; set; }
}

public class FilterRuleStats
{
    public string Name { get; set; } = string.Empty;
    public string EscapedName { get; set; } = string.Empty;
    public int FilteredCount { get; set; }
    public int FilteredFilesCount { get; set; }
    public string AnchorId { get; set; } = string.Empty;
    public bool IsWhitelist { get; set; }
}

public class FilterStatisticsData
{
    public List<FilterRuleStats> FilterRulesWithMatches { get; set; } = new();
    public List<FilterRuleStats> WhitelistRulesWithZeroMatches { get; set; } = new();
    public int TotalFilteredCount { get; set; }
    public int TotalFilteredFilesCount { get; set; }
}

public class ExtensionStats
{
    public string Extension { get; set; } = string.Empty;
    public int SourceCount { get; set; }
    public int TargetCount { get; set; }
    public int Different { get; set; }
}

public class FileListData
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<FileItemData> Files { get; set; } = new();
    public int FileCount { get; set; }
    public bool IsSource { get; set; }
}

public class FileItemData
{
    public string RelativePath { get; set; } = string.Empty;
    public string EscapedRelativePath { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public string TargetUri { get; set; } = string.Empty;
}

public class ExtensionStatisticsData
{
    public string Description { get; set; } = string.Empty;
    public List<TableHeader> Headers { get; set; } = new();
    public List<ExtensionRow> Extensions { get; set; } = new();
    public List<TotalRow> TotalRows { get; set; } = new();
}

public class TableHeader
{
    public string Name { get; set; } = string.Empty;
    public string TextAlign { get; set; } = "left";
}

public class ExtensionRow
{
    public string Extension { get; set; } = string.Empty;
    public string EscapedExtension { get; set; } = string.Empty;
    public List<CellValue> Values { get; set; } = new();
}

public class TotalRow
{
    public string Label { get; set; } = string.Empty;
    public bool BorderTop { get; set; }
    public List<CellValue> Values { get; set; } = new();
}

public class CellValue
{
    public string Value { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public bool IsBold { get; set; }
}

public class FilteredDifferencesData
{
    public string TotalFiles { get; set; } = string.Empty;
    public string TotalFilteredCount { get; set; } = string.Empty;
    public List<FilterRuleGroup> RuleGroups { get; set; } = new();
}

public class FilterRuleGroup
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string EscapedRuleName { get; set; } = string.Empty;
    public string FileCount { get; set; } = string.Empty;
    public string TotalDiffs { get; set; } = string.Empty;
    public List<FileDiffData> Files { get; set; } = new();
}

public class FileDiffData
{
    public string FileId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string EscapedRelativePath { get; set; } = string.Empty;
    public string DiffJson { get; set; } = string.Empty;
    public string EncodedDiffJson { get; set; } = string.Empty;
    public string DiffCount { get; set; } = string.Empty;
    public string SourceFileUri { get; set; } = string.Empty;
    public string TargetFileUri { get; set; } = string.Empty;
}

public class DifferencesData
{
    public string TotalFiles { get; set; } = string.Empty;
    public string TotalDiffLines { get; set; } = string.Empty;
    public List<FileDiffData> Files { get; set; } = new();
}
