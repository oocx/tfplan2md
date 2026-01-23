namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Exception thrown when parsing a Terraform plan fails.
/// </summary>
public class TerraformPlanParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerraformPlanParseException"/> class.
    /// </summary>
    public TerraformPlanParseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerraformPlanParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TerraformPlanParseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerraformPlanParseException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public TerraformPlanParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
