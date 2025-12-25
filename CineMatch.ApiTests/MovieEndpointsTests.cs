
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CineMatch.ApiTests;

public class MovieEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public MovieEndpointsTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async void GetMovies_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/movies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async void InvalidMovieVote_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/movies/vote", new { movieId = -1, liked = true });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
