namespace FolderCompare.Services;

public static class TextNormalizer
{
    public static List<string> NormalizeTextLines(
        string filePath,
        bool ignoreCase,
        bool sortInserts = false,
        int bufferSizeKb = 64)
    {
        var lines = new List<string>();
        var bufferSize = bufferSizeKb * 1024;

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.AsSpan().Trim();
            
            // Skip empty lines
            if (trimmed.IsEmpty)
                continue;

            var normalized = trimmed.ToString();
            
            if (ignoreCase)
                normalized = normalized.ToLowerInvariant();

            lines.Add(normalized);
        }

        // Sort INSERT statements if requested
        if (sortInserts)
        {
            lines = SortInsertStatements(lines);
            lines = SortIndexStatements(lines);
        }

        return lines;
    }

    public static (List<string> lines, Dictionary<int, int> lineMap) NormalizeTextLinesWithMap(
        string filePath,
        bool ignoreCase,
        bool sortInserts = false,
        int bufferSizeKb = 64)
    {
        var lines = new List<string>();
        var lineMap = new Dictionary<int, int>(); // normalizedIndex -> originalLineNumber
        var bufferSize = bufferSizeKb * 1024;

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
        using var reader = new StreamReader(stream);

        string? line;
        int originalLineNumber = 0;
        int normalizedIndex = 0;

        while ((line = reader.ReadLine()) != null)
        {
            originalLineNumber++;
            var trimmed = line.AsSpan().Trim();

            // Skip empty lines
            if (trimmed.IsEmpty)
                continue;

            var normalized = trimmed.ToString();

            if (ignoreCase)
                normalized = normalized.ToLowerInvariant();

            lines.Add(normalized);
            lineMap[normalizedIndex] = originalLineNumber;
            normalizedIndex++;
        }

        // Sort INSERT statements if requested
        if (sortInserts)
        {
            var sortedLines = SortInsertStatements(lines);
            sortedLines = SortIndexStatements(sortedLines);
            
            // Rebuild line map based on sorted order
            var newLineMap = new Dictionary<int, int>();
            for (int i = 0; i < sortedLines.Count; i++)
            {
                // Find original position of this line
                var originalIndex = lines.IndexOf(sortedLines[i]);
                if (originalIndex >= 0 && lineMap.ContainsKey(originalIndex))
                {
                    newLineMap[i] = lineMap[originalIndex];
                }
                else
                {
                    newLineMap[i] = i + 1; // Fallback
                }
            }
            
            return (sortedLines, newLineMap);
        }

        return (lines, lineMap);
    }

    private static List<string> SortInsertStatements(List<string> lines)
    {
        var insertLines = new List<string>();
        var otherLines = new List<(int index, string line)>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var upperLine = line.ToUpperInvariant();
            
            // Check if line starts with INSERT INTO
            if (upperLine.StartsWith("INSERT INTO") || upperLine.StartsWith("INSERT"))
            {
                insertLines.Add(line);
            }
            else
            {
                otherLines.Add((i, line));
            }
        }

        // If no INSERT statements found, return original
        if (insertLines.Count == 0)
            return lines;

        // Sort INSERT statements alphabetically
        insertLines.Sort(StringComparer.OrdinalIgnoreCase);

        // Rebuild the list: keep non-INSERT lines in original positions,
        // and place sorted INSERT lines in their original section
        var result = new List<string>();
        int insertIndex = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var upperLine = lines[i].ToUpperInvariant();
            if (upperLine.StartsWith("INSERT INTO") || upperLine.StartsWith("INSERT"))
            {
                if (insertIndex < insertLines.Count)
                {
                    result.Add(insertLines[insertIndex++]);
                }
            }
            else
            {
                result.Add(lines[i]);
            }
        }

        return result;
    }

    private static List<string> SortIndexStatements(List<string> lines)
    {
        var indexLines = new List<string>();
        var otherLines = new List<(int index, string line)>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var upperLine = line.ToUpperInvariant();
            
            // Check if line starts with CREATE NONCLUSTERED INDEX or CREATE INDEX
            if (upperLine.StartsWith("CREATE NONCLUSTERED INDEX") || 
                upperLine.StartsWith("CREATE INDEX") ||
                upperLine.StartsWith("CREATE CLUSTERED INDEX") ||
                upperLine.StartsWith("CREATE UNIQUE NONCLUSTERED INDEX") ||
                upperLine.StartsWith("CREATE UNIQUE CLUSTERED INDEX") ||
                upperLine.StartsWith("CREATE UNIQUE INDEX"))
            {
                indexLines.Add(line);
            }
            else
            {
                otherLines.Add((i, line));
            }
        }

        // If no INDEX statements found, return original
        if (indexLines.Count == 0)
            return lines;

        // Sort INDEX statements alphabetically
        indexLines.Sort(StringComparer.OrdinalIgnoreCase);

        // Rebuild the list: keep non-INDEX lines in original positions,
        // and place sorted INDEX lines in their original section
        var result = new List<string>();
        int indexIndex = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var upperLine = lines[i].ToUpperInvariant();
            if (upperLine.StartsWith("CREATE NONCLUSTERED INDEX") || 
                upperLine.StartsWith("CREATE INDEX") ||
                upperLine.StartsWith("CREATE CLUSTERED INDEX") ||
                upperLine.StartsWith("CREATE UNIQUE NONCLUSTERED INDEX") ||
                upperLine.StartsWith("CREATE UNIQUE CLUSTERED INDEX") ||
                upperLine.StartsWith("CREATE UNIQUE INDEX"))
            {
                if (indexIndex < indexLines.Count)
                {
                    result.Add(indexLines[indexIndex++]);
                }
            }
            else
            {
                result.Add(lines[i]);
            }
        }

        return result;
    }
}


