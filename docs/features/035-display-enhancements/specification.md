# Feature: Display Enhancements

## Overview

This feature introduces four focused display improvements to enhance readability and provide better context in generated reports:
1. Syntax highlighting for large JSON/XML values
2. Enhanced API Management subresource summaries
3. Named values display fix
4. Subscription attributes emoji

## User Goals

- Users want large JSON/XML values to be easily readable with proper formatting and syntax highlighting
- Users need better context when reviewing API Management subresources (policy fragments, named values, operations)
- Users expect to see actual values for non-secret named values (not always "(sensitive)")
- Users want consistent visual indicators for subscription-related attributes

## Scope

### In Scope

**1. Syntax Highlighting for Large Values**
- Automatically detect JSON or XML content in large values (using existing large value threshold)
- Pretty-print detected JSON/XML content
- Add syntax highlighting via code fence language markers (```json or ```xml)
- Preserve existing formatting when reliably detectable, otherwise always reformat

**2. API Management Subresource Summaries**
- Display `api_management_name` attribute in resource summaries when present
- For `azurerm_api_management_api_operation`: display `display_name`, `operation_id`, and `api_name` in summary
- Enhanced format: `azurerm_api_management_api_operation \`this\` \`display_name\` — \`operation_id\` \`api_management_name\` in \`📁 resource-group\``
- For `azurerm_api_management_named_value`: display `api_management_name` in summary
- Enhanced format: `azurerm_api_management_named_value \`this\` — \`🆔 name\` \`api_management_name\` in \`📁 resource-group\``
- Apply to all API Management subresources with `api_management_name` attribute

**3. Named Values Value Display**
- Fix provider bug: display actual value when `secret` attribute is `false`
- Use standard inline/large value handling for the value
- Override provider's sensitive marking when `secret=false`
- Continue to show "(sensitive)" when `secret=true`

**4. Subscription Attributes Emoji**
- Add 🔑 emoji to `subscription_id` and `subscription` attributes
- Apply universally across all providers and resource types
- Display emoji wherever these attributes are rendered (summaries, attribute details, etc.)

### Out of Scope

- Custom syntax highlighting themes or color schemes
- Support for other markup languages beyond JSON/XML
- Detection heuristics for minified/compressed values
- Changes to non-API Management resource summaries
- Emojis for other attribute types
- Changes to how sensitive values are handled (except named values with secret=false)

## User Experience

### Syntax Highlighting
**Before:**
```
policy_content: <very long unformatted JSON string>
```

**After:**
````
policy_content:
```json
{
  "properties": {
    "displayName": "Example Policy",
    "mode": "All"
  }
}
```
````

### API Management Summaries
**Before:**
```
azurerm_api_management_api_operation `this` — `📁 hubendpoints-rg-i001con-gwc`
azurerm_api_management_named_value `this` — `🆔 IDP-WEB-CLIENT-ID` in `📁 hubendpoints-rg-i001con-gwc`
```

**After:**
```
azurerm_api_management_api_operation `this` `Get User Profile` — `get-user-profile` `example-apim` in `📁 hubendpoints-rg-i001con-gwc`
azurerm_api_management_named_value `this` — `🆔 IDP-WEB-CLIENT-ID` `example-apim` in `📁 hubendpoints-rg-i001con-gwc`
```

### Named Values
**Before:**
```
value: (sensitive)  [when secret=false]
```

**After:**
```
value: https://example.com/api  [when secret=false]
value: (sensitive)               [when secret=true]
```

### Subscription Attributes
**Before:**
```
subscription_id: 12345678-1234-1234-1234-123456789abc
```

**After:**
```
🔑 subscription_id: 12345678-1234-1234-1234-123456789abc
```

## Success Criteria

- [ ] Large JSON values are automatically detected, pretty-printed, and highlighted with ```json
- [ ] Large XML values are automatically detected, pretty-printed, and highlighted with ```xml
- [ ] Already-formatted values are preserved when reliably detectable
- [ ] API Management operations display `display_name`, `operation_id`, `api_name`, and `api_management_name` in summary
- [ ] API Management named values display `api_management_name` in summary
- [ ] Other API Management subresources display `api_management_name` when the attribute exists
- [ ] Named values with `secret=false` display actual values using standard inline/large value handling
- [ ] Named values with `secret=true` continue to show "(sensitive)"
- [ ] 🔑 emoji appears for `subscription_id` attributes across all providers
- [ ] 🔑 emoji appears for `subscription` attributes across all providers
- [ ] All existing tests pass
- [ ] New test coverage for all four enhancements

## Open Questions

None at this time.
