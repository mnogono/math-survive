using Xunit;

namespace MathSurvive.Core.Tests;

public class GameConfigTests
{
    [Fact]
    public void Max_frame_delta_defaults_to_quarter_second()
        => Assert.Equal(0.25, GameConfig.Default.MaxFrameDeltaSec);
}
