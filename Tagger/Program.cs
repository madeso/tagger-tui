using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Tui;
using Spectre.Tui.App;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    config.AddCommand<RemoveCommand>("remove");
    config.AddCommand<ListCommand>("list");
    config.AddCommand<EditCommand>("edit");
    config.AddCommand<MoveCommand>("move");
    config.AddCommand<CleanCommand>("clean");
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
        foreach (var pathArg in Glob.Files(arg.FileNames))
        {
            var path = pathArg.CleanPath();
            if (File.Exists(path) == false)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Failed to add missing [blue]{path}[/]");
                continue;
            }

            if (store.Files.Find(x => path.IsSamePath(x.Path)) != null)
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

internal class RemoveCommand : Command<RemoveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The files to to remove")]
        public string[] FileNames { get; init; } = [];
    }

    protected override int Execute(CommandContext context, Settings arg, CancellationToken cancellation)
    {
        var store = Store.Load();
        if (store == null) return -1;

        var cwd = Environment.CurrentDirectory;

        int removed = 0;
        foreach (var pathArg in Glob.Files(arg.FileNames))
        {
            var path = pathArg.CleanPath();
            if (File.Exists(path) == false)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Failed to remove [blue]{path}[/]");
                continue;
            }

            if (store.Files.RemoveAll(x => path.IsSamePath(x.Path)) == 0)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: No files named [blue]{Rel(path)}[/] was in the store");
                continue;
            }

            removed += 1;
        }

        if (removed == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/]: Removed no files");
            return -1;
        }

        store.Save();

        AnsiConsole.MarkupLineInterpolated($"Removed [blue]{removed}[/] files");
        return 0;

        string Rel(string p) => Path.GetRelativePath(cwd, p);
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
            var fPath = f.Path.CleanPath();
            var rel = Path.GetRelativePath(Environment.CurrentDirectory, fPath);
            var ex = File.Exists(fPath) ? "exists" : "missing";
            var excol = File.Exists(fPath) ? "green" : "red";
            AnsiConsole.MarkupLineInterpolated($"* {rel}: [{excol}]{ex}[/]");
        }
        AnsiConsole.MarkupLineInterpolated($"[blue]{store.Files.Count}[/] file(s)");
        return 0;
    }
}


internal class CleanCommand : Command<CleanCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var store = Store.Load();
        if (store == null) return -1;

        int changes = 0;

        var removed = store.Files.Where(f => File.Exists(f.Path.CleanPath()) == false).ToImmutableArray();
        if (removed.Length > 0)
        {
            foreach (var x in removed)
            {
                AnsiConsole.MarkupLineInterpolated($"Removing missing [red]{x.Path.RelativePath()}[/]");
                store.Files.Remove(x);
            }

            AnsiConsole.MarkupLineInterpolated($"Removed [blue]{removed}[/] missing files");
            changes += removed.Length;
        }

        foreach (var f in store.Files)
        {
            var fPath = f.Path.CleanPath();

            if (fPath != f.Path)
            {
                f.Path = fPath;
                changes += 1;
                AnsiConsole.MarkupLineInterpolated($"Normalized [blue]{fPath.RelativePath()}[/]");
            }
        }

        store.Save();
        AnsiConsole.MarkupLineInterpolated($"Did [blue]{changes}[/] changes");
        return 0;
    }
}


// todo(Gustav): optionally take a mask glob and only operate on files that fit the mask
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

// todo(Gustav): add argument to also move "related" files (and figure out what related files include)
internal class MoveCommand : Command<MoveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The pattern to use")]
        public string TargetPattern { get; init; } = "";

        [CommandOption("-r|--run")]
        [Description("Run the actual move")]
        public bool Run { get; init; }

        [CommandOption("-x|--extension")]
        [Description("Pattern also include the file extension")]
        public bool PatternHasExtension { get; init; }
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
            var fPath = f.Path.CleanPath();
            var (relative, errorList) = pattern.Eval(functions, f.Properties, Pattern.AttributeEval.ErrorIfMissing);
            if (errorList.HasErrors())
            {
                AnsiConsole.MarkupLineInterpolated($"Found errors in: [blue]{fPath}[/]");
                foreach (var err in errorList.Errors)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Eval error[/]: {err}");
                }

                errorCount += 1;
                continue;
            }

            if (File.Exists(fPath) == false)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Missing file[/]: {fPath}");
                errorCount += 1;
                continue;
            }

            var relWithExt = arg.PatternHasExtension ? relative : relative + Path.GetExtension(fPath);
            var target = Path.Join(cwd, relWithExt).CleanPath();

            if (target == fPath.CleanPath())
            {
                AnsiConsole.MarkupLineInterpolated($"Ignoring [blue]{Rel(fPath)}[/] due to same target");
                ignoreCount += 1;
                continue;
            }

            var dir = Path.GetDirectoryName(target);
            if (dir != null && Directory.Exists(dir) == false && createdDirectories.Add(dir))
            {
                AnsiConsole.MarkupLineInterpolated($"Creating missing directory [blue]{Rel(dir)}[/]");
                try
                {
                    if (arg.Run)
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                catch (Exception x)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ERR[/]: {x.Message}");
                    errorCount += 1;
                }
            }

            AnsiConsole.MarkupLineInterpolated($"Moving file from [blue]{Rel(fPath)}[/] to [blue]{Rel(target)}[/]");
            try
            {
                if (arg.Run)
                {
                    File.Move(f.Path.CleanPath(), target);
                    f.Path = target;
                }
                moveCount += 1;
            }
            catch (Exception x)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]ERR[/]: {x.Message}");
                errorCount += 1;
            }
        }

        if (ignoreCount > 0)
        {
            AnsiConsole.MarkupLineInterpolated($"Moved [blue]{moveCount}[/] files with [red]{errorCount}[/] errors and [red]{ignoreCount}[/] ignored files.");
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated($"Moved [blue]{moveCount}[/] files with [red]{errorCount}[/] errors.");
        }
        store.Save();
        return errorCount > 0 || moveCount == 0 ? -1 : 0;

        string Rel(string p) => Path.GetRelativePath(cwd, p);
    }
}

internal static class FileTool
{
    public static string CleanPath(this string p) => new FileInfo(p).FullName;
    public static string RelativePath(this string p) => Path.GetRelativePath(Environment.CurrentDirectory, p);

    public static bool IsSamePath(this string lhsString, string rhsString)
    {
        var lhs = lhsString.CleanPath();
        var rhs = rhsString.CleanPath();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // case-sensitive file system
            lhs = lhs.ToLower();
            rhs = rhs.ToLower();
        }
        var isSamePath = lhs == rhs;
        return isSamePath;
    }
}
