using Spectre.Console;
using Spectre.Tui;
using Spectre.Tui.App;
using System.Security.Cryptography.X509Certificates;
using static System.Net.WebRequestMethods;
using Justify = Spectre.Tui.Justify;
using Layout = Spectre.Tui.Layout;
using Paragraph = Spectre.Tui.Paragraph;
using Size = Spectre.Tui.Size;

namespace Tagger;

public static class SpectreExtensions
{
    public static Rectangle FindArea(this Layout root, RenderContext context, Layout target)
    {
        return root.GetArea(context, target.Name ?? "");
    }

    public static Rectangle RenderFrame(RenderContext context, Layout root, Layout target)
    {
        var middle = root.FindArea(context, target);
        context.Render(new BoxWidget(Color.Red).Border(Border.Plain), middle);
        context.Render(new ClearWidget(' ', Color.Gray), middle.Inflate(-1, -1));

        // Active tab content
        return middle.Inflate(new Size(-10, -4));
    }

    public static BoxWidget Screen(string title, IWidget child)
    {
        return new BoxWidget()
            .Style(Color.Green)
            .Border(Border.Plain)
            .TitlePadding(1)
            .MarkupTitle($"[yellow]{title}[/]")
            .Inner(child);
    }
}


public static class SpectreStrings
{
    public const string CheckMark = "✓";
}

internal class KeymapHelper : IKeyMap
{
    private List<KeyBinding> Items { get; } = new();
    public IEnumerable<KeyBinding> Help()
    {
        return Items;
    }

    public KeymapHelper Add(params KeyBinding[] bind)
    {
        Items.AddRange(bind);
        return this;
    }
    public KeymapHelper Add(params IKeyMap[] bind)
    {
        foreach (var b in bind)
        {
            Items.AddRange(b.Help());
        }
        return this;
    }
    public KeymapHelper Add(IEnumerable<KeyBinding> bind)
    {
        Items.AddRange(bind);
        return this;
    }
}

internal class SingleAction(KeyBinding key, Action<ApplicationContext> click)
{
    public KeyBinding Key => key;

    public void Click(ApplicationContext ctx)
    {
        click(ctx);
    }
}

internal interface IForwardWidgetEvent
{
    bool IsForwardable(KeyBinding binding);
    void Handle(KeyMessage key);
}

internal class KeyActions
{
    private readonly List<SingleAction> _binds = new();

    public KeyActions Bind(KeyBinding key, Action<ApplicationContext> click)
    {
        _binds.Add(new SingleAction(key, click));
        return this;
    }

    public SingleAction? Match(KeyMessage key)
    {
        return _binds.FirstOrDefault(x => x.Key.Matches(key));
    }

    public IEnumerable<KeyBinding> KeyBinds() => _binds.Select(x => x.Key);

    public void HandleMessageGeneric(ApplicationContext context, ApplicationMessage message, IForwardWidgetEvent widget)
    {
        if (message is not KeyMessage key) return;
        var found = this.Match(key);
        if (found != null)
        {
            if(widget.IsForwardable(found.Key))
            {
                widget.Handle(key);
            }
            else
            {
                found.Click(context);
            }
        }
        else
        {
            widget.Handle(key);
        }
    }
}

internal static class KeyActionsForward
{
    private class ForwardTextBox(TextBoxWidget widget) : IForwardWidgetEvent
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
    }
    public static void HandleMessage(this KeyActions k, ApplicationContext context, ApplicationMessage message, TextBoxWidget text)
    {
        k.HandleMessageGeneric(context, message, new ForwardTextBox(text));
    }

    private class ForwardFileTable<T>(FileTableWidget<T> widget) : IForwardWidgetEvent where T : class
    {
        public bool IsForwardable(KeyBinding binding) => false;
        public void Handle(KeyMessage key) => widget.HandleKey(key);
    }
    public static void HandleMessage<T>(this KeyActions k, ApplicationContext context, ApplicationMessage message,
        FileTableWidget<T> widget) where T : class
    {
        k.HandleMessageGeneric(context, message, new ForwardFileTable<T>(widget));
    }
}


internal class RectCut(RenderContext context, Rectangle rect)
{
    public Rectangle Rect { get; set; } = rect;
    public RenderContext Context { get; } = context;

    public override string ToString() => $"{Rect} : {Context}";

    public RectCut CutLeft(int amount)
    {
        amount = Math.Max(0, amount);
        var width = Math.Min(amount, Rect.Width);

        var result = new Rectangle(
            Rect.X,
            Rect.Y,
            width,
            Rect.Height);

        Rect = new Rectangle(
            Rect.X + width,
            Rect.Y,
            Rect.Width - width,
            Rect.Height);

        return new(Context, result);
    }

    public RectCut CutRight(int amount)
    {
        amount = Math.Max(0, amount);
        var width = Math.Min(amount, Rect.Width);

        var result = new Rectangle(
            Rect.Right - width,
            Rect.Y,
            width,
            Rect.Height);

        Rect = new Rectangle(
            Rect.X,
            Rect.Y,
            Rect.Width - width,
            Rect.Height);

        return new(Context, result);
    }

    public RectCut CutTop(int amount)
    {
        amount = Math.Max(0, amount);
        var height = Math.Min(amount, Rect.Height);

        var result = new Rectangle(
            Rect.X,
            Rect.Y,
            Rect.Width,
            height);

        Rect = new Rectangle(
            Rect.X,
            Rect.Y + height,
            Rect.Width,
            Rect.Height - height);

        return new(Context, result);
    }

