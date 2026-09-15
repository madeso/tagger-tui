using Spectre.Console;
using Spectre.Tui;
using System.Collections.Immutable;
using System.Globalization;
using Padding = Spectre.Tui.Padding;
using TableColumn = Spectre.Tui.TableColumn;
using TableRow = Spectre.Tui.TableRow;
using Text = Spectre.Tui.Text;

namespace Tagger;


public sealed class TRow<T>(T t, ImmutableArray<ColumnDef<T>> columns)
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

    public FileTableWidget<T> Create()
    {
        var cols = _columns.ToImmutableArray();
        return new FileTableWidget<T>(items.Select(x => new TRow<T>(x, cols)).ToList(), cols);
    }
}

public sealed class FileTableWidget<T> : JustInTimeWidget
    where T : class
{
    private readonly TableWidget<TRow<T>> _table;

    public TableKeyMap<TRow<T>> KeyMap => _table.KeyMap;
    public IEnumerable<T> Items => _table.Rows.Select(x => x.Item);

    public FileTableWidget(List<TRow<T>> items, ImmutableArray<ColumnDef<T>> columns)
    {
        var t = new TableWidget<TRow<T>>(items);

        foreach (var c in columns)
        {
            t.AddColumn(c.CreateColumn());
        }

        t
            .HighlightStyle(new Style(decoration: Decoration.Invert))
            .HeaderStyle(new Style(Color.Green, decoration: Decoration.Bold))
            .WrapAround()
            .SelectedIndex(0);

        _table = t;
    }

    public void HandleKey(IKeyInfo info)
    {
        _table.KeyMap.HandleKey(info);
        MarkAsDirty();
    }

    public void MoveDown()
    {
        _table.MoveDown();
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
        return new CompositeWidget(
            new ClearWidget(' ', new Style(decoration: Decoration.Bold)),
            new PaddingWidget(new Padding(1, 0, 2, 0), this),
            new ScrollbarWidget()
                .VerticalRight()
                .Position(_table.SelectedIndex ?? 0)
                .Length(_table.Rows.Count)
                .ViewportLength(1)
                .Style(Color.Gray)
                .ThumbStyle(Color.Green)
                .BeginSymbol('\\')
                .EndSymbol('/')
        );
    }
}