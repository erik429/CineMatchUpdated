
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CineMatch.Tests;

public class MovieFilteringTests
{
    private static IEnumerable<string> FilterLikedMovies(IEnumerable<(string title, bool liked)> movies)
        => movies.Where(m => m.liked).Select(m => m.title);

    [Fact]
    public void OnlyLikedMovies_ShouldBeReturned()
    {
        var movies = new List<(string, bool)>
        {
            ("Inception", true),
            ("Titanic", false),
            ("Matrix", true),
            ("Cats", false)
        };

        var result = FilterLikedMovies(movies).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains("Inception", result);
        Assert.Contains("Matrix", result);
    }

    [Fact]
    public void NoLikedMovies_ShouldReturnEmptyList()
    {
        var movies = new List<(string, bool)>
        {
            ("Movie1", false),
            ("Movie2", false)
        };

        var result = FilterLikedMovies(movies);

        Assert.Empty(result);
    }
}
