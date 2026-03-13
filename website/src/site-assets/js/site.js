import { initThemeToggle } from "./modules/theme-toggle.js";
import { initMobileNav } from "./modules/mobile-nav.js";
import { initCopyButtons } from "./modules/copy-buttons.js";
import { initInteractiveExamples } from "./modules/interactive-examples.js";
import { initCodeTabs } from "./modules/code-tabs.js";
import { initCarousel } from "./modules/carousel.js";
import { initLightbox } from "./modules/lightbox.js";
import { initDocsToc } from "./modules/docs-toc.js";

function safeInit(name, init) {
  try {
    init();
  } catch (error) {
    console.error(`Site init failed: ${name}`, error);
  }
}

function initSite() {
  safeInit("theme-toggle", initThemeToggle);
  safeInit("mobile-nav", initMobileNav);
  safeInit("copy-buttons", initCopyButtons);
  safeInit("interactive-examples", initInteractiveExamples);
  safeInit("code-tabs", initCodeTabs);
  safeInit("carousel", initCarousel);
  safeInit("lightbox", initLightbox);
  safeInit("docs-toc", initDocsToc);
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initSite, { once: true });
} else {
  initSite();
}