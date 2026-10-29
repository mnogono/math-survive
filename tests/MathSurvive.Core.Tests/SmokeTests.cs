using Xunit;

namespace MathSurvive.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void Core_assembly_loads()
    {
        Assert.Equal("MathSurvive.Core", typeof(MathSurvive.Core.AssemblyMarker).Assembly.GetName().Name);
    }
}
