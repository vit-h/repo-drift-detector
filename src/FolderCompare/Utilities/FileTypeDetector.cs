namespace FolderCompare.Utilities;

public static class FileTypeDetector
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ps1", ".sql", ".cs", ".vb", ".js", ".ts", ".py", ".java", ".cpp", ".h", ".hpp", ".c",
        ".xml", ".json", ".yml", ".yaml", ".ini", ".config", ".txt", ".md", ".rst",
        ".html", ".css", ".scss", ".csv", ".tsv", ".log", ".bat", ".cmd", ".sh",
        ".jsx", ".tsx", ".go", ".rs", ".rb", ".php", ".pl", ".r", ".scala", ".kt",
        ".swift", ".m", ".mm", ".vue", ".dart", ".lua", ".groovy", ".gradle"
    };

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".so", ".dylib", ".msi", ".deb", ".rpm",
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".svg",
        ".mp3", ".mp4", ".avi", ".mkv", ".mov", ".wmv",
        ".pdf", ".docx", ".xlsx", ".pptx", ".doc", ".xls", ".ppt"
    };

    public static bool IsTextFile(string extension)
    {
        return TextExtensions.Contains(extension);
    }

    public static bool IsBinaryFile(string extension)
    {
        return BinaryExtensions.Contains(extension);
    }

    public static HashSet<string> GetDefaultTextExtensions()
    {
        return new HashSet<string>(TextExtensions, StringComparer.OrdinalIgnoreCase);
    }

    public static HashSet<string> GetDefaultBinaryExtensions()
    {
        return new HashSet<string>(BinaryExtensions, StringComparer.OrdinalIgnoreCase);
    }
}
