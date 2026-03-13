function getThemeAwareImageSource(element) {
  const theme = document.documentElement.dataset.theme || "light";
  const src1x = theme === "dark"
    ? (element.dataset.lightboxDark1x || element.dataset.dark1x)
    : (element.dataset.lightboxLight1x || element.dataset.light1x);
  const src2x = theme === "dark"
    ? (element.dataset.lightboxDark2x || element.dataset.dark2x)
    : (element.dataset.lightboxLight2x || element.dataset.light2x);
  return { src1x, src2x };
}

export function initLightbox() {
  const modal = document.getElementById("lightbox");
  const modalImage = document.getElementById("lightbox-img");
  const closeButton = modal?.querySelector(".lightbox-close");
  let activeTrigger = null;

  if (!modal || !modalImage || !closeButton) {
    return;
  }

  function closeModal() {
    activeTrigger = null;
    modal.hidden = true;
    modal.classList.remove("active");
    document.body.style.overflow = "";
  }

  function openModal(sourceElement) {
    const { src1x, src2x } = getThemeAwareImageSource(sourceElement);
    if (!src1x) {
      return;
    }

    activeTrigger = sourceElement;
    modalImage.src = src1x;
    modalImage.srcset = src2x ? `${src1x} 1x, ${src2x} 2x` : "";
    modal.hidden = false;
    modal.classList.add("active");
    document.body.style.overflow = "hidden";
  }

  document.querySelectorAll(".screenshot-clickable, img[data-lightbox-light1x], img[data-light1x]").forEach((element) => {
    element.removeAttribute("onclick");
    element.addEventListener("click", () => openModal(element));
  });

  closeButton.addEventListener("click", closeModal);
  modal.addEventListener("click", (event) => {
    if (event.target === modal) {
      closeModal();
    }
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      closeModal();
    }
  });
  document.addEventListener("tfplan2md:themechange", () => {
    if (modal.hidden || !activeTrigger) {
      return;
    }

    const { src1x, src2x } = getThemeAwareImageSource(activeTrigger);
    modalImage.src = src1x;
    modalImage.srcset = src2x ? `${src1x} 1x, ${src2x} 2x` : "";
  });
}