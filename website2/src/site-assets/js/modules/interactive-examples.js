function escapeHtml(text) {
  return text
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function highlightMarkdown() {
  document.querySelectorAll(".source-view code").forEach((block) => {
    let html = escapeHtml(block.textContent);
    html = html.replace(/(&lt;!--[\s\S]*?--&gt;)/g, '<span class="md-comment">$1</span>');
    html = html.replace(/^(#{1,6} .+)$/gm, '<span class="md-heading">$1</span>');
    html = html.replace(/(\*\*[^*]+\*\*)/g, '<span class="md-bold">$1</span>');
    html = html.replace(/(`[^`]+`)/g, '<span class="md-code">$1</span>');
    html = html.replace(/(\[[^\]]+\]\([^)]+\))/g, '<span class="md-link">$1</span>');
    html = html.replace(/^(\s*[-*+] )/gm, '<span class="md-list">$1</span>');
    html = html.replace(/(\|)/g, '<span class="md-table-sep">$1</span>');
    block.innerHTML = html;
  });
}

function alignComparisonHeights() {
  document.querySelectorAll(".feature-comparison").forEach((comparison) => {
    const columns = comparison.querySelectorAll(".comparison-column");
    if (columns.length !== 2) {
      return;
    }

    const withoutBlock = columns[0].querySelector(".code-block");
    const withBlock = columns[1].querySelector(".code-block");

    if (!withoutBlock || !withBlock) {
      return;
    }

    withoutBlock.style.removeProperty("height");
    withBlock.style.removeProperty("height");

    const height = withBlock.offsetHeight;
    withoutBlock.style.height = `${height}px`;
    withBlock.style.height = `${height}px`;
  });
}

export function initInteractiveExamples() {
  document.querySelectorAll(".interactive-example").forEach((example) => {
    const buttons = example.querySelectorAll(".toggle-btn");
    const panes = example.querySelectorAll(".view-pane");
    const fullscreenBtn = example.querySelector(".fullscreen-btn");

    buttons.forEach((button) => {
      button.addEventListener("click", () => {
        const view = button.dataset.view;
        buttons.forEach((item) => item.classList.remove("active"));
        panes.forEach((pane) => pane.classList.remove("active"));

        button.classList.add("active");
        example.querySelector(`.${view}-view`)?.classList.add("active");
      });
    });

    if (fullscreenBtn) {
      fullscreenBtn.addEventListener("click", () => {
        const isFullscreen = example.classList.toggle("fullscreen");
        fullscreenBtn.textContent = isFullscreen ? "✕" : "⛶";
        document.body.style.overflow = isFullscreen ? "hidden" : "";
      });
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape") {
      return;
    }

    document.querySelectorAll(".interactive-example.fullscreen").forEach((example) => {
      example.classList.remove("fullscreen");
      const button = example.querySelector(".fullscreen-btn");
      if (button) {
        button.textContent = "⛶";
      }
    });
    document.body.style.overflow = "";
  });

  highlightMarkdown();
  alignComparisonHeights();
  window.addEventListener("resize", alignComparisonHeights);
}