using Spectre.Tui;
using Spectre.Console;
using Spectre.Tui.App;
using Padding = Spectre.Tui.Padding;
using Text = Spectre.Tui.Text;

namespace Tagger.Widgets;

public sealed class ListItem<T>(T file, Func<T, string> markup) : IListWidgetItem
{
    public T Item => file;

    Text IListWidgetItem.CreateText(bool isHovering)
    {
        var decoration = isHovering
            ? "yellow"
            : "grey";

        var display = markup(file);
        return Text.FromMarkup($"[{decoration}]{display}[/]");
    }
}

public sealed class ScrollableListWidget<T>(List<T> items, Func<T, string> markup) : JustInTimeWidget, IForwardWidgetEvent
    where T: class
{
    private readonly ListWidget<ListItem<T>> _widget = new ListWidget<ListItem<T>>(items.Select(t => new ListItem<T>(t, markup)).ToList())
        .HighlightSymbol("->")
        .WrapAround()
        .SelectedIndex(0);

    public T? Selected => _widget.SelectedItem?.Item;

    public bool ShouldStealFromAction(KeyBinding binding)
    {
        return false;
    }

    public void Handle(KeyMessage key)
    {
        _widget.KeyMap.HandleKey(key);
        MarkAsDirty();
    }

    protected override void RenderDirty(RenderContext context)
    {
        context.Render(_widget);
    }

    public CompositeWidget Render()
    {
        return new CompositeWidget(
            new ClearWidget(' ', new Style(decoration: Decoration.Bold)),
            new PaddingWidget(new Padding(1, 0, 2, 0), this),
            new ScrollbarWidget()
                .VerticalRight()
                .Position(_widget.SelectedIndex ?? 0)
                .Length(_widget.Items.Count)
                .ViewportLength(1)
                .Style(Color.Gray)
                .ThumbStyle(Color.Green)
                .BeginSymbol('\\')
                .EndSymbol('/')
            );
    }

    public void WithSelected(Action<T> action)
    {
        var s = Selected;
        if (s == null) return;

        action(s);
        MarkAsDirty();
    }
}