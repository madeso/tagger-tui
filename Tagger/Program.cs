using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Tui;
using Spectre.Tui.App;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Tagger;
using Tagger.Screens;
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
    config.AddCommand<MoveCommand>("move");
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
            .RunAsync(new MainScreen(store));
        return 0;
    }
}

// todo(Gustav): add move files command

internal class MoveCommand : Command<MoveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The pattern to use")]
        public string TargetPattern { get; init; } = "";
    }

    protected override int Execute(CommandContext context, Settings arg, CancellationToken cancellation)
    {
        var store = Store.Load();
        if (store == null) return -1;

        var (pattern, patternErrors) = Pattern.Compile(arg.TargetPattern);
        if (patternErrors.HasErrors())
        {
            foreach (var err in patternErrors.Errors)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Pattern error[/]: {err}");
            }
            return -1;
        }

        int errorCount = 0;
        int moveCount = 0;
        int ignoreCount = 0;

        var cwd = Environment.CurrentDirectory;
        var functions = Pattern.DefaultFunctions();

        var createdDirectories = new HashSet<string>();

        foreach (var f in store.Files)
        {
            var (relative, errorList) = pattern.Eval(functions, f.Properties);
            if (errorList.HasErrors())
            {
                AnsiConsole.MarkupLineInterpolated($"Found errors in: [blue]{f.Path}[/]");
                foreach (var err in errorList.Errors)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Eval error[/]: {err}");
                }

                errorCount += 1;
                continue;
            }
            var target = Path.Join(cwd, relative + Path.GetExtension(f.Path));

            if (Clean(target) == Clean(f.Path))
            {
                AnsiConsole.MarkupLineInterpolated($"Ignoring [blue]{Rel(f.Path)}[/] due to same target");
                ignoreCount += 1;
                continue;
            }

            var dir = Path.GetDirectoryName(target);
            if (dir != null && Directory.Exists(dir) == false && createdDirectories.Contains(dir) == false)
            {
                AnsiConsole.MarkupLineInterpolated($"Created missing directory [blue]{Rel(dir)}[/]");
                createdDirectories.Add(dir);
            }

            AnsiConsole.MarkupLineInterpolated($"Moving file from [blue]{Rel(f.Path)}[/] to [blue]{Rel(target)}[/]");
            moveCount += 1;
        }

        if (ignoreCount > 0)
        {
            AnsiConsole.MarkupLineInterpolated($"Moved [blue]{moveCount}[/] files with [red]{errorCount}[/] errors and [red]{ignoreCount}[/] ignored files.");
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated($"Moved [blue]{moveCount}[/] files with [red]{errorCount}[/] errors.");
        }
        return errorCount > 0 || moveCount == 0 ? -1 : 0;

        string Rel(string p) => Path.GetRelativePath(cwd, p);
        string Clean(string p) => Path.GetFullPath(p);
    }
}
