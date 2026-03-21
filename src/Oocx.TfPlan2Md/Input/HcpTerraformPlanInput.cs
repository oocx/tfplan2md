using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Input;

/// <summary>
/// Fetches Terraform plan JSON from HCP Terraform using a run id.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Input adapter composes HTTP, URI validation, and JSON parsing concerns for a single external boundary.")]
internal sealed class HcpTerraformPlanInput(HttpClient httpClient)
{
    /// <summary>
    /// Default HCP Terraform address used when <c>TFE_ADDRESS</c> is not set.
    /// </summary>
    private const string DefaultTfeAddress = "https://app.terraform.io";

    /// <summary>
    /// HTTP client used to call HCP Terraform API endpoints.
    /// </summary>
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Resolves plan JSON for an HCP Terraform run id.
    /// </summary>
    /// <param name="runId">HCP Terraform run identifier.</param>
    /// <param name="cancellationToken">Cancellation signal for the HTTP workflow.</param>
    /// <returns>Terraform plan JSON text suitable for the existing parser pipeline.</returns>
    public async Task<string> GetPlanJsonAsync(string runId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new InvalidOperationException("HCP run ID is required when using --hcp-run-id.");
        }

        var token = Environment.GetEnvironmentVariable("TFE_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("TFE_TOKEN is required for HCP run-id input.");
        }

        var baseAddressRaw = Environment.GetEnvironmentVariable("TFE_ADDRESS");
        var baseAddress = string.IsNullOrWhiteSpace(baseAddressRaw) ? DefaultTfeAddress : baseAddressRaw;
        var normalizedBaseAddress = NormalizeAndValidateBaseAddress(baseAddress);

        var planId = await GetPlanIdAsync(normalizedBaseAddress, runId, token, cancellationToken);
        return await GetPlanJsonByIdAsync(normalizedBaseAddress, planId, token, cancellationToken);
    }

    /// <summary>
    /// Retrieves the plan id for a run from HCP Terraform API.
    /// </summary>
    /// <param name="baseAddress">Base TFE address.</param>
    /// <param name="runId">Run identifier.</param>
    /// <param name="token">Bearer token value.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Plan id resolved from the run record.</returns>
    private async Task<string> GetPlanIdAsync(
        string baseAddress,
        string runId,
        string token,
        CancellationToken cancellationToken)
    {
        var escapedRunId = Uri.EscapeDataString(runId);
        var requestUri = BuildApiUri(baseAddress, $"/api/v2/runs/{escapedRunId}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"HCP Terraform run lookup failed ({(int)response.StatusCode} {response.ReasonPhrase}).");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var data)
                && data.TryGetProperty("relationships", out var relationships)
                && relationships.TryGetProperty("plan", out var plan)
                && plan.TryGetProperty("data", out var planData)
                && planData.TryGetProperty("id", out var id)
                && id.ValueKind == JsonValueKind.String)
            {
                var planId = id.GetString();
                if (!string.IsNullOrWhiteSpace(planId))
                {
                    return planId;
                }
            }

            throw new InvalidOperationException("HCP Terraform run response did not include relationships.plan.data.id.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Malformed HCP Terraform run response payload.", ex);
        }
    }

    /// <summary>
    /// Retrieves plan JSON by plan id from HCP Terraform API.
    /// </summary>
    /// <param name="baseAddress">Base TFE address.</param>
    /// <param name="planId">Plan identifier resolved from run lookup.</param>
    /// <param name="token">Bearer token value.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Validated Terraform plan JSON string.</returns>
    private async Task<string> GetPlanJsonByIdAsync(
        string baseAddress,
        string planId,
        string token,
        CancellationToken cancellationToken)
    {
        var escapedPlanId = Uri.EscapeDataString(planId);
        var requestUri = BuildApiUri(baseAddress, $"/api/v2/plans/{escapedPlanId}/json-output");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException(
                "HCP Terraform plan JSON is not available yet (run is still in progress). Please retry when planning is complete.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"HCP Terraform plan JSON lookup failed ({(int)response.StatusCode} {response.ReasonPhrase}).");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.GetRawText();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Malformed plan JSON received from HCP Terraform.", ex);
        }
    }

    /// <summary>
    /// Builds an absolute API URI from base address and API path.
    /// </summary>
    /// <param name="baseAddress">Configured HCP Terraform address.</param>
    /// <param name="apiPath">API path beginning with slash.</param>
    /// <returns>Absolute URI for API invocation.</returns>
    private static Uri BuildApiUri(string baseAddress, string apiPath)
    {
        var normalized = baseAddress.TrimEnd('/');
        return new Uri($"{normalized}{apiPath}", UriKind.Absolute);
    }

    /// <summary>
    /// Validates and normalizes the configured TFE base address.
    /// </summary>
    /// <param name="baseAddress">Raw base address from configuration or default value.</param>
    /// <returns>Normalized absolute HTTPS base address string.</returns>
    private static string NormalizeAndValidateBaseAddress(string baseAddress)
    {
        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("TFE_ADDRESS must be a valid absolute HTTPS URL.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("TFE_ADDRESS must use https.");
        }

        return uri.ToString();
    }
}
