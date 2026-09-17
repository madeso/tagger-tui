using Spectre.Tui;
using Spectre.Tui.App;
using System.Collections.Immutable;
using Justify = Spectre.Tui.Justify;
using Paragraph = Spectre.Tui.Paragraph;
using Size = Spectre.Tui.Size;
using TableColumn = Spectre.Tui.TableColumn;
using Text = Spectre.Tui.Text;

namespace Tagger;

public class MainScreen : Screen
{
    private readonly KeyActions _actions;
    private readonly FileTableWidget<FileWithData> _files;

    public MainScreen(IEnumerable<FileWithData> data)
    {
        var dat = data.ToList();
        _files = new TableBuilder<FileWithData>(dat)
            .AddColumn(() => new TableColumn("Sel").RightAligned(), x => Text.FromString(x.IsSelected ? SpectreStrings.CheckMark : ""))
            .AddColumn(() => new TableColumn("Path").StarWidth(1), x => Text.FromString(x.Path))
            .Create();

        _actions = new KeyActions()
            .Bind(KeyBinding.For(Key.Space).WithHelp("Toggle"), (_) => { _files.WithSelected(s => s.Toggle());})
            .Bind(KeyBinding.For('a').WithHelp("Select all"), (_) => { _files.WithAll(s => s.IsSelected = true); })
            .Bind(KeyBinding.For('n').WithHelp("Select none"), (_) => { _files.WithAll(s => s.IsSelected = false); })
            .Bind(KeyBinding.For('x').WithHelp("Extract selected"), ctx =>
            {
                var selectedItems = _files.Items.Where(s => s.IsSelected).ToImmutableArray();
                if (selectedItems.Length == 0)
                {
                    ctx.Push(new PopUp("Nothing is selected"));
                    return;
                }
                ctx.Push(new ExtractScreen(selectedItems));
            })
            .Bind(KeyBinding.For('c').WithHelp("Test popup"), ctx => ctx.Push(new PopUp("This is a test")))
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Quit"), ctx => ctx.Pop())
            ;
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _files);
    }

    public override void Render(RenderContext context)
    {
        var r = RectCut.Begin(context);

        r.DrawClear();
        r.DrawKeymap(k => k.Add(_files.KeyMap).Add(_actions.KeyBinds()));
        r.DrawTitle("Items");
        r.Render(_files);
    }
}

public class ExtractScreen : Screen
{
    private readonly KeyActions _actions;
    private readonly TextBoxWidget _filter = new TextBoxWidget().AsSingleLine().Placeholder("filter");

    private readonly FileTableWidget<FileWithData> _files;

    private bool IsFullscreen { get; set; } = false;

    public ExtractScreen(IEnumerable<FileWithData> data)
    {
        _files = new TableBuilder<FileWithData>(data)
            .AddColumn(() => new TableColumn("Sel").RightAligned(), x => Text.FromString(x.IsSelected ? SpectreStrings.CheckMark : ""))
            .AddColumn(() => new TableColumn("Path").StarWidth(1), x => Text.FromString(x.Path))
            .Create();

        _actions = new KeyActions()
            .Bind(KeyBinding.For(Key.Space).WithHelp("Toggle"), _ => { _files.WithSelected(s => s.Toggle()); })
            .Bind(KeyBinding.For('a').WithHelp("Select all"), _ => { _files.WithAll(s => s.IsSelected = true); })
            .Bind(KeyBinding.For('n').WithHelp("Select none"), _ => { _files.WithAll(s => s.IsSelected = false); })
            .Bind(KeyBinding.For('c').WithHelp("Test popup"), ctx => ctx.Push(new PopUp("This is a test")))
            .Bind(KeyBinding.For('f').WithHelp("Toggle fullscreen"), _ => IsFullscreen = !IsFullscreen)
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Abort"), ctx => ctx.Pop())
            .Bind(KeyBinding.For(Key.Enter).WithHelp("Apply"), ctx => ctx.Pop())
            ;
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _filter);
    }

    public override bool IsTransparent => !IsFullscreen;

    public override void Render(RenderContext context)
    {
        var r = RectCut.Begin(context);

        var clear = IsFullscreen ? Clear.No : Clear.Yes;
        if (IsFullscreen)
        {
            r.DrawClear();
        }

        r.DrawKeymap(k => k.Add(_files.KeyMap).Add(_actions.KeyBinds()), clear);

        if (IsFullscreen == false)
        {
            r.Inset(12);
        }

        r.DrawTitle("Extract", clear);

        r.CutTop(3).Render("Filter", _filter);
        r.Render("Result", _files);
    }
}
