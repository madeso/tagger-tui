using System.Collections.Immutable;
using Spectre.Tui;
using Spectre.Tui.App;
using Tagger.Widgets;

namespace Tagger.Screens;

public class KeyValue(string key, bool allItemsHaveThis, ImmutableHashSet<string> values)
{
    public string Key { get; } = key;
    public bool AllItemsHaveThis { get; } = allItemsHaveThis;
    public ImmutableHashSet<string> Values { get; set; } = values;
    public bool IsDirty { get; private set; } = false;

    public void ApplyProperties()
    {
        // todo
    }

    public void ChangeValues(Func<string, string> changeValue)
    {
        Values = Values.Select(changeValue).ToImmutableHashSet();
        IsDirty = true;
    }
}

public class PropertiesScreen : Screen
{
    private readonly KeyActions _actions;

    private readonly ImmutableArray<FileWithData> _files;
    private readonly ImmutableArray<KeyValue> _items;

    private Widgets.TableWidget<KeyValue> _grid;

    public PropertiesScreen(IEnumerable<FileWithData> data, Action ok)
    {
        _files = [.. data];
        _items = BuildFiles(_files);
        _grid = BuildGrid(null);

        _actions = new KeyActions()
                .Bind(KeyBinding.For('c').WithHelp("Capitalize"), _ => Perform(k => k.ChangeValues(s => s.Capitalize())))
                .Bind(KeyBinding.For('l').WithHelp("Capitalize"), _ => Perform(k => k.ChangeValues(s => s.ToLowerInvariant())))
                .Bind(KeyBinding.For('t').WithHelp("Trim"), _ => Perform(k => k.ChangeValues(s => s.Trim())))
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

    private void Perform(Action<KeyValue> action)
    {
        var k = _grid.SelectedItem;
        if (k == null) return;
        action(k);
        _grid = BuildGrid(_grid);
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

    private Widgets.TableWidget<KeyValue> BuildGrid(Widgets.TableWidget<KeyValue>? oldGrid)
    {
        var builder = new TableBuilder<KeyValue>(_items);
        builder.AddColumn(() => new TableColumn("Modified"), prop => Text.FromString(prop.IsDirty ? SpectreStrings.CheckMark : ""));
        builder.AddColumn(() => new TableColumn("Key"), prop => Text.FromString(prop.Key));
        builder.AddColumn(() => new TableColumn("All"), prop => Text.FromString(prop.AllItemsHaveThis ? SpectreStrings.CheckMark : ""));
        builder.AddColumn(() => new TableColumn("Count"), prop => Text.FromString($"{prop.Values.Count}"));
        builder.AddColumn(() => new TableColumn("Value").StarWidth(1), prop => Text.FromString(
            prop.Values.Count == 1 ? prop.Values.First() :
                StringListCombiner.CommaAndNone.CombineFromEnumerable(prop.Values.Select(str => $"{str}"))
        ));

        var newGrid = builder.Create();
        if (oldGrid != null)
        {
            newGrid.SelectedIndex = oldGrid.SelectedIndex;
        }

        return newGrid;
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