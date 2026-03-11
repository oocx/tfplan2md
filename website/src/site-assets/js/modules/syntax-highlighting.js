function hasHighlightJs() {
  return "hljs" in globalThis;
}

let isInitialized = false;

function applyHighlighting() {
  if (!hasHighlightJs()) {
    return false;
  }

  globalThis.hljs.highlightAll();
  return true;
}

export function initSyntaxHighlighting() {
  if (isInitialized) {
    return;
  }

  isInitialized = true;

  let attemptsRemaining = 10;
  const retry = () => {
    if (applyHighlighting() || attemptsRemaining <= 0) {
      return;
    }

    attemptsRemaining -= 1;
    globalThis.setTimeout(retry, 100);
  };

  retry();
  globalThis.addEventListener("load", applyHighlighting, { once: true });
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initSyntaxHighlighting, { once: true });
} else {
  globalThis.queueMicrotask(initSyntaxHighlighting);
}