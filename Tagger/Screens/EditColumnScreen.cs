using Spectre.Tui;
using Spectre.Tui.App;
using Tagger.Widgets;

namespace Tagger.Screens;

public class EditColumnScreen : Screen
{
    private readonly KeyActions _actions;
    private readonly TextWidget _label = new TextWidget(new TextBoxWidget().AsSingleLine());
    private readonly TextWidget _pattern = new TextWidget(new TextBoxWidget().AsSingleLine());
    private string _previousLabel;
    private string _previousPattern;
    private bool _patternDirty = false;
    private readonly ColumnDef _column;
    private string? _patternError = null;


    private bool IsFullscreen { get; set; } = true;
    private readonly FocusHelper _focus;

    public EditColumnScreen(ColumnDef column, Action esc, Action ok)
    {
        _focus = new FocusHelper(_label, _pattern);
        _column = column;

        _label.Text = column.Label;
        _pattern.Text = column.Pattern;

        _previousLabel = _label.Text;
        _previousPattern = _pattern.Text;

        _actions = new KeyActions()
                .Bind(KeyBinding.For('f').WithHelp("Toggle fullscreen"), _ => IsFullscreen = !IsFullscreen)
                .Bind(KeyBinding.For(Key.Escape).WithHelp("Abort"), ctx =>
                {
                    ctx.Pop();
                    esc();
                })
                .Bind(KeyBinding.For(Key.Enter).WithHelp("Apply"), ctx =>
                {
                    if (string.IsNullOrEmpty(_label.Text) || string.IsNullOrEmpty(_pattern.Text))
                    {
                        ctx.RunPopup("Name and pattern can't be empty");
                    }

                    column.Label = _label.Text;
                    column.Pattern = _pattern.Text;

                    ctx.Pop();
                    ok();
                })
            ;
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        _actions.HandleMessage(context, message, _focus);

        CheckLabelChange();
        CheckPatternChange();
    }

    private void CheckLabelChange()
    {
        // has label changed?
        if (_label.Text == _previousLabel) return;
        _previousLabel = _label.Text;

        // we should only change pattern for new columns
        if (string.IsNullOrEmpty(_column.Label) && string.IsNullOrEmpty(_column.Label))
        {
            // new colum
        }
        else
        {
            // existing column, don't autogenerate
            return;
        }

        // don't update pattern if it has been changed
        if (_patternDirty) return;

        var lower = _label.Text.ToLowerInvariant();
        _pattern.Text = $"%{lower}%";
        _previousPattern = _pattern.Text;

        UpdatePatternInspection();
    }

    private void CheckPatternChange()
    {
        if (_pattern.Text == _previousPattern) return;
        _previousPattern = _pattern.Text;
        _patternDirty = true;

        UpdatePatternInspection();
    }

    private void UpdatePatternInspection()
    {
        var (pattern, err) = Pattern.Compile(_pattern.Text);
        _patternError = err.HasErrors() ? string.Join("\n", err.Errors) : null;

        // todo(Gustav): update grid with pattern result
    }

    public override bool IsTransparent => !IsFullscreen;

    public override void Render(RenderContext context)
    {
        var r = RectCut.Begin(context);

        var clear = IsFullscreen ? Clear.No : Clear.Yes;
        if (IsFullscreen)
        {
            r.DrawClear();
        }

        r.DrawKeymap(k => k.Add(_focus, _actions), clear);

        if (IsFullscreen == false)
        {
            r.Inset(4, 4);
        }

        r.DrawTitle("Add/edit column", clear);

        r.CutTop(3).Draw("Label", _label);
        r.CutTop(3).Draw("Pattern", _pattern);
        if (_patternError != null)
        {
            r.CutTop(1).Draw(Paragraph.FromMarkup($"[red]Pattern error[/]: {_patternError}"));
        }
    }
}