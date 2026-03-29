const mobileMenuToggle = document.getElementById("mobileMenuToggle");
const mobileDrawer = document.getElementById("mobileDrawer");
const mobileOverlay = document.getElementById("mobileOverlay");
const closeDrawer = document.getElementById("closeDrawer");
const headerBottom = document.querySelector(".header-bottom");
const stickyHeaderBreakpoint = window.matchMedia("(min-width: 768px)");
const headerBottomSticky = headerBottom ? headerBottom.cloneNode(true) : null;
const mobileDropdownToggles = document.querySelectorAll(
  ".mobile-dropdown-toggle"
);
const mobileDrawerLinks = document.querySelectorAll(".mobile-drawer-nav a");
const desktopBreakpoint = window.matchMedia("(min-width: 992px)");
let stickyHeaderTicking = false;

if (headerBottomSticky) {
  headerBottomSticky.classList.remove("header-bottom");
  headerBottomSticky.classList.add("header-bottom-sticky");
  headerBottomSticky.setAttribute("aria-hidden", "true");
  document.body.appendChild(headerBottomSticky);
}

function openDrawer() {
  if (!mobileDrawer || !mobileOverlay) return;

  mobileDrawer.classList.add("active");
  mobileOverlay.classList.add("active");
  mobileDrawer.setAttribute("aria-hidden", "false");

  if (mobileMenuToggle) {
    mobileMenuToggle.setAttribute("aria-expanded", "true");
  }

  document.body.style.overflow = "hidden";
}

function closeMenu() {
  if (!mobileDrawer || !mobileOverlay) return;

  mobileDrawer.classList.remove("active");
  mobileOverlay.classList.remove("active");
  mobileDrawer.setAttribute("aria-hidden", "true");

  if (mobileMenuToggle) {
    mobileMenuToggle.setAttribute("aria-expanded", "false");
  }

  document.body.style.overflow = "";
}

function handleEscape(event) {
  if (event.key === "Escape") {
    closeMenu();
  }
}

if (mobileMenuToggle) {
  mobileMenuToggle.addEventListener("click", openDrawer);
}

if (closeDrawer) {
  closeDrawer.addEventListener("click", closeMenu);
}

if (mobileOverlay) {
  mobileOverlay.addEventListener("click", closeMenu);
}

mobileDropdownToggles.forEach((toggle) => {
  toggle.addEventListener("click", () => {
    const submenu = toggle.nextElementSibling;

    if (!submenu) return;

    const isOpen = toggle.classList.toggle("open");
    submenu.classList.toggle("show", isOpen);
    toggle.setAttribute("aria-expanded", String(isOpen));
  });
});

mobileDrawerLinks.forEach((link) => {
  link.addEventListener("click", closeMenu);
});

document.addEventListener("keydown", handleEscape);

desktopBreakpoint.addEventListener("change", (event) => {
  if (event.matches) {
    closeMenu();
  }
});

function canUseStickyHeader() {
  if (!headerBottom || !headerBottomSticky || !stickyHeaderBreakpoint.matches) {
    return false;
  }

  return window.getComputedStyle(headerBottom).display !== "none";
}

function setStickyHeaderState(shouldStick) {
  if (!headerBottomSticky) return;

  headerBottomSticky.classList.toggle("is-visible", shouldStick);
}

function applyStickyHeader() {
  if (!canUseStickyHeader()) {
    setStickyHeaderState(false);
    return;
  }

  const headerBottomTop = headerBottom.getBoundingClientRect().top;
  const shouldStick = headerBottomTop <= 0;

  setStickyHeaderState(shouldStick);
}

function handleStickyHeaderScroll() {
  if (stickyHeaderTicking) return;

  stickyHeaderTicking = true;
  window.requestAnimationFrame(() => {
    applyStickyHeader();
    stickyHeaderTicking = false;
  });
}

applyStickyHeader();
window.addEventListener("scroll", handleStickyHeaderScroll, { passive: true });
window.addEventListener("resize", applyStickyHeader);

