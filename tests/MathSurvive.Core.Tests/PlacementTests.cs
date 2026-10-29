using MathSurvive.Core.Layout;
using Xunit;

namespace MathSurvive.Core.Tests;

public class PlacementTests
{
    [Fact]
    public void PlayerFeet_at_left_bottom()
        => Assert.Equal((70.0, 1040.0), Placement.PlayerFeet(1920, 1040, 1.0));

    [Fact]
    public void FireLineX_is_player_x_plus_fraction_of_width()
        => Assert.Equal(710.0, Placement.FireLineX(1920, 1040, 1.0, 1.0 / 3), 1e-6);

    [Fact]
    public void Scales_with_ui_scale()
    {
        Assert.Equal((105.0, 1400.0), Placement.PlayerFeet(2560, 1400, 1.5));
        Assert.Equal((36.0, 1400.0), Placement.DoorFeet(2560, 1400, 1.5));
    }
}
