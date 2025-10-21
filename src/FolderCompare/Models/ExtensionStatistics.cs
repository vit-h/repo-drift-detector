namespace FolderCompare.Models;

public class ExtensionStatistics
{
    public string Extension { get; set; } = string.Empty;
    public int SourceCount { get; set; }
    public int TargetCount { get; set; }
    public bool Included { get; set; }
    public bool Excluded { get; set; }
}
