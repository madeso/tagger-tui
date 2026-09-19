using Spectre.Console;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Tagger;

public class FileWithData
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();

    [JsonIgnore]
    public bool IsSelected { get; set; } = false;

    public void Toggle()
    {
        IsSelected = !IsSelected;
    }
}

public class Store
{
    private const string FileName = ".tagger.json";

    [JsonPropertyName("files")]
    public List<FileWithData> Files { get; set; } = [];

    [JsonIgnore]
    public string FilePath { get; set; } = Path.Join(Environment.CurrentDirectory, FileName);

    private static string GetLongestCommonPrefix(string[] s)
    {
        int k = s[0].Length;
        for (int i = 1; i < s.Length; i++)
        {
            k = Math.Min(k, s[i].Length);
            for (int j = 0; j < k; j++)
                if (s[i][j] != s[0][j])
                {
                    k = j;
                    break;
                }
        }
        return s[0].Substring(0, k);
    }

    private static string GetLongestCommonPrefix(string first, IEnumerable<string> strings)
    {
        var longestPrefix = first.Length;
        foreach (var current in strings)
        {
            longestPrefix = Math.Min(longestPrefix, current.Length);
            for (var index = 0; index < longestPrefix; index++)
            {
                if (current[index] == first[index]) continue;
                longestPrefix = index;
                break;
            }
        }
        return first[..longestPrefix];
    }

    public string? CalculateCommonFolder()
    {
        string common = GetLongestCommonPrefix(GetFileSystemCasing(Files[0].Path), Files.Select(x => GetFileSystemCasing(x.Path)));
        if (string.IsNullOrEmpty(common)) return null;
        var clean = Path.GetDirectoryName(common);
        return clean;
    }

    private string GetFileSystemCasing(string path)
    {
        if (!Path.IsPathRooted(path))
        {
            return path;
        }

        path = path.TrimEnd(Path.DirectorySeparatorChar); // if you type c:\foo\ instead of c:\foo
        try
        {
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name)) return path.ToUpper() + Path.DirectorySeparatorChar; // root reached

            var parent = Path.GetDirectoryName(path); // retrieving parent of element to be corrected
            if (parent == null)
            {
                return path;
            }

            parent = GetFileSystemCasing(parent); //to get correct casing on the entire string, and not only on the last element

            var diParent = new DirectoryInfo(parent);
            var fsiChildren = diParent.GetFileSystemInfos(name);
            var fsiChild = fsiChildren.First();
            return fsiChild.FullName; // coming from GetFileSystemImfos() this has the correct case
        }
        catch (Exception ex)
        {
            return path;
        }
    }

    public static Store? Load(bool print = true)
    {
        var dir = Environment.CurrentDirectory;

        var tried = new List<string>();

        while (true)
        {
            var file = Path.Join(dir, FileName);
            var loaded = JsonUtil.GetOrNull<Store>(file);
            if (loaded != null)
            {
                loaded.FilePath = file;
                return loaded;
            }
            tried.Add(file);

            var next = Path.GetDirectoryName(dir);
            if (next == null)
            {
                if (print)
                {
                    AnsiConsole.WriteLine($"Failed to find a valid tagger file, tried [{tried}]");
                }

                return null;
            }
            dir = next;
        }
    }

    public void Save()
    {
        JsonUtil.Save(FilePath, this);
    }
}