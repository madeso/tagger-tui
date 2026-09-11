using System.Text.Json;
using Spectre.Console;

namespace Tagger;

public static class JsonUtil
{
    private static readonly JsonSerializerOptions json_options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static T? Parse<T>(string file, string content)
        where T : class
    {
        try
        {
            var loaded = JsonSerializer.Deserialize<T>(content, json_options);
            if (loaded == null) { throw new Exception("internal error"); }
            return loaded;
        }
        catch (JsonException err)
        {
            AnsiConsole.WriteLine($"Unable to parse json {file}: {err.Message}");
            return null;
        }
        catch (NotSupportedException err)
        {
            AnsiConsole.WriteLine($"Unable to parse json {file}: {err.Message}");
            return null;
        }
    }

    public static T? GetOrNull<T>(string path)
        where T : class
    {
        if (!Path.Exists(path))
        {
            return null;
        }

        var content = File.ReadAllText(path);
        var parsed = Parse<T>(path, content);
        return parsed;
    }

    internal static string Write<T>(T self)
    {
        return JsonSerializer.Serialize(self, json_options);
    }

    internal static void Save<T>(string path, T data)
    {
        File.WriteAllText(path, Write(data));
    }
}
