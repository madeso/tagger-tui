using System.Collections.Immutable;
using Spectre.Tui;
using Spectre.Tui.App;
using Tagger.Widgets;

namespace Tagger.Screens;

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
                        var unique = _files.AllItems.Where(x => x.Properties.ContainsKey(key)).ToImmutableArray();
                        var values = unique.Select(x => x.Properties.GetValueOrDefault(key) ?? "")
                            .ToColCounter().MostCommon()
                            .Select(x => new {Value = x.Item1, Count = x.Item2})
                            .ToImmutableArray();
                        if (values.Length <= 1)
                        {
                            SelectItems(key, values[0].Value);
                            return;
                        }
                    
                        ctx.RunSelect(values, x => $"{x.Value} ({x.Count})", sel =>
                        {
                            SelectItems(key, sel.Value);
                        }, null, "Choose value to select");
                    }, null, "Select key");
                    _store.Save();

                    return;

                    void SelectItems(string key, string compareTo)
                    {
                        _files.WithAll(f =>
                        {
                            if (f.Properties.TryGetValue(key, out var value) == false) return;
                            if (compareTo != value) return;

                            f.IsSelected = true;
                        });
                    }
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