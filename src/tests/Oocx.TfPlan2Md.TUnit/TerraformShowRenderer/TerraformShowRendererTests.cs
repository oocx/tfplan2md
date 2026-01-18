using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;
using Renderer = Oocx.TfPlan2Md.TerraformShowRenderer.Rendering.TerraformShowRenderer;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Validates high-level rendering scaffolding for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
public sealed class TerraformShowRendererTests
{
    /// <summary>
    /// Ensures legend and header are rendered with ANSI styling enabled.
    /// Related acceptance: Task 2 legend and header.
    /// </summary>
    [Test]
    public async Task Render_WithColors_WritesLegendAndHeader()
    {
        var plan = CreateEmptyPlan();
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false);

        await Assert.That(output).Contains("Terraform used the selected providers");
        await Assert.That(output).Contains("Terraform will perform the following actions:");
        await Assert.That(output).Contains("\u001b[32m+\u001b[0m"); // create symbol
        await Assert.That(output).Contains("\u001b[33m~\u001b[0m"); // update symbol
    }

    /// <summary>
    /// Ensures legend and header render without ANSI escape sequences when color is suppressed.
    /// Related acceptance: Task 2 no-color flag.
    /// </summary>
    [Test]
    public async Task Render_NoColor_OmitsAnsiSequences()
    {
        var plan = CreateEmptyPlan();
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true);

        await Assert.That(output).DoesNotContain("\u001b[");
        await Assert.That(output).Contains("+ create");
        await Assert.That(output).Contains("Terraform will perform the following actions:");
    }

    /// <summary>
    /// Ensures resource headers and block markers render for create actions without color noise.
    /// Related acceptance: Task 3 action mapping.
    /// </summary>
    [Test]
    public async Task Render_CreateResource_WritesHeaderAndMarker()
    {
        var after = Json("{ \"name\": \"main\" }");
        var plan = CreatePlan(new ResourceChange("aws_s3_bucket.main", null, "managed", "aws_s3_bucket", "main", "aws", new Change(["create"], null, after)));
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true);

        await Assert.That(output).Contains("# aws_s3_bucket.main will be created");
        await Assert.That(output).Contains("  + resource \"aws_s3_bucket\" \"main\" {");
        await Assert.That(output).Contains("  }");
    }

    /// <summary>
    /// Ensures delete actions are emphasized in red and bold.
    /// Related acceptance: Task 3 action styling.
    /// </summary>
    [Test]
    public async Task Render_DeleteResource_UsesRedBoldPhrase()
    {
        var before = Json("{ \"name\": \"old\" }");
        var plan = CreatePlan(new ResourceChange("aws_s3_bucket.main", null, "managed", "aws_s3_bucket", "main", "aws", new Change(["delete"], before, null)));
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false);

        await Assert.That(output).Contains("will be \u001b[1m\u001b[31m");
        await Assert.That(output).Contains("destroyed");
        await Assert.That(output).Contains("\u001b[31m-\u001b[0m");
        await Assert.That(output).Contains("resource \"aws_s3_bucket\" \"main\" {");
    }

    /// <summary>
    /// Ensures replace actions show the combined marker and red bold keyword.
    /// Related acceptance: Task 3 replace mapping.
    /// </summary>
    [Test]
    public async Task Render_ReplaceResource_ShowsReplacementMarker()
    {
        var before = Json("{ \"name\": \"old\" }");
        var after = Json("{ \"name\": \"new\" }");
        var replacePaths = new List<IReadOnlyList<object>> { new List<object> { "name" } };
        var plan = CreatePlan(new ResourceChange("aws_s3_bucket.main", null, "managed", "aws_s3_bucket", "main", "aws", new Change(["delete", "create"], before, after, null, null, null, replacePaths)));
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false);

        await Assert.That(output).Contains("must be \u001b[1m\u001b[31m");
        await Assert.That(output).Contains("replaced");
        await Assert.That(output).Contains("-\u001b[0m/\u001b[32m+");
    }

    /// <summary>
    /// Ensures read actions use the cyan marker and data keyword.
    /// Related acceptance: Task 3 read mapping.
    /// </summary>
    [Test]
    public async Task Render_ReadAction_UsesDataKeywordAndCyanMarker()
    {
        var after = Json("{ \"account_id\": \"1234567890\" }");
        var plan = CreatePlan(new ResourceChange("data.aws_caller_identity.current", null, "data", "aws_caller_identity", "current", "aws", new Change(["read"], null, after)));
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: false);

        await Assert.That(output).Contains("will be read during apply");
        await Assert.That(output).Contains("\u001b[36m<=\u001b[0m");
        await Assert.That(output).Contains("data \"aws_caller_identity\" \"current\" {");
    }

    /// <summary>
    /// Ensures action reasons are rendered when supplied.
    /// Related acceptance: Task 3 action reasons.
    /// </summary>
    [Test]
    public async Task Render_ActionReason_RendersSecondHeaderLine()
    {
        var before = Json("{ \"name\": \"orphan\" }");
        var change = new ResourceChange("aws_s3_bucket.orphan", null, "managed", "aws_s3_bucket", "orphan", "aws", new Change(["delete"], before, null), "delete_because_no_resource_config");
        var plan = CreatePlan(change);
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true);

        await Assert.That(output).Contains("# aws_s3_bucket.orphan will be destroyed");
        await Assert.That(output).Contains("# (because aws_s3_bucket.orphan is not in configuration)");
    }

    /// <summary>
    /// Ensures unknown values render the known-after-apply placeholder.
    /// Related acceptance: Task 4 after_unknown handling.
    /// </summary>
    [Test]
    public async Task Render_CreateUnknown_RendersKnownAfterApply()
    {
        var after = Json("{ \"id\": null }");
        var afterUnknown = Json("{ \"id\": true }");
        var change = new Change(["create"], null, after, afterUnknown, null, null);
        var plan = CreatePlan(new ResourceChange("aws_s3_bucket.main", null, "managed", "aws_s3_bucket", "main", "aws", change));
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true);

        await Assert.That(output).Contains("id = (known after apply)");
    }

    /// <summary>
    /// Ensures sensitive values are hidden.
    /// Related acceptance: Task 4 sensitive handling.
    /// </summary>
    [Test]
    public async Task Render_SensitiveValue_RendersPlaceholder()
    {
        var after = Json("{ \"secret\": \"value\" }");
        var afterSensitive = Json("{ \"secret\": true }");
        var change = new Change(["create"], null, after, null, null, afterSensitive);
        var plan = CreatePlan(new ResourceChange("aws_s3_bucket.main", null, "managed", "aws_s3_bucket", "main", "aws", change));
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true);

        await Assert.That(output).Contains("secret = (sensitive value)");
    }

    /// <summary>
    /// Ensures unchanged attributes emit a hidden count comment on update.
    /// Related acceptance: Task 4 unchanged comment.
    /// </summary>
    [Test]
    public async Task Render_Update_ShowsHiddenUnchangedCount()
    {
        var before = Json("{ \"name\": \"old\", \"unchanged\": \"same\" }");
        var after = Json("{ \"name\": \"new\", \"unchanged\": \"same\" }");
        var change = new Change(["update"], before, after);
        var plan = CreatePlan(new ResourceChange("aws_s3_bucket.main", null, "managed", "aws_s3_bucket", "main", "aws", change));
        var renderer = new Renderer();

        var output = renderer.Render(plan, suppressColor: true);

        await Assert.That(output).Matches("name\\s*=\\s*\"old\"\\s*->\\s*\"new\"");
        await Assert.That(output).Contains("# (1 unchanged attributes hidden)");
    }

    /// <summary>
    /// Creates a minimal Terraform plan instance for scaffold testing.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md
    /// </summary>
    /// <returns>A Terraform plan with no resource changes.</returns>
    private static TerraformPlan CreateEmptyPlan()
    {
        return new TerraformPlan("1.2", "1.6.0", Array.Empty<ResourceChange>(), DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Builds a plan containing the provided resource changes.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md
    /// </summary>
    /// <param name="changes">Resource changes to include.</param>
    /// <returns>Terraform plan with supplied changes.</returns>
    private static TerraformPlan CreatePlan(params ResourceChange[] changes)
    {
        return new TerraformPlan("1.2", "1.6.0", changes, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Parses JSON into a cloneable <see cref="JsonElement"/> to keep ordering intact.
    /// </summary>
    /// <param name="json">JSON text.</param>
    /// <returns>Cloned JSON element.</returns>
    private static JsonElement Json(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
