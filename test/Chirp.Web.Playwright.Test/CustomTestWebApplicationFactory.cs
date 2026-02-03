using System.Data.Common;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chirp.Web.Playwright.Test;
/* Custom test environment for tests in ASP.NET Core with Playwright.
Defines a custom factory for the test server environment for the application
Referenced from: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0
 */
public class CustomTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _host;
    private static readonly Queue<int> PortQueue = new Queue<int>(Enumerable.Range(5000, 20));  // Range af porte, f.eks. 5000-5999

    // Hent den næste ledige port
    private static int GetNextAvailablePort()
    {
        lock (PortQueue)
        {
            if (PortQueue.Count > 0)
            {
                return PortQueue.Dequeue();  // Hent næste port
            }

            // Hvis køen er tom, så kør en exception eller reinitialize køen
            throw new InvalidOperationException("No available ports left in the range.");
        }
    }

    //Property for getting the server's base address
    public string ServerAddress
    {
        get
        {
            if (_host is null)
            {
                // This forces WebApplicationFactory to bootstrap the server
                using var client = CreateDefaultClient();
            }
            return ClientOptions.BaseAddress.ToString();
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        //building the test host.
        var testHost = builder.Build();
        var port = GetNextAvailablePort(); // Choose a range of ports

        // Set up the custom URL with the random port
        var baseUrl = $"http://127.0.0.1:{port}";

        //builder that configures the services needed for testing
        builder.ConfigureServices(services =>
        {
            //removing the existing CheepDBContext to replace it with an in-memory test database
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<CheepDBContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            //removing any existing database connection
            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbConnection));
            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            // Create open SqliteConnection so EF won't automatically close it.
            services.AddSingleton<DbConnection>(_ =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open(); //keeps the connection open for the test

                return connection;
            });

            //Configures the CheepDBContext to use the SQLite in-memory database connection
            services.AddDbContext<CheepDBContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });

            //removing the existing authentication service to replace it with a test authentication handler
            var authService = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(IAuthenticationService));
            if (authService != null)
            {
                services.Remove(authService);
            }

            //configures the test authentication with a custom scheme in "TestAuthenticationHandler" for Playwright tests
            services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme);
        });

        builder.UseEnvironment("Development");
        //configuring the server to use Kestrel, the ASP.NET Core web server
        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel().UseUrls(baseUrl));

        //building and starting the custom host for the test environment
        _host = builder.Build();
        _host.Start();

        //retrieving the server's address and set it as the base address for HTTP client options
        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>() ?? throw new InvalidOperationException("No server addresses found.");
        ClientOptions.BaseAddress = addresses.Addresses
            .Select(x => new Uri(x))
            .Last();

        //starting the initial test host instance
        testHost.Start();
        return testHost;
    }
    
    protected override void Dispose(bool disposing)
    {
        _host?.StopAsync().Wait();
        Thread.Sleep(2000);
        _host?.Dispose();
    }
}
