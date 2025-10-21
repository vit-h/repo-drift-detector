using Xunit;
using FolderCompare.Models;
using FolderCompare.Services;

namespace FolderCompare.Tests;

public class ReportGeneratorTests
{
    private readonly string _testOutputPath;
    private readonly string _testDataPath;

    public ReportGeneratorTests()
    {
        _testOutputPath = Path.Combine(Path.GetTempPath(), "FolderCompareTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputPath);
        _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
    }

    [Fact]
    public void GenerateAnalysisReport_WithUnfilteredDifferences_CreatesReport()
    {
        var sourcePath = Path.Combine(_testDataPath, "Source");
        var targetPath = Path.Combine(_testDataPath, "Target");
        var results = new List<FileComparisonResult>
        {
            new FileComparisonResult
            {
                RelativePath = "analysis.sql",
                Status = ComparisonStatus.Different,
                SourcePath = Path.Combine(sourcePath, "analysis.sql"),
                TargetPath = Path.Combine(targetPath, "analysis.sql"),
                DifferenceCount = 1,
                Differences = new List<Difference>
                {
                    new Difference 
                    { 
                        Type = DifferenceType.Modified, 
                        SourceContent = "Old", 
                        TargetContent = "New", 
                        SourceLineNumber = 1, 
                        TargetLineNumber = 1 
                    }
                }
            }
        };

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);
        var analysisPath = ReportGenerator.GenerateAnalysisReport(
            results, _testOutputPath, sourcePath, targetPath,
            "Source", "Target", timestamp, absoluteSourcePath, absoluteTargetPath);

        Assert.True(File.Exists(analysisPath));
        Assert.Contains("_ANALYSIS.txt", analysisPath);
        var content = File.ReadAllText(analysisPath);
        Assert.Contains("UNFILTERED DIFFERENCES ANALYSIS REPORT", content);
        Assert.Contains("analysis.sql", content);
        if (File.Exists(analysisPath)) File.Delete(analysisPath);
    }

    [Fact]
    public void GenerateAnalysisReport_WithNoUnfilteredDifferences_ShowsSuccess()
    {
        var sourcePath = Path.Combine(_testDataPath, "Source");
        var targetPath = Path.Combine(_testDataPath, "Target");
        var results = new List<FileComparisonResult>
        {
            new FileComparisonResult
            {
                RelativePath = "filtered.sql",
                Status = ComparisonStatus.Different,
                SourcePath = Path.Combine(sourcePath, "filtered.sql"),
                TargetPath = Path.Combine(targetPath, "filtered.sql"),
                DifferenceCount = 0
            }
        };

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);
        var analysisPath = ReportGenerator.GenerateAnalysisReport(
            results, _testOutputPath, sourcePath, targetPath,
            "Source", "Target", timestamp, absoluteSourcePath, absoluteTargetPath);

