# Decimal values no longer misclassified as IP addresses

This patch fixes semantic formatting so decimal numbers (for example `0.5`) are no longer rendered with the network icon (`🌐`).

## 🐛 Bug fixes

### Fixed decimal value rendering for SQL serverless capacity

- **Before:** Values like `min_capacity = 0.5` could render as `🌐 0.5`
- **After:** Decimal values render as plain numbers (`0.5`) with no IP icon

### Preserved expected behavior for real network values

The fix keeps icon rendering intact for:

- Full IPv4 addresses (for example `192.168.1.1`)
- IPv4 CIDR values (for example `10.0.0.0/16`)
- IPv6 addresses

## 🔗 Commits

- [`15256e4`](https://github.com/oocx/tfplan2md/commit/15256e4de4201cbf14374df1d23cfb6d55489512) fix: prevent decimal numbers from being rendered with IP icon
