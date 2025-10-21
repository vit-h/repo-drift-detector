using Xunit;
using FolderCompare.Models;
using FolderCompare.Services;

namespace FolderCompare.Tests;

/// <summary>
/// Tests for CommentFilter covering single-line, multi-line, and mixed comment scenarios.
/// Tests SQL-style comments (-- and /* */)
/// </summary>
public class CommentFilterTests
{
    private static CommentConfig CreateSqlCommentConfig()
    {
        return new CommentConfig
        {
            IgnoreComments = true,
            SingleLinePattern = "--",
            MultiLineStartPattern = "/*",
            MultiLineEndPattern = "*/"
        };
    }

    private static Difference CreateDifference(
        DifferenceType type,
        string sourceContent,
        string targetContent,
        int sourceLine = 1,
        int targetLine = 1)
    {
        return new Difference
        {
            Type = type,
            SourceContent = sourceContent,
            TargetContent = targetContent,
            SourceLineNumber = sourceLine,
            TargetLineNumber = targetLine
        };
    }

    #region StripComments Tests

    [Fact]
    public void StripComments_NoComments_ReturnsOriginal()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("SELECT * FROM Users", config, ref inMultiLine);

        Assert.Equal("SELECT * FROM Users", result);
        Assert.False(inMultiLine);
    }

    [Fact]
    public void StripComments_SingleLineComment_RemovesComment()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("SELECT * FROM Users -- Get all users", config, ref inMultiLine);

        Assert.Equal("SELECT * FROM Users ", result);
        Assert.False(inMultiLine);
    }

    [Fact]
    public void StripComments_OnlySingleLineComment_ReturnsEmpty()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("-- This is a comment", config, ref inMultiLine);

        Assert.Equal("", result);
        Assert.False(inMultiLine);
    }

    [Fact]
    public void StripComments_MultiLineCommentOnSameLine_RemovesComment()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("SELECT /* inline comment */ * FROM Users", config, ref inMultiLine);

        Assert.Equal("SELECT  * FROM Users", result);
        Assert.False(inMultiLine);
    }

    [Fact]
    public void StripComments_MultiLineCommentStart_SetsFlag()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("SELECT * FROM Users /* Comment starts", config, ref inMultiLine);

        Assert.Equal("SELECT * FROM Users ", result);
        Assert.True(inMultiLine);
    }

    [Fact]
    public void StripComments_MultiLineCommentContinuation_ReturnsEmpty()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = true; // Already in multi-line comment

        var result = CommentFilter.StripComments("This is still a comment", config, ref inMultiLine);

        Assert.Equal("", result);
        Assert.True(inMultiLine);
    }

    [Fact]
    public void StripComments_MultiLineCommentEnd_ResetsFlag()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = true; // Already in multi-line comment

        var result = CommentFilter.StripComments("Comment ends */ WHERE Id = 1", config, ref inMultiLine);

        Assert.Equal(" WHERE Id = 1", result);
        Assert.False(inMultiLine);
    }

    [Fact]
    public void StripComments_MultipleCommentsOnSameLine_RemovesAll()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("SELECT /* c1 */ * /* c2 */ FROM Users", config, ref inMultiLine);

        Assert.Equal("SELECT  *  FROM Users", result);
        Assert.False(inMultiLine);
    }

    [Fact]
    public void StripComments_MixedComments_HandlesCorrectly()
    {
        var config = CreateSqlCommentConfig();
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("SELECT /* inline */ * FROM Users -- end comment", config, ref inMultiLine);

        Assert.Equal("SELECT  * FROM Users ", result);
        Assert.False(inMultiLine);
    }

    [Fact]
    public void StripComments_DisabledConfig_ReturnsOriginal()
    {
        var config = new CommentConfig { IgnoreComments = false };
        bool inMultiLine = false;

        var result = CommentFilter.StripComments("SELECT * -- comment", config, ref inMultiLine);

        Assert.Equal("SELECT * -- comment", result);
        Assert.False(inMultiLine);
    }

    #endregion

    #region IsCommentOnlyDifference Tests

    [Fact]
    public void IsCommentOnlyDifference_BothHaveSameCode_DifferentComments_ReturnsTrue()
    {
        var config = CreateSqlCommentConfig();
        var diff = CreateDifference(
            DifferenceType.Modified,
            "SELECT * FROM Users -- Old comment",
            "SELECT * FROM Users -- New comment");

        var result = CommentFilter.IsCommentOnlyDifference(diff, config);

        Assert.True(result);
    }

    [Fact]
    public void IsCommentOnlyDifference_OnlyCommentLines_ReturnsTrue()
    {
        var config = CreateSqlCommentConfig();
        var diff = CreateDifference(
            DifferenceType.Modified,
            "-- Comment version 1",
            "-- Comment version 2");

        var result = CommentFilter.IsCommentOnlyDifference(diff, config);

        Assert.True(result);
    }

    [Fact]
    public void IsCommentOnlyDifference_DifferentCode_ReturnsFalse()
    {
        var config = CreateSqlCommentConfig();
        var diff = CreateDifference(
            DifferenceType.Modified,
            "SELECT * FROM Users",
            "SELECT * FROM Customers");

        var result = CommentFilter.IsCommentOnlyDifference(diff, config);

        Assert.False(result);
    }

    [Fact]
    public void IsCommentOnlyDifference_CodeAndCommentDifferent_ReturnsFalse()
    {
        var config = CreateSqlCommentConfig();
        var diff = CreateDifference(
            DifferenceType.Modified,
            "SELECT * FROM Users -- comment",
            "SELECT * FROM Customers -- different comment");

        var result = CommentFilter.IsCommentOnlyDifference(diff, config);

        Assert.False(result);
    }

    [Fact]
    public void IsCommentOnlyDifference_DisabledConfig_ReturnsFalse()
    {
        var config = new CommentConfig { IgnoreComments = false };
        var diff = CreateDifference(
            DifferenceType.Modified,
            "SELECT * -- comment 1",
            "SELECT * -- comment 2");

        var result = CommentFilter.IsCommentOnlyDifference(diff, config);

        Assert.False(result);
    }

    #endregion

    #region FilterComments Tests

    [Fact]
    public void FilterComments_NoCommentConfig_ReturnsAllDifferences()
    {
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "SELECT * -- c1", "SELECT * -- c2")
        };
        var sourceLines = new List<string> { "SELECT * -- c1" };
        var targetLines = new List<string> { "SELECT * -- c2" };

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, null, sourceLines, targetLines);

        Assert.Single(kept);
        Assert.Empty(commentOnly);
    }

    [Fact]
    public void FilterComments_CommentOnlyDifferences_FiltersCorrectly()
    {
        var config = CreateSqlCommentConfig();
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "SELECT * -- old", "SELECT * -- new", 1, 1),
            CreateDifference(DifferenceType.Modified, "SELECT Id FROM Users", "SELECT Name FROM Users", 2, 2),
            CreateDifference(DifferenceType.Modified, "-- Comment A", "-- Comment B", 3, 3)
        };
        var sourceLines = new List<string> { "SELECT * -- old", "SELECT Id FROM Users", "-- Comment A" };
        var targetLines = new List<string> { "SELECT * -- new", "SELECT Name FROM Users", "-- Comment B" };

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, config, sourceLines, targetLines);

        Assert.Single(kept); // Only the real code change
        Assert.Equal(2, commentOnly.Count); // Two comment-only changes
        Assert.Equal("Comment-Only Change", commentOnly[0].MatchedRuleName);
    }

    [Fact]
    public void FilterComments_MultiLineCommentAcrossLines_HandlesCorrectly()
    {
        var config = CreateSqlCommentConfig();
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "/* Start comment", "/* Different start", 1, 1),
            CreateDifference(DifferenceType.Modified, "Middle of comment", "Middle different", 2, 2),
            CreateDifference(DifferenceType.Modified, "End comment */", "End different */", 3, 3)
        };
        var sourceLines = new List<string> { "/* Start comment", "Middle of comment", "End comment */" };
        var targetLines = new List<string> { "/* Different start", "Middle different", "End different */" };

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, config, sourceLines, targetLines);

        Assert.Empty(kept);
        Assert.Equal(3, commentOnly.Count); // All are comment-only
    }

    [Fact]
    public void FilterComments_MixedCodeAndComments_SeparatesCorrectly()
    {
        var config = CreateSqlCommentConfig();
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "SELECT * -- comment", "SELECT * -- different comment", 1, 1),
            CreateDifference(DifferenceType.Modified, "FROM Users", "FROM Customers", 2, 2),
            CreateDifference(DifferenceType.Modified, "-- Just a comment", "-- Another comment", 3, 3)
        };
        var sourceLines = new List<string> { "SELECT * -- comment", "FROM Users", "-- Just a comment" };
        var targetLines = new List<string> { "SELECT * -- different comment", "FROM Customers", "-- Another comment" };

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, config, sourceLines, targetLines);

        Assert.Single(kept); // Only line 2 (FROM Users vs FROM Customers)
        Assert.Equal(2, commentOnly.Count); // Lines 1 and 3
        Assert.Equal("FROM Users", kept[0].SourceContent);
    }

    [Fact]
    public void FilterComments_EmptyDifferences_ReturnsEmpty()
    {
        var config = CreateSqlCommentConfig();
        var differences = new List<Difference>();
        var sourceLines = new List<string>();
        var targetLines = new List<string>();

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, config, sourceLines, targetLines);

        Assert.Empty(kept);
        Assert.Empty(commentOnly);
    }

    [Fact]
    public void FilterComments_MultiLineCommentWithCodeAfter_HandlesCorrectly()
    {
        var config = CreateSqlCommentConfig();
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "SELECT /* comment */ * FROM Users", "SELECT /* different */ * FROM Users", 1, 1)
        };
        var sourceLines = new List<string> { "SELECT /* comment */ * FROM Users" };
        var targetLines = new List<string> { "SELECT /* different */ * FROM Users" };

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, config, sourceLines, targetLines);

        Assert.Empty(kept);
        Assert.Single(commentOnly); // Same code, different comment
    }

    #endregion

    #region Real-World SQL Scenarios

    [Fact]
    public void FilterComments_SqlStoredProcedureHeader_FiltersCommentChanges()
    {
        var config = CreateSqlCommentConfig();
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "-- Author: John Doe", "-- Author: Jane Smith", 1, 1),
            CreateDifference(DifferenceType.Modified, "-- Created: 2024-01-01", "-- Created: 2025-01-01", 2, 2)
        };
        var sourceLines = new List<string> { "-- Author: John Doe", "-- Created: 2024-01-01" };
        var targetLines = new List<string> { "-- Author: Jane Smith", "-- Created: 2025-01-01" };

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, config, sourceLines, targetLines);

        Assert.Empty(kept);
        Assert.Equal(2, commentOnly.Count); // Author and date comments
    }

    [Fact]
    public void FilterComments_SqlTableDefinitionWithComments_MixedHandling()
    {
        var config = CreateSqlCommentConfig();
        var differences = new List<Difference>
        {
            CreateDifference(DifferenceType.Modified, "[UserId] INT NOT NULL, -- Primary key", "[UserId] INT NOT NULL, -- User identifier", 1, 1),
            CreateDifference(DifferenceType.Modified, "[Status] VARCHAR(20) NULL", "[Status] VARCHAR(50) NULL", 2, 2)
        };
        var sourceLines = new List<string> { "[UserId] INT NOT NULL, -- Primary key", "[Status] VARCHAR(20) NULL" };
        var targetLines = new List<string> { "[UserId] INT NOT NULL, -- User identifier", "[Status] VARCHAR(50) NULL" };

        var (kept, commentOnly) = CommentFilter.FilterComments(differences, config, sourceLines, targetLines);

        Assert.Single(kept); // Status field changed (VARCHAR length)
        Assert.Single(commentOnly); // UserId comment changed
    }

    #endregion
}
