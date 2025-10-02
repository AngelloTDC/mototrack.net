using Xunit;
using FluentAssertions;

namespace MotoTrack.Tests;

public class SmokeTests
{
    [Fact]
    public void MathShouldWork() => (2 + 2).Should().Be(4);
}