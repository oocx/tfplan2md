# Test Cases: Azure DevOps Principal Mapping

## Overview

This document provides detailed test case specifications for the Azure DevOps principal mapping feature. Each test case includes specific inputs, expected outputs, and implementation guidance for the Developer agent.

**Related Documents:**
- Test Plan: `docs/features/085-azdo-principal-mapping/test-plan.md`
- Specification: `docs/features/085-azdo-principal-mapping/specification.md`

## Test Organization

Tests are organized by component and follow the naming convention: `MethodName_Scenario_ExpectedResult`

**Test Location:** `src/tests/Oocx.TfPlan2Md.TUnit/`

**Test Namespaces:**
- Data model tests: `Oocx.TfPlan2Md.Tests.Platforms.Azure`
- Mapper tests: `Oocx.TfPlan2Md.Tests.Providers.AzureDevOps`
- Helper tests: `Oocx.TfPlan2Md.Tests.MarkdownGeneration`
- Integration tests: `Oocx.TfPlan2Md.Tests.MarkdownGeneration`

---

## Data Model Tests

### TC-01: PrincipalMappingFile_DeserializeAzdoUsers_PopulatesProperty

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/PrincipalMappingFileTests.cs`

**Test Method:**
```csharp
[Test]
public void PrincipalMappingFile_DeserializeAzdoUsers_PopulatesProperty()
{
    var json = """
    {
      "azdoUsers": {
        "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
        "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
      }
    }
    """;

    var mappingFile = JsonSerializer.Deserialize<PrincipalMappingFile>(json);

    mappingFile.Should().NotBeNull();
    mappingFile!.AzdoUsers.Should().NotBeNull();
    mappingFile.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b")
        .WhoseValue.Should().Be("John Smith");
    mappingFile.AzdoUsers.Should().ContainKey("7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f")
        .WhoseValue.Should().Be("Alice Johnson");
    mappingFile.AzdoUsers.Count.Should().Be(2);
}
```

**Notes:**
- Uses `System.Text.Json` deserialization
- Verifies `JsonPropertyName("azdoUsers")` attribute works correctly
- Similar pattern to existing Azure AD property tests

---

### TC-02: PrincipalMappingFile_DeserializeAllAzdoSections_PopulatesAllProperties

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/PrincipalMappingFileTests.cs`

**Test Method:**
```csharp
[Test]
public void PrincipalMappingFile_DeserializeAllAzdoSections_PopulatesAllProperties()
{
    var json = """
    {
      "azdoUsers": {
        "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith"
      },
      "azdoGroups": {
        "vssgp.Uy0xLTktMTU1MTM...": "Platform Team"
      },
      "azdoProjects": {
        "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project"
      }
    }
    """;

    var mappingFile = JsonSerializer.Deserialize<PrincipalMappingFile>(json);

    mappingFile.Should().NotBeNull();
    mappingFile!.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");
    mappingFile.AzdoGroups.Should().ContainKey("vssgp.Uy0xLTktMTU1MTM...");
    mappingFile.AzdoProjects.Should().ContainKey("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f");
}
```

**Notes:**
- Tests all three properties simultaneously
- Verifies independence of sections

---

## Parser Tests

### TC-03: AzureMappingFileLoader_LoadAzdoUsers_ReturnsAzdoUsersDictionary

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

**Test Method:**
```csharp
[Test]
public void Load_AzdoUsersSection_ParsesCorrectly()
{
    var filePath = GetTempPath($"azdo-users-{Guid.NewGuid():N}.json");
    File.WriteAllText(filePath, """
    {
      "azdoUsers": {
        "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
        "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
      }
    }
    """);
    var diagnostics = new DiagnosticContext();

    try
    {
        var result = AzureMappingFileLoader.Load(filePath, diagnostics);

        result.AzdoUsers.Should().NotBeNull();
        result.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b")
            .WhoseValue.Should().Be("John Smith");
        result.AzdoUsers.Should().ContainKey("7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f")
            .WhoseValue.Should().Be("Alice Johnson");
        result.AzdoUsers.Count.Should().Be(2);
        
        diagnostics.AzdoUserCount.Should().Be(2);
    }
    finally
    {
        File.Delete(filePath);
    }
}
```

