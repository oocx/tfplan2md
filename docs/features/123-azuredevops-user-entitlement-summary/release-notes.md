# Azure DevOps User Entitlement Summary Fields

## ✨ Features

- **Readable user entitlement summaries**: `azuredevops_user_entitlement` resources now display `principal_name`, `account_license_type`, and `licensing_source` in the summary line, so reviewers can immediately see who is being granted access and what license they receive without expanding the full change details.
- **No visual noise for absent fields**: Fields that are empty or not set in the plan are automatically omitted from the summary line — only populated values are shown.

### Example

When all three fields are set:

```
➕ azuredevops_user_entitlement john.doe@example.com — john.doe@example.com | express | msdn
```

When `licensing_source` is empty:

```
➕ azuredevops_user_entitlement john.doe@example.com — john.doe@example.com | express
```

When none of the three fields are set, the summary falls back to the resource address (existing behaviour — no regression).

## 🔗 Commits

- [`ed08647`](https://github.com/oocx/tfplan2md/commit/ed08647) feat: display principal_name, account_license_type, and licensing_source in azuredevops_user_entitlement summary
