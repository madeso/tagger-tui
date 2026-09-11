using System.Text.Json.Serialization;
using Spectre.Console;

namespace Tagger;

public class FileWithData
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class Store
{
    private const string FileName = ".tagger.json";

    [JsonPropertyName("files")]
    public List<FileWithData> Files { get; set; } = [];

    [JsonIgnore]
    public string FilePath { get; set; } = Path.Join(Environment.CurrentDirectory, FileName);
    
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