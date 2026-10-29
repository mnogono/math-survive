using MathSurvive.Core.Layout;
using Xunit;

namespace MathSurvive.Core.Tests;

public class QuizLayoutTests
{
    private static readonly PixelRect Window = new(0, 0, 1920, 1040);

    [Fact]
    public void Narrow_svg_options_go_to_grid()
        => Assert.Equal(OptionsLayout.Grid2x2,
            QuizLayout.Choose(new[] { (120, 0), (120, 0), (120, 0), (120, 0) }, 160, 0));

    [Fact]
    public void One_wide_option_switches_to_column()
        => Assert.Equal(OptionsLayout.Column,
            QuizLayout.Choose(new[] { (120, 0), (300, 0), (120, 0), (120, 0) }, 160, 0));

    [Fact]
    public void Long_text_option_switches_to_column()
        => Assert.Equal(OptionsLayout.Column,
            QuizLayout.Choose(new[] { (120, 0), (120, 40), (120, 0), (120, 0) }, 160, 0));

    [Fact]
    public void Text_of_24_chars_stays_in_grid()
        => Assert.Equal(OptionsLayout.Grid2x2,
            QuizLayout.Choose(new[] { (120, 24), (120, 6), (120, 6), (120, 6) }, 160, 0));

    [Fact]
    public void Long_prompt_switches_to_column()
        => Assert.Equal(OptionsLayout.Column,
            QuizLayout.Choose(new[] { (120, 6), (120, 6), (120, 6), (120, 6) }, 160, 121));

    [Fact]
    public void Prompt_of_120_chars_stays_in_grid()
        => Assert.Equal(OptionsLayout.Grid2x2,
            QuizLayout.Choose(new[] { (120, 6), (120, 6), (120, 6), (120, 6) }, 160, 120));

    [Fact]
    public void Clamp_moves_panel_inside_window_with_margin()
        => Assert.Equal(new PixelRect(8, 732, 400, 300),
            QuizLayout.Clamp(new PixelRect(-50, 900, 400, 300), Window, 8));

    [Fact]
    public void Clamp_keeps_panel_that_fits()
        => Assert.Equal(new PixelRect(100, 200, 400, 300),
            QuizLayout.Clamp(new PixelRect(100, 200, 400, 300), Window, 8));

    [Fact]
    public void Clamp_moves_panel_from_right_and_top_edges()
        => Assert.Equal(new PixelRect(1512, 8, 400, 300),
            QuizLayout.Clamp(new PixelRect(1700, -20, 400, 300), Window, 8));

    [Fact]
    public void Panel_taller_than_window_is_pinned_to_top_margin()
    {
        var clamped = QuizLayout.Clamp(new PixelRect(100, 300, 400, 1200), Window, 8);
        Assert.Equal(8, clamped.Y);
        Assert.Equal(1200, clamped.H);
    }
}