if (typeof stickyHeaderBreakpoint.addEventListener === "function") {
  stickyHeaderBreakpoint.addEventListener("change", applyStickyHeader);
} else if (typeof stickyHeaderBreakpoint.addListener === "function") {
  stickyHeaderBreakpoint.addListener(applyStickyHeader);
}

const ambientGlow = document.querySelector(".ambient-glow");
const motionPreference = window.matchMedia("(prefers-reduced-motion: reduce)");
let ambientGlowFrame = null;

function updateAmbientGlow(x, y, scale, opacity) {
  if (!ambientGlow) return;

  ambientGlow.style.setProperty("--glow-x", `${x}px`);
  ambientGlow.style.setProperty("--glow-y", `${y}px`);
  ambientGlow.style.setProperty("--glow-scale", scale.toFixed(3));
  ambientGlow.style.setProperty("--glow-opacity", opacity.toFixed(3));
}

function applyStaticAmbientGlow() {
  updateAmbientGlow(
    window.innerWidth * 0.5,
    window.innerHeight * 0.58,
    1.06,
    0.44
  );
}

function stopAmbientGlow() {
  if (!ambientGlowFrame) return;

  window.cancelAnimationFrame(ambientGlowFrame);
  ambientGlowFrame = null;
}

function startAmbientGlow() {
  if (!ambientGlow) return;

  stopAmbientGlow();

  const animate = (time) => {
    const t = time * 0.00072;
    const motionFactor = motionPreference.matches ? 0.65 : 1;
    const centerX = window.innerWidth * 0.5;
    const centerY = window.innerHeight * 0.58;
    const driftX =
      (Math.sin(t * 1.12) * window.innerWidth * 0.075 +
        Math.cos(t * 0.42) * window.innerWidth * 0.028) *
      motionFactor;
    const driftY =
      (Math.cos(t * 0.88) * window.innerHeight * 0.095 +
        Math.sin(t * 0.36) * window.innerHeight * 0.032) *
      motionFactor;
    const scale =
      1.04 +
      (Math.sin(t * 1.88) * 0.12 + Math.cos(t * 0.98) * 0.05) * motionFactor;
    const opacity =
      0.32 +
      (((Math.sin(t * 1.72 - 0.5) + 1) * 0.5) * 0.2 +
        ((Math.cos(t * 0.64) + 1) * 0.5) * 0.05) *
        motionFactor;

    updateAmbientGlow(centerX + driftX, centerY + driftY, scale, opacity);
    ambientGlowFrame = window.requestAnimationFrame(animate);
  };

  ambientGlowFrame = window.requestAnimationFrame(animate);
}

function handleAmbientGlowPreferenceChange() {
  startAmbientGlow();
}

startAmbientGlow();

if (typeof motionPreference.addEventListener === "function") {
  motionPreference.addEventListener("change", handleAmbientGlowPreferenceChange);
} else if (typeof motionPreference.addListener === "function") {
  motionPreference.addListener(handleAmbientGlowPreferenceChange);
}

document.addEventListener("visibilitychange", () => {
  if (document.hidden) {
    stopAmbientGlow();
    stopDiscoverAutoplay();
    return;
  }

  startAmbientGlow();
  startDiscoverAutoplay();
});

const discoverSection = document.querySelector(".discover-section");
const discoverSlides = Array.from(document.querySelectorAll(".discover-slide"));
const discoverDots = Array.from(document.querySelectorAll(".discover-dot"));
const discoverSideItems = Array.from(
  document.querySelectorAll(".discover-side-item")
);
const discoverSlidesViewport = document.querySelector(".discover-slides");
const discoverPrevButton = document.querySelector(".discover-control--prev");
const discoverNextButton = document.querySelector(".discover-control--next");
let discoverActiveIndex = 0;
let discoverAutoplay = null;
let discoverTouchStartX = 0;
let discoverTouchStartY = 0;
let discoverTouchCurrentX = 0;
let discoverTouchCurrentY = 0;
let discoverTouchActive = false;

