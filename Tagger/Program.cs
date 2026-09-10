using System.ComponentModel;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<InitCommand>("init");
    config.AddCommand<AddCommand>("add");
    config.AddCommand<ListCommand>("list");
});
return app.Run(args);

internal class InitCommand : Command<InitCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        System.Console.WriteLine($"Initialized");
        return 0;
    }
}


internal class AddCommand : Command<AddCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<name>")]
        [Description("The file or dir to to add")]
        public string PackageName { get; init; } = string.Empty;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        System.Console.WriteLine($"Added package {settings.PackageName}");
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
        System.Console.WriteLine("Packages:");
        System.Console.WriteLine("  (none yet)");
        return 0;
    }
}
