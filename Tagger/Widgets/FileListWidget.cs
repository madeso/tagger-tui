using Spectre.Tui;
using Spectre.Console;
using Padding = Spectre.Tui.Padding;
using Text = Spectre.Tui.Text;

namespace Tagger.Widgets;

public sealed class FileItem(FileWithData file) : IListWidgetItem
{
    public bool IsSelected { get => file.IsSelected; set => file.IsSelected = value; }

    public void Toggle()
    {
        IsSelected = !IsSelected;
    }

    Text IListWidgetItem.CreateText(bool isHovering)
    {
        var symbol = IsSelected ? SpectreStrings.CheckMark : " ";
        var decoration = isHovering
            ? "yellow"
            : (IsSelected ? "green" : "grey");

        var display = file.Path;
        return Text.FromMarkup(
            IsSelected
                ? $"[{decoration}]{symbol} {display}[/]"
                : $"[{decoration}]{symbol} {display.RemoveMarkup()}[/]");
    }
}

public sealed class ScrollableListWidget<T>(List<T> items) : JustInTimeWidget
    where T : IListWidgetItem
{
    private readonly ListWidget<T> _widget = new ListWidget<T>(items)
        .HighlightSymbol("->")
        .WrapAround()
        .SelectedIndex(0);

    private T? Selected => _widget.SelectedItem;
    public ListKeyMap<T> KeyMap => _widget.KeyMap;

    public void HandleKey(IKeyInfo info)
    {
        _widget.KeyMap.HandleKey(info);
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