function setDiscoverSlide(index) {
  if (!discoverSlides.length) return;

  discoverActiveIndex =
    (index + discoverSlides.length) % discoverSlides.length;

  discoverSlides.forEach((slide, slideIndex) => {
    const isActive = slideIndex === discoverActiveIndex;
    slide.classList.toggle("is-active", isActive);
    slide.setAttribute("aria-hidden", String(!isActive));
  });

  discoverDots.forEach((dot, dotIndex) => {
    const isActive = dotIndex === discoverActiveIndex;
    dot.classList.toggle("is-active", isActive);
    dot.setAttribute("aria-pressed", String(isActive));
  });

  let matchedSidebarItem = false;

  discoverSideItems.forEach((item) => {
    const shouldActivate =
      !matchedSidebarItem &&
      Number(item.dataset.slideTo) === discoverActiveIndex;

    item.classList.toggle("is-active", shouldActivate);

    if (shouldActivate) {
      matchedSidebarItem = true;
    }
  });
}

function stopDiscoverAutoplay() {
  if (!discoverAutoplay) return;

  window.clearInterval(discoverAutoplay);
  discoverAutoplay = null;
}

function startDiscoverAutoplay() {
  stopDiscoverAutoplay();

  if (!discoverSection || discoverSlides.length < 2 || motionPreference.matches) {
    return;
  }

  discoverAutoplay = window.setInterval(() => {
    setDiscoverSlide(discoverActiveIndex + 1);
  }, 6500);
}

function handleDiscoverTouchStart(event) {
  if (!event.touches.length) return;

  const touch = event.touches[0];
  discoverTouchStartX = touch.clientX;
  discoverTouchStartY = touch.clientY;
  discoverTouchCurrentX = touch.clientX;
  discoverTouchCurrentY = touch.clientY;
  discoverTouchActive = true;
  stopDiscoverAutoplay();
}

function handleDiscoverTouchMove(event) {
  if (!discoverTouchActive || !event.touches.length) return;

  const touch = event.touches[0];
  discoverTouchCurrentX = touch.clientX;
  discoverTouchCurrentY = touch.clientY;
}

function handleDiscoverTouchEnd() {
  if (!discoverTouchActive) return;

  const deltaX = discoverTouchCurrentX - discoverTouchStartX;
  const deltaY = discoverTouchCurrentY - discoverTouchStartY;
  const horizontalDistance = Math.abs(deltaX);
  const verticalDistance = Math.abs(deltaY);

  discoverTouchActive = false;

  if (horizontalDistance > 46 && horizontalDistance > verticalDistance * 1.15) {
    if (deltaX < 0) {
      setDiscoverSlide(discoverActiveIndex + 1);
    } else {
      setDiscoverSlide(discoverActiveIndex - 1);
    }
  }

  startDiscoverAutoplay();
}

if (discoverSection && discoverSlides.length) {
  const initialActiveSlide = discoverSlides.findIndex((slide) =>
    slide.classList.contains("is-active")
  );

  setDiscoverSlide(initialActiveSlide >= 0 ? initialActiveSlide : 0);

  if (discoverPrevButton) {
    discoverPrevButton.addEventListener("click", () => {
      setDiscoverSlide(discoverActiveIndex - 1);
      startDiscoverAutoplay();
    });
  }

  if (discoverNextButton) {
    discoverNextButton.addEventListener("click", () => {
      setDiscoverSlide(discoverActiveIndex + 1);
      startDiscoverAutoplay();
    });
  }

  discoverDots.forEach((dot) => {
    dot.addEventListener("click", () => {
      setDiscoverSlide(Number(dot.dataset.slideTo));
      startDiscoverAutoplay();
    });
  });

  discoverSideItems.forEach((item) => {
    item.addEventListener("click", () => {
      setDiscoverSlide(Number(item.dataset.slideTo));
      startDiscoverAutoplay();
    });
  });

  discoverSection.addEventListener("mouseenter", stopDiscoverAutoplay);
  discoverSection.addEventListener("mouseleave", startDiscoverAutoplay);
  discoverSection.addEventListener("focusin", stopDiscoverAutoplay);
  discoverSection.addEventListener("focusout", (event) => {
    if (discoverSection.contains(event.relatedTarget)) {
      return;
    }

    startDiscoverAutoplay();
  });

  if (discoverSlidesViewport) {
    discoverSlidesViewport.addEventListener("touchstart", handleDiscoverTouchStart, {
      passive: true,
    });
    discoverSlidesViewport.addEventListener("touchmove", handleDiscoverTouchMove, {
      passive: true,
    });
    discoverSlidesViewport.addEventListener("touchend", handleDiscoverTouchEnd, {
      passive: true,
    });
    discoverSlidesViewport.addEventListener(
      "touchcancel",
      handleDiscoverTouchEnd,
      {
        passive: true,
      }
    );
  }

  startDiscoverAutoplay();
}

