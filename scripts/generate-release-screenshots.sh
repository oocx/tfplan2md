#!/bin/bash
set -euo pipefail

# generate-release-screenshots.sh - Generate screenshots for release notes
#
# This script is a simplified wrapper around the screenshot generation tooling,
# optimized for release notes with sensible defaults:
# - Single screenshot at 580×400 (max size for release notes)
# - Light mode only
# - 1x DPI only
# - Single render target (GitHub or Azure DevOps)
#
# For full control with all variants (dark mode, 2x DPI, thumbnails, lightbox),
# use scripts/generate-screenshot.sh instead.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Default values for release screenshots
WIDTH=580
HEIGHT=400
RENDER_TARGET="github"
MARKDOWN_FILE=""
PLAN_FILE=""
OUTPUT_PREFIX=""
OUTPUT_DIR=""
SELECTOR=""
TARGET_RESOURCE_ID=""
OPEN_DETAILS_SELECTOR="details"

# Display usage information
show_help() {
    cat <<EOF
Usage: $0 [options]

Generate screenshots for release notes (max 580×400, light mode, 1x DPI).

Required (one of):
  --plan FILE              Path to Terraform plan JSON file
  --markdown-file FILE     Use existing markdown file instead of generating

Required:
  --output-prefix NAME     Prefix for screenshot file (e.g., 'feature-065-icons')
  --output-dir DIR         Directory for screenshots (typically docs/features/NNN/ or docs/issues/NNN/)

Required (one of):
  --selector SELECTOR      CSS/Playwright selector to capture
  --target-resource-id ID  Terraform resource ID to capture

Optional:
  --width N                Screenshot width in pixels (default: 580)
  --height N               Screenshot height in pixels (default: 400)
  --render-target TARGET   Rendering target: azdo or github (default: github)
  --open-details-selector SELECTOR  Playwright selector for <details> elements to open (default: "details")
  --help                   Show this help message

Example:
  $0 \\
    --plan examples/comprehensive-demo/comprehensive-demo.json \\
    --output-prefix feature-065-icons \\
    --output-dir docs/features/065-tenant-display-mapping \\
    --selector "details:has(summary:has-text('azurerm_role_assignment'))" \\
    --render-target github

Output:
  Creates: {output-dir}/{output-prefix}.png (light mode, 1x DPI)

For full control (dark mode, 2x DPI, thumbnails, lightbox):
  Use scripts/generate-screenshot.sh instead
EOF
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --plan)
            PLAN_FILE="$2"
            shift 2
            ;;
        --markdown-file)
            MARKDOWN_FILE="$2"
            shift 2
            ;;
        --output-prefix)
            OUTPUT_PREFIX="$2"
            shift 2
            ;;
        --output-dir)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --selector)
            SELECTOR="$2"
            shift 2
            ;;
        --target-resource-id)
            TARGET_RESOURCE_ID="$2"
            shift 2
            ;;
        --width)
            WIDTH="$2"
            shift 2
            ;;
        --height)
            HEIGHT="$2"
            shift 2
            ;;
        --render-target)
            RENDER_TARGET="$2"
            shift 2
            ;;
        --open-details-selector)
            OPEN_DETAILS_SELECTOR="$2"
            shift 2
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            echo "Error: Unknown option: $1" >&2
            echo "Use --help for usage information" >&2
            exit 1
            ;;
    esac
done

# Validate required arguments
if [[ -z "$PLAN_FILE" ]] && [[ -z "$MARKDOWN_FILE" ]]; then
    echo "Error: Either --plan or --markdown-file is required" >&2
    echo "Use --help for usage information" >&2
    exit 1
fi

if [[ -z "$OUTPUT_PREFIX" ]]; then
    echo "Error: --output-prefix is required" >&2
    echo "Use --help for usage information" >&2
    exit 1
fi

if [[ -z "$OUTPUT_DIR" ]]; then
    echo "Error: --output-dir is required" >&2
    echo "Use --help for usage information" >&2
    exit 1
fi

