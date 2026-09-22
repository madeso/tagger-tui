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
    private Widgets.TableWidget<FileWithData> _files;
    private readonly Store _store;

    public MainScreen(Store store)
    {
        _store = store;

        _files = BuildGrid(null);

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
                ctx.Push(new ExtractScreen(selectedItems, () =>
                {
                    _files = BuildGrid(_files);
                }));
            })
            .Bind(KeyBinding.For('c').WithHelp("change columns"), ctx => ctx.Push(new ColumnScreen(_store, () =>
            {
                _files = BuildGrid(_files);
            })))
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Quit"), ctx => ctx.Pop())
            ;
    }

    private Widgets.TableWidget<FileWithData> BuildGrid(Widgets.TableWidget<FileWithData>? oldGrid)
    {
        var common = _store.CalculateCommonFolder();
        var builder = new TableBuilder<FileWithData>(_store.Files);
        builder.AddColumn(() => new TableColumn("Sel").RightAligned(), x => Text.FromString(x.IsSelected ? SpectreStrings.CheckMark : ""));
        builder.AddColumn(() => new TableColumn("Path"), x => Text.FromString(SolveCommon(common, x.Path)));

        foreach (var src in _store.Columns)
        {
            // todo(Gustav): improve pattern
            builder.AddColumn(() => new TableColumn(src.Label), x => Text.FromString(x.Properties.GetValueOrDefault(src.Pattern) ?? ""));
        }

        var newGrid = builder.Create();
        newGrid.SelectedIndex = oldGrid?.SelectedIndex ?? 0;
        return newGrid;
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
    private readonly TextWidget _filter = new TextWidget(new TextBoxWidget().AsSingleLine().Placeholder("filter"));
    private string _previousFilter = "";
    private string? _parserError = null;
    private readonly ImmutableArray<ExtractedFile> _files;

    private Widgets.TableWidget<ExtractedFile> _grid;
    private bool IsFullscreen { get; set; } = true;
    private readonly FocusHelper _focus;

    public ExtractScreen(IEnumerable<FileWithData> data, Action ok)
    {
        _files = [..data.Select(x => new ExtractedFile(x))];
        _grid = BuildGrid(null);

        _focus = new FocusHelper(_filter, _grid);

        _actions = new KeyActions()
            .Bind(KeyBinding.For('c').WithHelp("Test popup"), ctx => ctx.Push(new PopUp("This is a test")))
            .Bind(KeyBinding.For('f').WithHelp("Toggle fullscreen"), _ => IsFullscreen = !IsFullscreen)
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


public class ColumnScreen : Screen
{
    private readonly KeyActions _actions;
    private Widgets.TableWidget<ColumnDef> _grid;
    private readonly Store _store;

    public ColumnScreen(Store store, Action onDone)
    {
        _store = store;
        _grid = BuildGrid(null);

        _actions = new KeyActions()
            .Bind(KeyBinding.For('c').WithHelp("Test popup"), ctx => ctx.Push(new PopUp("This is a test")))
            .Bind(KeyBinding.For('a').WithHelp("Add column"), ctx =>
            {
                var add = new ColumnDef();
                ctx.Push(new EditColumnScreen(add, () => { },
                    () =>
                    {
                        _store.Columns.Add(add);
                        _grid = BuildGrid(_grid);
                    }));
            })
            .Bind(KeyBinding.For('e').WithHelp("Edit column"), ctx =>
            {
                var selected = _grid.SelectedItem;
                if(selected == null)
                {
                    ctx.Push(new PopUp("Nothing is selected"));
                    return;
                }
                ctx.Push(new EditColumnScreen(selected, () => { },
                    () =>
                    {
                        _grid = BuildGrid(_grid);
                    }));
            })
            .Bind(KeyBinding.For('x').WithHelp("Delete column"), ctx => ctx.Push(new PopUp("todo")))
            .Bind(KeyBinding.For(Key.Enter, Key.Escape).WithHelp("Done"), ctx =>
            {
                ctx.Pop();
                onDone();
            })
            ;
    }

    private Widgets.TableWidget<ColumnDef> BuildGrid(Widgets.TableWidget<ColumnDef>? previousGrid)
    {
        var builder = new TableBuilder<ColumnDef>(_store.Columns);
        builder.AddColumn(() => new TableColumn("Label").StarWidth(1), x => Text.FromString(x.Label));
        builder.AddColumn(() => new TableColumn("Pattern").StarWidth(1), x => Text.FromString(x.Pattern));
        var newGrid = builder.Create();
        newGrid.SelectedIndex = previousGrid?.SelectedIndex ?? 0;
        return newGrid;
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _grid);
    }

    public override bool IsTransparent => false;

    public override void Render(RenderContext context)
    {
        var r = RectCut.Begin(context);

        r.DrawClear();

        r.DrawKeymap(k => k.Add(_grid, _actions));

        r.DrawTitle("Columns");

        r.Draw("Result", _grid);
    }
}


public class EditColumnScreen : Screen
{
    private readonly KeyActions _actions;
    private readonly TextWidget _label = new TextWidget(new TextBoxWidget().AsSingleLine().Placeholder("label"));
    private readonly TextWidget _pattern = new TextWidget(new TextBoxWidget().AsSingleLine().Placeholder("pattern"));
    
    private bool IsFullscreen { get; set; } = true;
    private readonly FocusHelper _focus;

    public EditColumnScreen(ColumnDef column, Action esc, Action ok)
    {
        _focus = new FocusHelper(_label, _pattern);

        _actions = new KeyActions()
            .Bind(KeyBinding.For('f').WithHelp("Toggle fullscreen"), _ => IsFullscreen = !IsFullscreen)
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Abort"), ctx =>
            {
                ctx.Pop();
                esc();
            })
            .Bind(KeyBinding.For(Key.Enter).WithHelp("Apply"), ctx =>
            {
                if (string.IsNullOrEmpty(_label.Text) || string.IsNullOrEmpty(_pattern.Text))
                {
                    ctx.Push(new PopUp("Name and pattern can't be empty"));
                }

                column.Label = _label.Text;
                column.Pattern = _pattern.Text;

                ctx.Pop();
                ok();
            })
            ;
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _focus);

        FilterHasChanged();
    }

    private void FilterHasChanged()
    {
        // todo(Gustav): set pattern from name
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

        r.DrawTitle("Add/edit column", clear);

        r.CutTop(3).Draw("Label", _label);
        r.CutTop(3).Draw("Pattern", _pattern);
    }
}
