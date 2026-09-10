using System.ComponentModel;
using Spectre.Console.Cli;
using Spectre.Tui;
using Spectre.Tui.App;

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


internal class EditCommand : AsyncCommand<EditCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await Application.Create()
            .RunAsync(new MainScreen());
        return 0;
    }
}

public class MainScreen : Screen
{
    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        if (message is KeyMessage key && key.Key == Key.Space)
        {
            context.Push(new PopUp());
        }
    }

    public override void Render(RenderContext context)
    {
        context.Render(
            Paragraph.FromMarkup(
                    """
                    Press [yellow]SPACE[/] to open
                    Press [blue]CTRL+C[/] to quit the application
                    """
                )
                .Centered()
                .AlignedMiddle()
        );
    }
}

public class PopUp : Screen
{
    public override bool IsTransparent => true;

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        if (message is KeyMessage key && key.Key == Key.Escape)
        {
            context.Pop();
        }
    }

    public override void Render(RenderContext context)
    {
        context.Render(
            new PopupWidget(new Size(50, 10))
                .Content(
                    new BoxWidget()
                        .Border(Border.Rounded)
                        .Title("Popup", TitlePosition.Top, Justify.Center)
                        .Inner(Paragraph.FromMarkup("Press [yellow]ESC[/] to close"))
                ));
    }
}