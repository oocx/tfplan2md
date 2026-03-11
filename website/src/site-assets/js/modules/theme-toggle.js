function updateThemeButton(themeToggle, theme) {
  if (!themeToggle) {
    return;
  }

  const label = theme === "dark" ? "Switch to light mode" : "Switch to dark mode";
  themeToggle.setAttribute("aria-label", label);
  themeToggle.setAttribute("title", label);
}

function updateThemeAwareImages(theme) {
  document.querySelectorAll("img[data-thumb-light1x]").forEach((img) => {
    const src1x = theme === "dark" ? img.dataset.thumbDark1x : img.dataset.thumbLight1x;
    const src2x = theme === "dark" ? img.dataset.thumbDark2x : img.dataset.thumbLight2x;

    if (src1x) {
      img.src = src1x;
    }

    if (src1x && src2x) {
      img.srcset = `${src1x} 1x, ${src2x} 2x`;
    }
  });
}

function updateHighlightTheme(theme) {
  const lightTheme = document.getElementById("highlight-light");
  const darkTheme = document.getElementById("highlight-dark");

  if (!lightTheme || !darkTheme) {
    return;
  }

  lightTheme.disabled = theme === "dark";
  darkTheme.disabled = theme !== "dark";
}

export function applyTheme(theme) {
  const html = document.documentElement;
  html.dataset.theme = theme;
  localStorage.setItem("theme", theme);
  updateThemeButton(document.querySelector(".theme-toggle"), theme);
  updateThemeAwareImages(theme);
  updateHighlightTheme(theme);
  document.dispatchEvent(new CustomEvent("tfplan2md:themechange", { detail: { theme } }));
}

export function initThemeToggle() {
  const themeToggle = document.querySelector(".theme-toggle");
  const savedTheme = localStorage.getItem("theme") || "light";

  applyTheme(savedTheme);

  if (!themeToggle) {
    return;
  }

  themeToggle.addEventListener("click", () => {
    const currentTheme = document.documentElement.dataset.theme || "light";
    const nextTheme = currentTheme === "light" ? "dark" : "light";
    applyTheme(nextTheme);
  });
}