using Spectre.Tui;
using Spectre.Tui.App;
using System.Collections.Immutable;
using Tagger.Widgets;
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
            .Bind(KeyBinding.For(Key.Space).WithHelp("Toggle"), _ =>
            {
                _files.WithSelected(s => s.Toggle());
                // todo(Gustav): should we save here or only when "major" changes have been done?
            })
            .Bind(KeyBinding.For('a').WithHelp("Select all"), _ =>
            {
                _files.WithAll(s => s.IsSelected = true);
                _store.Save();
            })
            .Bind(KeyBinding.For('n').WithHelp("Select none"), _ =>
            {
                _files.WithAll(s => s.IsSelected = false);
                _store.Save();
            })
            .Bind(KeyBinding.For('d').WithHelp("Select props"), ctx =>
            {
                var allKeys = _files.AllItems.SelectMany(x => x.Properties.Keys).ToHashSet();
                ctx.RunSelect(allKeys, x => x, key =>
                {
                    ctx.RunPopup($"You selected {key}");
                }, null, "Select key");
                _store.Save();
            })
            .Bind(KeyBinding.For('x').WithHelp("Extract selected"), ctx =>
            {
                var selectedItems = _files.Items.Where(s => s.IsSelected).ToImmutableArray();
                if (selectedItems.Length == 0)
                {
                    ctx.RunPopup("Nothing is selected");
                    return;
                }
                ctx.Push(new ExtractScreen(selectedItems, () =>
                {
                    _files = BuildGrid(_files);
                    _store.Save();
                }));
            })
            .Bind(KeyBinding.For('p').WithHelp("Properties"), ctx =>
            {
                var selectedItems = _files.Items.Where(s => s.IsSelected).ToImmutableArray();
                if (selectedItems.Length == 0)
                {
                    ctx.RunPopup("Nothing is selected");
                    return;
                }
                ctx.Push(new PropertiesScreen(selectedItems, () =>
                {
                    _files = BuildGrid(_files);
                    _store.Save();
                }));
            })
            .Bind(KeyBinding.For('c').WithHelp("change columns"), ctx => ctx.Push(new ColumnScreen(_store, () =>
            {
                _files = BuildGrid(_files);
                _store.Save();
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
            var (pattern, parse) = Pattern.Compile(src.Pattern);
            builder.AddColumn(() => new TableColumn(src.Label), x => Text.FromString(EvalPattern(pattern, x.Properties)));
        }

        var newGrid = builder.Create();
        newGrid.SelectedIndex = oldGrid?.SelectedIndex ?? 0;
        return newGrid;

        static string EvalPattern(Pattern pattern, Dictionary<string, string> props)
        {
            var (eval, err) = pattern.Eval(Pattern.DefaultFunctions(), props);
            return eval;
        }
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

public class KeyValue(string key, bool allItemsHaveThis, ImmutableHashSet<string> values)
{
    public string Key { get; } = key;
    public bool AllItemsHaveThis { get; } = allItemsHaveThis;
    public ImmutableHashSet<string> Values { get; } = values;

    public void ApplyProperties()
    {
        // todo
    }
}

public class PropertiesScreen : Screen
{
    private readonly KeyActions _actions;

    private readonly ImmutableArray<FileWithData> _files;
    private readonly ImmutableArray<KeyValue> _items;

    private readonly Widgets.TableWidget<KeyValue> _grid;

    public PropertiesScreen(IEnumerable<FileWithData> data, Action ok)
    {
        _files = [.. data];
        _items = BuildFiles(_files);
        _grid = BuildGrid();

        _actions = new KeyActions()
            .Bind(KeyBinding.For(Key.Escape).WithHelp("Abort"), ctx => ctx.Pop())
            .Bind(KeyBinding.For(Key.Enter).WithHelp("Apply"), ctx =>
            {
                foreach (var x in _items)
                {
                    x.ApplyProperties();
                }
                ctx.Pop();
                ok();
            })
            ;
    }

    private ImmutableArray<KeyValue> BuildFiles(ImmutableArray<FileWithData> data)
    {
        // figure out keys
        var keys = data.SelectMany(x => x.Properties.Keys).ToImmutableHashSet();
        return [..keys.Select(MakeKeyValue)];

        KeyValue MakeKeyValue(string key)
        {
            var hasAll = data.All(x => x.Properties.ContainsKey(key));
            var values = data.Select(x => x.Properties.GetValueOrDefault(key)).Where(x => x != null)
                .Select(x => x ?? "").ToImmutableHashSet();
            return new KeyValue(key, hasAll, values);
        }
    }

    private Widgets.TableWidget<KeyValue> BuildGrid()
    {
        var builder = new TableBuilder<KeyValue>(_items);
        builder.AddColumn(() => new TableColumn("Key"), prop => Text.FromString(prop.Key));
        builder.AddColumn(() => new TableColumn("All"), prop => Text.FromString(prop.AllItemsHaveThis ? SpectreStrings.CheckMark : ""));
        builder.AddColumn(() => new TableColumn("Count"), prop => Text.FromString($"{prop.Values.Count}"));
        builder.AddColumn(() => new TableColumn("Value").StarWidth(1), prop => Text.FromString(
            prop.Values.Count == 1 ? prop.Values.First() :
            StringListCombiner.CommaAndNone.CombineFromEnumerable(prop.Values.Select(str => $"{str}"))
            ));

        return builder.Create();
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _grid);
    }

    public override void Render(RenderContext context)
    {
        var r = RectCut.Begin(context);

        r.DrawClear();

        r.DrawKeymap(k => k.Add(_grid, _actions));

        r.Inset(4, 4);

        r.DrawTitle($"Properties ({_files.Length})");

        r.Draw(_grid);
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
                    ctx.RunPopup("Nothing is selected");
                    return;
                }
                ctx.Push(new EditColumnScreen(selected, () => { },
                    () =>
                    {
                        _grid = BuildGrid(_grid);
                    }));
            })
            .Bind(KeyBinding.For('x').WithHelp("Delete column"), ctx =>
            {
                var selected = _grid.SelectedIndex;
                if (selected.HasValue == false)
                {
                    ctx.RunPopup("Nothing is selected");
                    return;
                }
                _store.Columns.RemoveAt(selected.Value);
                _grid.SelectedIndex = CalculateSelectedIndex(selected.Value, _store);
                _grid = BuildGrid(_grid);
            })
            .Bind(KeyBinding.For(Key.Enter, Key.Escape).WithHelp("Done"), ctx =>
            {
                ctx.Pop();
                onDone();
            })
            ;
    }

    private int? CalculateSelectedIndex(int selected, Store store)
    {
        if (store.Columns.Count == 0) return null;

        if (selected == store.Columns.Count && selected > 0)
        {
            return selected - 1;
        }

        return selected;
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
    private readonly TextWidget _label = new TextWidget(new TextBoxWidget().AsSingleLine());
    private readonly TextWidget _pattern = new TextWidget(new TextBoxWidget().AsSingleLine());
    private string _previousLabel;
    private string _previousPattern;
    private bool _patternDirty = false;
    private readonly ColumnDef _column;
    private string? _patternError = null;


    private bool IsFullscreen { get; set; } = true;
    private readonly FocusHelper _focus;

    public EditColumnScreen(ColumnDef column, Action esc, Action ok)
    {
        _focus = new FocusHelper(_label, _pattern);
        _column = column;

        _label.Text = column.Label;
        _pattern.Text = column.Pattern;

        _previousLabel = _label.Text;
        _previousPattern = _pattern.Text;

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
                    ctx.RunPopup("Name and pattern can't be empty");
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

        CheckLabelChange();
        CheckPatternChange();
    }

    private void CheckLabelChange()
    {
        // has label changed?
        if (_label.Text == _previousLabel) return;
        _previousLabel = _label.Text;

        // we should only change pattern for new columns
        if (string.IsNullOrEmpty(_column.Label) && string.IsNullOrEmpty(_column.Label))
        {
            // new colum
        }
        else
        {
            // existing column, don't autogenerate
            return;
        }

        // don't update pattern if it has been changed
        if (_patternDirty) return;

        var lower = _label.Text.ToLowerInvariant();
        _pattern.Text = $"%{lower}%";
        _previousPattern = _pattern.Text;

        UpdatePatternInspection();
    }

    private void CheckPatternChange()
    {
        if (_pattern.Text == _previousPattern) return;
        _previousPattern = _pattern.Text;
        _patternDirty = true;

        UpdatePatternInspection();
    }

    private void UpdatePatternInspection()
    {
        var (pattern, err) = Pattern.Compile(_pattern.Text);
        _patternError = err.HasErrors() ? string.Join("\n", err.Errors) : null;

        // todo(Gustav): update grid with pattern result
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
        if (_patternError != null)
        {
            r.CutTop(1).Draw(Paragraph.FromMarkup($"[red]Pattern error[/]: {_patternError}"));
        }
    }
}
