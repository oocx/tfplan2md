#!/bin/bash
set -euo pipefail

# generate-screenshot.sh - Generate website screenshots with all variants
#
# Generates screenshots from Terraform plan files with automatic handling of:
# - Light and dark themes
# - 1x and 2x DPI versions
# - Thumbnail and lightbox crops
# - Azure DevOps and/or GitHub rendering styles

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Default values
THUMBNAIL_WIDTH=580
THUMBNAIL_HEIGHT=400
THUMBNAIL_OFFSET_X=0
THUMBNAIL_OFFSET_Y=0
LIGHTBOX_WIDTH=1200
LIGHTBOX_HEIGHT=800
LIGHTBOX_OFFSET_X=""
LIGHTBOX_OFFSET_Y=""
WIDTH=1200
FULL_PAGE_WIDTH=1920
RENDER_TARGET="all"
MARKDOWN_FILE=""
PLAN_FILE=""
OUTPUT_PREFIX=""
SELECTOR=""

# run_screenshotter: wraps dotnet run with xvfb-run when available.
# Playwright's new headless Chromium requires a compositor context to render screenshots.
# xvfb-run provides a virtual framebuffer that satisfies this requirement in
# server/CI environments where the primary display (:0) may not be accessible.
run_screenshotter() {
    if command -v xvfb-run &>/dev/null; then
        xvfb-run --auto-servernum --server-args="-screen 0 1920x1080x24" dotnet run "$@"
    else
        dotnet run "$@"
    fi
}

TARGET_RESOURCE_ID=""
OPEN_DETAILS_SELECTOR="details"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --plan)
            PLAN_FILE="$2"
            shift 2
            ;;
        --output-prefix)
            OUTPUT_PREFIX="$2"
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
        --thumbnail-width)
            THUMBNAIL_WIDTH="$2"
            shift 2
            ;;
        --thumbnail-height)
            THUMBNAIL_HEIGHT="$2"
            shift 2
            ;;
        --thumbnail-offset-x)
            THUMBNAIL_OFFSET_X="$2"
            shift 2
            ;;
        --thumbnail-offset-y)
            THUMBNAIL_OFFSET_Y="$2"
            shift 2
            ;;
        --lightbox-width)
            LIGHTBOX_WIDTH="$2"
            shift 2
            ;;
        --lightbox-height)
            LIGHTBOX_HEIGHT="$2"
            shift 2
            ;;
        --lightbox-offset-x)
            LIGHTBOX_OFFSET_X="$2"
            shift 2
            ;;
        --lightbox-offset-y)
            LIGHTBOX_OFFSET_Y="$2"
            shift 2
            ;;
        --width)
            WIDTH="$2"
            shift 2
            ;;
        --full-page-width)
            FULL_PAGE_WIDTH="$2"
            shift 2
            ;;
        --render-target)
            RENDER_TARGET="$2"
            shift 2
            ;;
        --markdown-file)
            MARKDOWN_FILE="$2"
            shift 2
            ;;
        --open-details-selector)
            OPEN_DETAILS_SELECTOR="$2"
            shift 2
            ;;
        --help)
            cat <<EOF
Usage: $0 [options]

Required:
  --plan FILE              Path to Terraform plan JSON file
  --output-prefix NAME     Prefix for generated screenshot files (e.g., 'firewall-example')
  --selector SELECTOR      CSS selector to capture (mutually exclusive with --target-resource-id)
  --target-resource-id ID  Terraform resource ID to capture (mutually exclusive with --selector)

Optional:
  --thumbnail-width N      Thumbnail width in pixels (default: 580)
  --thumbnail-height N     Thumbnail height in pixels (default: 400)
  --thumbnail-offset-x N   Thumbnail crop offset X (default: 0)
  --thumbnail-offset-y N   Thumbnail crop offset Y (default: 0)
  --lightbox-width N       Lightbox width in pixels (default: 1200)
  --lightbox-height N      Lightbox height in pixels (default: 800)
  --lightbox-offset-x N    Lightbox crop offset X (default: same as thumbnail-offset-x)
  --lightbox-offset-y N    Lightbox crop offset Y (default: same as thumbnail-offset-y)
  --width N                Screenshot width for targeted captures (default: 1200)
  --full-page-width N      Screenshot width for full-page captures (default: 1920)
  --render-target TARGET   Rendering target: azdo, github, or all (default: all)
  --markdown-file FILE     Use existing markdown file instead of generating from plan
  --open-details-selector SELECTOR  Playwright selector for <details> elements to open (default: "details")
  --help                   Show this help message