**Notes:**
- Uses temporary file in `.tmp/mapping-loader-tests/`
- Follows pattern from existing `AzureMappingFileLoaderTests`
- Verifies diagnostic count is updated

---

### TC-04: AzureMappingFileLoader_LoadAzdoGroups_PreservesLongDescriptors

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

**Test Method:**
```csharp
[Test]
public void Load_AzdoGroupsSection_PreservesLongDescriptors()
{
    var filePath = GetTempPath($"azdo-groups-{Guid.NewGuid():N}.json");
    var longDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3LTExMDE5ODM1NjMtMzU0Nzk5MjM2MS0zNzAyMTIxNjI4LTEtMTIzNDU2Nzg5MC0xMjM0NTY3ODkwLTEyMzQ1Njc4OTAtMTIzNDU2Nzg5MA";
    File.WriteAllText(filePath, $$"""
    {
      "azdoGroups": {
        "{{longDescriptor}}": "Platform Team",
        "vssgp.Short": "Security Team"
      }
    }
    """);
    var diagnostics = new DiagnosticContext();

    try
    {
        var result = AzureMappingFileLoader.Load(filePath, diagnostics);

        result.AzdoGroups.Should().ContainKey(longDescriptor)
            .WhoseValue.Should().Be("Platform Team");
        result.AzdoGroups.Keys.Should().Contain(key => key.Length > 100);
        diagnostics.AzdoGroupCount.Should().Be(2);
    }
    finally
    {
        File.Delete(filePath);
    }
}
```

**Notes:**
- Tests realistic long group descriptor (100+ characters)
- Verifies no truncation or corruption

---

### TC-05: AzureMappingFileLoader_LoadAzdoProjects_ReturnsProjectsDictionary

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

**Test Method:**
```csharp
[Test]
public void Load_AzdoProjectsSection_ParsesCorrectly()
{
    var filePath = GetTempPath($"azdo-projects-{Guid.NewGuid():N}.json");
    File.WriteAllText(filePath, """
    {
      "azdoProjects": {
        "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project",
        "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": "Application Platform"
      }
    }
    """);
    var diagnostics = new DiagnosticContext();

    try
    {
        var result = AzureMappingFileLoader.Load(filePath, diagnostics);

        result.AzdoProjects.Should().ContainKey("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f")
            .WhoseValue.Should().Be("Infrastructure Project");
        result.AzdoProjects.Count.Should().Be(2);
        diagnostics.AzdoProjectCount.Should().Be(2);
    }
    finally
    {
        File.Delete(filePath);
    }
}
```

---

### TC-06: AzureMappingFileLoader_LoadMixedSections_SegregatesMappingsCorrectly

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

