(() => {
  const root = document.querySelector("[data-game-workspace]");
  if (!root) {
    return;
  }

  const q = (id) => document.getElementById(id);
  const r = {
    msg: root.querySelector("[data-workspace-message]"),
    stats: root.querySelector("[data-workspace-stats]"),
    gameSummary: root.querySelector("[data-game-summary]"),
    cats: root.querySelector("[data-category-choices]"),
    catPills: root.querySelector("[data-selected-categories]"),
    gameList: root.querySelector("[data-game-list]"),
    versionList: root.querySelector("[data-version-list]"),
    accountList: root.querySelector("[data-account-list]"),
    fileList: root.querySelector("[data-file-list]"),
    mediaList: root.querySelector("[data-media-list]"),
    previewMeta: root.querySelector("[data-article-preview-meta]"),
    preview: root.querySelector("[data-article-preview]"),
    statePill: root.querySelector("[data-article-state-pill]"),
    blockCount: root.querySelector("[data-article-block-count]"),
    blockList: root.querySelector("[data-article-block-list]"),
    editorEmpty: root.querySelector("[data-article-editor-empty]"),
    gameCount: root.querySelector("[data-game-count-pill]"),
    versionCount: root.querySelector("[data-version-count-label]"),
    accountCount: root.querySelector("[data-account-count-label]"),
    fileCount: root.querySelector("[data-file-count-label]"),
    mediaCount: root.querySelector("[data-media-count-label]"),
    versionNote: root.querySelector("[data-current-game-context]"),
    fileNote: root.querySelector("[data-file-context]"),
    mediaNote: root.querySelector("[data-media-context]"),
    articleNote: root.querySelector("[data-article-context]"),
    panels: {
      game: root.querySelector('[data-workspace-panel="game"]'),
      version: root.querySelector('[data-workspace-panel="version"]'),
      account: root.querySelector('[data-workspace-panel="account"]'),
      file: root.querySelector('[data-workspace-panel="file"]'),
      media: root.querySelector('[data-workspace-panel="media"]'),
      article: root.querySelector('[data-workspace-panel="article"]'),
      preview: root.querySelector('[data-workspace-panel="preview"]')
    },
    search: q("game-list-search"),
    filterCat: q("game-list-category-filter"),
    gameName: q("game-name"),
    gameRating: q("game-rating"),
    gameOldPrice: q("game-old-price"),
    gameNewPrice: q("game-new-price"),
    gameSlug: q("game-slug-preview"),
    versionName: q("version-name"),
    versionAccount: q("version-account-summary"),
    versionRemoved: q("version-is-removed"),
    accountVersion: q("account-version-id"),
    accountActive: q("account-is-active"),
    fileAccount: q("file-account-id"),
    fileType: q("file-type"),
    fileActive: q("file-is-active"),
    mediaType: q("media-type"),
    mediaUrl: q("media-url"),
    articleEyebrow: q("article-eyebrow"),
    articleTitle: q("article-title"),
    articleSummary: q("article-summary"),
    blockType: q("article-block-type"),
    blockTitle: q("article-block-title"),
    blockText: q("article-block-text"),
    blockIntro: q("article-block-intro"),
    blockItems: q("article-block-items"),
    blockUrl: q("article-block-url"),
    blockAlt: q("article-block-alt"),
    blockUp: q("article-block-move-up"),
    blockDown: q("article-block-move-down"),
    blockDup: q("article-block-duplicate"),
    blockDel: q("article-block-delete"),
    blockImg: q("article-block-upload-image"),
    blockFile: q("article-block-upload-file"),
    articleJson: q("article-json"),
    newGame: q("workspace-new-game"),
    reloadBtn: q("workspace-refresh-all"),
    gameCreate: q("game-create"),
    gameUpdate: q("game-update"),
    gameDelete: q("game-delete"),
    gameReset: q("game-reset-form"),
    versionCreate: q("version-create"),
    versionUpdate: q("version-update"),
    versionDelete: q("version-delete"),
    versionReset: q("version-reset"),
    accountCreate: q("account-create"),
    accountUpdate: q("account-update"),
    accountDelete: q("account-delete"),
    accountReset: q("account-reset"),
    fileCreate: q("file-create"),
    fileUpdate: q("file-update"),
    fileDelete: q("file-delete"),
    fileReset: q("file-reset"),
    mediaCreate: q("media-create"),
    mediaUpdate: q("media-update"),
    mediaDelete: q("media-delete"),
    mediaReset: q("media-reset"),
    mediaUpload: q("media-upload-local"),
    copyJson: q("article-copy-json"),
    articleDelete: q("article-delete"),
    articleSave: q("article-save"),
    addBlocks: [...root.querySelectorAll("[data-article-add-block]")],
    editorGroups: [...root.querySelectorAll("[data-article-editor-group]")],
    fileBtns: [...root.querySelectorAll("[data-file-upload]")]
  };
  r.fileUrls = [1, 2, 3, 4, 5].map((i) => q(`file-url-${i}`));

  const s = {
    boot: null,
    detail: null,
    gameId: null,
    versionId: null,
    accountId: null,
    fileId: null,
    mediaId: null,
    blockId: null,
    draft: {
      eyebrow: "",
      title: "",
      summary: "",
      blocks: []
    },
    busy: 0
  };

  const B = {
    paragraph: { label: "Đoạn văn", groups: ["text"] },
    heading: { label: "Tiêu đề phụ", groups: ["text"] },
    image: { label: "Hình ảnh", groups: ["url", "alt", "upload"] },
    video: { label: "Video", groups: ["title", "url", "upload"] },
    list: { label: "Danh sách", groups: ["intro", "items"] },
    quote: { label: "Trích dẫn", groups: ["text"] }
  };

  const empty = (title, text) =>
    `<article class="admin-workspace-empty"><strong>${e(title)}</strong><span>${e(text)}</span></article>`;

  function e(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  function trim(value) {
    return String(value ?? "").trim();
  }

  function num(value) {
    if (value === "" || value == null) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  function bool(value) {
    return value === true || value === "true";
  }

  function gid() {
    return window.crypto?.randomUUID
      ? window.crypto.randomUUID()
      : `tmp-${Date.now()}-${Math.round(Math.random() * 99999)}`;
  }

  function slug(value) {
    return trim(value)
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/đ/g, "d")
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "");
  }

  function short(value) {
    return value ? String(value).split("-")[0].toUpperCase() : "—";
  }

  function versionStateLabel(isRemoved) {
    return isRemoved ? "Đã gỡ" : "Đang hoạt động";
  }

  function versionName(item) {
    return trim(item?.versionName) || `Version ${short(item?.versionId)}`;
  }

  function purchaseLabel(isPurchased) {
    return isPurchased ? "Đã bán" : "Chưa bán";
  }

  function purchaseChipClass(isPurchased) {
    return isPurchased ? "admin-soft-chip--sold" : "admin-soft-chip--unsold";
  }

  function money(value) {
    return value == null || value === ""
      ? "—"
      : `${new Intl.NumberFormat("vi-VN").format(Number(value))} đ`;
  }

  function dt(value) {
    if (!value || Number.isNaN(new Date(value).getTime())) {
      return "Chưa có dữ liệu";
    }

    return new Intl.DateTimeFormat("vi-VN", {
      hour: "2-digit",
      minute: "2-digit",
      day: "2-digit",
      month: "2-digit",
      year: "numeric"
    }).format(new Date(value));
  }

  function isYoutube(value) {
    return /youtu\.be|youtube\.com/i.test(String(value ?? ""));
  }

  function toYoutubeEmbed(value) {
    try {
      const url = new URL(value);
      const id = url.hostname.includes("youtu.be")
        ? url.pathname.replaceAll("/", "")
        : url.searchParams.get("v");
      return id ? `https://www.youtube-nocookie.com/embed/${id}?rel=0` : value;
    } catch {
      return value;
    }
  }

  function isImage(value) {
    return /\.(png|jpe?g|webp|gif|bmp|svg)(\?|#|$)/i.test(String(value ?? ""));
  }

  function isVideo(value) {
    return /\.(mp4|webm|ogg|mov|m4v)(\?|#|$)/i.test(String(value ?? ""));
  }

  function busy() {
    return s.busy > 0;
  }

  function msg(text, tone = "muted") {
    r.msg.hidden = !text;
    r.msg.textContent = text || "";
    r.msg.classList.remove("is-success", "is-error", "is-muted");
    if (text) {
      r.msg.classList.add(
        tone === "success" ? "is-success" : tone === "error" ? "is-error" : "is-muted"
      );
    }
  }

  function stepBusy(delta) {
    s.busy = Math.max(0, s.busy + delta);
    renderState();
  }

  function ask(text) {
    return window.confirm(text);
  }
  function games() {
    return s.boot?.games ?? [];
  }

  function cats() {
    return s.boot?.categories ?? [];
  }

  function accs() {
    return s.boot?.accounts ?? [];
  }

  function curGame() {
    return s.detail?.game?.gameId === s.gameId ? s.detail.game : null;
  }

  function curArticle() {
    return s.detail?.article ?? null;
  }

  function linkedIds() {
    const versionIds = new Set(versions().map((item) => item.versionId));
    return [...new Set(
      accs()
        .filter((item) => item.versionId && versionIds.has(item.versionId))
        .map((item) => item.accountId)
    )];
  }

  function linkedAccs() {
    const versionIds = new Set(versions().map((item) => item.versionId));
    return accs().filter((item) => item.versionId && versionIds.has(item.versionId));
  }

  function versions() {
    return s.detail?.versions ?? [];
  }

  function linkedVersionForAccount(accountId = s.accountId) {
    const account = accs().find((item) => item.accountId === accountId);
    return account?.versionId
      ? versions().find((item) => item.versionId === account.versionId) ?? null
      : null;
  }

  function creatableVersions() {
    return versions();
  }

  function curBlock() {
    return s.draft.blocks.find((item) => item.id === s.blockId) ?? null;
  }

  function setPanel(key, disabled) {
    r.panels[key]?.classList.toggle("is-disabled", disabled);
  }

  function errText(data) {
    return (
      data?.message ||
      Object.values(data?.errors ?? {}).flat()[0] ||
      data?.title ||
      "Không thể xử lý yêu cầu workspace."
    );
  }

  async function req(url, options = {}) {
    const isForm = options.body instanceof FormData;
    const response = await fetch(url, {
      method: options.method || "GET",
      credentials: "same-origin",
      headers: {
        Accept: "application/json",
        ...(isForm ? {} : { "Content-Type": "application/json" })
      },
      body: options.body
    });

    const data = (response.headers.get("content-type") || "").includes("application/json")
      ? await response.json()
      : null;

    if (!response.ok) {
      throw new Error(errText(data));
    }

    return data;
  }

  async function upload(url, file) {
    const formData = new FormData();
    formData.append("file", file);
    return req(url, {
      method: "POST",
      body: formData
    });
  }

  function pick(accept) {
    return new Promise((resolve) => {
      const input = document.createElement("input");
      input.type = "file";
      input.accept = accept;
      input.hidden = true;
      document.body.appendChild(input);
      input.addEventListener(
        "change",
        () => {
          const file = input.files?.[0] ?? null;
          input.remove();
          resolve(file);
        },
        { once: true }
      );
      input.click();
    });
  }

  function block(raw) {
    const type = B[raw?.type] ? raw.type : "paragraph";
    if (type === "image") {
      return { id: raw?.id || gid(), type, url: trim(raw?.url), alt: trim(raw?.alt) };
    }

    if (type === "video") {
      return { id: raw?.id || gid(), type, title: trim(raw?.title), url: trim(raw?.url) };
    }

    if (type === "list") {
      return {
        id: raw?.id || gid(),
        type,
        intro: trim(raw?.intro),
        items: Array.isArray(raw?.items)
          ? raw.items.map(trim).filter(Boolean)
          : String(raw?.items ?? "")
              .split(/\r?\n/)
              .map(trim)
              .filter(Boolean)
      };
    }

    return { id: raw?.id || gid(), type, text: trim(raw?.text) };
  }

  function stripBlock(item) {
    if (item.type === "image") {
      return { type: item.type, url: trim(item.url), alt: trim(item.alt) };
    }

    if (item.type === "video") {
      return { type: item.type, title: trim(item.title), url: trim(item.url) };
    }

    if (item.type === "list") {
      return {
        type: item.type,
        intro: trim(item.intro),
        items: item.items.map(trim).filter(Boolean)
      };
    }

    return { type: item.type, text: trim(item.text) };
  }

  function draftJson() {
    return JSON.stringify(
      {
        eyebrow: trim(s.draft.eyebrow),
        title: trim(s.draft.title),
        summary: trim(s.draft.summary),
        blocks: s.draft.blocks.map(stripBlock)
      },
      null,
      2
    );
  }

  function normJson(json) {
    try {
      return JSON.stringify(JSON.parse(json || "{}"), null, 2);
    } catch {
      return "";
    }
  }

  function loadDraft(article) {
    let parsed = {};
    try {
      parsed = JSON.parse(article?.contentJson || "{}");
    } catch {
      parsed = {};
    }

    s.draft = {
      eyebrow: trim(parsed.eyebrow),
      title: trim(parsed.title),
      summary: trim(article?.summary ?? parsed.summary),
      blocks: Array.isArray(parsed.blocks) ? parsed.blocks.map(block) : []
    };
    s.blockId = s.draft.blocks[0]?.id ?? null;
  }

  function dirty() {
    if (!curArticle()) {
      return Boolean(
        trim(s.draft.eyebrow) ||
          trim(s.draft.title) ||
          trim(s.draft.summary) ||
          s.draft.blocks.length
      );
    }

    return (
      trim(curArticle().summary) !== trim(s.draft.summary) ||
      normJson(curArticle().contentJson) !== draftJson()
    );
  }

  async function reload(options = {}) {
    stepBusy(1);
    try {
      s.boot = await req("/api/admin/game-workspace/bootstrap");

      let gameId = Object.prototype.hasOwnProperty.call(options, "gameId")
        ? options.gameId
        : s.gameId;

      if (gameId && !games().some((item) => item.gameId === gameId)) {
        gameId = null;
      }

      if (!gameId && options.first && games().length) {
        gameId = games()[0].gameId;
      }

      s.gameId = gameId;

      if (s.accountId && !accs().some((item) => item.accountId === s.accountId)) {
        s.accountId = null;
      }

      if (gameId) {
        s.detail = await req(`/api/admin/game-workspace/games/${gameId}`);
        if (s.versionId && !s.detail.versions.some((item) => item.versionId === s.versionId)) {
          s.versionId = null;
        }
        if (s.fileId && !s.detail.files.some((item) => item.fileId === s.fileId)) {
          s.fileId = null;
        }
        if (s.mediaId && !s.detail.mediaItems.some((item) => item.mediaId === s.mediaId)) {
          s.mediaId = null;
        }
        loadDraft(s.detail.article);
      } else {
        s.detail = null;
        s.versionId = null;
        s.fileId = null;
        s.mediaId = null;
        s.blockId = null;
        s.draft = { eyebrow: "", title: "", summary: "", blocks: [] };
      }

      render();
      if (options.message) {
        msg(options.message, options.tone || "success");
      }
    } catch (error) {
      msg(error instanceof Error ? error.message : "Không thể tải workspace.", "error");
    } finally {
      stepBusy(-1);
    }
  }

  async function openGame(id) {
    if (!id) {
      return;
    }

    s.gameId = id;
    s.versionId = null;
    s.fileId = null;
    s.mediaId = null;

    stepBusy(1);
    try {
      s.detail = await req(`/api/admin/game-workspace/games/${id}`);
      loadDraft(s.detail.article);
      render();
      msg("");
    } catch (error) {
      msg(error instanceof Error ? error.message : "Không thể mở game.", "error");
    } finally {
      stepBusy(-1);
    }
  }
  function render() {
    renderStats();
    renderFilters();
    renderGamePanel();
    renderVersionPanel();
    renderAccountPanel();
    renderFilePanel();
    renderMediaPanel();
    renderArticlePanel();
    renderNotes();
    renderState();
  }

  function renderStats() {
    const stats = s.boot?.stats;
    if (!stats) {
      return;
    }

    const cards = [
      ["Game", stats.totalGames, "Số game trong catalog"],
      ["Version", stats.totalVersions, "Version theo từng game"],
      ["Account", stats.totalAccounts, "Kho account dùng chung"],
      ["File", stats.totalFiles, "File package hiện có"],
      ["Media", stats.totalMedia, "Ảnh và trailer"],
      ["Article", stats.totalArticles, "Article JSON đã lưu"]
    ];

    r.stats.innerHTML = cards
      .map(
        ([label, value, detail]) =>
          `<article class="admin-crud-kpi"><span>${e(label)}</span><strong>${e(
            value
          )}</strong><small>${e(detail)}</small></article>`
      )
      .join("");
  }

  function renderFilters() {
    const previous = r.filterCat.value;
    r.filterCat.innerHTML = [
      '<option value="">Tất cả</option>',
      ...cats().map((item) => `<option value="${e(item.categoryId)}">${e(item.name)}</option>`)
    ].join("");
    r.filterCat.value = cats().some((item) => item.categoryId === previous) ? previous : "";
  }

  function renderGamePanel() {
    const game = curGame();
    const checked = new Set(game?.categoryIds ?? []);
    const name = game?.name || "";

    r.gameName.value = name;
    r.gameRating.value = game?.rating ?? "";
    r.gameOldPrice.value = game?.oldPrice ?? "";
    r.gameNewPrice.value = game?.newPrice ?? "";
    r.gameSlug.value = slug(name || r.gameName.value);

    r.cats.innerHTML = cats()
      .map(
        (item) => `
          <label class="admin-soft-chip admin-workspace-choice ${checked.has(item.categoryId) ? "is-active" : ""}">
            <input type="checkbox" value="${e(item.categoryId)}" ${checked.has(item.categoryId) ? "checked" : ""} />
            <span>${e(item.name)}</span>
          </label>
        `
      )
      .join("");

    const selectedIds = [...r.cats.querySelectorAll("input:checked")].map((input) => input.value);
    const chosen = cats().filter((item) => selectedIds.includes(item.categoryId));

    r.catPills.innerHTML = chosen.length
      ? chosen
          .map(
            (item) => `<span class="admin-soft-chip is-active">${e(item.name)} · #${e(
              item.displayOrder
            )}</span>`
          )
          .join("")
      : `<span class="admin-soft-chip">Chưa gắn category nào.</span>`;

    const summaryName = game?.name || trim(r.gameName.value) || "Game mới";
    r.gameSummary.innerHTML = [
      ["Game hiện tại", summaryName],
      ["Slug preview", `/${slug(summaryName) || "game-moi"}`],
      ["Category", chosen.map((item) => item.name).join(", ") || "Chưa chọn"],
      ["Giá mới", money(r.gameNewPrice.value || game?.newPrice)],
      [
        "Version / File / Media",
        `${s.detail?.versions.length || 0} / ${s.detail?.files.length || 0} / ${
          s.detail?.mediaItems.length || 0
        }`
      ],
      [
        "Article",
        games().find((item) => item.gameId === s.gameId)?.hasArticle ? "Đã có article" : "Chưa có article"
      ]
    ]
      .map(
        ([label, value]) =>
          `<article class="admin-crud-summary-card"><span>${e(label)}</span><strong>${e(
            value
          )}</strong></article>`
      )
      .join("");

    const keyword = trim(r.search.value).toLowerCase();
    const categoryId = r.filterCat.value;
    const list = games()
      .filter(
        (item) =>
          (!keyword ||
            item.name.toLowerCase().includes(keyword) ||
            item.slug.toLowerCase().includes(keyword)) &&
          (!categoryId || item.categoryIds.includes(categoryId))
      )
      .sort((left, right) => new Date(right.updatedAt) - new Date(left.updatedAt));

    r.gameCount.textContent = `${list.length} record`;
    r.gameList.innerHTML = list.length
      ? list
          .map(
            (item) => `
              <article class="admin-mini-card admin-crud-record admin-workspace-record ${item.gameId === s.gameId ? "is-selected" : ""}">
                <div class="admin-record-footer">
                  <strong>${e(item.name)}</strong>
                  <small>${e(dt(item.updatedAt))}</small>
                </div>
                <div class="admin-crud-record__slug">/${e(item.slug)}</div>
                <div class="admin-chip-row admin-crud-meta">
                  <span class="admin-soft-chip">${e(`${item.versionCount} version`)}</span>
                  <span class="admin-soft-chip">${e(`${item.mediaCount} media`)}</span>
                  <span class="admin-soft-chip ${item.hasArticle ? "is-active" : ""}">${e(
                    item.hasArticle ? "Article ready" : "No article"
                  )}</span>
                </div>
                <p class="admin-workspace-record__description">${e(
                  [
                    item.categoryNames.join(", ") || "Chưa gắn category",
                    `Rating: ${item.rating ?? "—"}`,
                    `Giá mới: ${money(item.newPrice)}`
                  ].join(" · ")
                )}</p>
                <div class="admin-toolbar-actions admin-crud-item-actions">
                  <button class="admin-button admin-button--secondary" type="button" data-game="${e(item.gameId)}">Mở</button>
                  <button class="admin-button admin-button--danger" type="button" data-game-del="${e(item.gameId)}">Xóa</button>
                </div>
              </article>
            `
          )
          .join("")
      : empty("Chưa có game phù hợp", "Đổi bộ lọc hoặc tạo game mới từ form bên trái.");
  }

  function refreshGameDraftUi(useSelectedGameFallback = true) {
    const selectedIds = [...r.cats.querySelectorAll("input:checked")].map((input) => input.value);
    const chosen = cats().filter((item) => selectedIds.includes(item.categoryId));
    const fallbackGame = useSelectedGameFallback ? curGame() : null;
    const summaryName = trim(r.gameName.value) || fallbackGame?.name || "Game mới";

    r.gameSlug.value = trim(r.gameName.value) ? slug(r.gameName.value) : "";
    r.cats.querySelectorAll(".admin-workspace-choice").forEach((choice) => {
      const input = choice.querySelector("input");
      choice.classList.toggle("is-active", Boolean(input?.checked));
    });
    r.catPills.innerHTML = chosen.length
      ? chosen
          .map(
            (item) => `<span class="admin-soft-chip is-active">${e(item.name)} · #${e(
              item.displayOrder
            )}</span>`
          )
          .join("")
      : `<span class="admin-soft-chip">Chưa gắn category nào.</span>`;

    r.gameSummary.innerHTML = [
      ["Game hiện tại", summaryName],
      ["Slug preview", `/${slug(summaryName) || "game-moi"}`],
      ["Category", chosen.map((item) => item.name).join(", ") || "Chưa chọn"],
      ["Giá mới", money(r.gameNewPrice.value || fallbackGame?.newPrice)],
      [
        "Version / File / Media",
        `${s.detail?.versions.length || 0} / ${s.detail?.files.length || 0} / ${
          s.detail?.mediaItems.length || 0
        }`
      ],
      [
        "Article",
        games().find((item) => item.gameId === s.gameId)?.hasArticle ? "Đã có article" : "Chưa có article"
      ]
    ]
      .map(
        ([label, value]) =>
          `<article class="admin-crud-summary-card"><span>${e(label)}</span><strong>${e(
            value
          )}</strong></article>`
      )
      .join("");
  }

  function enterCreateMode(messageText) {
    s.gameId = null;
    s.detail = null;
    s.versionId = null;
    s.fileId = null;
    s.mediaId = null;
    s.blockId = null;
    s.draft = {
      eyebrow: "",
      title: "",
      summary: "",
      blocks: []
    };
    render();
    msg(messageText, "muted");
  }

  function renderGameListOnly() {
    const keyword = trim(r.search.value).toLowerCase();
    const categoryId = r.filterCat.value;
    const list = games()
      .filter(
        (item) =>
          (!keyword ||
            item.name.toLowerCase().includes(keyword) ||
            item.slug.toLowerCase().includes(keyword)) &&
          (!categoryId || item.categoryIds.includes(categoryId))
      )
      .sort((left, right) => new Date(right.updatedAt) - new Date(left.updatedAt));

    r.gameCount.textContent = `${list.length} record`;
    r.gameList.innerHTML = list.length
      ? list
          .map(
            (item) => `
              <article class="admin-mini-card admin-crud-record admin-workspace-record ${item.gameId === s.gameId ? "is-selected" : ""}">
                <div class="admin-record-footer">
                  <strong>${e(item.name)}</strong>
                  <small>${e(dt(item.updatedAt))}</small>
                </div>
                <div class="admin-crud-record__slug">/${e(item.slug)}</div>
                <div class="admin-chip-row admin-crud-meta">
                  <span class="admin-soft-chip">${e(`${item.versionCount} version`)}</span>
                  <span class="admin-soft-chip">${e(`${item.mediaCount} media`)}</span>
                  <span class="admin-soft-chip ${item.hasArticle ? "is-active" : ""}">${e(
                    item.hasArticle ? "Article ready" : "No article"
                  )}</span>
                </div>
                <p class="admin-workspace-record__description">${e(
                  [
                    item.categoryNames.join(", ") || "Chưa gắn category",
                    `Rating: ${item.rating ?? "—"}`,
                    `Giá mới: ${money(item.newPrice)}`
                  ].join(" · ")
                )}</p>
                <div class="admin-toolbar-actions admin-crud-item-actions">
                  <button class="admin-button admin-button--secondary" type="button" data-game="${e(item.gameId)}">Mở</button>
                  <button class="admin-button admin-button--danger" type="button" data-game-del="${e(item.gameId)}">Xóa</button>
                </div>
              </article>
            `
          )
          .join("")
      : empty("Chưa có game phù hợp", "Đổi bộ lọc hoặc tạo game mới từ form bên trái.");
  }

  function renderVersionPanel() {
    const current = s.detail?.versions.find((item) => item.versionId === s.versionId);
    r.versionName.value = current?.versionName || "";
    r.versionAccount.value = current
      ? `${current.linkedAccountCount ?? 0} account đang gắn với version này`
      : "Gắn account ở panel Kho account";
    r.versionRemoved.value = String(current?.isRemoved || false);
    r.versionCount.textContent = `${s.detail?.versions.length || 0} record`;

    r.versionList.innerHTML = !s.gameId
      ? empty("Chưa chọn game", "Chọn một game trong danh sách để mở version tương ứng.")
      : !(s.detail?.versions.length)
        ? empty("Game này chưa có version", "Tạo version trước để gắn nhiều account.")
        : s.detail.versions
            .map((item) => {
              const linkedCount = item.linkedAccountCount ?? accs().filter((entry) => entry.versionId === item.versionId).length;
              return `
                <article class="admin-mini-card admin-crud-record admin-workspace-record ${item.versionId === s.versionId ? "is-selected" : ""}">
                  <div class="admin-record-footer">
                    <strong>${e(versionName(item))}</strong>
                    <small>${e(dt(item.updatedAt))}</small>
                  </div>
                  <div class="admin-crud-record__slug">${e(
                    trim(item.versionName) ? `Tên: ${item.versionName}` : "Chưa đặt tên riêng"
                  )} · #${e(short(item.versionId))}</div>
                  <div class="admin-chip-row admin-crud-meta">
                    <span class="admin-soft-chip ${item.isRemoved ? "" : "is-active"}">${e(
                      item.isRemoved ? "Removed" : "Live"
                    )}</span>
                    <span class="admin-soft-chip">${e(`${linkedCount} account`)}</span>
                  </div>
                  <p class="admin-workspace-record__description">${e(
                    linkedCount
                      ? `Version này đang có ${linkedCount} account trong kho account.`
                      : "Version này chưa có account nào."
                  )}</p>
                  <div class="admin-toolbar-actions admin-crud-item-actions">
                    <button class="admin-button admin-button--secondary" type="button" data-version="${e(item.versionId)}">Sửa</button>
                    <button class="admin-button admin-button--danger" type="button" data-version-del="${e(item.versionId)}">Xóa</button>
                  </div>
                </article>
              `;
            })
            .join("");
  }

  function renderAccountPanel() {
    const current = accs().find((item) => item.accountId === s.accountId);
    const ids = new Set(linkedIds());
    const currentLinkedVersion = linkedVersionForAccount();
    const availableVersions = versions();
    const list = [...accs()].sort(
      (left, right) =>
        Number(ids.has(right.accountId)) -
          Number(ids.has(left.accountId)) ||
        new Date(right.updatedAt) - new Date(left.updatedAt)
    );

    r.accountVersion.innerHTML = !s.gameId
      ? '<option value="">Chọn game trước</option>'
      : !versions().length
        ? '<option value="">Tạo version trước</option>'
        : [
            '<option value="">Không gắn version</option>',
            ...availableVersions.map(
              (item) =>
                `<option value="${e(item.versionId)}">${e(
                  `${versionName(item)} · ${item.isRemoved ? "Removed" : "Live"}`
                )}</option>`
            )
          ].join("");

    r.accountVersion.value = current?.versionId || currentLinkedVersion?.versionId || "";
    r.accountActive.value = String(current?.isActive ?? true);
    r.accountCount.textContent = `${list.length} record`;

    r.accountList.innerHTML = list.length
      ? list
          .map((item) => {
            const linkedVersion = item.versionId
              ? versions().find((entry) => entry.versionId === item.versionId) ?? null
              : null;
            return `
              <article class="admin-mini-card admin-crud-record admin-workspace-record ${item.accountId === s.accountId ? "is-selected" : ""} ${ids.has(item.accountId) ? "is-linked" : ""}">
                <div class="admin-record-footer">
                  <strong>Account ${e(short(item.accountId))}</strong>
                  <small>${e(dt(item.updatedAt))}</small>
                </div>
                <div class="admin-chip-row admin-crud-meta">
                  <span class="admin-soft-chip ${item.isActive ? "is-active" : ""}">${e(
                    item.isActive ? "Active" : "Inactive"
                  )}</span>
                  <span class="admin-soft-chip ${purchaseChipClass(item.isPurchased)}">${e(
                    purchaseLabel(item.isPurchased)
                  )}</span>
                  <span class="admin-soft-chip">${e(linkedVersion ? versionName(linkedVersion) : "No version")}</span>
                  <span class="admin-soft-chip">${e(`${item.gameFileCount} file`)}</span>
                </div>
                <p class="admin-workspace-record__description">${e(
                  linkedVersion
                    ? ids.has(item.accountId)
                      ? `Account này đang gắn với ${versionName(linkedVersion)} của game đang mở.`
                      : `Account đang gắn với ${versionName(linkedVersion)} ở game khác.`
                    : "Account đang nằm trong kho toàn cục, chưa gắn version."
                )}</p>
                <div class="admin-toolbar-actions admin-crud-item-actions">
                  <button class="admin-button admin-button--secondary" type="button" data-account="${e(item.accountId)}">Sửa</button>
                  <button class="admin-button admin-button--danger" type="button" data-account-del="${e(item.accountId)}">Xóa</button>
                </div>
              </article>
            `;
          })
          .join("")
      : empty("Chưa có account", "Tạo account ở panel này rồi gắn vào version mong muốn.");
  }

  function renderFilePanel() {
    const linked = linkedAccs();
    const current = s.detail?.files.find((item) => item.fileId === s.fileId);

    r.fileAccount.innerHTML = !s.gameId
      ? '<option value="">Chọn game trước</option>'
      : !linked.length
        ? '<option value="">Chưa có account gắn qua version</option>'
        : linked
            .map(
              (item) =>
                `<option value="${e(item.accountId)}">${e(
                  `${short(item.accountId)} · ${item.isActive ? "Active" : "Inactive"}`
                )}</option>`
            )
            .join("");

    r.fileAccount.value = current?.accountId || linked[0]?.accountId || "";
    r.fileType.value = current?.fileType || "";
    r.fileActive.value = String(current?.isActive ?? true);
    r.fileUrls.forEach((input, index) => {
      input.value = current?.[`fileUrl0${index + 1}`] || "";
    });

    r.fileCount.textContent = `${s.detail?.files.length || 0} record`;
    r.fileList.innerHTML = !s.gameId
      ? empty("Chưa chọn game", "Chọn game trước để thấy file package theo account đã gắn.")
      : !linked.length
        ? empty("Chưa có account khả dụng", "Hãy tạo version và gắn account trước.")
        : !(s.detail?.files.length)
          ? empty("Chưa có file package", "Tạo file package mới hoặc upload local vào từng URL package.")
          : s.detail.files
              .map((item) => {
                const urls = [
                  item.fileUrl01,
                  item.fileUrl02,
                  item.fileUrl03,
                  item.fileUrl04,
                  item.fileUrl05
                ].filter(Boolean);

                return `
                  <article class="admin-mini-card admin-crud-record admin-workspace-record ${item.fileId === s.fileId ? "is-selected" : ""}">
                    <div class="admin-record-footer">
                      <strong>File ${e(short(item.fileId))}</strong>
                      <small>${e(dt(item.updatedAt))}</small>
                    </div>
                    <div class="admin-chip-row admin-crud-meta">
                      <span class="admin-soft-chip">${e(`Account ${short(item.accountId)}`)}</span>
                      <span class="admin-soft-chip ${item.isActive ? "is-active" : ""}">${e(
                        item.isActive ? "Active" : "Inactive"
                      )}</span>
                      <span class="admin-soft-chip">${e(item.fileType || "Không type")}</span>
                    </div>
                    <div class="admin-workspace-link-list">
                      ${
                        urls.length
                          ? urls
                              .map(
                                (url, index) =>
                                  `<a href="${e(url)}" target="_blank" rel="noreferrer">URL 0${index + 1}</a>`
                              )
                              .join("")
                          : "<span>Chưa có URL nào.</span>"
                      }
                    </div>
                    <div class="admin-toolbar-actions admin-crud-item-actions">
                      <button class="admin-button admin-button--secondary" type="button" data-file="${e(item.fileId)}">Sửa</button>
                      <button class="admin-button admin-button--danger" type="button" data-file-del="${e(item.fileId)}">Xóa</button>
                    </div>
                  </article>
                `;
              })
              .join("");
  }

  function renderMediaPanel() {
    const current = s.detail?.mediaItems.find((item) => item.mediaId === s.mediaId);
    r.mediaType.value = current?.mediaType || "";
    r.mediaUrl.value = current?.url || "";
    r.mediaCount.textContent = `${s.detail?.mediaItems.length || 0} record`;

    r.mediaList.innerHTML = !s.gameId
      ? empty("Chưa chọn game", "Chọn game trước khi quản lý media gallery hoặc trailer.")
      : !(s.detail?.mediaItems.length)
        ? empty("Chưa có media", "Dùng nút chọn media local hoặc nhập URL thủ công.")
        : s.detail.mediaItems
            .map((item) => {
              const preview = isImage(item.url)
                ? `<img src="${e(item.url)}" alt="${e(item.mediaType || "media")}" />`
                : isVideo(item.url)
                  ? `<video src="${e(item.url)}" muted playsinline></video>`
                  : `<span>${e(item.mediaType || "asset")}</span>`;

              return `
                <article class="admin-mini-card admin-crud-record admin-workspace-record ${item.mediaId === s.mediaId ? "is-selected" : ""}">
                  <div class="admin-workspace-thumbnail">${preview}</div>
                  <div class="admin-record-footer">
                    <strong>${e(item.mediaType || "Media")}</strong>
                    <small>${e(dt(item.updatedAt))}</small>
                  </div>
                  <div class="admin-workspace-link-list">
                    <a href="${e(item.url)}" target="_blank" rel="noreferrer">${e(item.url)}</a>
                  </div>
                  <div class="admin-toolbar-actions admin-crud-item-actions">
                    <button class="admin-button admin-button--secondary" type="button" data-media="${e(item.mediaId)}">Sửa</button>
                    <button class="admin-button admin-button--danger" type="button" data-media-del="${e(item.mediaId)}">Xóa</button>
                  </div>
                </article>
              `;
            })
            .join("");
  }

  function renderArticlePanel() {
    r.articleEyebrow.value = s.draft.eyebrow;
    r.articleTitle.value = s.draft.title;
    r.articleSummary.value = s.draft.summary;
    r.blockCount.textContent = `${s.draft.blocks.length} box`;
    r.articleJson.value = draftJson();
    r.statePill.textContent = !s.gameId ? "idle" : !curArticle() && !s.draft.blocks.length ? "empty" : dirty() ? "dirty" : "saved";

    r.blockList.innerHTML = s.draft.blocks.length
      ? s.draft.blocks
          .map((item, index) => {
            const text =
              item.type === "image"
                ? trim(item.alt) || trim(item.url) || "Chưa có ảnh"
                : item.type === "video"
                  ? trim(item.title) || trim(item.url) || "Chưa có video"
                  : item.type === "list"
                    ? trim(item.intro) || `${item.items.length} mục danh sách`
                    : trim(item.text) || "Chưa có nội dung";

            return `
              <button class="admin-article-block-item ${item.id === s.blockId ? "is-selected" : ""}" type="button" data-block="${e(item.id)}">
                <strong>${e(`${index + 1}. ${B[item.type].label}`)}</strong>
                <span>${e(text)}</span>
              </button>
            `;
          })
          .join("")
      : empty("Article chưa có block", "Chọn loại block ở panel bên trái để thêm nội dung.");

    const current = curBlock();
    const groups = current ? B[current.type].groups : [];
    r.editorEmpty.hidden = Boolean(current);
    r.blockType.disabled = !current;
    r.blockType.value = current?.type || "paragraph";
    r.blockTitle.value = current?.title || "";
    r.blockText.value = current?.text || "";
    r.blockIntro.value = current?.intro || "";
    r.blockItems.value = current?.items?.join("\n") || "";
    r.blockUrl.value = current?.url || "";
    r.blockAlt.value = current?.alt || "";
    r.editorGroups.forEach((group) => {
      group.hidden = !groups.includes(group.dataset.articleEditorGroup);
    });

    r.previewMeta.innerHTML = !s.gameId
      ? `<span class="admin-soft-chip">Chưa chọn game</span>`
      : `<span class="admin-soft-chip is-active">${e(curGame()?.name || "Game")}</span><span class="admin-soft-chip">${e(
          `${s.draft.blocks.length} block`
        )}</span><span class="admin-soft-chip">${e(
          curArticle() ? `Lưu lần cuối ${dt(curArticle().updatedAt)}` : "Chưa lưu article"
        )}</span>`;

    r.preview.innerHTML = !s.gameId
      ? empty("Preview đang chờ game", "Chọn game rồi bắt đầu biên tập article JSON.")
      : `<article class="admin-workspace-article-preview">${s.draft.eyebrow ? `<span class="admin-workspace-article-preview__eyebrow">${e(s.draft.eyebrow)}</span>` : ""}<h2>${e(
          s.draft.title || curGame()?.name || "Bài báo game"
        )}</h2>${s.draft.summary ? `<p class="admin-workspace-article-preview__summary">${e(s.draft.summary)}</p>` : ""}${s.draft.blocks.length ? s.draft.blocks.map((item) => item.type === "heading" ? `<h3 class="admin-workspace-preview-heading">${e(item.text)}</h3>` : item.type === "paragraph" ? `<p class="admin-workspace-preview-paragraph">${e(item.text)}</p>` : item.type === "quote" ? `<blockquote class="admin-workspace-preview-quote">${e(item.text)}</blockquote>` : item.type === "image" ? (item.url ? `<figure class="admin-workspace-preview-media"><img src="${e(item.url)}" alt="${e(item.alt || "article image")}" />${item.alt ? `<figcaption>${e(item.alt)}</figcaption>` : ""}</figure>` : `<p class="admin-workspace-preview-paragraph">Block ảnh chưa có URL.</p>`) : item.type === "video" ? (!item.url ? `<p class="admin-workspace-preview-paragraph">Block video chưa có URL.</p>` : isYoutube(item.url) ? `<div class="admin-workspace-preview-media admin-workspace-preview-media--video"><iframe src="${e(
            toYoutubeEmbed(item.url)
          )}" title="${e(item.title || "Video article")}" loading="lazy" allowfullscreen></iframe></div>` : `<div class="admin-workspace-preview-media admin-workspace-preview-media--video"><video controls src="${e(
            item.url
          )}"></video></div>`) : `<div class="admin-workspace-preview-list">${item.intro ? `<p>${e(item.intro)}</p>` : ""}<ul>${item.items.map((entry) => `<li>${e(entry)}</li>`).join("")}</ul></div>`).join("") : `<p class="admin-workspace-article-preview__placeholder">Chưa có block nào để preview.</p>`}</article>`;
  }

  function renderNotes() {
    const currentVersion = s.detail?.versions.find((item) => item.versionId === s.versionId);

    r.versionNote.textContent = !curGame()
      ? "Hãy chọn game trước khi thao tác version."
      : currentVersion
        ? `Đang chỉnh "${versionName(currentVersion)}" của game "${curGame().name}".`
        : `Version đang thao tác cho game "${curGame().name}". Một version có thể gắn nhiều account.`;
    r.fileNote.textContent = !curGame()
      ? "Chọn game rồi gắn account vào version trước khi thao tác file package."
      : !linkedAccs().length
        ? `Game "${curGame().name}" chưa có account gắn qua version.`
        : `Đang quản lý file package cho ${linkedAccs().length} account gắn với "${curGame().name}".`;
    r.mediaNote.textContent = curGame()
      ? `Media đang gắn cho game "${curGame().name}".`
      : "Chọn game trước khi thêm media cho gallery hoặc trailer.";
    r.articleNote.textContent = curGame()
      ? `Article đang biên tập cho "${curGame().name}". Summary lưu riêng, phần layout bài báo nằm trong JSON.`
      : "Chọn game trước khi biên tập article JSON.";
  }

  function renderState() {
    const hasGame = Boolean(s.gameId);
    const hasVersion = Boolean(s.versionId);
    const hasAccount = Boolean(s.accountId);
    const hasFile = Boolean(s.fileId);
    const hasMedia = Boolean(s.mediaId);
    const hasArticle = Boolean(curArticle());
    const hasBlock = Boolean(curBlock());
    const canBlockUpload = hasBlock && ["image", "video"].includes(curBlock().type);
    const canCreateAccount = hasGame && !hasAccount && versions().length > 0;
    const off = busy();

    setPanel("version", !hasGame);
    setPanel("file", !hasGame || !linkedAccs().length);
    setPanel("media", !hasGame);
    setPanel("article", !hasGame);
    setPanel("preview", !hasGame);

    [
      ["newGame", true],
      ["reloadBtn", true],
      ["gameCreate", !hasGame],
      ["gameReset", true],
      ["versionReset", true],
      ["accountCreate", canCreateAccount],
      ["accountReset", true],
      ["fileReset", true],
      ["mediaReset", true],
      ["copyJson", hasGame],
      ["articleSave", hasGame]
    ].forEach(([key, enabled]) => {
      r[key].disabled = off || !enabled;
    });

    [
      ["gameUpdate", hasGame],
      ["gameDelete", hasGame],
      ["versionCreate", hasGame],
      ["versionUpdate", hasVersion],
      ["versionDelete", hasVersion],
      ["accountUpdate", hasAccount],
      ["accountDelete", hasAccount],
      ["fileCreate", hasGame && linkedAccs().length],
      ["fileUpdate", hasFile],
      ["fileDelete", hasFile],
      ["mediaCreate", hasGame],
      ["mediaUpdate", hasMedia],
      ["mediaDelete", hasMedia],
      ["mediaUpload", hasGame],
      ["articleDelete", hasArticle],
      ["blockUp", hasBlock],
      ["blockDown", hasBlock],
      ["blockDup", hasBlock],
      ["blockDel", hasBlock],
      ["blockImg", canBlockUpload],
      ["blockFile", canBlockUpload]
    ].forEach(([key, enabled]) => {
      r[key].disabled = off || !enabled;
    });

    [r.versionName, r.versionRemoved].forEach((item) => {
      item.disabled = off || !hasGame;
    });
    r.versionAccount.disabled = true;
    r.accountVersion.disabled = off || !hasGame || !versions().length;
    r.accountActive.disabled = off || (!hasAccount && !canCreateAccount);
    [r.fileAccount, r.fileType, r.fileActive, ...r.fileUrls, ...r.fileBtns].forEach((item) => {
      item.disabled = off || !hasGame || !linkedAccs().length;
    });
    [r.mediaType, r.mediaUrl].forEach((item) => {
      item.disabled = off || !hasGame;
    });
    [r.articleEyebrow, r.articleTitle, r.articleSummary].forEach((item) => {
      item.disabled = off || !hasGame;
    });
    r.addBlocks.forEach((item) => {
      item.disabled = off || !hasGame;
    });
    [r.blockType, r.blockTitle, r.blockText, r.blockIntro, r.blockItems, r.blockUrl, r.blockAlt].forEach((item) => {
      item.disabled = off || !hasBlock;
    });

    [r.gameCreate, r.gameUpdate, r.gameDelete].forEach((button) => {
      button.classList.remove("is-enabled");
    });

    if (!off) {
      if (hasGame) {
        r.gameUpdate.classList.add("is-enabled");
        r.gameDelete.classList.add("is-enabled");
      } else {
        r.gameCreate.classList.add("is-enabled");
      }
    }
  }
  function gamePayload() {
    return {
      name: trim(r.gameName.value),
      rating: num(r.gameRating.value),
      oldPrice: num(r.gameOldPrice.value),
      newPrice: num(r.gameNewPrice.value),
      categoryIds: [...r.cats.querySelectorAll("input:checked")].map((item) => item.value)
    };
  }

  function clearGameValidation() {
    [r.gameName, r.gameRating, r.gameOldPrice, r.gameNewPrice].forEach((input) => {
      input.setCustomValidity("");
    });
  }

  function showGameValidation(input, message) {
    input.setCustomValidity(message);
    input.reportValidity();
    input.focus();
    msg(message, "error");
    return false;
  }

  function validateGamePayload(payload) {
    clearGameValidation();

    if (!payload.name) {
      return showGameValidation(r.gameName, "Tên game là bắt buộc.");
    }

    if (payload.rating === null) {
      return showGameValidation(r.gameRating, "Rating là bắt buộc.");
    }

    if (payload.rating < 0 || payload.rating > 5) {
      return showGameValidation(r.gameRating, "Rating phải nằm trong khoảng từ 0 đến 5.");
    }

    if (payload.oldPrice === null) {
      return showGameValidation(r.gameOldPrice, "Giá cũ là bắt buộc.");
    }

    if (payload.oldPrice < 0) {
      return showGameValidation(r.gameOldPrice, "Giá cũ phải lớn hơn hoặc bằng 0.");
    }

    if (payload.newPrice === null) {
      return showGameValidation(r.gameNewPrice, "Giá mới là bắt buộc.");
    }

    if (payload.newPrice < 0) {
      return showGameValidation(r.gameNewPrice, "Giá mới phải lớn hơn hoặc bằng 0.");
    }

    return true;
  }

  async function createGameValidated() {
    if (!validateGamePayload(gamePayload())) {
      return;
    }

    await createGame();
  }

  async function updateGameValidated() {
    if (!validateGamePayload(gamePayload())) {
      return;
    }

    await updateGame();
  }

  function versionPayload() {
    return {
      versionName: trim(r.versionName.value),
      isRemoved: bool(r.versionRemoved.value)
    };
  }

  function accountPayload() {
    const current = accs().find((item) => item.accountId === s.accountId);
    return {
      versionId: r.accountVersion.value || null,
      isActive: bool(r.accountActive.value),
      isPurchased: current?.isPurchased ?? false
    };
  }

  function filePayload() {
    return {
      accountId: r.fileAccount.value,
      fileType: trim(r.fileType.value),
      isActive: bool(r.fileActive.value),
      fileUrl01: trim(r.fileUrls[0].value),
      fileUrl02: trim(r.fileUrls[1].value),
      fileUrl03: trim(r.fileUrls[2].value),
      fileUrl04: trim(r.fileUrls[3].value),
      fileUrl05: trim(r.fileUrls[4].value)
    };
  }

  function mediaPayload() {
    return {
      mediaType: trim(r.mediaType.value),
      url: trim(r.mediaUrl.value)
    };
  }

  async function runAction(fallbackMessage, executor) {
    stepBusy(1);
    try {
      await executor();
    } catch (error) {
      msg(error instanceof Error ? error.message : fallbackMessage, "error");
    } finally {
      stepBusy(-1);
    }
  }

  async function createGame() {
    const payload = gamePayload();
    if (!payload.name) {
      msg("Tên game là bắt buộc trước khi tạo record mới.", "error");
      return;
    }

    await runAction("Không thể tạo game.", async () => {
      const response = await req("/api/admin/game-workspace/games", {
        method: "POST",
        body: JSON.stringify(payload)
      });
      await reload({ gameId: response.gameId, message: response.message || `Đã tạo game mới: ${payload.name}.` });
    });
  }

  async function updateGame() {
    if (!s.gameId) {
      msg("Hãy chọn game cần cập nhật trước.", "error");
      return;
    }

    const payload = gamePayload();
    if (!payload.name) {
      msg("Tên game không được để trống.", "error");
      return;
    }

    await runAction("Không thể cập nhật game.", async () => {
      const response = await req(`/api/admin/game-workspace/games/${s.gameId}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      });
      await reload({ gameId: s.gameId, message: response.message || `Đã cập nhật game ${payload.name}.` });
    });
  }

  async function deleteGame(id = s.gameId) {
    const current = games().find((item) => item.gameId === id);
    if (!current) {
      msg("Không tìm thấy game để xóa.", "error");
      return;
    }

    if (!ask(`Xóa game "${current.name}" cùng version, media và article liên quan?`)) {
      return;
    }

    await runAction("Không thể xóa game.", async () => {
      const response = await req(`/api/admin/game-workspace/games/${id}`, { method: "DELETE" });
      await reload({
        gameId: s.gameId === id ? null : s.gameId,
        first: s.gameId === id,
        message: response.message || `Đã xóa game ${current.name}.`
      });
    });
  }

  async function createVersion() {
    if (!s.gameId) {
      msg("Hãy chọn game trước khi tạo version.", "error");
      return;
    }

    await runAction("Không thể tạo version.", async () => {
      if (!trim(r.versionName.value)) {
        msg("Version name là bắt buộc.", "error");
        r.versionName.focus();
        return;
      }

      const response = await req(`/api/admin/game-workspace/games/${s.gameId}/versions`, {
        method: "POST",
        body: JSON.stringify(versionPayload())
      });
      s.versionId = response.versionId || null;
      await reload({ gameId: s.gameId, message: response.message || "Đã tạo version mới." });
    });
  }

  async function updateVersion() {
    if (!s.versionId) {
      msg("Hãy chọn version cần cập nhật trước.", "error");
      return;
    }

    await runAction("Không thể cập nhật version.", async () => {
      if (!trim(r.versionName.value)) {
        msg("Version name là bắt buộc.", "error");
        r.versionName.focus();
        return;
      }

      const response = await req(`/api/admin/game-workspace/versions/${s.versionId}`, {
        method: "PUT",
        body: JSON.stringify(versionPayload())
      });
      await reload({ gameId: s.gameId, message: response.message || "Đã cập nhật version." });
    });
  }

  async function deleteVersion(id = s.versionId) {
    if (!id) {
      msg("Hãy chọn version cần xóa trước.", "error");
      return;
    }

    if (!ask(`Xóa version ${short(id)}?`)) {
      return;
    }

    await runAction("Không thể xóa version.", async () => {
      const response = await req(`/api/admin/game-workspace/versions/${id}`, { method: "DELETE" });
      s.versionId = null;
      await reload({ gameId: s.gameId, message: response.message || "Đã xóa version." });
    });
  }

  async function createAccount() {
    if (!s.gameId) {
      msg("Hãy chọn game trước khi tạo account.", "error");
      return;
    }

    if (!versions().length) {
      msg("Hãy tạo version trước rồi mới tạo account.", "error");
      return;
    }

    if (!r.accountVersion.value) {
      msg("Hãy chọn version để liên kết account.", "error");
      r.accountVersion.focus();
      return;
    }

    await runAction("Không thể tạo account.", async () => {
      const response = await req("/api/admin/game-workspace/accounts", {
        method: "POST",
        body: JSON.stringify(accountPayload())
      });
      s.accountId = response.accountId || null;
      await reload({ gameId: s.gameId, message: response.message || "Đã tạo account mới." });
    });
  }

  async function updateAccount() {
    if (!s.accountId) {
      msg("Hãy chọn account cần cập nhật trước.", "error");
      return;
    }

    await runAction("Không thể cập nhật account.", async () => {
      const response = await req(`/api/admin/game-workspace/accounts/${s.accountId}`, {
        method: "PUT",
        body: JSON.stringify(accountPayload())
      });
      await reload({ gameId: s.gameId, message: response.message || "Đã cập nhật account." });
    });
  }

  async function deleteAccount(id = s.accountId) {
    if (!id) {
      msg("Hãy chọn account cần xóa trước.", "error");
      return;
    }

    if (!ask(`Xóa account ${short(id)} và file liên quan?`)) {
      return;
    }

    await runAction("Không thể xóa account.", async () => {
      const response = await req(`/api/admin/game-workspace/accounts/${id}`, { method: "DELETE" });
      s.accountId = null;
      await reload({ gameId: s.gameId, message: response.message || "Đã xóa account." });
    });
  }

  async function createFile() {
    if (!s.gameId) {
      msg("Hãy chọn game trước khi tạo file package.", "error");
      return;
    }

    const payload = filePayload();
    if (!payload.accountId) {
      msg("Hãy chọn account hợp lệ cho file package.", "error");
      return;
    }

    await runAction("Không thể tạo file package.", async () => {
      const response = await req("/api/admin/game-workspace/files", {
        method: "POST",
        body: JSON.stringify(payload)
      });
      s.fileId = response.fileId || null;
      await reload({ gameId: s.gameId, message: response.message || "Đã tạo file package mới." });
    });
  }

  async function updateFile() {
    if (!s.fileId) {
      msg("Hãy chọn file package cần cập nhật trước.", "error");
      return;
    }

    const payload = filePayload();
    if (!payload.accountId) {
      msg("Hãy chọn account hợp lệ cho file package.", "error");
      return;
    }

    await runAction("Không thể cập nhật file package.", async () => {
      const response = await req(`/api/admin/game-workspace/files/${s.fileId}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      });
      await reload({ gameId: s.gameId, message: response.message || "Đã cập nhật file package." });
    });
  }

  async function deleteFile(id = s.fileId) {
    if (!id) {
      msg("Hãy chọn file package cần xóa trước.", "error");
      return;
    }

    if (!ask(`Xóa file package ${short(id)}?`)) {
      return;
    }

    await runAction("Không thể xóa file package.", async () => {
      const response = await req(`/api/admin/game-workspace/files/${id}`, { method: "DELETE" });
      s.fileId = null;
      await reload({ gameId: s.gameId, message: response.message || "Đã xóa file package." });
    });
  }

  async function createMedia() {
    if (!s.gameId) {
      msg("Hãy chọn game trước khi tạo media.", "error");
      return;
    }

    const payload = mediaPayload();
    if (!payload.url) {
      msg("URL media là bắt buộc.", "error");
      return;
    }

    await runAction("Không thể tạo media.", async () => {
      const response = await req(`/api/admin/game-workspace/games/${s.gameId}/media`, {
        method: "POST",
        body: JSON.stringify(payload)
      });
      s.mediaId = response.mediaId || null;
      await reload({ gameId: s.gameId, message: response.message || "Đã tạo media mới." });
    });
  }

  async function updateMedia() {
    if (!s.mediaId) {
      msg("Hãy chọn media cần cập nhật trước.", "error");
      return;
    }

    const payload = mediaPayload();
    if (!payload.url) {
      msg("URL media là bắt buộc.", "error");
      return;
    }

    await runAction("Không thể cập nhật media.", async () => {
      const response = await req(`/api/admin/game-workspace/media/${s.mediaId}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      });
      await reload({ gameId: s.gameId, message: response.message || "Đã cập nhật media." });
    });
  }

  async function deleteMedia(id = s.mediaId) {
    if (!id) {
      msg("Hãy chọn media cần xóa trước.", "error");
      return;
    }

    if (!ask(`Xóa media ${short(id)}?`)) {
      return;
    }

    await runAction("Không thể xóa media.", async () => {
      const response = await req(`/api/admin/game-workspace/media/${id}`, { method: "DELETE" });
      s.mediaId = null;
      await reload({ gameId: s.gameId, message: response.message || "Đã xóa media." });
    });
  }
  async function saveArticle() {
    if (!s.gameId) {
      msg("Hãy chọn game trước khi lưu article.", "error");
      return;
    }

    await runAction("Không thể lưu article.", async () => {
      const response = await req(`/api/admin/game-workspace/games/${s.gameId}/article`, {
        method: "PUT",
        body: JSON.stringify({
          summary: trim(s.draft.summary),
          contentJson: draftJson()
        })
      });
      await reload({ gameId: s.gameId, message: response.message || "Đã lưu article." });
    });
  }

  async function deleteArticle() {
    if (!curArticle()) {
      msg("Game hiện tại chưa có article để xóa.", "error");
      return;
    }

    if (!ask("Xóa article của game hiện tại?")) {
      return;
    }

    await runAction("Không thể xóa article.", async () => {
      const response = await req(`/api/admin/game-workspace/articles/${curArticle().articleId}`, {
        method: "DELETE"
      });
      await reload({ gameId: s.gameId, message: response.message || "Đã xóa article." });
    });
  }

  async function copyJson() {
    try {
      await navigator.clipboard.writeText(r.articleJson.value);
      msg("Đã sao chép content_json vào clipboard.", "success");
    } catch {
      msg("Không thể sao chép content_json trên trình duyệt này.", "error");
    }
  }

  function resetGame() {
    enterCreateMode("Đã xóa hết nội dung input trong form game.");
  }

  function resetVersion() {
    s.versionId = null;
    renderVersionPanel();
    renderNotes();
    renderState();
    msg("Form version đã được làm mới.", "muted");
  }

  function resetAccount() {
    s.accountId = null;
    renderAccountPanel();
    renderState();
    msg("Form account đã được làm mới.", "muted");
  }

  function resetFile() {
    s.fileId = null;
    renderFilePanel();
    renderState();
    msg("Form file package đã được làm mới.", "muted");
  }

  function resetMedia() {
    s.mediaId = null;
    renderMediaPanel();
    renderState();
    msg("Form media đã được làm mới.", "muted");
  }

  function updateDraft(key, value) {
    s.draft[key] = value;
    renderArticlePanel();
    renderState();
  }

  function addBlock(type) {
    if (!s.gameId) {
      msg("Hãy chọn game trước khi thêm block cho article.", "error");
      return;
    }

    const item = block({ type });
    s.draft.blocks.push(item);
    s.blockId = item.id;
    renderArticlePanel();
    renderState();
  }

  function selectBlock(id) {
    s.blockId = id;
    renderArticlePanel();
    renderState();
  }

  function editBlock(key, value) {
    const current = curBlock();
    if (!current) {
      return;
    }

    current[key] = key === "items"
      ? String(value)
          .split(/\r?\n/)
          .map(trim)
          .filter(Boolean)
      : value;

    renderArticlePanel();
  }

  function changeBlockType(type) {
    const current = curBlock();
    if (!current) {
      return;
    }

    const index = s.draft.blocks.findIndex((item) => item.id === current.id);
    s.draft.blocks[index] = block({ ...current, type });
    renderArticlePanel();
    renderState();
  }

  function moveBlock(step) {
    const index = s.draft.blocks.findIndex((item) => item.id === s.blockId);
    const nextIndex = index + step;
    if (index < 0 || nextIndex < 0 || nextIndex >= s.draft.blocks.length) {
      return;
    }

    const [item] = s.draft.blocks.splice(index, 1);
    s.draft.blocks.splice(nextIndex, 0, item);
    renderArticlePanel();
  }

  function duplicateBlock() {
    const current = curBlock();
    if (!current) {
      return;
    }

    const index = s.draft.blocks.findIndex((item) => item.id === current.id);
    const copy = block(JSON.parse(JSON.stringify({ ...current, id: gid() })));
    s.draft.blocks.splice(index + 1, 0, copy);
    s.blockId = copy.id;
    renderArticlePanel();
    renderState();
  }

  function deleteBlock() {
    const index = s.draft.blocks.findIndex((item) => item.id === s.blockId);
    if (index < 0) {
      return;
    }

    s.draft.blocks.splice(index, 1);
    s.blockId = s.draft.blocks[index]?.id || s.draft.blocks[index - 1]?.id || null;
    renderArticlePanel();
    renderState();
  }

  async function uploadFileSlot(index) {
    const file = await pick("*/*");
    if (!file) {
      return;
    }

    await runAction("Không thể upload file local.", async () => {
      const response = await upload("/api/admin/game-workspace/uploads/file", file);
      r.fileUrls[index - 1].value = response.url;
      msg(`Đã upload file local vào URL 0${index}.`, "success");
    });
  }

  async function uploadMediaLocal() {
    const file = await pick("image/*,video/*");
    if (!file) {
      return;
    }

    await runAction("Không thể upload media local.", async () => {
      const response = await upload(
        file.type.startsWith("image/")
          ? "/api/admin/game-workspace/uploads/image"
          : "/api/admin/game-workspace/uploads/file",
        file
      );
      r.mediaUrl.value = response.url;
      if (!trim(r.mediaType.value)) {
        r.mediaType.value = file.type.startsWith("image/") ? "gallery" : "trailer";
      }
      msg("Đã upload media local và điền URL vào form.", "success");
    });
  }

  async function uploadBlockAsset(mode) {
    const current = curBlock();
    if (!current) {
      msg("Hãy chọn block article trước khi upload asset.", "error");
      return;
    }

    const file = await pick(mode === "image" ? "image/*" : "video/*,*/*");
    if (!file) {
      return;
    }

    await runAction("Không thể upload asset article.", async () => {
      const response = await upload(
        mode === "image"
          ? "/api/admin/game-workspace/uploads/image"
          : "/api/admin/game-workspace/uploads/file",
        file
      );
      current.url = response.url;
      if (mode === "image" && !trim(current.alt)) {
        current.alt = file.name;
      }
      if (mode === "file" && current.type === "video" && !trim(current.title)) {
        current.title = file.name;
      }
      renderArticlePanel();
      msg("Đã upload asset local cho block article.", "success");
    });
  }

  r.newGame.addEventListener("click", () => {
    enterCreateMode("Form game đã trở về chế độ tạo mới.");
  });

  r.reloadBtn.addEventListener("click", () => {
    reload({ gameId: s.gameId, message: "Đã tải lại dữ liệu workspace mới nhất từ API." });
  });

  r.search.addEventListener("input", renderGameListOnly);
  r.filterCat.addEventListener("change", renderGameListOnly);
  [r.gameName, r.gameRating, r.gameOldPrice, r.gameNewPrice].forEach((item) => {
    item.addEventListener("input", () => {
      item.setCustomValidity("");
      refreshGameDraftUi();
    });
  });
  r.cats.addEventListener("change", refreshGameDraftUi);

  r.gameCreate.addEventListener("click", createGameValidated);
  r.gameUpdate.addEventListener("click", updateGameValidated);
  r.gameDelete.addEventListener("click", () => deleteGame());
  r.gameReset.addEventListener("click", () => {
    clearGameValidation();
    resetGame();
  });

  r.versionCreate.addEventListener("click", createVersion);
  r.versionUpdate.addEventListener("click", updateVersion);
  r.versionDelete.addEventListener("click", () => deleteVersion());
  r.versionReset.addEventListener("click", resetVersion);

  r.accountCreate.addEventListener("click", createAccount);
  r.accountUpdate.addEventListener("click", updateAccount);
  r.accountDelete.addEventListener("click", () => deleteAccount());
  r.accountReset.addEventListener("click", resetAccount);

  r.fileCreate.addEventListener("click", createFile);
  r.fileUpdate.addEventListener("click", updateFile);
  r.fileDelete.addEventListener("click", () => deleteFile());
  r.fileReset.addEventListener("click", resetFile);
  r.fileBtns.forEach((button) => {
    button.addEventListener("click", () => uploadFileSlot(Number(button.dataset.fileUpload)));
  });

  r.mediaCreate.addEventListener("click", createMedia);
  r.mediaUpdate.addEventListener("click", updateMedia);
  r.mediaDelete.addEventListener("click", () => deleteMedia());
  r.mediaReset.addEventListener("click", resetMedia);
  r.mediaUpload.addEventListener("click", uploadMediaLocal);

  r.articleEyebrow.addEventListener("input", (event) => updateDraft("eyebrow", event.target.value));
  r.articleTitle.addEventListener("input", (event) => updateDraft("title", event.target.value));
  r.articleSummary.addEventListener("input", (event) => updateDraft("summary", event.target.value));
  r.addBlocks.forEach((button) => {
    button.addEventListener("click", () => addBlock(button.dataset.articleAddBlock));
  });
  r.blockList.addEventListener("click", (event) => {
    const button = event.target.closest("[data-block]");
    if (button) {
      selectBlock(button.dataset.block);
    }
  });

  r.blockType.addEventListener("change", (event) => changeBlockType(event.target.value));
  r.blockTitle.addEventListener("input", (event) => editBlock("title", event.target.value));
  r.blockText.addEventListener("input", (event) => editBlock("text", event.target.value));
  r.blockIntro.addEventListener("input", (event) => editBlock("intro", event.target.value));
  r.blockItems.addEventListener("input", (event) => editBlock("items", event.target.value));
  r.blockUrl.addEventListener("input", (event) => editBlock("url", event.target.value));
  r.blockAlt.addEventListener("input", (event) => editBlock("alt", event.target.value));
  r.blockUp.addEventListener("click", () => moveBlock(-1));
  r.blockDown.addEventListener("click", () => moveBlock(1));
  r.blockDup.addEventListener("click", duplicateBlock);
  r.blockDel.addEventListener("click", deleteBlock);
  r.blockImg.addEventListener("click", () => uploadBlockAsset("image"));
  r.blockFile.addEventListener("click", () => uploadBlockAsset("file"));
  r.copyJson.addEventListener("click", copyJson);
  r.articleDelete.addEventListener("click", deleteArticle);
  r.articleSave.addEventListener("click", saveArticle);

  r.gameList.addEventListener("click", (event) => {
    const open = event.target.closest("[data-game]");
    const remove = event.target.closest("[data-game-del]");
    if (open) {
      openGame(open.dataset.game);
    }
    if (remove) {
      deleteGame(remove.dataset.gameDel);
    }
  });

  r.versionList.addEventListener("click", (event) => {
    const edit = event.target.closest("[data-version]");
    const remove = event.target.closest("[data-version-del]");
    if (edit) {
      s.versionId = edit.dataset.version;
      renderVersionPanel();
      renderNotes();
      renderState();
    }
    if (remove) {
      deleteVersion(remove.dataset.versionDel);
    }
  });

  r.accountList.addEventListener("click", (event) => {
    const edit = event.target.closest("[data-account]");
    const remove = event.target.closest("[data-account-del]");
    if (edit) {
      s.accountId = edit.dataset.account;
      renderAccountPanel();
      renderState();
    }
    if (remove) {
      deleteAccount(remove.dataset.accountDel);
    }
  });

  r.fileList.addEventListener("click", (event) => {
    const edit = event.target.closest("[data-file]");
    const remove = event.target.closest("[data-file-del]");
    if (edit) {
      s.fileId = edit.dataset.file;
      renderFilePanel();
      renderState();
    }
    if (remove) {
      deleteFile(remove.dataset.fileDel);
    }
  });

  r.mediaList.addEventListener("click", (event) => {
    const edit = event.target.closest("[data-media]");
    const remove = event.target.closest("[data-media-del]");
    if (edit) {
      s.mediaId = edit.dataset.media;
      renderMediaPanel();
      renderState();
    }
    if (remove) {
      deleteMedia(remove.dataset.mediaDel);
    }
  });

  reload({ first: true });
})();
