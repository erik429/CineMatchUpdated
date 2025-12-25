using Microsoft.Data.Sqlite;
using System.Text;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// ✅ Lägg till CORS-policy för just din frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevOnly", policy =>
        policy.WithOrigins("http://localhost:5173")  // Frontend
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ✅ Lägg till HTTPS-redirect (eftersom backend kör på https://localhost:64360)
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 64360;
});

var app = builder.Build();

// ✅ Middleware
app.UseHttpsRedirection();
app.UseCors("DevOnly");

var dbPath = Path.Combine(AppContext.BaseDirectory, "cine.db");
var connStr = $"Data Source={dbPath}";

// Skapa tabeller om de inte finns
using (var conn = new SqliteConnection(connStr))
{
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS Users(Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT UNIQUE, Password TEXT);
    CREATE TABLE IF NOT EXISTS Preferences(UserId INTEGER, GenreId INTEGER, MaxMinutes INTEGER);
    CREATE TABLE IF NOT EXISTS Likes(UserId INTEGER, MovieId INTEGER, Liked INTEGER, CreatedAt TEXT);
    CREATE TABLE IF NOT EXISTS Matches(Id INTEGER PRIMARY KEY AUTOINCREMENT, MovieId INTEGER, UserA INTEGER, UserB INTEGER, CreatedAt TEXT);
    ";
    cmd.ExecuteNonQuery();
}

// Enkel token (Base64 av användarnamnet)
string MakeToken(string username) => Convert.ToBase64String(Encoding.UTF8.GetBytes(username));

string? GetUsername(HttpRequest request)
{
    if (!request.Headers.TryGetValue("Authorization", out var auth)) return null;
    var raw = auth.ToString().Replace("Bearer ", "").Trim();
    try { return Encoding.UTF8.GetString(Convert.FromBase64String(raw)); }
    catch { return null; }
}

app.MapPost("/auth/register", (UserCred cred) =>
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO Users(Username, Password) VALUES ($u, $p)";
    cmd.Parameters.AddWithValue("$u", cred.Username);
    cmd.Parameters.AddWithValue("$p", cred.Password); // ⚠️ Ingen hash enligt krav
    try { cmd.ExecuteNonQuery(); }
    catch (Exception ex) { return Results.BadRequest(new { error = "Username taken", detail = ex.Message }); }
    return Results.Ok(new { token = MakeToken(cred.Username) });
});

app.MapPost("/auth/login", (UserCred cred) =>
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username=$u AND Password=$p";
    cmd.Parameters.AddWithValue("$u", cred.Username);
    cmd.Parameters.AddWithValue("$p", cred.Password);
    var count = Convert.ToInt32(cmd.ExecuteScalar());
    if (count == 1) return Results.Ok(new { token = MakeToken(cred.Username) });
    return Results.Unauthorized();
});

app.MapPost("/preferences", (HttpRequest req, Pref pref) =>
{
    var user = GetUsername(req);
    if (user is null) return Results.Unauthorized();
    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var getId = conn.CreateCommand();
    getId.CommandText = "SELECT Id FROM Users WHERE Username=$u";
    getId.Parameters.AddWithValue("$u", user);
    var userId = Convert.ToInt32(getId.ExecuteScalar());

    // Upsert
    var del = conn.CreateCommand();
    del.CommandText = "DELETE FROM Preferences WHERE UserId=$id";
    del.Parameters.AddWithValue("$id", userId);
    del.ExecuteNonQuery();

    var ins = conn.CreateCommand();
    ins.CommandText = "INSERT INTO Preferences(UserId, GenreId, MaxMinutes) VALUES ($id,$g,$m)";
    ins.Parameters.AddWithValue("$id", userId);
    ins.Parameters.AddWithValue("$g", pref.GenreId);
    ins.Parameters.AddWithValue("$m", pref.MaxMinutes);
    ins.ExecuteNonQuery();

    return Results.Ok();
});

