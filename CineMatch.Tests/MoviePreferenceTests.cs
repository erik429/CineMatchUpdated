
using System.Collections.Generic;
using Xunit;

namespace CineMatch.Tests;

public class MoviePreferenceTests
{
    // Förenklad matchningslogik: true = gillad, false = ogillad
    private static bool IsMatch(bool userLikes, bool partnerLikes)
        => userLikes && partnerLikes;

    public static IEnumerable<object[]> MatchingCases()
    {
        yield return new object[] { true, true, true };
        yield return new object[] { true, false, false };
        yield return new object[] { false, true, false };
        yield return new object[] { false, false, false };
    }

    [Theory]
    [MemberData(nameof(MatchingCases))]
    public void MovieMatch_ShouldBehaveCorrectly(bool userLikes, bool partnerLikes, bool expected)
    {
        Assert.Equal(expected, IsMatch(userLikes, partnerLikes));
    }

    [Fact]
    public void BothUsersLikeMovie_ShouldCreateMatch()
    {
        Assert.True(IsMatch(true, true));
    }

    [Fact]
    public void OneUserDislikesMovie_ShouldNotCreateMatch()
    {
        Assert.False(IsMatch(true, false));
    }
}
