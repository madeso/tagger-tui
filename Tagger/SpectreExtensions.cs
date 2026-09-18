using Spectre.Console;
using Spectre.Tui;
using Spectre.Tui.App;
using Justify = Spectre.Tui.Justify;
using Paragraph = Spectre.Tui.Paragraph;
using Size = Spectre.Tui.Size;

namespace Tagger;

public interface IForwardWidgetEvent
{
    bool IsForwardable(KeyBinding binding);
    void Handle(KeyMessage key);
}
public interface IKeyBindable
{
    void RegisterKeyBinds(KeymapHelper helper);
}


public static class SpectreStrings
{
    public const string CheckMark = "✓";
}

public class KeymapHelper : IKeyMap
{
    private List<KeyBinding> Items { get; } = [];

    public IEnumerable<KeyBinding> Help()
    {
        return Items;
    }
    public KeymapHelper AddBinds(IEnumerable<KeyBinding> bind)
    {
        Items.AddRange(bind);
        return this;
    }

    public KeymapHelper AddMaps(params IKeyMap[] bind)
    {
        foreach (var b in bind)
        {
            Items.AddRange(b.Help());
        }
        return this;
    }

    public KeymapHelper Add(params IKeyBindable[] widgets)
    {
        foreach (var wid in widgets)
        {
            wid.RegisterKeyBinds(this);
        }

        return this;
    }
}

public class SingleAction(KeyBinding key, Action<ApplicationContext> click)
{
    public KeyBinding Key => key;

    public void Click(ApplicationContext ctx)
    {
        click(ctx);
    }
}

public class KeyActions : IKeyBindable
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

    void IKeyBindable.RegisterKeyBinds(KeymapHelper helper)
    {
        helper.AddBinds(_binds.Select(x => x.Key));
    }

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

public class FocusHelper(params IFocusable[] items) : IKeyBindable
{
    private readonly FocusRing _ring = new(items);

    public bool HandleInput(ApplicationMessage message)
    {
        return _ring.HandleInput(message);
    }

    public void OnMessage(KeyActions k, ApplicationContext context, ApplicationMessage message)
    {
        var focus = _ring.Focused;

        if (focus is IForwardWidgetEvent forward)
        {
            k.HandleMessage(context, message, forward);
        }
        else
        {
            throw new ArgumentException($"Unabled type {focus}");
        }
    }

    public void RegisterKeyBinds(KeymapHelper helper)
    {
        var focus = _ring.Focused;
        if (focus is IKeyBindable forward)
        {
            forward.RegisterKeyBinds(helper);
        }
        else
        {
            throw new ArgumentException($"Unabled type {focus}");
        }
    }
}

internal static class KeyActionsForward
{
    public static void HandleMessage(this KeyActions k, ApplicationContext context, ApplicationMessage message, IForwardWidgetEvent widget)
    {
        k.HandleMessageGeneric(context, message, widget);
    }

    public static void HandleMessage(this KeyActions k, ApplicationContext context, ApplicationMessage message, FocusHelper ring)
    {
        if (ring.HandleInput(message))
        {
            return;
        }

        ring.OnMessage(k, context, message);
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
    public static void Draw(this RectCut r, IWidget widget)
    {
        r.Context.Render(widget, r.Rect);
    }

    public static void Draw(this RectCut r, string title, IWidget widget)
    {
        var c = r.Clone();

        var isFocused = widget is not IFocusable focus || focus.IsFocused;

        c.Draw(new BoxWidget(isFocused ? Color.Red : Color.Gray).Border(Border.Plain).TitlePadding(1).MarkupTitle(isFocused ? $"[yellow]{title}[/]" : title));
        c.Inset(1);
        c.Draw(widget);
    }

    public static RectCut DrawClear(this RectCut r)
    {
        r.Draw(new ClearWidget());
        return r;
    }

    public static RectCut DrawKeymap(this RectCut r, Action<KeymapHelper> map, Clear clear = Clear.No)
    {
        // todo(Gustav): this might draw keys that are blocked by the current widget

        var helper = new KeymapHelper();
        map(helper);
        var bottom = r.CutBottom(1);
        if (clear == Clear.Yes)
        {
            bottom.DrawClear();
        }
        bottom.Draw(new HelpWidget(helper));
        return r;
    }

    public static RectCut DrawTitle(this RectCut r, string title, Clear clear = Clear.No)
    {
        r.Inset(1, 2, 3);
        if (clear == Clear.Yes)
        {
            r.DrawClear();
        }
        r.Draw(new BoxWidget(Color.Red).Border(Border.Plain).TitlePadding(1).MarkupTitle($"[yellow]{title}[/]"));
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
