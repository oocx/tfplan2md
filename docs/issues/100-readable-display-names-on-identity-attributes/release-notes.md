## Fix: Resource `id` attribute no longer rendered as full display name

When displaying an `azurerm` resource's own `id` attribute in the attribute table, the value is now rendered as the resource type label followed by the raw ARM identifier in backticks (e.g., `MetricAlerts \`/subscriptions/.../metricAlerts/my-alert\``), instead of the expanded human-readable display name (`MetricAlerts 🆔 \`name\` in resource group 📁 \`rg\` of subscription 🔑 \`sub\``).

Readable display names are still applied when referencing other resources (e.g., a `key_vault_id` attribute that points to another resource).
