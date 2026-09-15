using System.ComponentModel.Design.Serialization;
using Spectre.Console;
using Spectre.Tui;
using Spectre.Tui.App;
using Justify = Spectre.Tui.Justify;
using Layout = Spectre.Tui.Layout;
using Paragraph = Spectre.Tui.Paragraph;
using Size = Spectre.Tui.Size;

namespace Tagger;


public static class SpectreExtension
{
    public static Rectangle FindArea(this Layout root, RenderContext context, Layout target)
    {
        return root.GetArea(context, target.Name ?? "");
    }

    public static Rectangle RenderFrame(RenderContext context, Layout root, Layout target)
    {
        var middle = root.FindArea(context, target);
        context.Render(new BoxWidget(Color.Red).Border(Border.Plain), middle);
        context.Render(new ClearWidget(' ', Color.Gray), middle.Inflate(-1, -1));

        // Active tab content
        return middle.Inflate(new Size(-10, -4));
    }

    public static BoxWidget Screen(string title, IWidget child)
    {
        return new BoxWidget()
            .Style(Color.Green)
            .Border(Border.Plain)
            .TitlePadding(1)
            .MarkupTitle($"[yellow]{title}[/]")
            .Inner(child);
    }
}

internal class KeymapHelper : IKeyMap
{
    private List<KeyBinding> Items { get; }= new();
    public IEnumerable<KeyBinding> Help()
    {
        return Items;
    }

    public KeymapHelper Add(params KeyBinding[] bind)
    {
        Items.AddRange(bind);
        return this;
    }
    public KeymapHelper Add(params IKeyMap[] bind)
    {
        foreach(var b in bind)
        {
            Items.AddRange(b.Help());
        }
        return this;
    }
    public KeymapHelper Add(IEnumerable<KeyBinding> bind)
    {
        Items.AddRange(bind);
        return this;
    }
}

internal class SingleAction(KeyBinding key, Action<ApplicationContext> click)
{
    public KeyBinding Key => key;

    public void Click(ApplicationContext ctx)
    {
        click(ctx);
    }
}

internal class KeyActions
{
    private List<SingleAction> binds = new();

    public KeyActions Bind(KeyBinding key, Action<ApplicationContext> click)
    {
        binds.Add(new SingleAction(key, click));
        return this;
    }

    public SingleAction? Match(KeyMessage key)
    {
        return binds.FirstOrDefault(x => x.Key.Matches(key));
    }

    public IEnumerable<KeyBinding> KeyBinds() => binds.Select(x => x.Key);
}

public class MainScreen : Screen
{
    private Layout middle = new Layout("middle");
    private Layout bottom = new Layout("bottom").Size(1);
    private Layout layout;
    private FileWidget files;
    private KeyActions actions;

    public MainScreen(IEnumerable<FileWithData> data)
    {
        layout = new Layout("root").SplitRows(middle, bottom);
        files = new FileWidget(data.Select(x => new FileItem(x)).ToList());

        actions = new KeyActions()
            .Bind(KeyBinding.For(Key.Space).WithHelp("Toggle"), (_) => { files.Toggle();})
            .Bind(KeyBinding.For('c').WithHelp("Test popup"), ctx => ctx.Push(new PopUp()))
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Quit"), ctx => ctx.Pop())
            ;
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        if (message is KeyMessage key)
        {
            var found = actions.Match(key);
            if (found != null)
            {
                found.Click(context);
            }
            else
            {
                files.HandleKey(key);
            }
        }
    }

    public override void Render(RenderContext context)
    {
        var body = SpectreExtension.RenderFrame(context, layout, middle);

        context.Render(new HelpWidget(new KeymapHelper().Add(files.KeyMap).Add(actions.KeyBinds())), layout.FindArea(context, bottom));
        context.Render(SpectreExtension.Screen("Items", files.Render()), body);
        /*
        context.Render(
            Paragraph.FromMarkup(
                    """
                    Press [yellow]SPACE[/] to open
                    Press [blue]CTRL+C[/] to quit the application
                    """
                ).Centered()
                .AlignedMiddle()
        );*/
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