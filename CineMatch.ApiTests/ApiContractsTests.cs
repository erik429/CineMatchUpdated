using System.Net.Http.Json;
using Xunit;

namespace CineMatch.ApiTests;

// Very light "in-memory" style tests using WebApplicationFactory would require a Program class entry point.
// For simplicity, below shows sample request contracts to guide real tests.
public class ContractsCompileTests
{
    public record UserCred(string Username, string Password);
    public record Swipe(int MovieId, bool Liked);

    [Fact]
    public void ContractsAreUsable()
    {
        var cred = new UserCred("alice", "pw");
        var swipe = new Swipe(123, true);
        Assert.Equal("alice", cred.Username);
        Assert.True(swipe.Liked);
    }
}
