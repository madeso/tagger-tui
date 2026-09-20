using Spectre.Tui;
using Spectre.Tui.App;
using System.Collections.Immutable;
using Tagger.Widgets;
using Util;
using static System.Runtime.InteropServices.JavaScript.JSType;
using TableColumn = Spectre.Tui.TableColumn;
using Text = Spectre.Tui.Text;

namespace Tagger;

public class MainScreen : Screen
{
    private readonly KeyActions _actions;
    private readonly Widgets.TableWidget<FileWithData> _files;

    public MainScreen(Store store)
    {
        var common = store.CalculateCommonFolder();
        _files = new TableBuilder<FileWithData>(store.Files)
            .AddColumn(() => new TableColumn("Sel").RightAligned(), x => Text.FromString(x.IsSelected ? SpectreStrings.CheckMark : ""))
            .AddColumn(() => new TableColumn("Path").StarWidth(1), x => Text.FromString(SolveCommon(common, x.Path)))
            .Create();

        _actions = new KeyActions()
            .Bind(KeyBinding.For(Key.Space).WithHelp("Toggle"), _ => { _files.WithSelected(s => s.Toggle());})
            .Bind(KeyBinding.For('a').WithHelp("Select all"), _ => { _files.WithAll(s => s.IsSelected = true); })
            .Bind(KeyBinding.For('n').WithHelp("Select none"), _ => { _files.WithAll(s => s.IsSelected = false); })
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

    private static string SolveCommon(string? common, string path)
    {
        if (common == null) return path;
        return path.Remove(0, common.Length + 1); // +1 includes directory separator
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _files);
    }

    public override void Render(RenderContext context)
    {
        var r = RectCut.Begin(context);

        r.DrawClear();
        r.DrawKeymap(k => k.Add(_files, _actions));
        r.DrawTitle("Items");
        r.Draw(_files.Render());
    }
}

internal class ExtractedFile(FileWithData file)
{
    public Dictionary<string, string> Properties { get; private set; } = new();

    public string Path => file.Path;
    public string? Message { get; private set; } = null;

    public void UpdateProperties(KeyValueExtractor kve)
    {
        Properties = kve.Extract(new FileInfo(Path), out var message);
        Message = message;
    }
}

public class ExtractScreen : Screen
{
    private readonly KeyActions _actions;
    private readonly TextWidget _filter = new TextWidget(new TextBoxWidget().AsSingleLine().Placeholder("filter"));
    private string _previousFilter = "";
    private string? _parserError = null;
    private readonly ImmutableArray<ExtractedFile> _files;

    private Widgets.TableWidget<ExtractedFile> _grid;
    private bool IsFullscreen { get; set; } = false;
    private readonly FocusHelper _focus;

    public ExtractScreen(IEnumerable<FileWithData> data)
    {
        _files = [..data.Select(x => new ExtractedFile(x))];
        _grid = BuildGrid(null);

        _focus = new FocusHelper(_filter, _grid);

        _actions = new KeyActions()
            .Bind(KeyBinding.For('c').WithHelp("Test popup"), ctx => ctx.Push(new PopUp("This is a test")))
            .Bind(KeyBinding.For('f').WithHelp("Toggle fullscreen"), _ => IsFullscreen = !IsFullscreen)
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Abort"), ctx => ctx.Pop())
            .Bind(KeyBinding.For(Key.Enter).WithHelp("Apply"), ctx => ctx.Pop())
            ;
    }

    private Widgets.TableWidget<ExtractedFile> BuildGrid(KeyValueExtractor? kve)
    {
        var builder = new TableBuilder<ExtractedFile>(_files);
        builder.AddColumn(() => new TableColumn("Path").StarWidth(1), x => Text.FromString(x.Path));

        if (kve != null)
        {
            foreach(var pattern in kve.Patterns)
            {
                builder.AddColumn(() => new TableColumn(pattern).StarWidth(1), x => Text.FromString(x.Properties.GetValueOrDefault(pattern, "")));
            }

            builder.AddColumn(() => new TableColumn("Msg").StarWidth(1), x => Text.FromString(x.Message ?? ""));
        }

        return builder.Create();
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _focus);

        FilterHasChanged();
    }

    private void FilterHasChanged()
    {
        if (_filter.Text == _previousFilter) return;
        _previousFilter = _filter.Text;

        var (parsed, error) = KeyValueExtractor.Compile(_filter.Text);
        _parserError = error;
        if (error != null) return;

        // update all properties
        foreach (var k in _files)
        {
            k.UpdateProperties(parsed);
        }
        _grid = BuildGrid(parsed);
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

        r.DrawKeymap(k => k.Add(_focus, _actions), clear);

        if (IsFullscreen == false)
        {
            r.Inset(4, 4);
        }

        r.DrawTitle("Extract", clear);

        r.CutTop(3).Draw("Filter", _filter);
        if(_parserError != null)
        {
            // todo(Gustav): add a skip/drop function to rect cut that returns this instead of the cut rect
            r.CutTop(1).Draw(Paragraph.FromMarkup($"[red]Parser error[/]: {_parserError}"));
        }
        r.Draw("Result", _grid);
    }
}
