import { escapeHtml } from "./utils.js";

function highlightMarkdown() {
  document.querySelectorAll(".source-view code").forEach((block) => {
    let html = escapeHtml(block.textContent);
    html = html.replaceAll(/(&lt;!--[\s\S]*?--&gt;)/g, '<span class="md-comment">$1</span>');
    html = html.replaceAll(/^(#{1,6} .+)$/gm, '<span class="md-heading">$1</span>');
    html = html.replaceAll(/(\*\*[^*]+\*\*)/g, '<span class="md-bold">$1</span>');
    html = html.replaceAll(/(`[^`]+`)/g, '<span class="md-code">$1</span>');
    html = html.replaceAll(/(\[[^\]]+\]\([^)]+\))/g, '<span class="md-link">$1</span>');
    html = html.replaceAll(/^(\s*[-*+] )/gm, '<span class="md-list">$1</span>');
    html = html.replaceAll(/(\|)/g, '<span class="md-table-sep">$1</span>');
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

function activateExampleView(example, buttons, panes, view) {
  buttons.forEach((item) => item.classList.remove("active"));
  panes.forEach((pane) => pane.classList.remove("active"));

  example.querySelector(`[data-view="${view}"]`)?.classList.add("active");
  example.querySelector(`.${view}-view`)?.classList.add("active");
}

function initExample(example) {
  const buttons = example.querySelectorAll(".toggle-btn");
  const panes = example.querySelectorAll(".view-pane");
  const fullscreenBtn = example.querySelector(".fullscreen-btn");

  buttons.forEach((button) => {
    button.addEventListener("click", () => {
      activateExampleView(example, buttons, panes, button.dataset.view);
    });
  });

  if (!fullscreenBtn) {
    return;
  }

  fullscreenBtn.addEventListener("click", () => {
    const isFullscreen = example.classList.toggle("fullscreen");
    fullscreenBtn.textContent = isFullscreen ? "✕" : "⛶";
    document.body.style.overflow = isFullscreen ? "hidden" : "";
  });
}

export function initInteractiveExamples() {
  document.querySelectorAll(".interactive-example").forEach(initExample);

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