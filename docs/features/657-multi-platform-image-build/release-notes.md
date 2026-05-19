# Multi-Platform Docker Image Build (linux/amd64 + linux/arm64)

The official Docker image is now published as a multi-platform manifest supporting both `linux/amd64` and `linux/arm64`. Pulls on ARM64 hosts (e.g., Apple Silicon, AWS Graviton, Raspberry Pi) will automatically receive a native binary without any extra flags.

## ✨ Features

- **ARM64 Docker image support.** The `ghcr.io/oocx/tfplan2md` image now includes a native `linux/arm64` layer alongside the existing `linux/amd64` layer. Docker automatically selects the right layer based on the host architecture.

## 🔗 Commits

- [`cc4de0cf`](https://github.com/oocx/tfplan2md/commit/cc4de0cf) feat: enable multi-platform Docker image build (linux/amd64, linux/arm64)
