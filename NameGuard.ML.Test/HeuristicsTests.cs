using NameGuard.ML.Core.Heuristics;
using Xunit;

namespace NameGuard.ML.Test;

public class HeuristicsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public void RejectsTooShort(string input)
    {
        var rejected = JunkDetector.TryReject(input, out var reason);
        Assert.True(rejected);
        Assert.Equal("Too short", reason);
    }

    [Fact]
    public void RejectsTooLong()
    {
        var s = new string('a', 100);
        var rejected = JunkDetector.TryReject(s, out var reason);
        Assert.True(rejected);
        Assert.Equal("Too long", reason);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("!!!!!")]
    public void RejectsNoLetters(string input)
    {
        var rejected = JunkDetector.TryReject(input, out var reason);
        Assert.True(rejected);
        Assert.Equal("No letters", reason);
    }

    [Theory]
    [InlineData("a1234567")]
    [InlineData("x999")]
    public void RejectsMostlyDigits(string input)
    {
        var rejected = JunkDetector.TryReject(input, out var reason);
        Assert.True(rejected);
        Assert.Equal("Mostly digits", reason);
    }

    [Theory]
    [InlineData("aaaaaa")]
    [InlineData("zzzz")]
    public void RejectsRepeatingChar(string input)
    {
        var rejected = JunkDetector.TryReject(input, out _);
        Assert.True(rejected);
    }

    [Theory]
    [InlineData("bcdfgh")]
    [InlineData("xkqzpw")]
    public void RejectsNoVowels(string input)
    {
        var rejected = JunkDetector.TryReject(input, out var reason);
        Assert.True(rejected);
        Assert.Equal("No vowels", reason);
    }

    [Theory]
    [InlineData("qwerty")]
    [InlineData("asdfgh")]
    [InlineData("ytrewq")]
    public void RejectsKeyboardRolls(string input)
    {
        var rejected = JunkDetector.TryReject(input, out var reason);
        Assert.True(rejected);
        Assert.Equal("Keyboard roll detected", reason);
    }

    [Theory]
    [InlineData("John")]
    [InlineData("Mary Johnson")]
    [InlineData("Khaled Hossain")]
    [InlineData("Jean-Luc")]
    [InlineData("José García")]
    [InlineData("Yuki Tanaka")]
    public void AcceptsRealNames(string input)
    {
        var rejected = JunkDetector.TryReject(input, out _);
        Assert.False(rejected, $"Expected '{input}' to pass heuristics");
    }
}
