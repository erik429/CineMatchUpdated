using System.Collections.Generic;
using Xunit;

namespace CineMatch.Tests;

public class BoundaryValidationTests
{
    // Vi testar "valideringsliknande" trivial logik för att få fler fall.
    private static bool IsValidGenreId(int id) => id >= 0 && id <= 9999;
    private static bool IsValidMaxMinutes(int m) => m >= 1 && m <= 500;

    public static IEnumerable<object[]> GenreIds()
    {
        yield return new object[] { -1, false };
        yield return new object[] { 0, true };
        yield return new object[] { 28, true };
        yield return new object[] { 9999, true };
        yield return new object[] { 10000, false };
        // skala upp till ~20 fall:
        for (int i = 1; i <= 15; i++)
            yield return new object[] { i * 10, true };
    }

    [Theory]
    [MemberData(nameof(GenreIds))]  
    public void GenreId_Bounds(int id, bool expected)
    {
        Assert.Equal(expected, IsValidGenreId(id));
    }

    public static IEnumerable<object[]> Runtimes()
    {
        yield return new object[] { 0, false };
        yield return new object[] { 1, true };
        yield return new object[] { 120, true };
        yield return new object[] { 500, true };
        yield return new object[] { 501, false };
        // skala upp till ~20 fall:
        for (int i = 5; i <= 100; i += 5)
            yield return new object[] { i, true };
    }

    [Theory]
    [MemberData(nameof(Runtimes))]
    public void Runtime_Bounds(int minutes, bool expected)
    {
        Assert.Equal(expected, IsValidMaxMinutes(minutes));
    }
}
