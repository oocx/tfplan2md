import { initThemeToggle } from "./modules/theme-toggle.js";
import { initMobileNav } from "./modules/mobile-nav.js";
import { initCopyButtons } from "./modules/copy-buttons.js";
import { initInteractiveExamples } from "./modules/interactive-examples.js";
import { initCodeTabs } from "./modules/code-tabs.js";
import { initCarousel } from "./modules/carousel.js";
import { initLightbox } from "./modules/lightbox.js";
import { initDocsToc } from "./modules/docs-toc.js";

function initSite() {
  initThemeToggle();
  initMobileNav();
  initCopyButtons();
  initInteractiveExamples();
  initCodeTabs();
  initCarousel();
  initLightbox();
  initDocsToc();
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initSite, { once: true });
} else {
  initSite();
}