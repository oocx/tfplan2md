using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Tests for ConfigurationDeprecationReader helper.
/// Related feature: docs/features/122-terraform-1-15-support/test-plan.md Task 3.
/// </summary>
public class ConfigurationDeprecationReaderTests
{
    [Test]
    public void ReadDeprecations_VariableWithDeprecated_Yields()
    {
        // Arrange
        var json = """
        {
          "root_module": {
            "variables": {
              "deprecated_var": {
                "deprecated": "Use new_var instead"
              }
            }
          }
        }
        """;
        var config = JsonDocument.Parse(json).RootElement;

        // Act
        var deprecations = ConfigurationDeprecationReader.ReadDeprecations(config).ToList();

        // Assert
        deprecations.Should().HaveCount(1);
        deprecations[0].Name.Should().Be("deprecated_var");
        deprecations[0].Kind.Should().Be("variable");
        deprecations[0].DeprecationMessage.Should().Be("Use new_var instead");
        deprecations[0].CtyType.Should().BeNull();
    }

    [Test]
    public void ReadDeprecations_OutputWithDeprecatedAndType_Yields()
    {
        // Arrange
        var json = """
        {
          "root_module": {
            "outputs": {
              "deprecated_output": {
                "deprecated": "This output will be removed",
                "type": "string"
              }
            }
          }
        }
        """;
        var config = JsonDocument.Parse(json).RootElement;

        // Act
        var deprecations = ConfigurationDeprecationReader.ReadDeprecations(config).ToList();

        // Assert
        deprecations.Should().HaveCount(1);
        deprecations[0].Name.Should().Be("deprecated_output");
        deprecations[0].Kind.Should().Be("output");
        deprecations[0].DeprecationMessage.Should().Be("This output will be removed");
        deprecations[0].CtyType.Should().Be("string");
    }

    [Test]
    public void ReadDeprecations_OutputWithOnlyType_DoesNotYield()
    {
        // Arrange
        var json = """
        {
          "root_module": {
            "outputs": {
              "regular_output": {
                "type": "string"
              }
            }
          }
        }
        """;
        var config = JsonDocument.Parse(json).RootElement;

        // Act
        var deprecations = ConfigurationDeprecationReader.ReadDeprecations(config).ToList();

        // Assert
        deprecations.Should().BeEmpty();
    }

    [Test]
    public void ReadDeprecations_ConfigurationWithoutRootModule_ReturnsEmpty()
    {
        // Arrange
        var json = """
        {
          "other_field": "value"
        }
        """;
        var config = JsonDocument.Parse(json).RootElement;

        // Act
        var deprecations = ConfigurationDeprecationReader.ReadDeprecations(config).ToList();

        // Assert
        deprecations.Should().BeEmpty();
    }

    [Test]
    public void ReadDeprecations_NullConfiguration_ReturnsEmpty()
    {
        // Act
        var deprecations = ConfigurationDeprecationReader.ReadDeprecations(null).ToList();

        // Assert
        deprecations.Should().BeEmpty();
    }

    [Test]
    public void ReadDeprecations_MultipleDeprecations_YieldsAll()
    {
        // Arrange
        var json = """
        {
          "root_module": {
            "variables": {
              "var1": {
                "deprecated": "Message 1"
              },
              "var2": {
                "deprecated": "Message 2"
              }
            },
            "outputs": {
              "out1": {
                "deprecated": "Message 3",
                "type": "list(string)"
              }
            }
          }
        }
        """;
        var config = JsonDocument.Parse(json).RootElement;

        // Act
        var deprecations = ConfigurationDeprecationReader.ReadDeprecations(config).ToList();

        // Assert
        deprecations.Should().HaveCount(3);
        deprecations.Count(d => d.Kind == "variable").Should().Be(2);
        deprecations.Count(d => d.Kind == "output").Should().Be(1);
        deprecations.Single(d => d.Name == "out1").CtyType.Should().Be("list(string)");
    }
}
