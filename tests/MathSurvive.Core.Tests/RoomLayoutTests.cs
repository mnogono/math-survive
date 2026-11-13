using System;
using MathSurvive.Core.Layout;
using Xunit;

namespace MathSurvive.Core.Tests;

public class RoomLayoutTests
{
    [Theory]
    [InlineData(1920, 1040, 1.0)]
    [InlineData(1920, 1040, 1.5)]
    [InlineData(1024, 700, 1.0)]
    [InlineData(1024, 700, 1.5)]
    [InlineData(640, 480, 2.0)]
    public void Icons_are_ordered_inside_window_and_do_not_overlap(int w, int h, double scale)
    {
        var rects = RoomLayout.IconRects(w, h, scale);

        Assert.Equal(RoomLayout.IconCount, rects.Count);
        for (var i = 0; i < rects.Count; i++)
        {
            var r = rects[i];
            Assert.True(r.W > 0 && r.H > 0);
            Assert.InRange(r.X, 0, w - r.W);
            Assert.InRange(r.Y, 0, h - r.H);
            Assert.True(r.Y >= RoomLayout.WallTop(h, scale), "иконка на стене");

            var (feetX, feetY) = RoomLayout.PlayerFeet(w, h, scale, i);
            Assert.Equal(r.X + r.W / 2.0, feetX, 1.0);
            Assert.Equal(h, feetY);

            if (i > 0)
                Assert.True(rects[i - 1].X + rects[i - 1].W < r.X, $"иконки {i - 1} и {i} пересекаются");
        }
    }

    [Fact]
    public void Icons_hang_above_player_head()
    {
        var rects = RoomLayout.IconRects(1920, 1040, 1.0);
        // Персонаж 60 логических px, облачко «!» над ним — ещё 36.
        Assert.All(rects, r => Assert.True(r.Y + r.H <= 1040 - 96));
        Assert.Equal(192.0, RoomLayout.PlayerFeet(1920, 1040, 1.0, 0).x, 1e-6);
        Assert.Equal(1728.0, RoomLayout.PlayerFeet(1920, 1040, 1.0, 4).x, 1e-6);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void PlayerFeet_rejects_unknown_icon(int index)
        => Assert.Throws<ArgumentOutOfRangeException>(() => RoomLayout.PlayerFeet(1920, 1040, 1.0, index));
}
