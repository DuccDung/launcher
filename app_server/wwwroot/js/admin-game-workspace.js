(() => {
  const defaultPreviewImage = "/admin-ui/assets/img/g_item_02.png";

  const elements = {
    message: document.getElementById("workspaceMessage"),
    steamAppIdInput: document.getElementById("steamAppIdInput"),
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
    steamPreviewImage: document.getElementById("steamPreviewImage"),
    steamPreviewTitle: document.getElementById("steamPreviewTitle"),
    steamPreviewMeta: document.getElementById("steamPreviewMeta"),
    steamPreviewTags: document.getElementById("steamPreviewTags"),
    steamPreviewStatus: document.getElementById("steamPreviewStatus"),
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
    steamPreview: createEmptySteamPreview()
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
    if (value == null || value === "") {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  function clampPercentage(value) {
    const parsed = Number.parseFloat(value);
    if (!Number.isFinite(parsed)) {
      return 0;
    }

    return Math.min(100, Math.max(0, Math.round(parsed)));
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
      .replace(/đ/g, "d")
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
    if (!elements.message) {
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
    if (!contentType.includes("application/json")) {
      return null;
    }

    return response.json();
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
    const basePrice = getBaseSteamPrice();
    return Math.max(0, Math.round(basePrice * (1 - clampPercentage(discount) / 100)));
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
    elements.steamPreviewMeta.textContent = metaItems.length
      ? metaItems.join(" • ")
      : "Nhap app id de lay du lieu.";
    elements.steamPreviewStatus.textContent = preview.name ? "Da lay tu Steam" : "Steam";

    const tags = preview.tags?.length ? preview.tags : ["Tags"];
    elements.steamPreviewTags.innerHTML = tags
      .map((tag) => `<span class="admin-soft-chip">${escapeHtml(tag)}</span>`)
      .join("");
  }

  function renderPricePreview() {
    const preview = state.steamPreview;
    const hasSteamData = preview.name || state.detail?.game?.name;
    const standardEnabled = elements.versionStandardEnabled.checked;
    const standardDiscount = clampPercentage(elements.versionStandardDiscount.value);
    const standardPrice = parseCurrencyInput(elements.versionStandardPrice.value);

    elements.steamOriginalPrice.textContent = preview.isFree ? "Mien phi" : preview.originalPriceText || "0 VND";
    elements.steamOriginalPriceNote.textContent = hasSteamData ? "Steam list price" : "Chua co du lieu";
    elements.steamSalePrice.textContent = preview.isFree ? "Mien phi" : preview.salePriceText || formatMoney(getBaseSteamPrice());
    elements.steamSalePriceNote.textContent = hasSteamData ? "Theo region VN" : "Chua import";

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

      const text = [game.name, game.slug, game.steamAppId, ...(game.categoryNames || [])].join(" ").toLowerCase();
      return text.includes(keyword);
    });
  }

  function renderStoreGameList() {
    const games = filteredGames();
    elements.storeListPill.textContent = `${games.length} record`;

    if (!games.length) {
      elements.storeGameList.innerHTML = `
        <article class="admin-mini-card admin-crud-record">
          <strong>Chua co game</strong>
          <span class="admin-card-subtitle">Them game moi de hien thi o day.</span>
        </article>
      `;
      return;
    }

    elements.storeGameList.innerHTML = games
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
                <div class="admin-record-footer">
                  <strong>${escapeHtml(game.name)}</strong>
                  <span class="admin-status is-ok">${escapeHtml(game.versionCount)} version</span>
                </div>
                <div class="admin-crud-record__slug">
                  steam_app_id: ${escapeHtml(game.steamAppId ?? "")} • /${escapeHtml(game.slug)}
                </div>
                <div class="admin-game-list-price">
                  <strong>${escapeHtml(formatMoney(game.steamPrice ?? 0))}</strong>
                  <span>${escapeHtml(formatDate(game.updatedAt))}</span>
                </div>
                <div class="admin-chip-row admin-game-list-meta">
                  ${(game.categoryNames || [])
                    .map((name) => `<span class="admin-soft-chip">${escapeHtml(name)}</span>`)
                    .join("")}
                </div>
                <div class="admin-toolbar-actions admin-crud-item-actions">
                  <button class="admin-button admin-button--secondary" type="button" data-action="edit" data-record-id="${escapeHtml(game.gameId)}">
                    Sua
                  </button>
                  <button class="admin-button admin-button--danger" type="button" data-action="delete" data-record-id="${escapeHtml(game.gameId)}">
                    Xoa
                  </button>
                </div>
              </div>
            </div>
          </article>
        `;
      })
      .join("");
  }

  function resolveManagedVersions(versions) {
    const ordered = [...(versions || [])];
    let full = ordered.find((item) => /dlc/i.test(item.versionName || ""));
    let standard = ordered.find((item) => item.versionId !== full?.versionId) ?? null;

    if (!standard && ordered.length > 0) {
      standard = ordered[0];
    }

    if (!full && ordered.length > 1) {
      full = ordered.find((item) => item.versionId !== standard?.versionId) ?? null;
    }

    return { standard, full };
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
  }

  function renderAll() {
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
      const preview = await request(`/api/admin/game-workspace/steam-preview/${appId}`);
      state.steamPreview = preview;
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
      rating: state.detail?.game?.rating ?? null,
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
    const priceValue = isStandard
      ? parseCurrencyInput(elements.versionStandardPrice.value)
      : parseCurrencyInput(elements.versionFullPrice.value);

    return {
      enabled,
      name: name || (isStandard ? "Ban thuong" : "Ban full DLC"),
      price: priceValue
    };
  }

  async function upsertVersion(gameId, versionId, payload) {
    if (!payload.enabled) {
      if (!versionId) {
        return null;
      }

      await request(`/api/admin/game-workspace/versions/${versionId}`, {
        method: "PUT",
        body: JSON.stringify({
          versionName: payload.name,
          price: payload.price,
          isRemoved: true
        })
      });

      return versionId;
    }

    if (versionId) {
      await request(`/api/admin/game-workspace/versions/${versionId}`, {
        method: "PUT",
        body: JSON.stringify({
          versionName: payload.name,
          price: payload.price,
          isRemoved: false
        })
      });

      return versionId;
    }

    const response = await request(`/api/admin/game-workspace/games/${gameId}/versions`, {
      method: "POST",
      body: JSON.stringify({
        versionName: payload.name,
        price: payload.price,
        isRemoved: false
      })
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

    await request(`/api/admin/game-workspace/games/${gameId}`, {
      method: "DELETE"
    });

    await refreshWorkspace({ keepSelection: false });
    setMessage("Da danh dau isremove = true cho game.", "success");
  }

  function handleRecordListClick(event) {
    const button = event.target.closest("[data-action]");
    if (!button) {
      return;
    }

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

  function bindEvents() {
    elements.steamFetchButton.addEventListener("click", () => {
      handleSteamFetch().catch((error) => setMessage(error.message, "error"));
    });

    elements.steamAppIdInput.addEventListener("keydown", (event) => {
      if (event.key === "Enter") {
        event.preventDefault();
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
      saveGame().catch((error) => setMessage(error.message, "error"));
    });

    elements.resetFormButton.addEventListener("click", resetForm);

    elements.deleteGameButton.addEventListener("click", () => {
      deleteGame().catch((error) => setMessage(error.message, "error"));
    });

    elements.storeKeywordInput.addEventListener("input", renderStoreGameList);
    elements.storeGameList.addEventListener("click", handleRecordListClick);
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
    document.addEventListener("DOMContentLoaded", () => {
      init().catch((error) => setMessage(error.message, "error"));
    });
  } else {
    init().catch((error) => setMessage(error.message, "error"));
  }
})();
