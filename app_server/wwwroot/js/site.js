const recentPurchases = [
  {
    buyer: "Hoang Nam",
    product: "Drive Beyond Horizons - Key Steam Deluxe Edition",
    accent: "linear-gradient(135deg, #33106d, #ff5f6d)"
  },
  {
    buyer: "Thanh Tung",
    product: "Resident Evil Requiem - Steam Key Viet Hoa",
    accent: "linear-gradient(135deg, #8c1d18, #d98b7c)"
  },
  {
    buyer: "Minh Chau",
    product: "Mystery Box Ultimate - Ty le trung Legendary x2",
    accent: "linear-gradient(135deg, #f79f1f, #e11515)"
  },
  {
    buyer: "Bao Ngoc",
    product: "Forza Horizon 6 - Ban Pre-order + Qua Tet",
    accent: "linear-gradient(135deg, #8ec5fc, #e0c3fc)"
  }
];

const featuredGames = [
  "Forza Horizon 6",
  "007 First Light",
  "Resident Evil Requiem",
  "Crimson Desert"
];

document.addEventListener("DOMContentLoaded", () => {
  setupToastRotation();
  setupSideListRotation();
  duplicateTickerContent();
});

function setupToastRotation() {
  const toast = document.getElementById("purchaseToast");
  if (!toast) return;

  const title = toast.querySelector(".floating-toast__title");
  const text = toast.querySelector(".floating-toast__text");
  const thumb = toast.querySelector(".floating-toast__thumb");
  const closeButton = toast.querySelector(".floating-toast__close");

  let index = 0;

  const render = () => {
    const item = recentPurchases[index];
    title.textContent = `${item.buyer} da them vao gio`;
    text.textContent = item.product;
    thumb.style.background = item.accent;
    index = (index + 1) % recentPurchases.length;
  };

  render();
  const intervalId = window.setInterval(render, 4200);

  closeButton?.addEventListener("click", () => {
    toast.style.display = "none";
    window.clearInterval(intervalId);
  });
}

function setupSideListRotation() {
  const items = Array.from(document.querySelectorAll("#sideGameList .side-item"));
  if (!items.length) return;

  let activeIndex = 0;
  window.setInterval(() => {
    items[activeIndex].classList.remove("is-active");
    activeIndex = (activeIndex + 1) % items.length;
    items[activeIndex].classList.add("is-active");
  }, 2600);

  items.forEach((item, index) => {
    item.addEventListener("mouseenter", () => {
      items[activeIndex].classList.remove("is-active");
      activeIndex = index;
      items[activeIndex].classList.add("is-active");
    });
  });
}

function duplicateTickerContent() {
  const ticker = document.getElementById("winnerTicker");
  if (!ticker) return;

  const originalItems = Array.from(ticker.children).map((node) => node.textContent?.trim()).filter(Boolean);
  originalItems.forEach((text, index) => {
    if (index >= featuredGames.length) return;
    const clone = document.createElement("span");
    clone.textContent = `${text} | Hot: ${featuredGames[index]}`;
    ticker.appendChild(clone);
  });
}
