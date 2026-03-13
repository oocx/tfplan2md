export function initDocsToc() {
    const navLinks = document.querySelectorAll(".docs-nav-link");
    const navSublinks = document.querySelectorAll(".docs-nav-sublink");
    const allNavLinks = [...navLinks, ...navSublinks];

    if (allNavLinks.length === 0) {
        return;
    }

    const sidebar = document.getElementById("docs-sidebar");
    const toggleBtn = document.querySelector(".toc-toggle-btn");

    allNavLinks.forEach((link) => {
        link.addEventListener("click", (event) => {
            event.preventDefault();
            const targetId = link.getAttribute("href").substring(1);
            const targetSection = document.getElementById(targetId);

            if (!targetSection) {
                return;
            }

            if (window.innerWidth <= 1024 && sidebar && toggleBtn) {
                sidebar.classList.remove("mobile-open");
                toggleBtn.setAttribute("aria-expanded", "false");
            }

            const headerOffset = document.querySelector("nav")?.offsetHeight + 8 || 96;
            const offsetPosition = targetSection.getBoundingClientRect().top + window.scrollY - headerOffset;

            window.scrollTo({ top: offsetPosition, behavior: "smooth" });
        });
    });

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) {
                return;
            }

            const id = entry.target.id;
            allNavLinks.forEach((link) => link.classList.remove("active"));

            const activeLink = document.querySelector(
                `.docs-nav-link[href="#${id}"], .docs-nav-sublink[href="#${id}"]`
            );

            if (!activeLink) {
                return;
            }

            activeLink.classList.add("active");

            if (!activeLink.classList.contains("docs-nav-sublink")) {
                return;
            }

            const parentLi = activeLink.closest("li")?.parentElement?.closest("li");
            const parentLink = parentLi?.querySelector(".docs-nav-link");
            parentLink?.classList.add("active");
        });
    }, {
        root: null,
        rootMargin: "-100px 0px -66%",
        threshold: 0
    });

    document.querySelectorAll(".docs-content [id]").forEach((section) => {
        if (section.id) {
            observer.observe(section);
        }
    });
}
