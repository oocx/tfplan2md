import { escapeHtml } from "./utils.js";

function highlightInlineTokens(line) {
  return line
    .replaceAll(
      /("([^"\\]|\\.)*"|'([^'\\]|\\.)*')/g,
      '<span class="code-token-string">$1</span>'
    )
    .replaceAll(/\b(const|let|var|return|uses|run|with|script)\b/g, '<span class="code-token-keyword">$1</span>')
    .replaceAll(/\b(require|createComment|readFileSync)\b/g, '<span class="code-token-function">$1</span>')
    .replaceAll(/\b(true|false|null)\b/g, '<span class="code-token-literal">$1</span>');
}

function highlightYamlLikeBlock(block) {
  const lines = escapeHtml(block.textContent).split("\n");
  const highlighted = lines.map((line) => {
    if (line.trimStart().startsWith("#")) {
      return `<span class="code-token-comment">${line}</span>`;
    }

    let nextLine = highlightInlineTokens(line);

    nextLine = nextLine.replace(
      /^(\s*)(-\s+)([A-Za-z_][\w.-]*:)(\s*\|)?/, 
      '$1<span class="code-token-bullet">$2</span><span class="code-token-key">$3</span><span class="code-token-punctuation">$4</span>'
    );

    nextLine = nextLine.replace(
      /^(\s*)([A-Za-z_][\w.-]*:)(\s*\|)?/, 
      '$1<span class="code-token-key">$2</span><span class="code-token-punctuation">$3</span>'
    );

    nextLine = nextLine.replaceAll(/(\|)$/g, '<span class="code-token-punctuation">$1</span>');
    nextLine = nextLine.replaceAll(/([{}()[\],])/g, '<span class="code-token-punctuation">$1</span>');

    return nextLine;
  });

  block.innerHTML = highlighted.join("\n");
}

function initTabGroup(tabSelector, contentSelector) {
  document.querySelectorAll(tabSelector).forEach((tab) => {
    tab.addEventListener("click", () => {
      const tabName = tab.dataset.tab;
      document.querySelectorAll(tabSelector).forEach((item) => item.classList.remove("active"));
      document.querySelectorAll(contentSelector).forEach((item) => item.classList.remove("active"));
      tab.classList.add("active");
      document.getElementById(tabName)?.classList.add("active");
    });
  });
}

export function initCodeTabs() {
  document.querySelectorAll(".code-tab-content code.language-yaml, .cicd-tab-content code.language-yaml").forEach((block) => {
    block.classList.add("nohighlight");
    highlightYamlLikeBlock(block);
  });

  initTabGroup(".code-tab", ".code-tab-content");
  initTabGroup(".cicd-tab", ".cicd-tab-content");
}