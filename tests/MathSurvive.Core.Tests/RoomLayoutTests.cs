using MathSurvive.Core.Layout;
using Xunit;

namespace MathSurvive.Core.Tests;

public class RoomLayoutTests
{
    private static void AssertInside(PixelRect r, int w, int h, double wallTop)
    {
        Assert.True(r.W > 0 && r.H > 0);
        Assert.InRange(r.X, 0, w - r.W);
        Assert.InRange(r.Y, 0, h - r.H);
        Assert.True(r.Y >= wallTop, "элемент на стене");
    }

    [Theory]
    [InlineData(1920, 1040, 1.0)]
    [InlineData(1920, 1040, 1.5)]
    [InlineData(1024, 700, 1.0)]
    [InlineData(1024, 700, 1.5)]
    [InlineData(640, 480, 2.0)]
    public void Slots_are_ordered_inside_wall_and_do_not_overlap(int w, int h, double scale)
    {
        var slots = RoomLayout.SectionSlots(w, h, scale);
        var wallTop = RoomLayout.WallTop(h, scale);
        var (feetX, feetY) = RoomLayout.PlayerFeet(w, h);

        Assert.Equal(RoomLayout.SectionCount, slots.Count);
        Assert.Equal(h, feetY);
        for (var i = 0; i < slots.Count; i++)
        {
            var (plate, coin) = slots[i];
            AssertInside(plate, w, h, wallTop);
            AssertInside(coin, w, h, wallTop);
            Assert.Equal(coin.W, coin.H);
            Assert.True(plate.X + plate.W < coin.X, $"монета {i} справа от таблички");
            Assert.Equal(plate.Y + plate.H / 2.0, coin.Y + coin.H / 2.0, 1.0);
            Assert.True(feetX < plate.X, "персонаж левее разделов");

            if (i > 0)
                Assert.True(slots[i - 1].Coin.X + slots[i - 1].Coin.W < plate.X, $"разделы {i - 1} и {i} пересекаются");
        }
    }

    [Fact]
    public void Slots_hang_above_player_head()
    {
        var slots = RoomLayout.SectionSlots(1920, 1040, 1.0);
        // Персонаж 60 логических px, облачко «!» над ним — ещё 36.
        Assert.All(slots, s => Assert.True(s.Plate.Y + s.Plate.H <= 1040 - 96 && s.Coin.Y + s.Coin.H <= 1040 - 96));
        Assert.Equal(new PixelRect(489, 848, 128, 84), slots[0].Plate);
        Assert.Equal(new PixelRect(623, 870, 40, 40), slots[0].Coin);
        Assert.Equal(192.0, RoomLayout.PlayerFeet(1920, 1040).x, 1e-6);
    }
}