Example:
  $0 \\
    --plan examples/comprehensive-demo/comprehensive-demo.json \\
    --output-prefix firewall-example \\
    --selector "article:has(h2:has-text('azurerm_firewall_policy_rule_collection_group.example'))" \\
    --thumbnail-width 580 --thumbnail-height 400 \\
    --thumbnail-offset-x 400 --thumbnail-offset-y 1300 \\
    --lightbox-width 1200 --lightbox-height 800 \\
    --lightbox-offset-x 370 --lightbox-offset-y 1200 \\
    --full-page-width 1920

Generates 16 files per render target:
  - {output-prefix}-crop.png (thumbnail, light, 1x)
  - {output-prefix}-crop@2x.png (thumbnail, light, 2x)
  - {output-prefix}-crop-dark.png (thumbnail, dark, 1x)
  - {output-prefix}-crop-dark@2x.png (thumbnail, dark, 2x)
  - {output-prefix}-lightbox.png (lightbox, light, 1x)
  - {output-prefix}-lightbox@2x.png (lightbox, light, 2x)
  - {output-prefix}-lightbox-dark.png (lightbox, dark, 1x)
  - {output-prefix}-lightbox-dark@2x.png (lightbox, dark, 2x)
EOF
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            echo "Use --help for usage information" >&2
            exit 1
            ;;
    esac
done

# Validate required arguments
if [[ -z "$PLAN_FILE" ]] && [[ -z "$MARKDOWN_FILE" ]]; then
    echo "Error: --plan or --markdown-file is required" >&2
    exit 1
fi

if [[ -z "$OUTPUT_PREFIX" ]]; then
    echo "Error: --output-prefix is required" >&2
    exit 1
fi

if [[ -z "$SELECTOR" ]] && [[ -z "$TARGET_RESOURCE_ID" ]]; then
    echo "Error: Either --selector or --target-resource-id is required" >&2
    exit 1
fi

if [[ -n "$SELECTOR" ]] && [[ -n "$TARGET_RESOURCE_ID" ]]; then
    echo "Error: --selector and --target-resource-id are mutually exclusive" >&2
    exit 1
fi

if [[ "$RENDER_TARGET" != "azdo" ]] && [[ "$RENDER_TARGET" != "github" ]] && [[ "$RENDER_TARGET" != "all" ]]; then
    echo "Error: --render-target must be 'azdo', 'github', or 'all'" >&2
    exit 1
fi

# Default lightbox offsets to thumbnail offsets if not specified
if [[ -z "$LIGHTBOX_OFFSET_X" ]]; then
    LIGHTBOX_OFFSET_X="$THUMBNAIL_OFFSET_X"
fi
if [[ -z "$LIGHTBOX_OFFSET_Y" ]]; then
    LIGHTBOX_OFFSET_Y="$THUMBNAIL_OFFSET_Y"
fi

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

# Determine which targets to process
TARGETS=()
if [[ "$RENDER_TARGET" == "all" ]]; then
    TARGETS=("azdo" "github")
elif [[ "$RENDER_TARGET" == "azdo" ]]; then
    TARGETS=("azdo")
else
    TARGETS=("github")
fi

