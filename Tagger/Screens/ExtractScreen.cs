using System.Collections.Immutable;
using Spectre.Tui;
using Spectre.Tui.App;
using Tagger.Widgets;

namespace Tagger.Screens;

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

    public void ApplyProperties()
    {
        if (Message == null) return;
        foreach (var (key, value) in Properties)
        {
            file.Properties[key] = value;
        }
    }
}

public class ExtractScreen : Screen
{
    private readonly KeyActions _actions;
    private readonly TextWidget _filter = new TextWidget(new TextBoxWidget().AsSingleLine());
    private string _previousFilter = "";
    private string? _parserError = null;
    private readonly ImmutableArray<ExtractedFile> _files;

    private Widgets.TableWidget<ExtractedFile> _grid;
    private bool IsFullscreen { get; set; } = true;
    private FocusHelper _focus;

    private bool DisplayErrors { get; set; } = true;

    public ExtractScreen(IEnumerable<FileWithData> data, Action ok)
    {
        _files = [..data.Select(x => new ExtractedFile(x))];
        (_grid, _focus) = BuildGrid(null, null, null);

        _actions = new KeyActions()
                .Bind(KeyBinding.For('f').WithHelp("Toggle fullscreen"), _ => IsFullscreen = !IsFullscreen)
                .Bind(KeyBinding.For('e').WithHelp("Display errors"), _ =>
                {
                    DisplayErrors = !DisplayErrors;
                    UpdateGrid();
                })
                .Bind(KeyBinding.For(Key.Escape).WithHelp("Abort"), ctx => ctx.Pop())
                .Bind(KeyBinding.For(Key.Enter).WithHelp("Apply"), ctx =>
                {
                    foreach (var x in _files)
                    {
                        x.ApplyProperties();
                    }
                    ctx.Pop();
                    ok();
                })
            ;
    }

    private (Widgets.TableWidget<ExtractedFile>, FocusHelper) BuildGrid(KeyValueExtractor? kve, Widgets.TableWidget<ExtractedFile>? prevGrid, FocusHelper? prevFocus)
    {
        IEnumerable<ExtractedFile> filesToView = _files;
        if (DisplayErrors == false)
        {
            filesToView = _files.Where(x => string.IsNullOrEmpty(x.Message));
        }
        var builder = new TableBuilder<ExtractedFile>(filesToView);
        builder.AddColumn(() => new TableColumn("Path").StarWidth(1), x => Text.FromString(x.Path));

        if (kve != null)
        {
            foreach(var pattern in kve.Patterns)
            {
                builder.AddColumn(() => new TableColumn(pattern).StarWidth(1), x => Text.FromString(x.Properties.GetValueOrDefault(pattern, "")));
            }

            builder.AddColumn(() => new TableColumn("Msg").StarWidth(1), x => Text.FromString(x.Message ?? ""));
        }

        var newGrid = builder.Create();
        var newFocus = new FocusHelper(_filter, newGrid);

        if (prevGrid != null)
        {
            newGrid.SelectedIndex = prevGrid.SelectedIndex;
        }
        if (prevFocus != null)
        {
            newFocus.SelectedIndex = prevFocus.SelectedIndex;
        }

        return (newGrid, newFocus);
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

        UpdateGrid();
    }

    private void UpdateGrid()
    {
        var (parsed, error) = KeyValueExtractor.Compile(_filter.Text);
        _parserError = error;
        if (error != null) return;

        // update all properties
        foreach (var k in _files)
        {
            k.UpdateProperties(parsed);
        }
        (_grid, _focus) = BuildGrid(parsed, _grid, _focus);
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