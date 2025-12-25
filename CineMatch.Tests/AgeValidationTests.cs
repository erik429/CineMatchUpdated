
using System.Collections.Generic;
using Xunit;

namespace CineMatch.Tests;

public class AgeValidationTests
{
    private static bool IsValidAge(int age) => age >= 18 && age <= 120;

    public static IEnumerable<object[]> ValidAges()
    {
        yield return new object[] {18};
        yield return new object[] {21};
        yield return new object[] {30};
        yield return new object[] {65};
        yield return new object[] {120};
        for (int i = 18; i <= 80; i += 2)
            yield return new object[] { i };
    }

    public static IEnumerable<object[]> InvalidAges()
    {
        yield return new object[] {0};
        yield return new object[] {17};
        yield return new object[] {-5};
        yield return new object[] {121};
        yield return new object[] {200};
    }

    [Theory]
    [MemberData(nameof(ValidAges))]
    public void ValidAge_ShouldPass(int age)
    {
        Assert.True(IsValidAge(age));
    }

    [Theory]
    [MemberData(nameof(InvalidAges))]
    public void InvalidAge_ShouldFail(int age)
    {
        Assert.False(IsValidAge(age));
    }
}
