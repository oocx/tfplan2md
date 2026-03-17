# Fix: musl build failures (UPX install permission denied)

Build-only fix: the release pipeline's musl binary build jobs were failing when trying to install UPX. No user-facing output changes.

## 🐛 Bug fixes

- **musl builds failed with `permission denied` installing UPX**: The "Install UPX (Linux)" step called `apt-get` without `sudo`. Non-musl Linux builds (`linux-x64`, `linux-arm64`) run inside a `.NET SDK Noble` Docker container as root, so the bare `apt-get` call worked. musl builds (`linux-musl-x64`, `linux-musl-arm64`) run directly on the GitHub-hosted runner as a non-root user, so the call failed with permission denied. The fix adds a `uid` check: when running as root (uid 0), `apt-get` is called directly; otherwise `sudo apt-get` is used. Simply always using `sudo` is not viable because the `.NET SDK Noble` container used by non-musl builds has no `sudo` binary.

## 🔗 Commits

- [`22e1b9b`](https://github.com/oocx/tfplan2md/commit/22e1b9b) fix: add sudo for UPX install in musl builds