        Assert.True(File.Exists(analysisPath));
        var content = File.ReadAllText(analysisPath);
        Assert.Contains("NO UNFILTERED DIFFERENCES FOUND", content);
        Assert.Contains("All differences have been successfully filtered", content);
        if (File.Exists(analysisPath)) File.Delete(analysisPath);
    }

    [Fact]
    public void GenerateAnalysisReport_WithMultipleFiles_ListsAll()
    {
        var sourcePath = Path.Combine(_testDataPath, "Source");
        var targetPath = Path.Combine(_testDataPath, "Target");
        var results = new List<FileComparisonResult>
        {
            new FileComparisonResult
            {
                RelativePath = "file1.sql",
                Status = ComparisonStatus.Different,
                SourcePath = Path.Combine(sourcePath, "file1.sql"),
                TargetPath = Path.Combine(targetPath, "file1.sql"),
                DifferenceCount = 2,
                Differences = new List<Difference>
                {
                    new Difference { Type = DifferenceType.Added, TargetContent = "Line1", SourceLineNumber = 1, TargetLineNumber = 1 },
                    new Difference { Type = DifferenceType.Removed, SourceContent = "Line2", SourceLineNumber = 2, TargetLineNumber = 2 }
                }
            },
            new FileComparisonResult
            {
                RelativePath = "file2.sql",
                Status = ComparisonStatus.Different,
                SourcePath = Path.Combine(sourcePath, "file2.sql"),
                TargetPath = Path.Combine(targetPath, "file2.sql"),
                DifferenceCount = 1,
                Differences = new List<Difference>
                {
                    new Difference { Type = DifferenceType.Modified, SourceContent = "A", TargetContent = "B", SourceLineNumber = 1, TargetLineNumber = 1 }
                }
            }
        };

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);
        var analysisPath = ReportGenerator.GenerateAnalysisReport(
            results, _testOutputPath, sourcePath, targetPath,
            "Source", "Target", timestamp, absoluteSourcePath, absoluteTargetPath);

        Assert.True(File.Exists(analysisPath));
        var content = File.ReadAllText(analysisPath);
        Assert.Contains("Files with unfiltered differences: 2", content);
        Assert.Contains("Total unfiltered line differences: 3", content);
        Assert.Contains("file1.sql", content);
        Assert.Contains("file2.sql", content);
        if (File.Exists(analysisPath)) File.Delete(analysisPath);
    }

    [Fact]
    public void GenerateAnalysisReport_FileNameFormat_ContainsAllParts()
    {
        var sourcePath = Path.Combine(_testDataPath, "Source");
        var targetPath = Path.Combine(_testDataPath, "Target");
        var results = new List<FileComparisonResult>();
        var timestamp = "20251021_143022";
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);

        var analysisPath = ReportGenerator.GenerateAnalysisReport(
            results, _testOutputPath, sourcePath, targetPath,
            "SourceFolder", "TargetFolder", timestamp, absoluteSourcePath, absoluteTargetPath);

        var fileName = Path.GetFileName(analysisPath);
        Assert.Contains("SourceFolder", fileName);
        Assert.Contains("TargetFolder", fileName);
        Assert.Contains(timestamp, fileName);
        Assert.Contains("_ANALYSIS.txt", fileName);
        Assert.EndsWith("_ANALYSIS.txt", fileName);
        if (File.Exists(analysisPath)) File.Delete(analysisPath);
    }

    [Fact]
    public void GenerateAnalysisReport_IgnoresIdenticalFiles()
    {
        var sourcePath = Path.Combine(_testDataPath, "Source");
        var targetPath = Path.Combine(_testDataPath, "Target");
        var results = new List<FileComparisonResult>
        {
            new FileComparisonResult
            {
                RelativePath = "identical.sql",
                Status = ComparisonStatus.Identical,
                SourcePath = Path.Combine(sourcePath, "identical.sql"),
                TargetPath = Path.Combine(targetPath, "identical.sql"),
                DifferenceCount = 0
            },
            new FileComparisonResult
            {
                RelativePath = "different.sql",
                Status = ComparisonStatus.Different,
                SourcePath = Path.Combine(sourcePath, "different.sql"),
                TargetPath = Path.Combine(targetPath, "different.sql"),
                DifferenceCount = 1,
                Differences = new List<Difference>
                {
                    new Difference { Type = DifferenceType.Added, TargetContent = "New", SourceLineNumber = 1, TargetLineNumber = 1 }
                }
            }
        };

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);
        var analysisPath = ReportGenerator.GenerateAnalysisReport(
            results, _testOutputPath, sourcePath, targetPath,
            "Source", "Target", timestamp, absoluteSourcePath, absoluteTargetPath);

        var content = File.ReadAllText(analysisPath);
        Assert.DoesNotContain("identical.sql", content);
        Assert.Contains("different.sql", content);
        Assert.Contains("Files with unfiltered differences: 1", content);
        if (File.Exists(analysisPath)) File.Delete(analysisPath);
    }

    [Fact]
    public void GenerateAnalysisReport_CountsAllDifferenceTypes()
    {
        var sourcePath = Path.Combine(_testDataPath, "Source");
        var targetPath = Path.Combine(_testDataPath, "Target");
        var results = new List<FileComparisonResult>
        {
            new FileComparisonResult
            {
                RelativePath = "test.sql",
                Status = ComparisonStatus.Different,
                SourcePath = Path.Combine(sourcePath, "test.sql"),
                TargetPath = Path.Combine(targetPath, "test.sql"),
                DifferenceCount = 5,
                Differences = new List<Difference>
                {
                    new Difference { Type = DifferenceType.Added, TargetContent = "1", SourceLineNumber = 1, TargetLineNumber = 1 },
                    new Difference { Type = DifferenceType.Added, TargetContent = "2", SourceLineNumber = 2, TargetLineNumber = 2 },
                    new Difference { Type = DifferenceType.Removed, SourceContent = "3", SourceLineNumber = 3, TargetLineNumber = 3 },
                    new Difference { Type = DifferenceType.Modified, SourceContent = "4a", TargetContent = "4b", SourceLineNumber = 4, TargetLineNumber = 4 },
                    new Difference { Type = DifferenceType.Modified, SourceContent = "5a", TargetContent = "5b", SourceLineNumber = 5, TargetLineNumber = 5 }
                }
            }
        };

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteTargetPath = Path.GetFullPath(targetPath);
        var analysisPath = ReportGenerator.GenerateAnalysisReport(
            results, _testOutputPath, sourcePath, targetPath,
            "Source", "Target", timestamp, absoluteSourcePath, absoluteTargetPath);

        var content = File.ReadAllText(analysisPath);
        Assert.Contains("Total unfiltered line differences: 5", content);
        Assert.Contains("test.sql", content);
        if (File.Exists(analysisPath)) File.Delete(analysisPath);
    }
}