const productGalleries = document.querySelectorAll("[data-product-gallery]");

productGalleries.forEach((gallery) => {
  const mainFrame = gallery.querySelector(".product-gallery-main");
  const mainImage = gallery.querySelector("[data-product-main-image]");
  const thumbnails = Array.from(gallery.querySelectorAll("[data-product-thumb]"));
  const prevButton = gallery.querySelector("[data-product-prev]");
  const nextButton = gallery.querySelector("[data-product-next]");
  const expandButton = gallery.querySelector(".product-gallery-expand");
  const thumbsViewport = gallery.querySelector("[data-product-thumb-viewport]");
  const thumbsTrack = gallery.querySelector("[data-product-thumb-track]");
  const thumbsPrevButton = gallery.querySelector("[data-product-thumb-prev]");
  const thumbsNextButton = gallery.querySelector("[data-product-thumb-next]");

  if (!mainFrame || !mainImage || !thumbnails.length) return;

  let activeIndex = Math.max(
    0,
    thumbnails.findIndex((thumb) => thumb.classList.contains("is-active"))
  );
  let productTouchStartX = 0;
  let productTouchStartY = 0;

  function scrollActiveThumbIntoView() {
    const activeThumb = thumbnails[activeIndex];

    if (!activeThumb || !thumbsViewport) return;

    activeThumb.scrollIntoView({
      behavior: "smooth",
      block: "nearest",
      inline: "center",
    });
  }

  function applyProductGallery(index) {
    activeIndex = (index + thumbnails.length) % thumbnails.length;

    thumbnails.forEach((thumb, thumbIndex) => {
      const isActive = thumbIndex === activeIndex;
      thumb.classList.toggle("is-active", isActive);
      thumb.setAttribute("aria-pressed", String(isActive));
    });

    const activeThumb = thumbnails[activeIndex];
    const fullSrc = activeThumb.dataset.fullSrc || activeThumb.querySelector("img")?.src;
    const fullAlt =
      activeThumb.dataset.fullAlt || activeThumb.querySelector("img")?.alt || "";
    const mainPosition = activeThumb.dataset.mainPosition || "center center";

    if (fullSrc) {
      mainImage.src = fullSrc;
    }

    mainImage.alt = fullAlt;
    mainImage.style.objectPosition = mainPosition;
    scrollActiveThumbIntoView();
  }

  thumbnails.forEach((thumb, thumbIndex) => {
    thumb.addEventListener("click", () => {
      applyProductGallery(thumbIndex);
    });
  });

  if (prevButton) {
    prevButton.addEventListener("click", () => {
      applyProductGallery(activeIndex - 1);
    });
  }

  if (nextButton) {
    nextButton.addEventListener("click", () => {
      applyProductGallery(activeIndex + 1);
    });
  }

  if (thumbsPrevButton) {
    thumbsPrevButton.addEventListener("click", () => {
      applyProductGallery(activeIndex - 1);
    });
  }

  if (thumbsNextButton) {
    thumbsNextButton.addEventListener("click", () => {
      applyProductGallery(activeIndex + 1);
    });
  }

  if (expandButton) {
    expandButton.addEventListener("click", async () => {
      const fullscreenElement =
        document.fullscreenElement || document.webkitFullscreenElement;

      if (fullscreenElement === mainFrame) {
        if (document.exitFullscreen) {
          await document.exitFullscreen();
        } else if (document.webkitExitFullscreen) {
          document.webkitExitFullscreen();
        }
        return;
      }

      if (mainFrame.requestFullscreen) {
        await mainFrame.requestFullscreen();
      } else if (mainFrame.webkitRequestFullscreen) {
        mainFrame.webkitRequestFullscreen();
      }
    });
  }

  function handleProductTouchStart(event) {
    const touch = event.changedTouches?.[0];
    if (!touch) return;

    productTouchStartX = touch.clientX;
    productTouchStartY = touch.clientY;
  }

  function handleProductTouchEnd(event) {
    const touch = event.changedTouches?.[0];
    if (!touch) return;

    const deltaX = touch.clientX - productTouchStartX;
    const deltaY = touch.clientY - productTouchStartY;

    if (Math.abs(deltaX) > 44 && Math.abs(deltaX) > Math.abs(deltaY) * 1.15) {
      applyProductGallery(activeIndex + (deltaX < 0 ? 1 : -1));
    }

    productTouchStartX = 0;
    productTouchStartY = 0;
  }

  if (thumbsViewport && thumbsTrack) {
    thumbsViewport.addEventListener(
      "wheel",
      (event) => {
        if (Math.abs(event.deltaY) <= Math.abs(event.deltaX)) return;

        thumbsViewport.scrollBy({
          left: event.deltaY,
          behavior: "smooth",
        });
        event.preventDefault();
      },
      { passive: false }
    );
  }

  if (mainFrame) {
    mainFrame.addEventListener("touchstart", handleProductTouchStart, {
      passive: true,
    });
    mainFrame.addEventListener("touchend", handleProductTouchEnd, {
      passive: true,
    });
    mainFrame.addEventListener("touchcancel", handleProductTouchEnd, {
      passive: true,
    });
  }

  applyProductGallery(activeIndex);
});

