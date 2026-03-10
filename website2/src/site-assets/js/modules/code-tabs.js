function initTabGroup(tabSelector, contentSelector) {
  document.querySelectorAll(tabSelector).forEach((tab) => {
    tab.addEventListener("click", () => {
      const tabName = tab.getAttribute("data-tab");
      document.querySelectorAll(tabSelector).forEach((item) => item.classList.remove("active"));
      document.querySelectorAll(contentSelector).forEach((item) => item.classList.remove("active"));
      tab.classList.add("active");
      document.getElementById(tabName)?.classList.add("active");
    });
  });
}

export function initCodeTabs() {
  initTabGroup(".code-tab", ".code-tab-content");
  initTabGroup(".cicd-tab", ".cicd-tab-content");
}