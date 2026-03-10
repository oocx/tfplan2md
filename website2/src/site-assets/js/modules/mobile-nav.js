export function initMobileNav() {
  const mobileMenuBtn = document.querySelector(".mobile-menu-btn");
  const navMenu = document.querySelector(".nav-menu");

  if (!mobileMenuBtn || !navMenu) {
    return;
  }

  mobileMenuBtn.addEventListener("click", () => {
    navMenu.classList.toggle("active");
    mobileMenuBtn.classList.toggle("active");
  });

  document.addEventListener("click", (event) => {
    if (!navMenu.classList.contains("active")) {
      return;
    }

    if (navMenu.contains(event.target) || mobileMenuBtn.contains(event.target)) {
      return;
    }

    navMenu.classList.remove("active");
    mobileMenuBtn.classList.remove("active");
  });
}