using System.Text.Json;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Parses Terraform plan JSON into strongly-typed objects.
/// </summary>
public class TerraformPlanParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Parses a Terraform plan from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string representing the Terraform plan.</param>
    /// <returns>The parsed Terraform plan.</returns>
    /// <exception cref="TerraformPlanParseException">Thrown when parsing fails.</exception>
    public TerraformPlan Parse(string json)
    {
        try
        {
            var plan = JsonSerializer.Deserialize<TerraformPlan>(json, Options);
            return plan ?? throw new TerraformPlanParseException("Deserialization returned null.");
        }
        catch (JsonException ex)
        {
            throw new TerraformPlanParseException($"Failed to parse Terraform plan JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a Terraform plan from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the Terraform plan JSON.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The parsed Terraform plan.</returns>
    /// <exception cref="TerraformPlanParseException">Thrown when parsing fails.</exception>
    public async Task<TerraformPlan> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await JsonSerializer.DeserializeAsync<TerraformPlan>(stream, Options, cancellationToken);
            return plan ?? throw new TerraformPlanParseException("Deserialization returned null.");
        }
        catch (JsonException ex)
        {
            throw new TerraformPlanParseException($"Failed to parse Terraform plan JSON: {ex.Message}", ex);
        }
    }
}
