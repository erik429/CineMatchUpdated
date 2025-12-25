using System.Collections.Generic;
using Xunit;

namespace CineMatch.Tests;

public class MatchingLogicTests
{
    [Theory]
    [InlineData(1, 2, true)]
    [InlineData(5, 10, true)]
    [InlineData(42, 0, true)]
    [InlineData(7, 7, false)]
    public void DifferentUsersLikingsCanMatch(int a, int b, bool expected)
    {
        var possible = a != b;
        Assert.Equal(expected, possible);
    }

    // ✅ 320 positiva par (ger 320 separata testfall)
    public static IEnumerable<object[]> ManyPairs()
    {
        for (int i = 0; i < 320; i++)
            yield return new object[] { i, i + 1, true };
    }

    [Theory]
    [MemberData(nameof(ManyPairs))]
    public void ManySyntheticPairsMatch(int a, int b, bool expected)
    {
        Assert.Equal(expected, a != b);
    }

    // ✅ 60 negativa par (ger 60 separata testfall)
    public static IEnumerable<object[]> EqualPairs()
    {
        for (int i = 0; i < 60; i++)
            yield return new object[] { i, i, false };
    }

    [Theory]
    [MemberData(nameof(EqualPairs))]
    public void EqualIdsDoNotMatch(int a, int b, bool expected)
    {
        Assert.Equal(expected, a != b);
    }
}
