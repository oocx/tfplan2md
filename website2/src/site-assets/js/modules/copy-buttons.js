const copyIcon = '<svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M5 3.5C5 2.67157 5.67157 2 6.5 2H12.5C13.3284 2 14 2.67157 14 3.5V9.5C14 10.3284 13.3284 11 12.5 11H11V12.5C11 13.3284 10.3284 14 9.5 14H3.5C2.67157 14 2 13.3284 2 12.5V6.5C2 5.67157 2.67157 5 3.5 5H5V3.5ZM6 5H9.5C10.3284 5 11 5.67157 11 6.5V10H12.5C12.7761 10 13 9.77614 13 9.5V3.5C13 3.22386 12.7761 3 12.5 3H6.5C6.22386 3 6 3.22386 6 3.5V5ZM3.5 6C3.22386 6 3 6.22386 3 6.5V12.5C3 12.7761 3.22386 13 3.5 13H9.5C9.77614 13 10 12.7761 10 12.5V6.5C10 6.22386 9.77614 6 9.5 6H3.5Z" fill="currentColor"/></svg>';
const copiedIcon = '<svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M13.78 4.22a.75.75 0 010 1.06l-7.25 7.25a.75.75 0 01-1.06 0L2.22 9.28a.75.75 0 011.06-1.06L6 10.94l6.72-6.72a.75.75 0 011.06 0z" fill="currentColor"/></svg>';

function getCopyText(button) {
  return button.getAttribute("data-copy")
    || button.closest(".code-block")?.querySelector("code")?.textContent
    || "";
}

export function initCopyButtons() {
  // Inject copy buttons into .code-block elements that don't already have one.
  // Skip .interactive-example blocks — those use a tab-based rendered/source UI.
  document.querySelectorAll(".code-block:not(.interactive-example)").forEach((codeBlock) => {
    if (!codeBlock.querySelector(".copy-button")) {
      const button = document.createElement("button");
      button.className = "copy-button";
      button.setAttribute("aria-label", "Copy code to clipboard");
      button.innerHTML = copyIcon;
      codeBlock.appendChild(button);
    }
  });

  document.querySelectorAll(".copy-button").forEach((button) => {
    if (!button.innerHTML.trim()) {
      button.innerHTML = copyIcon;
    }

    button.addEventListener("click", async () => {
      const text = getCopyText(button);
      if (!text) {
        return;
      }

      await navigator.clipboard.writeText(text);
      button.innerHTML = copiedIcon;
      button.classList.add("copied");
      window.setTimeout(() => {
        button.innerHTML = copyIcon;
        button.classList.remove("copied");
      }, 2000);
    });
  });
}