**Test Method:**
```csharp
[Test]
public void Load_MixedAzureAndAzdoSections_ParsesBothCorrectly()
{
    var filePath = GetTempPath($"mixed-mapping-{Guid.NewGuid():N}.json");
    File.WriteAllText(filePath, """
    {
      "users": {
        "00000000-0000-0000-0000-000000000001": "Azure AD User"
      },
      "groups": {
        "00000000-0000-0000-0000-000000000002": "Azure AD Group"
      },
      "azdoUsers": {
        "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "Azure DevOps User"
      },
      "azdoGroups": {
        "vssgp.Uy0xLTktMTU1MTM...": "Azure DevOps Group"
      },
      "azdoProjects": {
        "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Azure DevOps Project"
      }
    }
    """);
    var diagnostics = new DiagnosticContext();

    try
    {
        var result = AzureMappingFileLoader.Load(filePath, diagnostics);

        // Azure AD principals in Principals dictionary
        result.Principals.Should().ContainKey("00000000-0000-0000-0000-000000000001")
            .WhoseValue.Should().Be("Azure AD User");
        result.Principals.Should().ContainKey("00000000-0000-0000-0000-000000000002")
            .WhoseValue.Should().Be("Azure AD Group");
        
        // Azure DevOps entities in separate dictionaries
        result.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b")
            .WhoseValue.Should().Be("Azure DevOps User");
        result.AzdoGroups.Should().ContainKey("vssgp.Uy0xLTktMTU1MTM...")
            .WhoseValue.Should().Be("Azure DevOps Group");
        result.AzdoProjects.Should().ContainKey("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f")
            .WhoseValue.Should().Be("Azure DevOps Project");
        
        // Verify no cross-contamination
        result.Principals.Should().NotContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");
        result.AzdoUsers.Should().NotContainKey("00000000-0000-0000-0000-000000000001");
    }
    finally
    {
        File.Delete(filePath);
    }
}
```

**Notes:**
- Critical test for segregation between Azure AD and Azure DevOps mappings
- Verifies no cross-contamination

---

### TC-07: AzureMappingFileLoader_NullAzdoSections_ReturnsEmptyDictionaries

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

**Test Method:**
```csharp
[Test]
public void Load_NullAzdoSections_HandlesGracefully()
{
    var filePath = GetTempPath($"null-azdo-{Guid.NewGuid():N}.json");
    File.WriteAllText(filePath, """
    {
      "users": {
        "user-1": "Test User"
      },
      "azdoUsers": null,
      "azdoGroups": null,
      "azdoProjects": null
    }
    """);
    var diagnostics = new DiagnosticContext();

    try
    {
        var result = AzureMappingFileLoader.Load(filePath, diagnostics);

        result.Principals.Should().ContainKey("user-1");
        result.AzdoUsers.Should().BeEmpty();
        result.AzdoGroups.Should().BeEmpty();
        result.AzdoProjects.Should().BeEmpty();
        
        diagnostics.AzdoUserCount.Should().Be(0);
        diagnostics.AzdoGroupCount.Should().Be(0);
        diagnostics.AzdoProjectCount.Should().Be(0);
        diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
    }
    finally
    {
        File.Delete(filePath);
    }
}
```

**Notes:**
- Tests null handling
- Should not throw exceptions

---

### TC-08: AzureMappingFileLoader_MissingAzdoSections_BackwardsCompatible

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AzureMappingFileLoaderTests.cs`

**Test Method:**
```csharp
[Test]
public void Load_LegacyFileWithoutAzdoSections_WorksAsExpected()
{
    // Use existing example file without azdo sections
    var filePath = Path.Combine(GetRepoRoot(), "examples/comprehensive-demo/demo-principals-nested.json");
    var diagnostics = new DiagnosticContext();

    var result = AzureMappingFileLoader.Load(filePath, diagnostics);

    // Azure AD mappings work
    result.Principals.Should().NotBeEmpty();
    
    // Azdo dictionaries are empty but not null
    result.AzdoUsers.Should().NotBeNull().And.BeEmpty();
    result.AzdoGroups.Should().NotBeNull().And.BeEmpty();
    result.AzdoProjects.Should().NotBeNull().And.BeEmpty();
    
    diagnostics.AzdoUserCount.Should().Be(0);
    diagnostics.AzdoGroupCount.Should().Be(0);
    diagnostics.AzdoProjectCount.Should().Be(0);
    diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
}
```

**Notes:**
- Critical backwards compatibility test
- Uses existing production mapping file

---

## Mapper Tests

### TC-09: AzdoUserMapper_GetEntityName_KnownUser_ReturnsFormattedName

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoUserMapperTests.cs`

**Test Method:**
```csharp
[Test]
public void GetEntityName_KnownUserId_ReturnsFormattedName()
{
    var mappings = new Dictionary<string, string>
    {
        ["4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b"] = "John Smith"
    }.ToFrozenDictionary();
    var mapper = new AzdoUserMapper(mappings, null);

    var result = mapper.GetEntityName("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");

    result.Should().Be("John Smith [4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b]");
}
```

