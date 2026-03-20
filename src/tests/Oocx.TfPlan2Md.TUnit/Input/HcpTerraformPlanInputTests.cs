using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Input;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Input;

[NotInParallel("EnvironmentVariables")]
public class HcpTerraformPlanInputTests
{
    [Test]
    public async Task GetPlanJsonAsync_WithValidRunId_ReturnsPlanJson()
    {
        // Arrange
        var previousToken = Environment.GetEnvironmentVariable("TFE_TOKEN");
        var previousAddress = Environment.GetEnvironmentVariable("TFE_ADDRESS");
        Environment.SetEnvironmentVariable("TFE_TOKEN", "test-token");
        Environment.SetEnvironmentVariable("TFE_ADDRESS", "https://app.terraform.io");

        var handler = new StubHttpMessageHandler(req =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri?.AbsolutePath == "/api/v2/runs/run-abc123/plan")
            {
                req.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "test-token"));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        { "data": { "id": "plan-123" } }
                        """, Encoding.UTF8, "application/json")
                };
            }

            if (req.Method == HttpMethod.Get && req.RequestUri?.AbsolutePath == "/api/v2/plans/plan-123/json-output")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        { "format_version": "1.2", "terraform_version": "1.9.0", "resource_changes": [] }
                        """, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler);
        var sut = new HcpTerraformPlanInput(httpClient);

        // Act
        var planJson = await sut.GetPlanJsonAsync("run-abc123", CancellationToken.None);

        // Assert
        planJson.Should().Contain("\"format_version\"");
        planJson.Should().Contain("\"resource_changes\"");

        Environment.SetEnvironmentVariable("TFE_TOKEN", previousToken);
        Environment.SetEnvironmentVariable("TFE_ADDRESS", previousAddress);
    }

    [Test]
    public async Task GetPlanJsonAsync_WithoutToken_ThrowsActionableError()
    {
        // Arrange
        var previousToken = Environment.GetEnvironmentVariable("TFE_TOKEN");
        Environment.SetEnvironmentVariable("TFE_TOKEN", null);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var sut = new HcpTerraformPlanInput(httpClient);

        // Act
        var act = () => sut.GetPlanJsonAsync("run-abc123", CancellationToken.None);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        ex.Should().NotBeNull();
        ex!.Message.Should().Contain("TFE_TOKEN");

        Environment.SetEnvironmentVariable("TFE_TOKEN", previousToken);
    }

    [Test]
    public async Task GetPlanJsonAsync_WhenApiReturnsUnauthorized_ThrowsActionableError()
    {
        // Arrange
        var previousToken = Environment.GetEnvironmentVariable("TFE_TOKEN");
        Environment.SetEnvironmentVariable("TFE_TOKEN", "test-token");
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var httpClient = new HttpClient(handler);
        var sut = new HcpTerraformPlanInput(httpClient);

        // Act
        var act = () => sut.GetPlanJsonAsync("run-abc123", CancellationToken.None);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        ex.Should().NotBeNull();
        ex!.Message.Should().Contain("401");

        Environment.SetEnvironmentVariable("TFE_TOKEN", previousToken);
    }

    [Test]
    public async Task GetPlanJsonAsync_WhenPayloadMalformed_ThrowsActionableError()
    {
        // Arrange
        var previousToken = Environment.GetEnvironmentVariable("TFE_TOKEN");
        Environment.SetEnvironmentVariable("TFE_TOKEN", "test-token");

        var handler = new StubHttpMessageHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath == "/api/v2/runs/run-abc123/plan")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        { "data": { "id": "plan-123" } }
                        """, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ not-json", Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var sut = new HcpTerraformPlanInput(httpClient);

        // Act
        var act = () => sut.GetPlanJsonAsync("run-abc123", CancellationToken.None);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        ex.Should().NotBeNull();
        ex!.Message.Should().Contain("Malformed plan JSON");

        Environment.SetEnvironmentVariable("TFE_TOKEN", previousToken);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
