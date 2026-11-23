using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Assignment.Tests;

/// <summary>Runs the real app (in-memory database, Development demo accounts) and drives it over HTTP.</summary>
public class WebAppTests : IClassFixture<WebAppTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            // UseSetting applies before Program.cs reads configuration (ConfigureAppConfiguration is too late for that).
            builder.UseSetting("Database:Provider", "InMemory");
            builder.UseSetting("Database:Name", "web-tests-" + Guid.NewGuid());
            builder.UseSetting("Workflow:EnableAutomation", "false");
        }
    }

    private readonly Factory _factory;

    public WebAppTests(Factory factory) => _factory = factory;

    private HttpClient Client() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<string> TokenFrom(HttpResponseMessage page)
    {
        string html = await page.Content.ReadAsStringAsync();
        return Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
    }

    private static async Task<HttpClient> SignIn(HttpClient client, string email, string password)
    {
        string token = await TokenFrom(await client.GetAsync("/Account/Login"));
        var res = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email, ["Password"] = password, ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        return client;
    }

    [Fact]
    public async Task Anonymous_visitors_are_sent_to_sign_in()
    {
        var res = await Client().GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Account/Login", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Responses_carry_OWASP_security_headers_with_a_script_nonce()
    {
        var res = await Client().GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(res.Headers.Contains("Referrer-Policy"));
        Assert.True(res.Headers.Contains("Permissions-Policy"));
        string csp = res.Headers.GetValues("Content-Security-Policy").Single();
        string nonce = Regex.Match(csp, "'nonce-([^']+)'").Groups[1].Value;
        Assert.NotEmpty(nonce);
        Assert.Contains("frame-ancestors 'none'", csp);
        string html = await res.Content.ReadAsStringAsync();
        Assert.Contains($"nonce=\"{nonce}\"", html);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var client = Client();
        string token = await TokenFrom(await client.GetAsync("/Account/Login"));
        var res = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "admin@hotel.local", ["Password"] = "wrong", ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Invalid email or password", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Posts_without_an_antiforgery_token_are_rejected()
    {
        var res = await Client().PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "admin@hotel.local", ["Password"] = "Admin#2025",
        }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Admin_and_receptionist_get_their_own_dashboards()
    {
        var admin = await SignIn(Client(), "admin@hotel.local", "Admin#2025");
        string adminHome = await (await admin.GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains("Admin dashboard", adminHome);

        var desk = await SignIn(Client(), "reception@hotel.local", "Reception#2025");
        string deskHome = await (await desk.GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains("Front desk", deskHome);
    }

    [Theory]
    [InlineData("/Hotels/Create")]
    [InlineData("/Rooms/Create")]
    public async Task Only_admins_can_manage_hotels_and_rooms(string url)
    {
        var desk = await SignIn(Client(), "reception@hotel.local", "Reception#2025");
        var denied = await desk.GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("/Account/AccessDenied", denied.Headers.Location!.ToString());

        var admin = await SignIn(Client(), "admin@hotel.local", "Admin#2025");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(url)).StatusCode);
    }

    [Theory]
    [InlineData("/Reservations/ViewReservations")]
    [InlineData("/Reservations/Create")]
    [InlineData("/Customers")]
    [InlineData("/Customers/Create")]
    [InlineData("/Invoices")]
    [InlineData("/Hotels")]
    [InlineData("/Rooms")]
    public async Task Staff_pages_render(string url)
    {
        var desk = await SignIn(Client(), "reception@hotel.local", "Reception#2025");
        var res = await desk.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Room_search_returns_demo_rooms_with_estimates()
    {
        var desk = await SignIn(Client(), "reception@hotel.local", "Reception#2025");
        string from = DateTime.Now.AddDays(30).ToString("yyyy-MM-ddTHH:mm");
        string to = DateTime.Now.AddDays(32).ToString("yyyy-MM-ddTHH:mm");
        string json = await (await desk.GetAsync($"/Reservations/SearchRooms?from={from}&to={to}")).Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", json);
        Assert.Contains("estimate", json);
    }

    [Fact]
    public void Web_config_connection_string_wins_once_filled_in()
    {
        IConfiguration Config(string? webConfig) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ConnectionStrings.WebConfigKey] = webConfig,
            ["ConnectionStrings:DefaultConnection"] = "appsettings",
        }).Build();

        Assert.Equal("Data Source=db1", ConnectionStrings.Resolve(Config("Data Source=db1")));
        Assert.Equal("appsettings", ConnectionStrings.Resolve(Config("Data Source=YOUR_SERVER;")));
        Assert.Equal("appsettings", ConnectionStrings.Resolve(Config(null)));
        Assert.Throws<InvalidOperationException>(() => ConnectionStrings.Resolve(new ConfigurationBuilder().Build()));
    }
}