if [[ -z "$SELECTOR" ]] && [[ -z "$TARGET_RESOURCE_ID" ]]; then
    echo "Error: Either --selector or --target-resource-id is required" >&2
    echo "Use --help for usage information" >&2
    exit 1
fi

if [[ -n "$SELECTOR" ]] && [[ -n "$TARGET_RESOURCE_ID" ]]; then
    echo "Error: --selector and --target-resource-id are mutually exclusive" >&2
    exit 1
fi

if [[ "$RENDER_TARGET" != "azdo" ]] && [[ "$RENDER_TARGET" != "github" ]]; then
    echo "Error: --render-target must be 'azdo' or 'github'" >&2
    exit 1
fi

# Create output directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

# Convert plan to markdown if needed
if [[ -z "$MARKDOWN_FILE" ]]; then
    echo "Converting plan to markdown..."
    PLAN_BASENAME="$(basename "$PLAN_FILE" .json)"
    MARKDOWN_FILE="$REPO_ROOT/artifacts/${PLAN_BASENAME}.md"
    
    # Build additional arguments for code analysis if SARIF file exists
    ANALYSIS_DIR="$(dirname "$REPO_ROOT/$PLAN_FILE")"
    EXTRA_ARGS=""
    if [[ -f "$ANALYSIS_DIR/analysis.sarif" ]]; then
        EXTRA_ARGS="--code-analysis-results $ANALYSIS_DIR/analysis.sarif"
    fi
    
    dotnet run --project "$REPO_ROOT/src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj" -- \
        $EXTRA_ARGS \
        --output "$MARKDOWN_FILE" \
        "$REPO_ROOT/$PLAN_FILE"
fi

echo ""
echo "Generating HTML for $RENDER_TARGET rendering..."

MARKDOWN_BASENAME="$(basename "$MARKDOWN_FILE" .md)"
HTML_FILE="$REPO_ROOT/artifacts/${MARKDOWN_BASENAME}.${RENDER_TARGET}.html"

# Generate HTML with appropriate template
if [[ "$RENDER_TARGET" == "azdo" ]]; then
    TEMPLATE="$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html"
else
    TEMPLATE="$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html"
fi

dotnet run --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer" -- \
    --input "$MARKDOWN_FILE" \
    --flavor "$RENDER_TARGET" \
    --template "$TEMPLATE" \
    --output "$HTML_FILE"

# Build target arguments for ScreenshotGenerator
TARGET_ARGS=()
if [[ -n "$SELECTOR" ]]; then
    TARGET_ARGS+=(--target-selector "$SELECTOR")
elif [[ -n "$TARGET_RESOURCE_ID" ]]; then
    TARGET_ARGS+=(--target-terraform-resource-id "$TARGET_RESOURCE_ID")
fi

# Open details elements based on selector parameter
OPEN_DETAILS_ARGS=(--open-details "$OPEN_DETAILS_SELECTOR")

# Generate screenshot
OUTPUT_FILE="$OUTPUT_DIR/${OUTPUT_PREFIX}.png"

echo ""
echo "Generating release screenshot..."
echo "  Target: $RENDER_TARGET"
echo "  Size: ${WIDTH}×${HEIGHT}"
echo "  Output: $OUTPUT_FILE"

dotnet run --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.ScreenshotGenerator" -- \
    --input "$HTML_FILE" \
    --output "$OUTPUT_FILE" \
    --width "$WIDTH" \
    --height "$HEIGHT" \
    "${TARGET_ARGS[@]}" \
    "${OPEN_DETAILS_ARGS[@]}"

echo ""
echo "✅ Release screenshot generated successfully!"
echo ""
echo "Output: $OUTPUT_FILE"
echo "Size: ${WIDTH}×${HEIGHT}"
echo ""
echo "Next steps:"
echo "  1. Review the screenshot to ensure it captures the intended content"
echo "  2. Reference it in release notes using: ![Description](./${OUTPUT_PREFIX}.png)"
echo "  3. Keep release note screenshots under 580×400 for optimal readability"
