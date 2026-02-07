using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Tests.TestData;

/// <summary>
/// Creates principal mappers for tests using the shared mapping loader.
/// </summary>
internal static class PrincipalMapperFactory
{
    /// <summary>
    /// Creates a principal mapper from a mapping file path.
    /// </summary>
    /// <param name="mappingFile">The mapping file path, or null when none is provided.</param>
    /// <param name="diagnosticContext">Optional diagnostic context to populate.</param>
    /// <returns>A configured <see cref="PrincipalMapper"/> instance.</returns>
    internal static PrincipalMapper Create(string? mappingFile, DiagnosticContext? diagnosticContext = null)
    {
        var mappingResult = AzureMappingFileLoader.Load(mappingFile, diagnosticContext);
        return new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes, diagnosticContext);
    }
}