**Notes:**
- Tests the core formatting behavior
- Pattern follows `PrincipalMapper.GetPrincipalName()`

---

### TC-10: AzdoGroupMapper_GetEntityName_LongDescriptor_ReturnsFullDescriptor

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoGroupMapperTests.cs`

**Test Method:**
```csharp
[Test]
public void GetEntityName_LongDescriptor_PreservesFullDescriptor()
{
    var longDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3LTExMDE5ODM1NjMtMzU0Nzk5MjM2MS0zNzAyMTIxNjI4LTEtMTIzNDU2Nzg5MC0xMjM0NTY3ODkwLTEyMzQ1Njc4OTAtMTIzNDU2Nzg5MA";
    var mappings = new Dictionary<string, string>
    {
        [longDescriptor] = "Platform Team"
    }.ToFrozenDictionary();
    var mapper = new AzdoGroupMapper(mappings, null);

    var result = mapper.GetEntityName(longDescriptor);

    result.Should().Be($"Platform Team [{longDescriptor}]");
    result.Should().Contain(longDescriptor); // Full descriptor preserved
    result.Length.Should().BeGreaterThan(100); // Verify not truncated
}
```

---

### TC-11: AzdoProjectMapper_GetEntityName_KnownProject_ReturnsFormattedName

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureDevOps/AzdoProjectMapperTests.cs`

**Test Method:**
```csharp
[Test]
public void GetEntityName_KnownProjectId_ReturnsFormattedName()
{
    var mappings = new Dictionary<string, string>
    {
        ["8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f"] = "Infrastructure Project"
    }.ToFrozenDictionary();
    var mapper = new AzdoProjectMapper(mappings, null);

    var result = mapper.GetEntityName("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f");

    result.Should().Be("Infrastructure Project [8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f]");
}
```

---

### TC-12: AzdoMappers_GetEntityName_UnknownId_ReturnsRawId

**File:** Multiple mapper test files

**Test Methods:**
```csharp
// In AzdoUserMapperTests.cs
[Test]
public void GetEntityName_UnknownUserId_ReturnsRawId()
{
    var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, null);

    var result = mapper.GetEntityName("unknown-user-id");

    result.Should().Be("unknown-user-id");
}

// In AzdoGroupMapperTests.cs
[Test]
public void GetEntityName_UnknownGroupDescriptor_ReturnsRawDescriptor()
{
    var mapper = new AzdoGroupMapper(FrozenDictionary<string, string>.Empty, null);

    var result = mapper.GetEntityName("unknown-descriptor");

    result.Should().Be("unknown-descriptor");
}

// In AzdoProjectMapperTests.cs
[Test]
public void GetEntityName_UnknownProjectId_ReturnsRawId()
{
    var mapper = new AzdoProjectMapper(FrozenDictionary<string, string>.Empty, null);

    var result = mapper.GetEntityName("unknown-project-id");

    result.Should().Be("unknown-project-id");
}
```

**Notes:**
- Tests fallback behavior when mapping not found
- Should not throw exceptions

---

### TC-13: AzdoMappers_GetName_UnknownId_RecordsFailedResolution

**File:** Multiple mapper test files

**Test Method:**
```csharp
// In AzdoUserMapperTests.cs
[Test]
public void GetName_UnknownUserIdWithAddress_RecordsFailedResolution()
{
    var diagnostics = new DiagnosticContext();
    var mapper = new AzdoUserMapper(FrozenDictionary<string, string>.Empty, diagnostics);

    var result = mapper.GetName("unknown-user", "azuredevops_group_membership.example");

    result.Should().BeNull();
    diagnostics.FailedResolutions.Should().ContainSingle();
    diagnostics.FailedResolutions[0].Type.Should().Be(FailedResolutionType.AzdoUser);
    diagnostics.FailedResolutions[0].Id.Should().Be("unknown-user");
    diagnostics.FailedResolutions[0].ResourceAddress.Should().Be("azuredevops_group_membership.example");
}
```

