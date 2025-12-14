namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Exception thrown when parsing a Terraform plan fails.
/// </summary>
public class TerraformPlanParseException : ApplicationException
{
    public TerraformPlanParseException(string message) : base(message)
    {
    }

    public TerraformPlanParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
