namespace FolderCompare.Services;

public static class FileDiscovery
{
    public static IEnumerable<FileInfo> DiscoverFiles(
        string path,
        HashSet<string> includeExtensions,
        HashSet<string> excludeExtensions,
        List<string>? ignoreFolders = null)
    {
        return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f))
            .Where(f => ShouldIncludeFile(f, path, includeExtensions, excludeExtensions, ignoreFolders));
    }

    private static bool ShouldIncludeFile(
        FileInfo file,
        string basePath,
        HashSet<string> includeExtensions,
        HashSet<string> excludeExtensions,
        List<string>? ignoreFolders)
    {
        // Check if file is in an ignored folder
        if (ignoreFolders?.Count > 0)
        {
            var relativePath = Path.GetRelativePath(basePath, file.FullName);
            var pathSegments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // Check if any segment matches an ignored folder pattern
            foreach (var ignorePattern in ignoreFolders)
            {
                foreach (var segment in pathSegments)
                {
                    if (MatchesPattern(segment, ignorePattern))
                    {
                        return false;
                    }
                }
            }
        }

        // Check extension filters
        return ShouldIncludeExtension(file.Extension, includeExtensions, excludeExtensions);
    }

    private static bool MatchesPattern(string segment, string pattern)
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

    private static bool ShouldIncludeExtension(
        string extension,
        HashSet<string> includeExtensions,
        HashSet<string> excludeExtensions)
    {
        // If exclude list contains this extension, exclude it
        if (excludeExtensions.Contains(extension))
            return false;

        // If include list is empty, include all non-excluded files
        if (includeExtensions.Count == 0)
            return true;

        // Otherwise, only include if in include list
        return includeExtensions.Contains(extension);
    }

    public static (List<string> onlyInSource, List<string> onlyInTarget, List<string> inBoth) 
        CategorizeFiles(
            Dictionary<string, FileInfo> sourceFiles,
            Dictionary<string, FileInfo> targetFiles)
    {
        var onlyInSource = sourceFiles.Keys.Except(targetFiles.Keys).ToList();
        var onlyInTarget = targetFiles.Keys.Except(sourceFiles.Keys).ToList();
        var inBoth = sourceFiles.Keys.Intersect(targetFiles.Keys).ToList();

        return (onlyInSource, onlyInTarget, inBoth);
    }

    public static Dictionary<string, FileInfo> BuildFileMap(
        IEnumerable<FileInfo> files,
        string basePath)
    {
        var map = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(basePath, file.FullName);
            map[relativePath] = file;
        }

        return map;
    }
}