**Notes:**
- Tests diagnostic tracking
- Requires `FailedResolutionType` enum to include `AzdoUser`, `AzdoGroup`, `AzdoProject` values

---

## Scriban Helper Tests

### TC-14: AzdoUserNameHelper_ResolveKnownUser_ReturnsFormattedName

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersAzdoTests.cs`

**Test Method:**
```csharp
[Test]
public async Task AzdoUserName_KnownUserId_ReturnsFormattedName()
{
    var mappings = new Dictionary<string, string>
    {
        ["4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b"] = "John Smith"
    }.ToFrozenDictionary();
    var mapper = new AzdoUserMapper(mappings, null);
    
    // Create Scriban template context with helper
    var template = Template.Parse("{{ azdo_user_name '4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b' }}");
    var context = new TemplateContext();
    
    // Register helper (similar to how it's done in AzureDevOpsModule)
    context.PushGlobal(new ScriptObject
    {
        { "azdo_user_name", new Func<string, string>(userId => mapper.GetEntityName(userId)) }
    });

    var result = await template.RenderAsync(context);

    result.Should().Be("John Smith [4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b]");
}
```

**Notes:**
- Tests Scriban helper integration
- Similar pattern to `ScribanHelpersPrincipalInfoTests`

---

### TC-15: AzdoGroupNameHelper_ResolveLongDescriptor_ReturnsFullDescriptor

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersAzdoTests.cs`

**Test Method:**
```csharp
[Test]
public async Task AzdoGroupName_LongDescriptor_ReturnsFormattedNameWithFullDescriptor()
{
    var longDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3LTExMDE5ODM1NjMtMzU0Nzk5MjM2MS0zNzAyMTIxNjI4LTEtMTIzNDU2Nzg5MC0xMjM0NTY3ODkwLTEyMzQ1Njc4OTAtMTIzNDU2Nzg5MA";
    var mappings = new Dictionary<string, string>
    {
        [longDescriptor] = "Platform Team"
    }.ToFrozenDictionary();
    var mapper = new AzdoGroupMapper(mappings, null);
    
    var template = Template.Parse($"{{{{ azdo_group_name '{longDescriptor}' }}}}");
    var context = new TemplateContext();
    context.PushGlobal(new ScriptObject
    {
        { "azdo_group_name", new Func<string, string>(groupId => mapper.GetEntityName(groupId)) }
    });

    var result = await template.RenderAsync(context);

    result.Should().Be($"Platform Team [{longDescriptor}]");
}
```

---

### TC-16: AzdoProjectNameHelper_ResolveKnownProject_ReturnsFormattedName

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersAzdoTests.cs`

**Test Method:**
```csharp
[Test]
public async Task AzdoProjectName_KnownProjectId_ReturnsFormattedName()
{
    var mappings = new Dictionary<string, string>
    {
        ["8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f"] = "Infrastructure Project"
    }.ToFrozenDictionary();
    var mapper = new AzdoProjectMapper(mappings, null);
    
    var template = Template.Parse("{{ azdo_project_name '8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f' }}");
    var context = new TemplateContext();
    context.PushGlobal(new ScriptObject
    {
        { "azdo_project_name", new Func<string, string>(projectId => mapper.GetEntityName(projectId)) }
    });

    var result = await template.RenderAsync(context);

    result.Should().Be("Infrastructure Project [8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f]");
}
```

---

## Diagnostic Tests

### TC-17: DiagnosticContext_LoadMappingWithAzdoSections_TracksEntityCounts

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/DiagnosticContextTests.cs`

