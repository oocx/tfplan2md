using System.Globalization;
using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.Tests;

/// <summary>
/// Exercises validation rules for screenshot generator CLI inputs.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
[TestClass]
public sealed class CliValidationTests
{
    /// <summary>
    /// Ensures a missing input file triggers a validation failure.
    /// Related acceptance: TC-07.
    /// </summary>
    [TestMethod]
    public void Validate_MissingInputFile_Throws()
    {
        var options = new CliOptions("/path/to/missing.html", 1920, 1080, fullPage: false, format: ScreenshotFormat.Png);

        var exception = Assert.Throws<CliValidationException>(() => CliValidator.Validate(options));

        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures zero or negative dimensions are rejected.
    /// Related acceptance: TC-08.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1080)]
    [DataRow(1920, -1)]
    public void Validate_InvalidDimensions_Throws(int width, int height)
    {
        using var context = CreateTempInput();
        var options = new CliOptions(context.InputPath, width, height, fullPage: false, format: ScreenshotFormat.Png);

        var exception = Assert.Throws<CliValidationException>(() => CliValidator.Validate(options));

        var expectedFragment = width <= 0 ? "width" : "height";
        Assert.Contains(expectedFragment, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures quality outside the permitted range produces a validation error.
    /// Related acceptance: TC-09.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(101)]
    public void Validate_QualityOutOfRange_Throws(int quality)
    {
        using var context = CreateTempInput();
        var options = new CliOptions(context.InputPath, 1920, 1080, fullPage: false, format: ScreenshotFormat.Jpeg, quality: quality);

        var exception = Assert.Throws<CliValidationException>(() => CliValidator.Validate(options));

        Assert.Contains("quality", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures valid options pass validation without exceptions.
    /// Related acceptance: TC-01, TC-03.
    /// </summary>
    [TestMethod]
    public void Validate_WithValidOptions_Succeeds()
    {
        using var context = CreateTempInput();
        var options = new CliOptions(context.InputPath, 1280, 720, fullPage: true, format: ScreenshotFormat.Png);

        CliValidator.Validate(options);
    }

    /// <summary>
    /// Ensures mutually exclusive target options are enforced.
    /// Related acceptance: TC-05.
    /// </summary>
    [TestMethod]
    public void Validate_WithBothTargetOptions_Throws()
    {
        using var context = CreateTempInput();
        var options = new CliOptions(context.InputPath, 1280, 720, fullPage: true, format: ScreenshotFormat.Png, targetTerraformResourceId: "azurerm_firewall.example", targetSelector: "details");

        var exception = Assert.Throws<CliValidationException>(() => CliValidator.Validate(options));

        Assert.Contains("Specify only one", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a temporary HTML file for validation scenarios.
    /// </summary>
    /// <returns>A context that owns the created file for cleanup.</returns>
    private static TempInputContext CreateTempInput()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "cli-validation", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "input.html");
        File.WriteAllText(path, "<html></html>");
        return new TempInputContext(root, path);
    }

    /// <summary>
    /// Represents a disposable temporary input file context.
    /// </summary>
    private sealed class TempInputContext : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempInputContext"/> class.
        /// </summary>
        /// <param name="rootDirectory">Directory to clean on disposal.</param>
        /// <param name="inputPath">Path to the created input file.</param>
        public TempInputContext(string rootDirectory, string inputPath)
        {
            RootDirectory = rootDirectory;
            InputPath = inputPath;
        }

        /// <summary>
        /// Gets the root directory used for the temporary file.
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        /// Gets the path to the created input file.
        /// </summary>
        public string InputPath { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, true);
            }
        }
    }
}
