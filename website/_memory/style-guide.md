# Website Style Guide

This document captures **design and style decisions** for the tfplan2md website.

## Principles

- Keep the site technical and example-driven (no marketing fluff).
- Prefer simple, maintainable HTML/CSS.
- Use consistent spacing, typography, and component patterns across pages.

## Theme and tokens

- Theme support: Light and Dark modes.
- CSS variables are defined in `website/style.css` under `:root[data-theme="light"]` and `:root[data-theme="dark"]`.
- Keep new styling consistent with existing variables instead of introducing one-off values.

## Layout

- Primary content max width: 1280px (see `.nav-container`, `.hero-container` patterns).
- Use the existing container + section patterns (`.section`, `.section-container`, `.section-header`) for consistency.

## Typography

- Use the existing heading hierarchy in HTML (`h1` → `h2` → `h3`) and keep it consistent.
- Prefer the site’s configured system font stack (`--font-sans`) and mono stack (`--font-mono`).

## Navigation

- Keep the nav consistent across pages.
- Ensure keyboard accessibility (focus order, aria labels on controls).

## Icons and images

- Feature representation must follow `website/_memory/feature-definitions.md`.
- Do not reuse the same icon/image for different features.

## Decision log

- 2026-01-03: Initial style guide created based on the current `website/style.css` design system.
