using System.Security.Cryptography;

namespace FolderCompare.Utilities;

public static class HashComparer
{
    private static readonly ThreadLocal<SHA256> Sha256Provider = new(() => SHA256.Create());

    public static string ComputeFileHash(string filePath, int bufferSizeKb = 64)
    {
        var bufferSize = bufferSizeKb * 1024;
        
        using var stream = new FileStream(
            filePath, 
            FileMode.Open, 
            FileAccess.Read,
            FileShare.Read, 
            bufferSize, 
            FileOptions.SequentialScan);
        
        var sha256 = Sha256Provider.Value!;
        var hash = sha256.ComputeHash(stream);
        
        return Convert.ToHexString(hash);
    }

    public static bool CompareFileHash(string fileA, string fileB, int bufferSizeKb = 64)
    {
        try
        {
            var hashA = ComputeFileHash(fileA, bufferSizeKb);
            var hashB = ComputeFileHash(fileB, bufferSizeKb);
            return hashA.Equals(hashB, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
