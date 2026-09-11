using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Tui;
using Spectre.Tui.App;
using System.ComponentModel;
using System.Reflection;
using Tagger;
using Justify = Spectre.Tui.Justify;
using Layout = Spectre.Console.Layout;
using Paragraph = Spectre.Tui.Paragraph;
using Size = Spectre.Tui.Size;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<InitCommand>("init");
    config.AddCommand<AddCommand>("add");
    config.AddCommand<ListCommand>("list");
    config.AddCommand<EditCommand>("edit");
});
return app.Run(args);

internal class InitCommand : Command<InitCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var loaded = Store.Load(false);
        if (loaded != null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Found store at [blue]{loaded.FilePath}[/], aborting");
            return -1;
        }

        var store = new Store();
        store.Save();

        AnsiConsole.MarkupLineInterpolated($"Initialized to [blue]{store.FilePath}[/]");
        return 0;
    }
}

internal static class Glob
{
    public static IEnumerable<string> Files(IEnumerable<string> args)
    {
        var m = new Matcher();

        // does this recursive add all files?

        m.AddIncludePatterns(args);
        var r = m.GetResultsInFullPath(Environment.CurrentDirectory);

        // get absolute paths
        return r.Select(f => (new FileInfo(f)).FullName);
    }
}

internal class AddCommand : Command<AddCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The files to to add")]
        public string[] FileNames { get; init; } = [];
    }

    protected override int Execute(CommandContext context, Settings arg, CancellationToken cancellation)
    {
        var store = Store.Load();
        if (store == null) return -1;

        int added = 0;
        foreach (var path in Glob.Files(arg.FileNames))
        {
            if (File.Exists(path) == false)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Failed to add [blue]{path}[/]");
                continue;
            }

            if (store.Files.Find(x => path == x.Path) != null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Already contains [blue]{path}[/]");
                continue;
            }

            store.Files.Add(new FileWithData{Path = path});
            added += 1;
        }

        if (added == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Added no files");
            return -1;
        }

        store.Save();

        AnsiConsole.MarkupLineInterpolated($"Added [blue]{added}[/] files");
        return 0;
    }
}

internal class ListCommand : Command<ListCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {

        var store = Store.Load();
        if(store == null) return -1;
        foreach (var f in store.Files)
        {
            AnsiConsole.MarkupLineInterpolated($"* {Path.GetRelativePath(Environment.CurrentDirectory, f.Path)}");
        }
        AnsiConsole.MarkupLineInterpolated($"[blue]{store.Files.Count}[/] file(s)");
        return 0;
    }
}


internal class EditCommand : AsyncCommand<EditCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var store = Store.Load();
        if (store == null) return -1;

        await Application.Create()
            .RunAsync(new MainScreen(store.Files));
        return 0;
    }
}
