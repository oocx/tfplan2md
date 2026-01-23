using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.ScreenshotGenerator.ScreenshotGeneratorApp")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "type",
    Target = "~T:Oocx.TfPlan2Md.ScreenshotGenerator.Capturing.HtmlScreenshotCapturer")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.ScreenshotGenerator.Capturing.HtmlScreenshotCapturer.CaptureAsync(" +
             "Oocx.TfPlan2Md.ScreenshotGenerator.Capturing.CaptureSettings," +
             "System.Threading.CancellationToken)")]

[assembly: SuppressMessage(
    "Maintainability",
    "CA1502:Avoid excessive complexity",
    Justification = "Baseline for docs/features/046-code-quality-metrics-enforcement/.",
    Scope = "member",
    Target = "~M:Oocx.TfPlan2Md.ScreenshotGenerator.CLI.CliParser.Parse(" +
             "System.Collections.Generic.IReadOnlyList{System.String})")]