const editionSwitchers = document.querySelectorAll("[data-edition-switcher]");

editionSwitchers.forEach((switcher) => {
  const editionButtons = Array.from(
    switcher.querySelectorAll("[data-edition-option]")
  );
  const editionNote = switcher.querySelector("[data-edition-note]");

  if (!editionButtons.length || !editionNote) return;

  function applyEdition(activeButton) {
    editionButtons.forEach((button) => {
      const isActive = button === activeButton;
      button.classList.toggle("is-active", isActive);
      button.setAttribute("aria-pressed", String(isActive));
    });

    editionNote.textContent = activeButton.dataset.editionNote || "";
  }

  editionButtons.forEach((button) => {
    button.addEventListener("click", () => {
      applyEdition(button);
    });
  });

  const initiallyActiveButton =
    editionButtons.find((button) => button.classList.contains("is-active")) ||
    editionButtons[0];

  applyEdition(initiallyActiveButton);
});

const relatedCarousels = document.querySelectorAll("[data-related-carousel]");

relatedCarousels.forEach((carousel) => {
  const viewport = carousel.querySelector("[data-related-viewport]");
  const prevButton = carousel.querySelector("[data-related-prev]");
  const nextButton = carousel.querySelector("[data-related-next]");
  const firstCard = carousel.querySelector(".product-related-card");
  const track = carousel.querySelector(".product-related-track");

  if (!viewport || !firstCard) return;

  function getScrollAmount() {
    const gap = track
      ? Number.parseFloat(window.getComputedStyle(track).columnGap) ||
        Number.parseFloat(window.getComputedStyle(track).gap) ||
        24
      : 24;
    return firstCard.getBoundingClientRect().width + gap;
  }

  function scrollRelated(direction) {
    viewport.scrollBy({
      left: getScrollAmount() * direction * 2,
      behavior: "smooth",
    });
  }

  if (prevButton) {
    prevButton.addEventListener("click", () => {
      scrollRelated(-1);
    });
  }

  if (nextButton) {
    nextButton.addEventListener("click", () => {
      scrollRelated(1);
    });
  }
});
