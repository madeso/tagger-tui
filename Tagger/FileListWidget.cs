using Spectre.Tui;
using System;
using System.Collections.Generic;
using System.Text;
using Spectre.Console;
using Padding = Spectre.Tui.Padding;
using Text = Spectre.Tui.Text;

namespace Tagger;

public sealed class FileItem(FileWithData file) : IListWidgetItem
{
    public bool IsSelected { get => file.IsSelected; set => file.IsSelected = value; }

    public void Toggle()
    {
        IsSelected = !IsSelected;
    }

    Text IListWidgetItem.CreateText(bool isHovering)
    {
        var symbol = IsSelected ? "✓" : " ";
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

public sealed class FileWidget(List<FileItem> items) : JustInTimeWidget
{
    private readonly ListWidget<FileItem> _widget = new ListWidget<FileItem>(items)
        .HighlightSymbol("→ ")
        .WrapAround()
        .SelectedIndex(0);

    public int Position => _widget.SelectedIndex ?? 0;
    public int Length => _widget.Items.Count;
    public FileItem? Selected => _widget.SelectedItem;
    public ListKeyMap<FileItem> KeyMap => _widget.KeyMap;

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
                .Position(this.Position)
                .Length(this.Length)
                .ViewportLength(1)
                .Style(Color.Gray)
                .ThumbStyle(Color.Green));
    }
}