    public RectCut CutBottom(int amount)
    {
        amount = Math.Max(0, amount);
        var height = Math.Min(amount, Rect.Height);

        var result = new Rectangle(
            Rect.X,
            Rect.Bottom - height,
            Rect.Width,
            height);

        Rect = new Rectangle(
            Rect.X,
            Rect.Y,
            Rect.Width,
            Rect.Height - height);

        return new(Context, result);
    }

    // Non-mutating versions.

    public RectCut Clone()
    {
        var result = new Rectangle(Rect.X, Rect.Y, Rect.Width, Rect.Height);
        return new(Context, result);
    }

    public RectCut GetLeft(int amount) => Clone().CutLeft(amount);
    public RectCut GetRight(int amount) => Clone().CutRight(amount);
    public RectCut GetTop(int amount) => Clone().CutTop(amount);
    public RectCut GetBottom(int amount) => Clone().CutBottom(amount);

    // Rectangles immediately outside the current rectangle.

    public RectCut AddLeft(int amount)
    {
        amount = Math.Max(0, amount);

        var result = new Rectangle(
            Rect.X - amount,
            Rect.Y,
            amount,
            Rect.Height);

        return new(Context, result);
    }

    public RectCut AddRight(int amount)
    {
        amount = Math.Max(0, amount);

        var result = new Rectangle(
            Rect.Right,
            Rect.Y,
            amount,
            Rect.Height);

        return new(Context, result);
    }

    public RectCut AddTop(int amount)
    {
        amount = Math.Max(0, amount);

        var result = new Rectangle(
            Rect.X,
            Rect.Y - amount,
            Rect.Width,
            amount);

        return new(Context, result);
    }

    public RectCut AddBottom(int amount)
    {
        amount = Math.Max(0, amount);

        var result = new Rectangle(
            Rect.X,
            Rect.Bottom,
            Rect.Width,
            amount);

        return new(Context, result);
    }

    public static RectCut Begin(RenderContext c)
    {
        return new RectCut(c, c.Screen);
    }

    public RectCut Inset(int top, int right, int bottom, int left)
    {
        CutTop(top);
        CutBottom(bottom);
        CutLeft(left);
        CutRight(right);
        return this;
    }

    public RectCut Inset(int top, int leftRight, int bottom) => Inset(top, leftRight, bottom, leftRight);
    public RectCut Inset(int topBottom, int leftRight) => Inset(topBottom, leftRight, topBottom, leftRight);
    public RectCut Inset(int all) => Inset(all, all, all, all);
}

enum Side
{
    Left, Right, Top, Bottom
}

internal static class RectCutExtensions
{
    public static RectCut Cut(this RectCut rc, Side side, int amount)
    {
        return side switch
        {
            Side.Left => rc.CutLeft(amount),
            Side.Right => rc.CutRight(amount),
            Side.Top => rc.CutTop(amount),
            Side.Bottom => rc.CutBottom(amount),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static RectCut Add(this RectCut rc, Side side, int amount)
    {
        return side switch
        {
            Side.Left => rc.AddLeft(amount),
            Side.Right => rc.AddRight(amount),
            Side.Top => rc.AddTop(amount),
            Side.Bottom => rc.AddBottom(amount),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}

internal enum Clear
{
    No, Yes
}

internal static class RectCutTui
{
    public static void Render(this RectCut r, IWidget widget)
    {
        r.Context.Render(widget, r.Rect);
    }

    public static void Render(this RectCut r, string title, IWidget widget)
    {
        var c = r.Clone();

        c.Render(new BoxWidget(Color.Red).Border(Border.Plain).TitlePadding(1).MarkupTitle($"[yellow]{title}[/]"));
        c.Inset(1);
        c.Render(widget);
    }

    public static RectCut DrawClear(this RectCut r)
    {
        r.Render(new ClearWidget(' ', Color.Gray));
        return r;
    }

    public static RectCut DrawKeymap(this RectCut r, Action<KeymapHelper> map, Clear clear = Clear.No)
    {
        var helper = new KeymapHelper();
        map(helper);
        var bottom = r.CutBottom(1);
        if (clear == Clear.Yes)
        {
            bottom.DrawClear();
        }
        bottom.Render(new HelpWidget(helper));
        return r;
    }

    public static RectCut DrawTitle(this RectCut r, string title, Clear clear = Clear.No)
    {
        r.Inset(1, 2, 3);
        if (clear == Clear.Yes)
        {
            r.DrawClear();
        }
        r.Render(new BoxWidget(Color.Red).Border(Border.Plain).TitlePadding(1).MarkupTitle($"[yellow]{title}[/]"));
        r.Inset(2, 2);
        return r;
    }
}


public class PopUp(string message, string? title = null) : Screen
{
    public override bool IsTransparent => true;

    public override void OnMessage(ApplicationContext context, ApplicationMessage appMessage)
    {
        if (appMessage is KeyMessage key && key.Key == Key.Escape)
        {
            context.Pop();
        }
    }

    public override void Render(RenderContext context)
    {
        context.Render(
            new PopupWidget(new Size(50, 10))
                .Content(
                    new BoxWidget()
                        .Border(Border.Plain)
                        .Title(title ?? "", TitlePosition.Top, Justify.Center)
                        .Inner(Paragraph.FromMarkup($"{message}\n\nPress [yellow]ESC[/] to close").Centered().AlignedMiddle())
                ));
    }
}
