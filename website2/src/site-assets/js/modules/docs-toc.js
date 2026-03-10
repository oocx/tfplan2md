export function initDocsToc() {
    const navLinks = document.querySelectorAll(".docs-nav-link");
    const navSublinks = document.querySelectorAll(".docs-nav-sublink");
    const allNavLinks = [...navLinks, ...navSublinks];

    if (allNavLinks.length === 0) return;

    const sidebar = document.getElementById("docs-sidebar");
    const toggleBtn = document.querySelector(".toc-toggle-btn");

    allNavLinks.forEach((link) => {
        link.addEventListener("click", (e) => {
            e.preventDefault();
            const targetId = link.getAttribute("href").substring(1);
            const targetSection = document.getElementById(targetId);

            if (targetSection) {
                if (window.innerWidth <= 1024 && sidebar && toggleBtn) {
                    sidebar.classList.remove("mobile-open");
                    toggleBtn.setAttribute("aria-expanded", "false");
                }

                const headerOffset = document.querySelector("nav")?.offsetHeight + 8 || 96;
                const offsetPosition =
                    targetSection.getBoundingClientRect().top + window.pageYOffset - headerOffset;

                window.scrollTo({ top: offsetPosition, behavior: "smooth" });
            }
        });
    });

    const observerOptions = {
        root: null,
        rootMargin: "-100px 0px -66%",
        threshold: 0,
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                const id = entry.target.id;

                allNavLinks.forEach((link) => link.classList.remove("active"));

                const activeLink = document.querySelector(
                    `.docs-nav-link[href="#${id}"], .docs-nav-sublink[href="#${id}"]`
                );
                if (activeLink) {
                    activeLink.classList.add("active");

                    if (activeLink.classList.contains("docs-nav-sublink")) {
                        const parentLi = activeLink.closest("li").parentElement.closest("li");
                        if (parentLi) {
                            const parentLink = parentLi.querySelector(".docs-nav-link");
                            if (parentLink) {
                                parentLink.classList.add("active");
                            }
                        }
                    }
                }
            }
        });
    }, observerOptions);

    document.querySelectorAll(".docs-content [id]").forEach((section) => {
        if (section.id) {
            observer.observe(section);
        }
    });
}
