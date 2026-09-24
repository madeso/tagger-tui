using Spectre.Tui;
using Spectre.Tui.App;
using Tagger.Widgets;

namespace Tagger.Screens;

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