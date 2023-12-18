namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

public class TestBase
{
    protected readonly string OrganisationId = Guid.NewGuid().ToString();
    protected readonly HttpClient HttpClient;
    private readonly string _userId = Guid.NewGuid().ToString();

    protected TestBase()
    {
        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("https_port", "80");
            });

        HttpClient = application.CreateDefaultClient();
        HttpClient.BaseAddress = new Uri("https://localhost:8000");
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpClient.DefaultRequestHeaders.Add("organisationId", OrganisationId);
        HttpClient.DefaultRequestHeaders.Add("userId", _userId);
    }
}