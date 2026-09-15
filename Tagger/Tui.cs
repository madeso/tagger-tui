using Spectre.Console;
using Spectre.Tui;
using Spectre.Tui.App;
using System.ComponentModel.Design.Serialization;
using System.Xml.Linq;
using Justify = Spectre.Tui.Justify;
using Layout = Spectre.Tui.Layout;
using Paragraph = Spectre.Tui.Paragraph;
using Size = Spectre.Tui.Size;
using TableColumn = Spectre.Tui.TableColumn;
using Text = Spectre.Tui.Text;

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
    private readonly Layout _middle = new Layout("middle");
    private readonly Layout _bottom = new Layout("bottom").Size(1);
    private readonly Layout _layout;
    private readonly ScrollableListWidget<FileItem> _files; // _scrollableLists;
    private readonly KeyActions _actions;

    private readonly FileTableWidget<FileWithData> _scrollableLists; // _files;

    public MainScreen(IEnumerable<FileWithData> data)
    {
        _layout = new Layout("root").SplitRows(_middle, _bottom);

        var dat = data.ToList();
        _files = new ScrollableListWidget<FileItem>(dat.Select(x => new FileItem(x)).ToList());
        _scrollableLists = new TableBuilder<FileWithData>(dat)
            .AddColumn(() => new TableColumn("Sel").RightAligned(), x => Text.FromString(x.IsSelected ? "x" : ""))
            .AddColumn(() => new TableColumn("Path").StarWidth(1), x => Text.FromString(x.Path))
            .Create();

        _actions = new KeyActions()
            .Bind(KeyBinding.For(Key.Space).WithHelp("Toggle"), (_) => { _scrollableLists.WithSelected(s => s.Toggle());})
            .Bind(KeyBinding.For('c').WithHelp("Test popup"), ctx => ctx.Push(new PopUp()))
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Quit"), ctx => ctx.Pop())
            ;
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        if (message is KeyMessage key)
        {
            var found = _actions.Match(key);
            if (found != null)
            {
                found.Click(context);
            }
            else
            {
                _scrollableLists.HandleKey(key);
            }
        }
    }

    public override void Render(RenderContext context)
    {
        var body = SpectreExtension.RenderFrame(context, _layout, _middle);

        context.Render(new HelpWidget(new KeymapHelper().Add(_scrollableLists.KeyMap).Add(_actions.KeyBinds())), _layout.FindArea(context, _bottom));
        context.Render(SpectreExtension.Screen("Items", _scrollableLists.Render()), body);
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