using EPR.SubmissionMicroservice.Application.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Azure;
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
        builder.ConfigureServices((context, services) =>
        {
            var serviceBusConfig =
                context.Configuration.GetSection(ServiceBusOptions.ConfigSection).Get<ServiceBusOptions>() ??
                throw new InvalidOperationException("Cannot find 'ServiceBus' section in appSettings.");

            services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.AddServiceBusClient(serviceBusConfig.ConnectionString);
            });

            builder.UseSetting("https_port", "80");
        });
    }
}