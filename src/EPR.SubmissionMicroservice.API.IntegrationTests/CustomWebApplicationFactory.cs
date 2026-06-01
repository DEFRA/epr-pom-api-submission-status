using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EPR.SubmissionMicroservice.API.IntegrationTests;

internal class CustomWebApplicationFactory(IConfiguration? configuration = null) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            // config here is in the context of the host - the web application - so this
            // builds it from the web application's appsettings file
            config.AddJsonFile("appsettings.json");
            
            // And then add any custom config passed in from the test project
            if (configuration != null)
            {
                config.AddConfiguration(configuration);
            }
        });
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "80");
    }
}