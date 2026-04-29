using AwesomeAssertions;
using Oocx.TfPlan2Md.CLI;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CLI;

/// <summary>
/// Smoke tests guaranteeing Feature 122 introduces zero new CLI surface (per AC-13:
/// no new flags, no behavioural changes for existing options).
/// Related feature: docs/features/122-terraform-1-15-support/specification.md (Task 16).
/// </summary>
public class HelpOutputDoesNotExposeFeature122FlagsTests
{
    [Test]
    public void HelpText_DoesNotMentionNewActionOrDeprecationFlags()
    {
        var helpText = HelpTextProvider.GetHelpText();

        // Feature 122 was implemented entirely automatically based on plan content; no
        // user-facing flag should have been introduced. If any of these strings appear,
        // it indicates a CLI surface regression.
        helpText.Should().NotContain("--show-actions");
        helpText.Should().NotContain("--hide-actions");
        helpText.Should().NotContain("--show-deprecations");
        helpText.Should().NotContain("--hide-deprecations");
        helpText.Should().NotContain("--show-drift");
        helpText.Should().NotContain("--hide-drift");
        helpText.Should().NotContain("--plan-status");
    }
}
