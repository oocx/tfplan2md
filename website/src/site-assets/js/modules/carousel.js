export function initCarousel() {
  const track = document.querySelector(".features-carousel-track");
  const prevButton = document.querySelector(".carousel-prev");
  const nextButton = document.querySelector(".carousel-next");
  const dotsContainer = document.querySelector(".carousel-dots");

  if (!track || !prevButton || !nextButton || !dotsContainer) {
    return;
  }

  const slides = Array.from(track.children);
  if (!slides.length) {
    return;
  }

  let currentIndex = 0;
  let slidesPerView = 3;
  let slideGroups = 1;

  function calculateSlidesPerView() {
    if (window.innerWidth <= 768) {
      slidesPerView = 1;
    } else if (window.innerWidth <= 1024) {
      slidesPerView = 2;
    } else {
      slidesPerView = 3;
    }
    slideGroups = Math.ceil(slides.length / slidesPerView);
  }

  function updateCarousel() {
    const gap = Number.parseInt(getComputedStyle(track).gap, 10) || 24;
    const slideWidth = slides[0].offsetWidth + gap;
    track.style.transform = `translateX(-${currentIndex * slideWidth * slidesPerView}px)`;
    Array.from(dotsContainer.children).forEach((dot, index) => {
      dot.classList.toggle("active", index === currentIndex);
    });
    prevButton.disabled = currentIndex === 0;
    nextButton.disabled = currentIndex >= slideGroups - 1;
  }

  function createDots() {
    dotsContainer.innerHTML = "";
    for (let index = 0; index < slideGroups; index += 1) {
      const dot = document.createElement("button");
      dot.className = `carousel-dot${index === currentIndex ? " active" : ""}`;
      dot.setAttribute("aria-label", `Go to slide group ${index + 1}`);
      dot.addEventListener("click", () => {
        currentIndex = index;
        updateCarousel();
      });
      dotsContainer.appendChild(dot);
    }
  }

  prevButton.addEventListener("click", () => {
    currentIndex = Math.max(0, currentIndex - 1);
    updateCarousel();
  });

  nextButton.addEventListener("click", () => {
    currentIndex = Math.min(slideGroups - 1, currentIndex + 1);
    updateCarousel();
  });

  calculateSlidesPerView();
  createDots();
  updateCarousel();

  window.addEventListener("resize", () => {
    const previousSlidesPerView = slidesPerView;
    calculateSlidesPerView();
    if (previousSlidesPerView !== slidesPerView) {
      currentIndex = 0;
      createDots();
    }
    updateCarousel();
  });
}