app.MapGet("/movies/discover", async (HttpRequest req, IConfiguration cfg, int? genreId, int? maxMinutes) =>
{
    // 🔹 Hämtar filmer från TMDB
    var http = new HttpClient();
    var token = cfg["TMDB:ReadAccessToken"];
    if (string.IsNullOrWhiteSpace(token))
        return Results.BadRequest(new { error = "Missing TMDB token in appsettings.json (TMDB:ReadAccessToken)" });

    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var url = "https://api.themoviedb.org/3/discover/movie?sort_by=popularity.desc&page=1";
    if (genreId.HasValue) url += $"&with_genres={genreId.Value}";
    if (maxMinutes.HasValue) url += $"&with_runtime.lte={maxMinutes.Value}";

    var res = await http.GetAsync(url);
    if (!res.IsSuccessStatusCode) return Results.StatusCode((int)res.StatusCode);
    var json = await res.Content.ReadAsStringAsync();
    return Results.Text(json, "application/json");
});

app.MapPost("/swipe", (HttpRequest req, Swipe swipe) =>
{
    var user = GetUsername(req);
    if (user is null) return Results.Unauthorized();

    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var getId = conn.CreateCommand();
    getId.CommandText = "SELECT Id FROM Users WHERE Username=$u";
    getId.Parameters.AddWithValue("$u", user);
    var userId = Convert.ToInt32(getId.ExecuteScalar());

    // Spara swipe
    var ins = conn.CreateCommand();
    ins.CommandText = "INSERT INTO Likes(UserId, MovieId, Liked, CreatedAt) VALUES ($u,$m,$l,$t)";
    ins.Parameters.AddWithValue("$u", userId);
    ins.Parameters.AddWithValue("$m", swipe.MovieId);
    ins.Parameters.AddWithValue("$l", swipe.Liked ? 1 : 0);
    ins.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
    ins.ExecuteNonQuery();

    if (!swipe.Liked) return Results.Ok(new { matched = false });

    // Kolla match
    var find = conn.CreateCommand();
    find.CommandText = "SELECT UserId FROM Likes WHERE MovieId=$m AND Liked=1 AND UserId<>$u LIMIT 1";
    find.Parameters.AddWithValue("$m", swipe.MovieId);
    find.Parameters.AddWithValue("$u", userId);
    var other = find.ExecuteScalar();

    if (other != null && other != DBNull.Value)
    {
        var otherId = Convert.ToInt32(other);
        var mm = conn.CreateCommand();
        mm.CommandText = "INSERT INTO Matches(MovieId, UserA, UserB, CreatedAt) VALUES ($m,$a,$b,$t)";
        mm.Parameters.AddWithValue("$m", swipe.MovieId);
        mm.Parameters.AddWithValue("$a", Math.Min(userId, otherId));
        mm.Parameters.AddWithValue("$b", Math.Max(userId, otherId));
        mm.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
        mm.ExecuteNonQuery();
        return Results.Ok(new { matched = true });
    }

    return Results.Ok(new { matched = false });
});

app.MapGet("/matches", (HttpRequest req) =>
{
    var user = GetUsername(req);
    if (user is null) return Results.Unauthorized();

    using var conn = new SqliteConnection(connStr);
    conn.Open();

    var getId = conn.CreateCommand();
    getId.CommandText = "SELECT Id FROM Users WHERE Username=$u";
    getId.Parameters.AddWithValue("$u", user);
    var userId = Convert.ToInt32(getId.ExecuteScalar());

    var q = conn.CreateCommand();
    q.CommandText = "SELECT MovieId, UserA, UserB, CreatedAt FROM Matches WHERE UserA=$u OR UserB=$u ORDER BY CreatedAt DESC";
    q.Parameters.AddWithValue("$u", userId);

    var r = q.ExecuteReader();
    var list = new List<object>();
    while (r.Read())
    {
        list.Add(new
        {
            MovieId = r.GetInt32(0),
            UserA = r.GetInt32(1),
            UserB = r.GetInt32(2),
            CreatedAt = r.GetString(3)
        });
    }

    return Results.Json(list);
});

// Health check
app.MapGet("/", () => Results.Ok(new { status = "CineMatch API up" }));

app.Run();

// Record-typer för inputs
record UserCred(string Username, string Password);
record Pref(int GenreId, int MaxMinutes);
record Swipe(int MovieId, bool Liked);
