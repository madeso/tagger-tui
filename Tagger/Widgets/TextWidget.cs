using Spectre.Tui;
using Spectre.Tui.App;

namespace Tagger.Widgets;

public class TextWidget(TextBoxWidget widget) : IForwardWidgetEvent, IFocusable, IWidget, IKeyBindable
{
    public bool IsForwardable(KeyBinding binding) =>
        binding.Keys.Any(keyPress =>
            keyPress.Key switch
            {
                Key.Character => true,
                Key.Backspace => true,
                Key.Space => true,
                _ => false
            });

    public void Handle(KeyMessage key) => widget.KeyMap.HandleKey(key);

    public bool IsFocused { get => widget.IsFocused; set => widget.IsFocused = value; }
    
    void IWidget.Render(RenderContext context)
    {
        context.Render(widget);
    }

    public void RegisterKeyBinds(KeymapHelper helper)
    {
        helper.AddMaps(widget.KeyMap);
    }
}
