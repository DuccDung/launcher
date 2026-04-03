(() => {
  const defaultPreviewImage = "/admin-ui/assets/img/g_item_02.png";
  const maxTrendingGames = 6;

  const elements = {
    message: document.getElementById("workspaceMessage"),
    toastStack: document.getElementById("workspaceToastStack"),
    steamAppIdInput: document.getElementById("steamAppIdInput"),
    workspaceDetailsCard: document.getElementById("workspaceDetailsCard"),
    workspaceDetailsPill: document.getElementById("workspaceDetailsPill"),
    gameTitleInput: document.getElementById("gameTitleInput"),
    gameSlugInput: document.getElementById("gameSlugInput"),
    categoryChoices: document.getElementById("gameCategoryChoices"),
    selectedCategories: document.getElementById("gameSelectedCategories"),
    steamFetchButton: document.getElementById("steamFetchButton"),
    steamFetchStatus: document.getElementById("steamFetchStatus"),
    saveGameButton: document.getElementById("saveGameButton"),
    resetFormButton: document.getElementById("resetFormButton"),
    deleteGameButton: document.getElementById("deleteGameButton"),
    storeKeywordInput: document.getElementById("storeKeywordInput"),
    storeGameList: document.getElementById("storeGameList"),
    storeListPill: document.getElementById("storeListPill"),
    storeViewToggleButton: document.getElementById("storeViewToggleButton"),
    steamPreviewImage: document.getElementById("steamPreviewImage"),
    steamPreviewTitle: document.getElementById("steamPreviewTitle"),
    steamPreviewMeta: document.getElementById("steamPreviewMeta"),
    steamPreviewTags: document.getElementById("steamPreviewTags"),
    steamPreviewStatus: document.getElementById("steamPreviewStatus"),
    steamPreviewAppId: document.getElementById("steamPreviewAppId"),
    steamPreviewRelease: document.getElementById("steamPreviewRelease"),
    steamPreviewSource: document.getElementById("steamPreviewSource"),
    steamPreviewTagCount: document.getElementById("steamPreviewTagCount"),
    steamOriginalPrice: document.getElementById("steamOriginalPrice"),
    steamOriginalPriceNote: document.getElementById("steamOriginalPriceNote"),
    steamSalePrice: document.getElementById("steamSalePrice"),
    steamSalePriceNote: document.getElementById("steamSalePriceNote"),
    storeDiscountPreview: document.getElementById("storeDiscountPreview"),
    storeFinalPrice: document.getElementById("storeFinalPrice"),
    storeFinalPriceNote: document.getElementById("storeFinalPriceNote"),
    versionStandardEnabled: document.getElementById("versionStandardEnabled"),
    versionStandardName: document.getElementById("versionStandardName"),
    versionStandardDiscount: document.getElementById("versionStandardDiscount"),
    versionStandardPrice: document.getElementById("versionStandardPrice"),
    versionFullEnabled: document.getElementById("versionFullEnabled"),
    versionFullName: document.getElementById("versionFullName"),
    versionFullDiscount: document.getElementById("versionFullDiscount"),
    versionFullPrice: document.getElementById("versionFullPrice")
  };

  const state = {
    bootstrap: null,
    detail: null,
    selectedGameId: null,
    standardVersionId: null,
    fullVersionId: null,
    steamPreview: createEmptySteamPreview(),
    previewSource: "empty",
    hasWorkspaceDetails: false,
    storeViewMode: "card"
  };

  const soundState = {
    audioContext: null
  };

  function createEmptySteamPreview() {
    return {
      steamAppId: 0,
      name: "",
      photoUrl: defaultPreviewImage,
      tags: [],
      releaseDate: "",
      originalPrice: 0,
      salePrice: 0,
      originalPriceText: "0 VND",
      salePriceText: "0 VND",
      isFree: false
    };
  }

  function trim(value) {
    return String(value ?? "").trim();
  }

  function toNumber(value) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  function clampPercentage(value) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? Math.min(100, Math.max(0, Math.round(parsed))) : 0;
  }

  function parseCurrencyInput(value) {
    const digits = String(value ?? "").replace(/[^\d]/g, "");
    return digits ? Number.parseInt(digits, 10) : 0;
  }

  function formatMoney(value) {
    const safeValue = Number.isFinite(value) ? value : 0;
    return `${new Intl.NumberFormat("vi-VN").format(Math.max(0, Math.round(safeValue)))} VND`;
  }

  function formatDate(value) {
    if (!value || Number.isNaN(new Date(value).getTime())) {
      return "Chua co du lieu";
    }

    return new Intl.DateTimeFormat("vi-VN", {
      hour: "2-digit",
      minute: "2-digit",
      day: "2-digit",
      month: "2-digit",
      year: "numeric"
    }).format(new Date(value));
  }

  function slugify(value) {
    return trim(value)
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/\u0111|\u0110/g, "d")
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "")
      .replace(/-{2,}/g, "-");
  }

  function escapeHtml(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  function ensureAudioContext() {
    const AudioContextCtor = window.AudioContext || window.webkitAudioContext;
    if (!AudioContextCtor) {
      return null;
    }

    if (!soundState.audioContext) {
      soundState.audioContext = new AudioContextCtor();
    }

    if (soundState.audioContext.state === "suspended") {
      soundState.audioContext.resume().catch(() => {});
    }

    return soundState.audioContext;
  }

  function primeAudio() {
    ensureAudioContext();
  }

  function playGentleErrorSound() {
    const audioContext = ensureAudioContext();
    if (!audioContext) {
      return;
    }

    const now = audioContext.currentTime;
    const oscillator = audioContext.createOscillator();
    const gain = audioContext.createGain();

    oscillator.type = "sine";
    oscillator.frequency.setValueAtTime(560, now);
    oscillator.frequency.exponentialRampToValueAtTime(420, now + 0.16);

    gain.gain.setValueAtTime(0.0001, now);
    gain.gain.exponentialRampToValueAtTime(0.024, now + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.18);

    oscillator.connect(gain);
    gain.connect(audioContext.destination);
    oscillator.start(now);
    oscillator.stop(now + 0.18);
  }

  function showToast(message, tone = "error", { sound = false, title } = {}) {
    if (!elements.toastStack || !trim(message)) {
      return;
    }

    const toast = document.createElement("article");
    const heading =
      title || (tone === "success" ? "Thanh cong" : tone === "error" ? "Thieu thong tin" : "Thong bao");
    const icon = tone === "success" ? "bi-check2-circle" : "bi-exclamation-octagon";

    toast.className = `admin-workspace-toast is-${tone}`;
    toast.setAttribute("role", "alert");
    toast.innerHTML = `
      <div class="admin-workspace-toast__icon">
        <i class="bi ${icon}"></i>
      </div>
      <div class="admin-workspace-toast__body">
        <strong class="admin-workspace-toast__title">${escapeHtml(heading)}</strong>
        <span class="admin-workspace-toast__message">${escapeHtml(message)}</span>
      </div>
      <button class="admin-workspace-toast__close" type="button" aria-label="Dong thong bao">
        <i class="bi bi-x-lg"></i>
      </button>
    `;

    const removeToast = () => {
      toast.remove();
    };

    toast.querySelector(".admin-workspace-toast__close")?.addEventListener("click", removeToast);
    elements.toastStack.appendChild(toast);

    while (elements.toastStack.children.length > 3) {
      elements.toastStack.firstElementChild?.remove();
    }

    window.setTimeout(removeToast, tone === "error" ? 4200 : 3200);

    if (sound && tone === "error") {
      playGentleErrorSound();
    }
  }

  function getPreviewStatusLabel() {
    if (state.previewSource === "steam") {
      return "Da lay tu Steam";
    }

    if (state.previewSource === "store") {
      return "Dang sua trong store";
    }

    return "Steam";
  }

  function getPreviewSourceLabel() {
    if (state.previewSource === "steam") {
      return "Steam API";
    }

    if (state.previewSource === "store") {
      return "Store SQL";
    }

    return "Chua nap";
  }

  function selectedCategoryIds() {
    return [...elements.categoryChoices.querySelectorAll("[data-category-choice]:checked")].map((input) => input.value);
  }

  function setSelectedCategoryIds(ids) {
    const selected = new Set(ids || []);
    [...elements.categoryChoices.querySelectorAll("[data-category-choice]")].forEach((input) => {
      input.checked = selected.has(input.value);
      input.closest(".admin-choice-card")?.classList.toggle("is-selected", input.checked);
    });
    renderSelectedCategories();
  }

  function setMessage(text, tone = "muted") {
    if (tone === "error" && text) {
      elements.message.hidden = true;
      elements.message.textContent = "";
      elements.message.classList.remove("is-success", "is-error", "is-muted");
      showToast(text, "error", { sound: true, title: "Chua the thuc hien" });
      return;
    }

    elements.message.hidden = !text;
    elements.message.textContent = text || "";
    elements.message.classList.remove("is-success", "is-error", "is-muted");
    if (text) {
      elements.message.classList.add(
        tone === "success" ? "is-success" : tone === "error" ? "is-error" : "is-muted"
      );
    }
  }

  function setFetchStatus(text, tone = "idle") {
    elements.steamFetchStatus.textContent = text;
    elements.steamFetchStatus.className = `admin-game-fetch-status is-${tone}`;

    if (tone === "error" && text) {
      showToast(text, "error", { sound: true, title: "Loi du lieu" });
    }
  }

  function renderWorkspaceDetailsState() {
    elements.workspaceDetailsCard.hidden = !state.hasWorkspaceDetails;
    elements.workspaceDetailsPill.textContent = state.previewSource === "store" ? "edit mode" : "steam data";
  }

  function renderStoreViewToggle() {
    const isTableMode = state.storeViewMode === "table";
    elements.storeViewToggleButton.innerHTML = isTableMode
      ? '<i class="bi bi-grid-3x2-gap"></i>Xem card'
      : '<i class="bi bi-table"></i>Xem bang';
    elements.storeViewToggleButton.setAttribute("aria-pressed", String(isTableMode));
  }

  function renderSelectedCategories() {
    const selected = [...elements.categoryChoices.querySelectorAll("[data-category-choice]:checked")].map((input) => {
      const name = input.dataset.categoryName || input.value;
      return `<span class="admin-soft-chip">${escapeHtml(name)}</span>`;
    });

    elements.selectedCategories.innerHTML = selected.length
      ? selected.join("")
      : '<span class="admin-soft-chip">Chua chon category</span>';
  }

  function renderCategoryChoices() {
    const categories = state.bootstrap?.categories ?? [];
    elements.categoryChoices.innerHTML = categories.length
      ? categories
          .map(
            (item) => `
              <label class="admin-choice-card">
                <input type="checkbox" value="${escapeHtml(item.categoryId)}" data-category-choice data-category-name="${escapeHtml(
                  item.name
                )}" />
                <span>${escapeHtml(item.name)}</span>
              </label>
            `
          )
          .join("")
      : '<div class="admin-workspace-empty"><strong>Chua co category</strong><span>Hay tao category truoc khi them game.</span></div>';

    [...elements.categoryChoices.querySelectorAll("[data-category-choice]")].forEach((input) => {
      input.addEventListener("change", () => {
        input.closest(".admin-choice-card")?.classList.toggle("is-selected", input.checked);
        renderSelectedCategories();
      });
    });

    if (state.detail?.game?.categoryIds?.length) {
      setSelectedCategoryIds(state.detail.game.categoryIds);
    } else {
      renderSelectedCategories();
    }
  }

  async function parseJson(response) {
    const contentType = response.headers.get("content-type") || "";
    return contentType.includes("application/json") ? response.json() : null;
  }

  async function request(url, options = {}) {
    const response = await fetch(url, {
      headers: {
        "Content-Type": "application/json",
        ...(options.headers || {})
      },
      ...options
    });

    const data = await parseJson(response);
    if (!response.ok) {
      throw new Error(data?.message || "Yeu cau khong thanh cong.");
    }

    return data;
  }

  function getBaseSteamPrice() {
    if (state.steamPreview.salePrice > 0 || state.steamPreview.isFree) {
      return state.steamPreview.salePrice;
    }

    return state.detail?.game?.steamPrice ?? 0;
  }

  function calculatePriceFromDiscount(discount) {
    return Math.max(0, Math.round(getBaseSteamPrice() * (1 - clampPercentage(discount) / 100)));
  }

  function calculateDiscountFromPrice(price) {
    const basePrice = getBaseSteamPrice();
    if (!Number.isFinite(basePrice) || basePrice <= 0 || price >= basePrice) {
      return 0;
    }

    return Math.max(0, Math.min(100, Math.round((1 - price / basePrice) * 100)));
  }

  function syncStandardFromDiscount() {
    const discount = clampPercentage(elements.versionStandardDiscount.value);
    elements.versionStandardDiscount.value = String(discount);
    elements.versionStandardPrice.value = formatMoney(calculatePriceFromDiscount(discount));
    renderPricePreview();
  }

  function syncStandardFromPrice(formatOnFinish = false) {
    const price = parseCurrencyInput(elements.versionStandardPrice.value);
    elements.versionStandardDiscount.value = String(calculateDiscountFromPrice(price));
    if (formatOnFinish || trim(elements.versionStandardPrice.value)) {
      elements.versionStandardPrice.value = formatMoney(price);
    }
    renderPricePreview();
  }

  function syncFullFromDiscount() {
    const discount = clampPercentage(elements.versionFullDiscount.value);
    elements.versionFullDiscount.value = String(discount);
    elements.versionFullPrice.value = formatMoney(calculatePriceFromDiscount(discount));
  }

  function syncFullFromPrice(formatOnFinish = false) {
    const price = parseCurrencyInput(elements.versionFullPrice.value);
    elements.versionFullDiscount.value = String(calculateDiscountFromPrice(price));
    if (formatOnFinish || trim(elements.versionFullPrice.value)) {
      elements.versionFullPrice.value = formatMoney(price);
    }
  }

  function getActiveVersions(versions) {
    return (versions || []).filter((item) => !item?.isRemoved);
  }

  function resolveManagedVersions(versions) {
    const ordered = [...getActiveVersions(versions)];
    let full = ordered.find((item) => /dlc|ultimate|complete|full/i.test(item.versionName || ""));
    let standard = ordered.find((item) => item.versionId !== full?.versionId) ?? null;

    if (!standard && ordered.length > 0) {
      standard = ordered[0];
    }

    if (!full && ordered.length > 1) {
      full = ordered.find((item) => item.versionId !== standard?.versionId) ?? null;
    }

    return { standard, full };
  }

  function formatVersionLabel(version, fallbackLabel) {
    return trim(version?.versionName) || fallbackLabel;
  }

  function renderSteamPreview() {
    const preview = state.steamPreview;
    const title = trim(elements.gameTitleInput.value) || preview.name || "Chua co game";
    const photoUrl = trim(preview.photoUrl) || state.detail?.game?.photoUrl || defaultPreviewImage;
    const appId = trim(elements.steamAppIdInput.value);
    const metaItems = [];

    if (appId) {
      metaItems.push(`app_id: ${appId}`);
    }

    if (preview.releaseDate) {
      metaItems.push(preview.releaseDate);
    }

    elements.steamPreviewImage.src = photoUrl;
    elements.steamPreviewTitle.textContent = title;
    elements.steamPreviewMeta.textContent = metaItems.length ? metaItems.join(" | ") : "Nhap app id de lay du lieu.";
    elements.steamPreviewStatus.textContent = getPreviewStatusLabel();
    elements.steamPreviewAppId.textContent = appId || "-";
    elements.steamPreviewRelease.textContent = preview.releaseDate || "Chua ro";
    elements.steamPreviewSource.textContent = getPreviewSourceLabel();
    elements.steamPreviewTagCount.textContent = String(preview.tags?.length ?? 0);
    elements.steamPreviewTags.innerHTML = (preview.tags?.length ? preview.tags : ["Chua co tag"])
      .map((tag) => `<span class="admin-soft-chip">${escapeHtml(tag)}</span>`)
      .join("");
  }

  function renderPricePreview() {
    const preview = state.steamPreview;
    const isStoreSource = state.previewSource === "store";
    const standardEnabled = elements.versionStandardEnabled.checked;
    const standardDiscount = clampPercentage(elements.versionStandardDiscount.value);
    const standardPrice = parseCurrencyInput(elements.versionStandardPrice.value);

    elements.steamOriginalPrice.textContent = preview.isFree ? "Mien phi" : preview.originalPriceText || "0 VND";
    elements.steamOriginalPriceNote.textContent = state.hasWorkspaceDetails
      ? isStoreSource
        ? "Gia dang luu trong store"
        : "Steam list price"
      : "Chua co du lieu";
    elements.steamSalePrice.textContent = preview.isFree ? "Mien phi" : preview.salePriceText || formatMoney(getBaseSteamPrice());
    elements.steamSalePriceNote.textContent = state.hasWorkspaceDetails
      ? isStoreSource
        ? "Gia co so dang ap dung"
        : "Theo region VN"
      : "Chua import";
    elements.storeDiscountPreview.textContent = `${standardDiscount}%`;
    elements.storeFinalPrice.textContent = standardEnabled ? formatMoney(standardPrice) : "Dang tat";
    elements.storeFinalPriceNote.textContent = standardEnabled ? "Ban thuong" : "Khong kich hoat";
  }

  function filteredGames() {
    const keyword = trim(elements.storeKeywordInput.value).toLowerCase();
    const games = state.bootstrap?.games ?? [];

    return games.filter((game) => {
      if (!keyword) {
        return true;
      }

      return [
        game.name,
        game.slug,
        game.steamAppId,
        ...(game.categoryNames || []),
        ...getActiveVersions(game.versions).map((item) => item.versionName)
      ]
        .join(" ")
        .toLowerCase()
        .includes(keyword);
    });
  }

  function renderCategoryChips(categoryNames) {
    return (categoryNames || []).length
      ? categoryNames.map((name) => `<span class="admin-soft-chip">${escapeHtml(name)}</span>`).join("")
      : '<span class="admin-soft-chip">Chua co category</span>';
  }

  function getTrendingCount() {
    return (state.bootstrap?.games ?? []).filter((item) => item.isTrending).length;
  }

  function renderTrendingToggle(game) {
    return `
      <label class="admin-game-trending-toggle ${game.isTrending ? "is-active" : ""}">
        <input
          type="checkbox"
          data-trending-toggle
          data-record-id="${escapeHtml(game.gameId)}"
          ${game.isTrending ? "checked" : ""}
        />
        <span>Game thinh hanh</span>
      </label>
    `;
  }

  function renderVersionPriceSummary(game) {
    const managedVersions = resolveManagedVersions(game.versions);
    return [
      { label: "Steam", value: formatMoney(game.steamPrice ?? 0), tone: "is-steam" },
      {
        label: formatVersionLabel(managedVersions.standard, "Ban thuong"),
        value: managedVersions.standard ? formatMoney(managedVersions.standard.price ?? 0) : "Chua co",
        tone: "is-standard"
      },
      {
        label: formatVersionLabel(managedVersions.full, "Ban full DLC"),
        value: managedVersions.full ? formatMoney(managedVersions.full.price ?? 0) : "Chua bat",
        tone: ""
      }
    ]
      .map(
        (item) => `
          <div class="admin-game-store-price ${item.tone}">
            <span>${escapeHtml(item.label)}</span>
            <strong>${escapeHtml(item.value)}</strong>
          </div>
        `
      )
      .join("");
  }

  function renderStoreGameCards(games) {
    return games
      .map((game) => {
        const isSelected = game.gameId === state.selectedGameId;
        const photoUrl = trim(game.photoUrl) || defaultPreviewImage;

        return `
          <article class="admin-mini-card admin-crud-record admin-game-list-card ${isSelected ? "is-active" : ""}">
            <div class="admin-game-store-item">
              <div class="admin-game-store-item__thumb">
                <img src="${escapeHtml(photoUrl)}" alt="${escapeHtml(game.name)}" />
              </div>
              <div class="admin-game-store-item__body">
                <div class="admin-game-store-item__head">
                  <div>
                    <strong>${escapeHtml(game.name)}</strong>
                    <div class="admin-crud-record__slug">steam_app_id: ${escapeHtml(game.steamAppId ?? "")} | /${escapeHtml(game.slug)}</div>
                  </div>
                  <span class="admin-status is-ok">${escapeHtml(game.versionCount)} version</span>
                </div>
                <div class="admin-game-store-prices">${renderVersionPriceSummary(game)}</div>
                <div class="admin-game-store-item__meta">
                  <span class="admin-game-store-item__updated">Cap nhat: ${escapeHtml(formatDate(game.updatedAt))}</span>
                  ${renderTrendingToggle(game)}
                </div>
                <div class="admin-chip-row admin-game-list-meta">${renderCategoryChips(game.categoryNames)}</div>
                <div class="admin-toolbar-actions admin-crud-item-actions">
                  <button class="admin-button admin-button--secondary" type="button" data-action="edit" data-record-id="${escapeHtml(game.gameId)}">Sua</button>
                  <button class="admin-button admin-button--danger" type="button" data-action="delete" data-record-id="${escapeHtml(game.gameId)}">Xoa</button>
                </div>
              </div>
            </div>
          </article>
        `;
      })
      .join("");
  }

  function renderStoreGameTable(games) {
    const rows = games
      .map((game) => {
        const managedVersions = resolveManagedVersions(game.versions);
        const isSelected = game.gameId === state.selectedGameId;

        return `
          <tr class="${isSelected ? "is-active" : ""}">
            <td><div class="admin-game-table-name"><strong>${escapeHtml(game.name)}</strong><span>/${escapeHtml(game.slug)}</span></div></td>
            <td class="admin-font-mono">${escapeHtml(game.steamAppId ?? "-")}</td>
            <td>${escapeHtml(formatMoney(game.steamPrice ?? 0))}</td>
            <td>${escapeHtml(managedVersions.standard ? formatMoney(managedVersions.standard.price ?? 0) : "-")}</td>
            <td>${escapeHtml(managedVersions.full ? formatMoney(managedVersions.full.price ?? 0) : "-")}</td>
            <td>${renderTrendingToggle(game)}</td>
            <td><div class="admin-game-table-categories">${renderCategoryChips(game.categoryNames)}</div></td>
            <td>${escapeHtml(String(game.versionCount))}</td>
            <td>${escapeHtml(formatDate(game.updatedAt))}</td>
            <td>
              <div class="admin-game-table-actions">
                <button class="admin-button admin-button--secondary" type="button" data-action="edit" data-record-id="${escapeHtml(game.gameId)}">Sua</button>
                <button class="admin-button admin-button--danger" type="button" data-action="delete" data-record-id="${escapeHtml(game.gameId)}">Xoa</button>
              </div>
            </td>
          </tr>
        `;
      })
      .join("");

    return `
      <table class="admin-data-table admin-game-store-table">
        <thead>
          <tr>
            <th>Game</th>
            <th>Steam App ID</th>
            <th>Gia Steam</th>
            <th>Ban thuong</th>
            <th>Ban full DLC</th>
            <th>Thinh hanh</th>
            <th>Category</th>
            <th>Version</th>
            <th>Cap nhat</th>
            <th>Thao tac</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    `;
  }

  function renderStoreGameList() {
    const games = filteredGames();
    renderStoreViewToggle();
    elements.storeListPill.textContent = `${games.length} game | ${getTrendingCount()}/${maxTrendingGames} trending`;
    elements.storeGameList.className =
      state.storeViewMode === "table"
        ? "admin-table-wrap admin-table-wrap--scroll admin-game-store-results"
        : "admin-mini-stack admin-list-scroll admin-crud-list admin-game-store-results";

    elements.storeGameList.innerHTML = games.length
      ? state.storeViewMode === "table"
        ? renderStoreGameTable(games)
        : renderStoreGameCards(games)
      : `
        <article class="admin-mini-card admin-crud-record">
          <strong>Chua co game</strong>
          <span class="admin-card-subtitle">Them game moi de hien thi o day.</span>
        </article>
      `;
  }

  function applyVersionToForm(version, type) {
    const enabledElement = type === "standard" ? elements.versionStandardEnabled : elements.versionFullEnabled;
    const nameElement = type === "standard" ? elements.versionStandardName : elements.versionFullName;
    const discountElement = type === "standard" ? elements.versionStandardDiscount : elements.versionFullDiscount;
    const priceElement = type === "standard" ? elements.versionStandardPrice : elements.versionFullPrice;
    const defaultName = type === "standard" ? "Ban thuong" : "Ban full DLC";
    const price = version?.price ?? getBaseSteamPrice();

    enabledElement.checked = version ? !version.isRemoved : true;
    nameElement.value = trim(version?.versionName) || defaultName;
    discountElement.value = String(calculateDiscountFromPrice(price));
    priceElement.value = formatMoney(price);
  }

  function applyGameToForm(game) {
    elements.steamAppIdInput.value = game?.steamAppId ?? "";
    elements.gameTitleInput.value = game?.name ?? "";
    elements.gameSlugInput.value = game?.slug ?? "";
    setSelectedCategoryIds(game?.categoryIds ?? []);

    state.steamPreview = {
      steamAppId: game?.steamAppId ?? 0,
      name: game?.name ?? "",
      photoUrl: game?.photoUrl || defaultPreviewImage,
      tags: [],
      releaseDate: "",
      originalPrice: game?.steamPrice ?? 0,
      salePrice: game?.steamPrice ?? 0,
      originalPriceText: formatMoney(game?.steamPrice ?? 0),
      salePriceText: formatMoney(game?.steamPrice ?? 0),
      isFree: (game?.steamPrice ?? 0) === 0
    };
    state.previewSource = "store";
    state.hasWorkspaceDetails = true;
  }

  function renderAll() {
    renderWorkspaceDetailsState();
    renderSteamPreview();
    renderPricePreview();
    renderStoreGameList();
  }

  async function loadBootstrap() {
    state.bootstrap = await request("/api/admin/game-workspace/bootstrap");
    renderCategoryChoices();
    renderStoreGameList();
  }

  async function loadGame(gameId) {
    const detail = await request(`/api/admin/game-workspace/games/${gameId}`);
    state.detail = detail;
    state.selectedGameId = gameId;
    applyGameToForm(detail.game);

    const managedVersions = resolveManagedVersions(detail.versions);
    state.standardVersionId = managedVersions.standard?.versionId ?? null;
    state.fullVersionId = managedVersions.full?.versionId ?? null;
    applyVersionToForm(managedVersions.standard, "standard");
    applyVersionToForm(managedVersions.full, "full");
    renderAll();
  }

  function resetForm() {
    state.detail = null;
    state.selectedGameId = null;
    state.standardVersionId = null;
    state.fullVersionId = null;
    state.steamPreview = createEmptySteamPreview();
    state.previewSource = "empty";
    state.hasWorkspaceDetails = false;

    elements.steamAppIdInput.value = "";
    elements.gameTitleInput.value = "";
    elements.gameSlugInput.value = "";
    setSelectedCategoryIds([]);
    elements.versionStandardEnabled.checked = true;
    elements.versionStandardName.value = "Ban thuong";
    elements.versionStandardDiscount.value = "0";
    elements.versionStandardPrice.value = formatMoney(0);
    elements.versionFullEnabled.checked = true;
    elements.versionFullName.value = "Ban full DLC";
    elements.versionFullDiscount.value = "0";
    elements.versionFullPrice.value = formatMoney(0);
    setFetchStatus("Chua lay du lieu tu Steam.", "idle");
    setMessage("", "muted");
    renderAll();
  }

  async function refreshWorkspace({ keepSelection = true } = {}) {
    const selectedGameId = keepSelection ? state.selectedGameId : null;
    await loadBootstrap();

    if (selectedGameId && state.bootstrap?.games?.some((item) => item.gameId === selectedGameId)) {
      await loadGame(selectedGameId);
      return;
    }

    resetForm();
  }

  async function handleSteamFetch() {
    const appId = trim(elements.steamAppIdInput.value);
    if (!/^\d+$/.test(appId)) {
      setFetchStatus("steam_app_id phai la so.", "error");
      return;
    }

    setFetchStatus("Dang goi Steam API...", "loading");
    elements.steamFetchButton.disabled = true;

    try {
      const incomingAppId = toNumber(appId);
      const isRefreshingSelectedGame =
        Boolean(state.selectedGameId) && state.detail?.game?.steamAppId === incomingAppId;

      if (!isRefreshingSelectedGame) {
        state.detail = null;
        state.selectedGameId = null;
        state.standardVersionId = null;
        state.fullVersionId = null;
        setSelectedCategoryIds([]);
        elements.versionStandardEnabled.checked = true;
        elements.versionStandardName.value = "Ban thuong";
        elements.versionStandardDiscount.value = "0";
        elements.versionFullEnabled.checked = true;
        elements.versionFullName.value = "Ban full DLC";
        elements.versionFullDiscount.value = "0";
      }

      const preview = await request(`/api/admin/game-workspace/steam-preview/${appId}`);
      state.steamPreview = preview;
      state.previewSource = "steam";
      state.hasWorkspaceDetails = true;
      elements.gameTitleInput.value = preview.name || "";
      elements.gameSlugInput.value = slugify(preview.name || appId);
      syncStandardFromDiscount();
      syncFullFromDiscount();
      setFetchStatus("Da lay du lieu tu Steam.", "success");
      renderAll();
    } catch (error) {
      setFetchStatus(error.message || "Khong the lay du lieu Steam.", "error");
    } finally {
      elements.steamFetchButton.disabled = false;
    }
  }

  function buildGamePayload() {
    return {
      name: trim(elements.gameTitleInput.value),
      steamAppId: toNumber(elements.steamAppIdInput.value),
      steamPrice: getBaseSteamPrice(),
      photoUrl: trim(state.steamPreview.photoUrl) || null,
      isRemove: false,
      categoryIds: selectedCategoryIds()
    };
  }

  function buildVersionPayload(type) {
    const isStandard = type === "standard";
    const enabled = isStandard ? elements.versionStandardEnabled.checked : elements.versionFullEnabled.checked;
    const name = isStandard ? trim(elements.versionStandardName.value) : trim(elements.versionFullName.value);
    const price = isStandard
      ? parseCurrencyInput(elements.versionStandardPrice.value)
      : parseCurrencyInput(elements.versionFullPrice.value);

    return {
      enabled,
      name: name || (isStandard ? "Ban thuong" : "Ban full DLC"),
      price
    };
  }

  async function upsertVersion(gameId, versionId, payload) {
    if (!payload.enabled) {
      if (!versionId) {
        return null;
      }

      await request(`/api/admin/game-workspace/versions/${versionId}`, {
        method: "PUT",
        body: JSON.stringify({ versionName: payload.name, price: payload.price, isRemoved: true })
      });
      return versionId;
    }

    if (versionId) {
      await request(`/api/admin/game-workspace/versions/${versionId}`, {
        method: "PUT",
        body: JSON.stringify({ versionName: payload.name, price: payload.price, isRemoved: false })
      });
      return versionId;
    }

    const response = await request(`/api/admin/game-workspace/games/${gameId}/versions`, {
      method: "POST",
      body: JSON.stringify({ versionName: payload.name, price: payload.price, isRemoved: false })
    });
    return response.versionId;
  }

  async function saveGame() {
    const gamePayload = buildGamePayload();
    const standardPayload = buildVersionPayload("standard");
    const fullPayload = buildVersionPayload("full");

    if (!gamePayload.steamAppId) {
      throw new Error("Ban can nhap steam_app_id.");
    }

    if (!trim(gamePayload.name)) {
      throw new Error("Hay goi Steam API de lay ten game.");
    }

    if (!gamePayload.categoryIds.length) {
      throw new Error("Moi game phai chon it nhat mot category.");
    }

    if (!standardPayload.enabled && !fullPayload.enabled) {
      throw new Error("Hay bat it nhat mot version.");
    }

    let gameId = state.selectedGameId;
    if (gameId) {
      await request(`/api/admin/game-workspace/games/${gameId}`, {
        method: "PUT",
        body: JSON.stringify(gamePayload)
      });
    } else {
      const response = await request("/api/admin/game-workspace/games", {
        method: "POST",
        body: JSON.stringify(gamePayload)
      });
      gameId = response.gameId;
    }

    state.standardVersionId = await upsertVersion(gameId, state.standardVersionId, standardPayload);
    state.fullVersionId = await upsertVersion(gameId, state.fullVersionId, fullPayload);
    await refreshWorkspace({ keepSelection: false });
    await loadGame(gameId);
    setMessage("Da luu game vao SQL Server.", "success");
  }

  async function deleteGame(gameId = state.selectedGameId) {
    if (!gameId) {
      throw new Error("Chua chon game de xoa.");
    }

    await request(`/api/admin/game-workspace/games/${gameId}`, { method: "DELETE" });
    await refreshWorkspace({ keepSelection: false });
    setMessage("Da danh dau isremove = true cho game.", "success");
  }

  async function updateTrending(gameId, isTrending) {
    await request(`/api/admin/game-workspace/games/${gameId}/trending`, {
      method: "PUT",
      body: JSON.stringify({ isTrending })
    });

    await refreshWorkspace({ keepSelection: true });
  }

  function handleRecordListClick(event) {
    const button = event.target.closest("[data-action]");
    if (!button) {
      return;
    }

    primeAudio();

    const recordId = button.getAttribute("data-record-id");
    const action = button.getAttribute("data-action");
    if (!recordId || !action) {
      return;
    }

    if (action === "edit") {
      loadGame(recordId).catch((error) => setMessage(error.message, "error"));
      return;
    }

    if (action === "delete") {
      deleteGame(recordId).catch((error) => setMessage(error.message, "error"));
    }
  }

  function handleRecordListChange(event) {
    const input = event.target.closest("[data-trending-toggle]");
    if (!input) {
      return;
    }

    primeAudio();

    const recordId = input.getAttribute("data-record-id");
    const nextValue = input.checked;
    const targetGame = (state.bootstrap?.games ?? []).find((item) => item.gameId === recordId);
    if (!recordId || !targetGame) {
      input.checked = !nextValue;
      return;
    }

    if (nextValue && !targetGame.isTrending && getTrendingCount() >= maxTrendingGames) {
      input.checked = false;
      setMessage(`Chi duoc dat toi da ${maxTrendingGames} game thinh hanh. Hay bo chon mot game khac truoc.`, "error");
      return;
    }

    input.disabled = true;
    updateTrending(recordId, nextValue)
      .catch((error) => {
        input.checked = !nextValue;
        setMessage(error.message, "error");
      })
      .finally(() => {
        input.disabled = false;
      });
  }

  function bindEvents() {
    elements.steamFetchButton.addEventListener("click", () => {
      primeAudio();
      handleSteamFetch().catch((error) => setMessage(error.message, "error"));
    });
    elements.steamAppIdInput.addEventListener("keydown", (event) => {
      if (event.key === "Enter") {
        event.preventDefault();
        primeAudio();
        handleSteamFetch().catch((error) => setMessage(error.message, "error"));
      }
    });

    elements.versionStandardDiscount.addEventListener("input", syncStandardFromDiscount);
    elements.versionFullDiscount.addEventListener("input", syncFullFromDiscount);
    elements.versionStandardPrice.addEventListener("input", () => {
      const price = parseCurrencyInput(elements.versionStandardPrice.value);
      elements.versionStandardDiscount.value = String(calculateDiscountFromPrice(price));
      renderPricePreview();
    });
    elements.versionStandardPrice.addEventListener("blur", () => syncStandardFromPrice(true));
    elements.versionFullPrice.addEventListener("input", () => {
      const price = parseCurrencyInput(elements.versionFullPrice.value);
      elements.versionFullDiscount.value = String(calculateDiscountFromPrice(price));
    });
    elements.versionFullPrice.addEventListener("blur", () => syncFullFromPrice(true));
    elements.versionStandardEnabled.addEventListener("change", renderPricePreview);
    elements.saveGameButton.addEventListener("click", () => {
      primeAudio();
      saveGame().catch((error) => setMessage(error.message, "error"));
    });
    elements.resetFormButton.addEventListener("click", resetForm);
    elements.deleteGameButton.addEventListener("click", () => {
      primeAudio();
      deleteGame().catch((error) => setMessage(error.message, "error"));
    });
    elements.storeKeywordInput.addEventListener("input", renderStoreGameList);
    elements.storeViewToggleButton.addEventListener("click", () => {
      state.storeViewMode = state.storeViewMode === "table" ? "card" : "table";
      renderStoreGameList();
    });
    elements.storeGameList.addEventListener("click", handleRecordListClick);
    elements.storeGameList.addEventListener("change", handleRecordListChange);
  }

  async function init() {
    if (!elements.steamAppIdInput) {
      return;
    }

    bindEvents();
    resetForm();
    await refreshWorkspace({ keepSelection: false });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", () => init().catch((error) => setMessage(error.message, "error")));
  } else {
    init().catch((error) => setMessage(error.message, "error"));
  }
})();
