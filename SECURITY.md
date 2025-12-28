# Security Policy

## Supported Versions

We release patches for security vulnerabilities in the following versions:

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < Latest | :x:               |

**Note:** As a small, actively maintained project, we only support the latest released version. Security fixes are applied to the `main` branch and released immediately.

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them privately using one of these methods:

### Preferred: GitHub Security Advisories

1. Go to https://github.com/oocx/tfplan2md/security/advisories/new
2. Click "Report a vulnerability"
3. Fill in the details of the vulnerability
4. Submit the advisory

This keeps the vulnerability private until we've addressed it.

### Alternative: Email

Send an email to: **tfplan2md@outofcoffeeexception.de**

Please include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Any suggested fixes (if you have them)

## What to Expect

As a hobby project maintained in my free time, I cannot guarantee specific response times. However, I take security seriously and will:

- Respond as soon as I'm able to review the report
- Prioritize security fixes when possible
- Work with you on coordinated disclosure timing
- Credit you in the security advisory (if desired)

Please be patient - response times may vary depending on other commitments.

## Security Considerations for tfplan2md

### In Scope

Security issues related to:
- **Input validation**: Malformed JSON causing crashes or unexpected behavior
- **Output injection**: Markdown/HTML injection in generated reports
- **Sensitive data exposure**: Unintended exposure of sensitive values
- **Dependencies**: Known vulnerabilities in third-party packages
- **Docker image**: Vulnerabilities in the base image or build process

### Out of Scope

The following are generally **not** considered security vulnerabilities:
- Issues in Terraform itself (report to HashiCorp)
- Infrastructure security configurations shown in plan files (report to your team)
- DoS through extremely large plan files (resource exhaustion is expected)
- Issues requiring physical access to the system running tfplan2md

### Best Practices for Users

When using tfplan2md:
- ✅ Use `--show-sensitive` only in secure environments
- ✅ Review generated reports before sharing publicly
- ✅ Keep tfplan2md updated to the latest version
- ✅ Use the official Docker image from Docker Hub
- ✅ Validate plan files come from trusted sources

## Security Update Process

When a security vulnerability is confirmed:

1. **Patch Development**: We develop and test a fix
2. **Advisory Creation**: We create a GitHub Security Advisory
3. **Release**: We release a new version with the fix
4. **Notification**: The advisory is published with details
5. **Credit**: We credit the reporter (unless they prefer to remain anonymous)

## Disclosure Policy

We follow **coordinated disclosure**:
- We'll work with you to understand and fix the issue
- We'll agree on a disclosure timeline (typically 90 days)
- We'll credit you in the security advisory (if desired)
- We ask that you don't publicly disclose until we've released a fix

## Questions?

If you have questions about this security policy, feel free to:
- Open a [GitHub Discussion](https://github.com/oocx/tfplan2md/discussions)
- Email: tfplan2md@outofcoffeeexception.de

Thank you for helping keep tfplan2md and its users safe!