# Process each render target
for TARGET in "${TARGETS[@]}"; do
    echo ""
    echo "Processing $TARGET rendering..."
    
    MARKDOWN_BASENAME="$(basename "$MARKDOWN_FILE" .md)"
    HTML_LIGHT="$REPO_ROOT/artifacts/${MARKDOWN_BASENAME}.${TARGET}.html"
    HTML_DARK="$REPO_ROOT/artifacts/${MARKDOWN_BASENAME}.${TARGET}-dark.html"
    
    # Generate HTML with appropriate template
    echo "  Generating light mode HTML..."
    if [[ "$TARGET" == "azdo" ]]; then
        TEMPLATE="$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html"
    else
        TEMPLATE="$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html"
    fi
    
    dotnet run --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer" -- \
        --input "$MARKDOWN_FILE" \
        --flavor "$TARGET" \
        --template "$TEMPLATE" \
        --output "$HTML_LIGHT"
    
    # Create dark mode version
    echo "  Creating dark mode HTML..."
    if [[ "$TARGET" == "azdo" ]]; then
        sed 's/data-theme="light"/data-theme="dark"/' "$HTML_LIGHT" > "$HTML_DARK"
    else
        dotnet run --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer" -- \
            --input "$MARKDOWN_FILE" \
            --flavor "$TARGET" \
            --template "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html" \
            --output "$HTML_DARK"
    fi
    
    # Build target arguments for ScreenshotGenerator.
    # Use arrays to avoid word-splitting (selectors often contain spaces).
    TARGET_ARGS=()
    if [[ -n "$SELECTOR" ]]; then
        TARGET_ARGS+=(--target-selector "$SELECTOR")
    elif [[ -n "$TARGET_RESOURCE_ID" ]]; then
        TARGET_ARGS+=(--target-terraform-resource-id "$TARGET_RESOURCE_ID")
    fi

    # Open details elements based on selector parameter.
    OPEN_DETAILS_ARGS=(--open-details "$OPEN_DETAILS_SELECTOR")
    
    # Generate targeted screenshots
    FULL_LIGHT="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-full-${TARGET}.png"
    FULL_LIGHT_2X="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-full-${TARGET}@2x.png"
    FULL_DARK="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-full-${TARGET}-dark.png"
    FULL_DARK_2X="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-full-${TARGET}-dark@2x.png"
    
    echo "  Generating targeted screenshot (light, 1x)..."
    run_screenshotter --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.ScreenshotGenerator" -- \
        --input "$HTML_LIGHT" \
        --output "$FULL_LIGHT" \
        --width "$WIDTH" "${TARGET_ARGS[@]}" "${OPEN_DETAILS_ARGS[@]}"
    
    echo "  Generating targeted screenshot (light, 2x)..."
    run_screenshotter --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.ScreenshotGenerator" -- \
        --input "$HTML_LIGHT" \
        --output "$FULL_LIGHT_2X" \
        --width "$WIDTH" --device-scale-factor 2 "${TARGET_ARGS[@]}" "${OPEN_DETAILS_ARGS[@]}"
    
    echo "  Generating targeted screenshot (dark, 1x)..."
    run_screenshotter --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.ScreenshotGenerator" -- \
        --input "$HTML_DARK" \
        --output "$FULL_DARK" \
        --width "$WIDTH" "${TARGET_ARGS[@]}" "${OPEN_DETAILS_ARGS[@]}"
    
    echo "  Generating targeted screenshot (dark, 2x)..."
    run_screenshotter --project "$REPO_ROOT/src/tools/Oocx.TfPlan2Md.ScreenshotGenerator" -- \
        --input "$HTML_DARK" \
        --output "$FULL_DARK_2X" \
        --width "$WIDTH" --device-scale-factor 2 "${TARGET_ARGS[@]}" "${OPEN_DETAILS_ARGS[@]}"
    
    # Crop thumbnails
    CROP_LIGHT="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-crop-${TARGET}.png"
    CROP_LIGHT_2X="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-crop-${TARGET}@2x.png"
    CROP_DARK="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-crop-${TARGET}-dark.png"
    CROP_DARK_2X="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-crop-${TARGET}-dark@2x.png"
    
    # Calculate 2x offsets and dimensions
    THUMBNAIL_2X_WIDTH=$((THUMBNAIL_WIDTH * 2))
    THUMBNAIL_2X_HEIGHT=$((THUMBNAIL_HEIGHT * 2))
    THUMBNAIL_2X_OFFSET_X=$((THUMBNAIL_OFFSET_X * 2))
    THUMBNAIL_2X_OFFSET_Y=$((THUMBNAIL_OFFSET_Y * 2))
    
    echo "  Cropping thumbnail (light, 1x)..."
    magick "$FULL_LIGHT" \
        -crop "${THUMBNAIL_WIDTH}x${THUMBNAIL_HEIGHT}+${THUMBNAIL_OFFSET_X}+${THUMBNAIL_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${THUMBNAIL_WIDTH}x${THUMBNAIL_HEIGHT}" \
        "$CROP_LIGHT"
    
    echo "  Cropping thumbnail (light, 2x)..."
    magick "$FULL_LIGHT_2X" \
        -crop "${THUMBNAIL_2X_WIDTH}x${THUMBNAIL_2X_HEIGHT}+${THUMBNAIL_2X_OFFSET_X}+${THUMBNAIL_2X_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${THUMBNAIL_2X_WIDTH}x${THUMBNAIL_2X_HEIGHT}" \
        "$CROP_LIGHT_2X"
    
    echo "  Cropping thumbnail (dark, 1x)..."
    magick "$FULL_DARK" \
        -crop "${THUMBNAIL_WIDTH}x${THUMBNAIL_HEIGHT}+${THUMBNAIL_OFFSET_X}+${THUMBNAIL_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${THUMBNAIL_WIDTH}x${THUMBNAIL_HEIGHT}" \
        "$CROP_DARK"
    
    echo "  Cropping thumbnail (dark, 2x)..."
    magick "$FULL_DARK_2X" \
        -crop "${THUMBNAIL_2X_WIDTH}x${THUMBNAIL_2X_HEIGHT}+${THUMBNAIL_2X_OFFSET_X}+${THUMBNAIL_2X_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${THUMBNAIL_2X_WIDTH}x${THUMBNAIL_2X_HEIGHT}" \
        "$CROP_DARK_2X"
    
    # Crop lightbox views
    LIGHTBOX_LIGHT="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-lightbox-${TARGET}.png"
    LIGHTBOX_LIGHT_2X="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-lightbox-${TARGET}@2x.png"
    LIGHTBOX_DARK="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-lightbox-${TARGET}-dark.png"
    LIGHTBOX_DARK_2X="$REPO_ROOT/website/assets/screenshots/${OUTPUT_PREFIX}-lightbox-${TARGET}-dark@2x.png"
    
    # Calculate 2x offsets and dimensions for lightbox
    LIGHTBOX_2X_WIDTH=$((LIGHTBOX_WIDTH * 2))
    LIGHTBOX_2X_HEIGHT=$((LIGHTBOX_HEIGHT * 2))
    LIGHTBOX_2X_OFFSET_X=$((LIGHTBOX_OFFSET_X * 2))
    LIGHTBOX_2X_OFFSET_Y=$((LIGHTBOX_OFFSET_Y * 2))
    
    echo "  Cropping lightbox (light, 1x)..."
    magick "$FULL_LIGHT" \
        -crop "${LIGHTBOX_WIDTH}x${LIGHTBOX_HEIGHT}+${LIGHTBOX_OFFSET_X}+${LIGHTBOX_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${LIGHTBOX_WIDTH}x${LIGHTBOX_HEIGHT}" \
        "$LIGHTBOX_LIGHT"
    
    echo "  Cropping lightbox (light, 2x)..."
    magick "$FULL_LIGHT_2X" \
        -crop "${LIGHTBOX_2X_WIDTH}x${LIGHTBOX_2X_HEIGHT}+${LIGHTBOX_2X_OFFSET_X}+${LIGHTBOX_2X_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${LIGHTBOX_2X_WIDTH}x${LIGHTBOX_2X_HEIGHT}" \
        "$LIGHTBOX_LIGHT_2X"
    
    echo "  Cropping lightbox (dark, 1x)..."
    magick "$FULL_DARK" \
        -crop "${LIGHTBOX_WIDTH}x${LIGHTBOX_HEIGHT}+${LIGHTBOX_OFFSET_X}+${LIGHTBOX_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${LIGHTBOX_WIDTH}x${LIGHTBOX_HEIGHT}" \
        "$LIGHTBOX_DARK"
    
    echo "  Cropping lightbox (dark, 2x)..."
    magick "$FULL_DARK_2X" \
        -crop "${LIGHTBOX_2X_WIDTH}x${LIGHTBOX_2X_HEIGHT}+${LIGHTBOX_2X_OFFSET_X}+${LIGHTBOX_2X_OFFSET_Y}" \
        +repage \
        -background none \
        -extent "${LIGHTBOX_2X_WIDTH}x${LIGHTBOX_2X_HEIGHT}" \
        "$LIGHTBOX_DARK_2X"
done

echo ""
echo "✅ Screenshot generation complete!"
echo ""
echo "Generated files for render target(s): ${TARGETS[*]}"
echo "  - Thumbnails: ${THUMBNAIL_WIDTH}×${THUMBNAIL_HEIGHT} (1x), ${THUMBNAIL_2X_WIDTH}×${THUMBNAIL_2X_HEIGHT} (2x)"
echo "  - Lightbox: ${LIGHTBOX_WIDTH}×${LIGHTBOX_HEIGHT} (1x), ${LIGHTBOX_2X_WIDTH}×${LIGHTBOX_2X_HEIGHT} (2x)"
echo "  - Both light and dark modes"
echo ""
echo "Files are in: website/assets/screenshots/${OUTPUT_PREFIX}-*"
