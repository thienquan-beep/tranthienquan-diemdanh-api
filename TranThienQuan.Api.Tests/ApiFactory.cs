using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace TranThienQuan.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _tenDb = "test-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("TestDbName", _tenDb);
        builder.ConfigureLogging(l => l.ClearProviders());
    }
}