**Test Method:**
```csharp
[Test]
public void DiagnosticContext_AzdoEntityCounts_TrackedCorrectly()
{
    var filePath = GetTempPath($"azdo-counts-{Guid.NewGuid():N}.json");
    File.WriteAllText(filePath, """
    {
      "azdoUsers": {
        "user-1": "User 1",
        "user-2": "User 2"
      },
      "azdoGroups": {
        "group-1": "Group 1",
        "group-2": "Group 2",
        "group-3": "Group 3"
      },
      "azdoProjects": {
        "project-1": "Project 1"
      }
    }
    """);
    var diagnostics = new DiagnosticContext();

    try
    {
        var result = AzureMappingFileLoader.Load(filePath, diagnostics);

        diagnostics.AzdoUserCount.Should().Be(2);
        diagnostics.AzdoGroupCount.Should().Be(3);
        diagnostics.AzdoProjectCount.Should().Be(1);
    }
    finally
    {
        File.Delete(filePath);
    }
}
```

---

### TC-18: DiagnosticOutput_IncludesAzdoEntityCounts_InCorrectFormat

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Diagnostics/DiagnosticOutputTests.cs`

**Test Method:**
```csharp
[Test]
public void GenerateDiagnosticOutput_WithAzdoEntities_IncludesCounts()
{
    var diagnostics = new DiagnosticContext
    {
        PrincipalMappingLoadedSuccessfully = true,
        AzdoUserCount = 2,
        AzdoGroupCount = 3,
        AzdoProjectCount = 1
    };

    var output = DiagnosticOutputGenerator.Generate(diagnostics);

    output.Should().Contain("Principal Mapping");
    output.Should().Contain("2 azdo users");
    output.Should().Contain("3 azdo groups");
    output.Should().Contain("1 azdo project");
    // Or combined: "Found 2 azdo users, 3 azdo groups, 1 azdo project"
}
```

**Notes:**
- Exact format TBD by Developer based on existing diagnostic patterns
- Should match existing Azure AD diagnostic output style

---

## Integration Tests

### TC-19: ExampleMappingFile_IncludesAzdoSections_ParsesSuccessfully

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Examples/ComprehensiveDemoTests.cs`

**Test Method:**
```csharp
[Test]
public void ComprehensiveDemoMappingFile_WithAzdoSections_LoadsSuccessfully()
{
    var filePath = Path.Combine(GetRepoRoot(), "examples/comprehensive-demo/demo-principals-nested.json");
    var diagnostics = new DiagnosticContext();

    var result = AzureMappingFileLoader.Load(filePath, diagnostics);

    // Should have Azure AD mappings
    result.Principals.Should().NotBeEmpty();
    
    // Should have azdo mappings
    result.AzdoUsers.Should().NotBeEmpty();
    result.AzdoGroups.Should().NotBeEmpty();
    result.AzdoProjects.Should().NotBeEmpty();
    
    diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
    diagnostics.AzdoUserCount.Should().BeGreaterThan(0);
    diagnostics.AzdoGroupCount.Should().BeGreaterThan(0);
    diagnostics.AzdoProjectCount.Should().BeGreaterThan(0);
}
```

**Notes:**
- Verifies the example file is updated with azdo sections
- Production example file must be valid and comprehensive

---

