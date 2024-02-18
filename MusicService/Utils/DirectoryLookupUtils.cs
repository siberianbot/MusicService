using System.Text.RegularExpressions;

namespace MusicService.Utils;

public static class DirectoryLookupUtils
{
    public static IEnumerable<string> GetFilesRecursive(string path, IEnumerable<string> extensions)
    {
        string pattern = $@"\.({string.Join('|', extensions)})$";
        const RegexOptions options = RegexOptions.Compiled |
                                     RegexOptions.CultureInvariant |
                                     RegexOptions.IgnoreCase |
                                     RegexOptions.RightToLeft;

        return GetFilesRecursive(path, new Regex(pattern, options));
    }

    private static IEnumerable<string> GetFilesRecursive(string path, Regex regex)
    {
        // ReSharper disable once InvokeAsExtensionMethod
        return Enumerable.Concat(
            Directory.EnumerateFiles(path).Where(file => regex.IsMatch(file)),
            Directory.EnumerateDirectories(path, "*", new EnumerationOptions())
                .SelectMany(x => GetFilesRecursive(x, regex))
        ).Select(Path.GetFullPath);
    }
}