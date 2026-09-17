using Spectre.Console;
using Spectre.Tui;
using Spectre.Tui.App;
using System.Collections.Immutable;
using Padding = Spectre.Tui.Padding;
using TableColumn = Spectre.Tui.TableColumn;
using TableRow = Spectre.Tui.TableRow;
using Text = Spectre.Tui.Text;

namespace Tagger.Widgets;


public sealed class Row<T>(T t, ImmutableArray<ColumnDef<T>> columns)
    : TableRow
{
    protected override Text[] CreateCells(bool isSelected)
    {
        return columns.Select(x => x.CreateCell(Item)).ToArray();
    }

    public T Item => t;
}

public class ColumnDef<T>(Func<TableColumn> create, Func<T, Text> cell)
{
    public Func<TableColumn> CreateColumn { get; } = create;
    public Func<T, Text> CreateCell { get; } = cell;
}

public class TableBuilder<T>(IEnumerable<T> items) where T : class
{
    private readonly List<ColumnDef<T>> _columns = new();

    public TableBuilder<T> AddColumn(Func<TableColumn> create, Func<T, Text> cell)
    {
        _columns.Add(new ColumnDef<T>(create, cell));
        return this;
    }

    public TableWidget<T> Create()
    {
        var cols = _columns.ToImmutableArray();
        return new TableWidget<T>(items.Select(x => new Row<T>(x, cols)).ToList(), cols);
    }
}

public sealed class TableWidget<T> : JustInTimeWidget, IFocusable, IForwardWidgetEvent, IKeyBindable
    where T : class
{
    public bool IsForwardable(KeyBinding binding) => false;
    public void Handle(KeyMessage key) => HandleKey(key);

    private readonly Spectre.Tui.TableWidget<Row<T>> _table;

    public TableKeyMap<Row<T>> KeyMap => _table.KeyMap;
    public IEnumerable<T> Items => _table.Rows.Select(x => x.Item);

    public TableWidget(List<Row<T>> items, ImmutableArray<ColumnDef<T>> columns)
    {
        var t = new Spectre.Tui.TableWidget<Row<T>>(items);

        foreach (var c in columns)
        {
            t.AddColumn(c.CreateColumn());
        }

        t
            .WrapAround()
            .SelectedIndex(0);

        _table = t;

        SetTableStyle();
    }

    private void SetTableStyle()
    {
        Style? newHighlight = new Style(decoration: Decoration.Invert);

        if (IsFocused == false)
        {
            newHighlight = null;
        }

        _table.HeaderStyle(new Style(IsFocused ? Color.Green : Color.Gray, decoration: Decoration.Bold));
        _table.HighlightStyle(newHighlight);
    }

    public void HandleKey(IKeyInfo info)
    {
        _table.KeyMap.HandleKey(info);
        MarkAsDirty();
    }

    protected override void RenderDirty(RenderContext context)
    {
        context.Render(_table);
    }

    public void WithSelected(Action<T> action)
    {
        var s = _table?.SelectedItem?.Item;
        if (s == null) return;

        action(s);
        MarkAsDirty();
    }

    public void WithAll(Action<T> action)
    {
        foreach (var row in _table.Rows)
        {
            action(row.Item);
        }

        MarkAsDirty();
    }

    public CompositeWidget Render()
    {
        SetTableStyle();

        return new CompositeWidget(
            new ClearWidget(' ', new Style(decoration: Decoration.Bold)),
            new PaddingWidget(new Padding(1, 0, 2, 0), this),
            new ScrollbarWidget()
                .VerticalRight()
                .Position(_table.SelectedIndex ?? 0)
                .Length(_table.Rows.Count)
                .ViewportLength(1)
                .Style(Color.Gray)
                .ThumbStyle(IsFocused ? Color.Green : Color.Gray)
                .BeginSymbol('\\')
                .EndSymbol('/')
        );
    }

    public bool IsFocused
    {
        get;
        set
        {
            field = value;
            SetTableStyle();
            MarkAsDirty();
        }
    } = true;

    public void RegisterKeyBinds(KeymapHelper helper)
    {
        helper.AddMaps(KeyMap);
    }
}