### TC-20: AzureDevOpsGroupMembership_WithMapping_DisplaysMappedNames

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureDevOpsSnapshotTests.cs`

**Test Method:**
```csharp
[Test]
public void Snapshot_AzureDevOpsGroupMembers_WithMapping_MatchesBaseline()
{
    var planPath = Path.Combine(GetTestDataRoot(), "azuredevops-group-members-plan.json");
    var mappingPath = GetTempPath($"azdo-mapping-{Guid.NewGuid():N}.json");
    
    // Create mapping file with mappings for test data
    File.WriteAllText(mappingPath, """
    {
      "azdoUsers": {
        "aadgp.Uy0.AliceUser": "Alice Smith",
        "aadgp.Uy0.BobUser": "Bob Johnson"
      },
      "azdoGroups": {
        "aadgp.Uy0.ReleaseManagers": "Release Managers Team"
      }
    }
    """);

    try
    {
        var rendered = RenderMarkdown(planPath, mappingPath);
        var snapshotPath = Path.Combine(GetTestDataRoot(), "Snapshots/azuredevops-group-members-with-mapping.md");

        // Verify mapped names appear in output
        rendered.Should().Contain("Alice Smith [aadgp.Uy0.AliceUser]");
        rendered.Should().Contain("Bob Johnson [aadgp.Uy0.BobUser]");
        rendered.Should().Contain("Release Managers Team [aadgp.Uy0.ReleaseManagers]");

        // Compare against snapshot
        var snapshot = File.ReadAllText(snapshotPath);
        rendered.Should().Be(snapshot);
    }
    finally
    {
        File.Delete(mappingPath);
    }
}
```

**Notes:**
- Creates a new snapshot test with mapping applied
- Requires creating the snapshot baseline file
- Should show clear before/after comparison

---

### TC-21: AzureDevOpsProject_WithMapping_DisplaysMappedProjectName

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureDevOpsSnapshotTests.cs`

**Test Method:**
```csharp
[Test]
public void Snapshot_AzureDevOpsProject_WithMapping_MatchesBaseline()
{
    var planPath = GetTempPath($"azdo-projects-plan-{Guid.NewGuid():N}.json");
    var mappingPath = GetTempPath($"azdo-project-mapping-{Guid.NewGuid():N}.json");
    
    // Create test plan with project resources
    File.WriteAllText(planPath, """
    {
      "format_version": "1.2",
      "terraform_version": "1.14.0",
      "resource_changes": [
        {
          "address": "azuredevops_project.infrastructure",
          "type": "azuredevops_project",
          "change": {
            "actions": ["create"],
            "after": {
              "project_id": "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f",
              "name": "Infrastructure"
            }
          }
        }
      ]
    }
    """);
    
    File.WriteAllText(mappingPath, """
    {
      "azdoProjects": {
        "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project"
      }
    }
    """);

    try
    {
        var rendered = RenderMarkdown(planPath, mappingPath);

        // Verify mapped project name appears
        rendered.Should().Contain("Infrastructure Project [8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f]");
    }
    finally
    {
        File.Delete(planPath);
        File.Delete(mappingPath);
    }
}
```

**Notes:**
- Tests project mapping end-to-end
- May need to create templates for `azuredevops_project` if they don't exist

---

## Test Execution Notes

### Running Individual Test Classes

```bash
# Run all mapper tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/AzdoUserMapperTests/*

# Run parser tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/AzureMappingFileLoaderTests/*

# Run integration tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/AzureDevOpsSnapshotTests/*
```

### Updating Snapshots

After implementing the feature and verifying the output manually:

```bash
# Update all snapshots
scripts/update-snapshots.sh

# Or manually for specific tests
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/AzureDevOpsSnapshotTests/* -- --update-snapshots
```

### Test Dependencies

Some tests depend on:
1. **FailedResolutionType enum**: Must include `AzdoUser`, `AzdoGroup`, `AzdoProject` values
2. **DiagnosticContext properties**: Must include `AzdoUserCount`, `AzdoGroupCount`, `AzdoProjectCount`
3. **AzureMappingFileResult**: Must include `AzdoUsers`, `AzdoGroups`, `AzdoProjects` properties

Ensure these are implemented before writing dependent tests.

## Summary

- **Total Test Cases**: 21
- **Unit Tests**: 18 (TC-01 through TC-18)
- **Integration Tests**: 3 (TC-19 through TC-21)
- **Estimated Test Count**: ~25-30 test methods (some test cases expand to multiple methods for different entity types)
- **Test Data Files Required**: 2-3 new files + updates to existing example file
- **New Test Files Required**: 3-4 new test class files

All tests follow TUnit patterns and use AwesomeAssertions for fluent assertions.
