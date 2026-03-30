(function () {
  const root = document.querySelector("[data-category-crud-root]");
  if (!root) {
    return;
  }

  const state = {
    items: [],
    filteredItems: [],
    selectedCategoryId: null,
    summary: null,
    isBusy: false,
  };

  const formPanel = document.querySelector("[data-category-form-panel]");
  const selectionBanner = document.querySelector("[data-category-selection]");
  const selectedName = document.querySelector("[data-category-selected-name]");
  const feedback = document.querySelector("[data-category-feedback]");
  const listFeedback = document.querySelector("[data-category-list-feedback]");
  const listContainer = document.querySelector("[data-category-list]");
  const recordCount = document.querySelector("[data-category-record-count]");
  const modeBadge = document.querySelector("[data-category-mode-badge]");
  const modeLabel = document.querySelector("[data-category-mode-label]");
  const syncLabel = document.querySelector("[data-category-sync-label]");

  const summaryTotal = document.querySelector("[data-summary-total]");
  const summaryPublished = document.querySelector("[data-summary-published]");
  const summaryDraft = document.querySelector("[data-summary-draft]");
  const summaryUpdated = document.querySelector("[data-summary-updated]");
  const summaryNextOrder = document.querySelector("[data-summary-next-order]");

  const filterKeyword = document.querySelector("[data-category-filter='keyword']");
  const filterStatus = document.querySelector("[data-category-filter='status']");

  const nameInput = document.querySelector("[data-category-input='name']");
  const slugInput = document.querySelector("[data-category-input='slug']");
  const statusInput = document.querySelector("[data-category-input='status']");
  const displayOrderInput = document.querySelector("[data-category-input='displayOrder']");
  const descriptionInput = document.querySelector("[data-category-input='shortDescription']");

  const createButton = document.querySelector("[data-category-create]");
  const updateButton = document.querySelector("[data-category-update]");
  const deleteButton = document.querySelector("[data-category-delete]");
  const resetButton = document.querySelector("[data-category-reset]");
  const clearSelectionButton = document.querySelector("[data-category-clear-selection]");

  const recordTemplate = document.getElementById("adminCategoryRecordTemplate");

  function setFeedback(target, message, tone) {
    if (!target) {
      return;
    }

    if (!message) {
      target.hidden = true;
      target.textContent = "";
      target.classList.remove("is-success", "is-error", "is-muted");
      return;
    }

    target.hidden = false;
    target.textContent = message;
    target.classList.remove("is-success", "is-error", "is-muted");
    target.classList.add(tone || "is-muted");
  }

  function setBusy(isBusy) {
    state.isBusy = isBusy;
    [createButton, updateButton, deleteButton, resetButton, clearSelectionButton]
      .filter(Boolean)
      .forEach((button) => {
        if (!button) {
          return;
        }

        const shouldDisable =
          isBusy ||
          (button === updateButton || button === deleteButton ? !state.selectedCategoryId : false);

        button.disabled = shouldDisable;
      });
  }

  function highlightActionButtons() {
    [updateButton, deleteButton].forEach((button) => {
      if (!button) {
        return;
      }

      button.classList.remove("is-enabled");
      window.requestAnimationFrame(() => button.classList.add("is-enabled"));
      window.setTimeout(() => button.classList.remove("is-enabled"), 360);
    });
  }

  function animateFormPanel() {
    if (!formPanel) {
      return;
    }

    formPanel.classList.remove("is-switching");
    window.requestAnimationFrame(() => {
      formPanel.classList.add("is-switching");
      window.setTimeout(() => formPanel.classList.remove("is-switching"), 260);
    });
  }

  function getDefaultDisplayOrder() {
    return state.summary?.nextDisplayOrder ?? 1;
  }

  function resetFormFields() {
    nameInput.value = "";
    slugInput.value = "";
    statusInput.value = "Published";
    displayOrderInput.value = String(getDefaultDisplayOrder());
    descriptionInput.value = "";
  }

  function updateModeUi() {
    const selectedItem = state.items.find((item) => item.categoryId === state.selectedCategoryId) || null;
    const isEditing = Boolean(selectedItem);

    if (selectionBanner) {
      selectionBanner.hidden = !isEditing;
    }

    if (selectedName) {
      selectedName.textContent = selectedItem?.name || "Chưa chọn";
    }

    if (modeBadge) {
      modeBadge.textContent = isEditing ? "Chỉnh sửa" : "Tạo mới";
    }

    if (modeLabel) {
      modeLabel.textContent = isEditing
        ? `Đang sửa: ${selectedItem?.name ?? ""}`
        : "Tạo mới category";
    }

    if (syncLabel && state.summary) {
      syncLabel.textContent = `Đã đồng bộ ${formatDateTime(state.summary.lastUpdatedAt)}`;
    }

    if (updateButton) {
      updateButton.disabled = state.isBusy || !isEditing;
    }

    if (deleteButton) {
      deleteButton.disabled = state.isBusy || !isEditing;
    }

    renderRecordSelection();
  }

  function renderSummary() {
    if (!state.summary) {
      return;
    }

    summaryTotal.textContent = String(state.summary.totalCategories);
    summaryPublished.textContent = String(state.summary.publishedCategories);
    summaryDraft.textContent = String(state.summary.draftCategories);
    summaryUpdated.textContent = formatDateTime(state.summary.lastUpdatedAt);
    summaryNextOrder.textContent = `Thứ tự tiếp theo: #${padNumber(state.summary.nextDisplayOrder)}`;
  }

  function applyFilters() {
    const keyword = (filterKeyword?.value || "").trim().toLowerCase();
    const status = filterStatus?.value || "all";

    state.filteredItems = state.items.filter((item) => {
      const matchKeyword =
        keyword.length === 0 ||
        item.name.toLowerCase().includes(keyword) ||
        item.slug.toLowerCase().includes(keyword) ||
        (item.shortDescription || "").toLowerCase().includes(keyword);

      const matchStatus = status === "all" || item.status === status;
      return matchKeyword && matchStatus;
    });
  }

  function renderEmptyState() {
    listContainer.innerHTML = `
      <article class="admin-category-empty">
        <strong>Không có category phù hợp</strong>
        <span>Hãy đổi bộ lọc hoặc tạo category mới từ form bên trái.</span>
      </article>
    `;
  }

  function renderRecordSelection() {
    const records = listContainer.querySelectorAll("[data-category-record]");
    records.forEach((record) => {
      record.classList.toggle("is-selected", record.dataset.categoryId === state.selectedCategoryId);
    });
  }

  function renderList() {
    applyFilters();
    recordCount.textContent = `${state.filteredItems.length} record`;

    if (!state.filteredItems.length) {
      renderEmptyState();
      return;
    }

    listContainer.innerHTML = "";

    state.filteredItems.forEach((item) => {
      const fragment = recordTemplate.content.cloneNode(true);
      const record = fragment.querySelector("[data-category-record]");
      const name = fragment.querySelector("[data-category-record-name]");
      const status = fragment.querySelector("[data-category-record-status]");
      const slug = fragment.querySelector("[data-category-record-slug]");
      const order = fragment.querySelector("[data-category-record-order]");
      const count = fragment.querySelector("[data-category-record-count]");
      const updated = fragment.querySelector("[data-category-record-updated]");
      const description = fragment.querySelector("[data-category-record-description]");
      const editButton = fragment.querySelector("[data-category-action='edit']");
      const deleteActionButton = fragment.querySelector("[data-category-action='delete']");

      record.dataset.categoryId = item.categoryId;
      name.textContent = item.name;
      slug.textContent = `/${item.slug || "(chưa có slug)"}`;
      order.textContent = `Order: #${padNumber(item.displayOrder)}`;
      count.textContent = `${item.gameCount} game`;
      updated.textContent = `Cập nhật: ${formatDateTime(item.updatedAt, true)}`;
      description.textContent = item.shortDescription || "Chưa có mô tả ngắn.";

      status.textContent = item.status;
      status.classList.add(item.status === "Published" ? "is-ok" : "is-check");

      editButton.addEventListener("click", () => {
        startEdit(item.categoryId, true);
      });

      deleteActionButton.addEventListener("click", async () => {
        startEdit(item.categoryId, false);
        await deleteSelectedCategory();
      });

      listContainer.appendChild(fragment);
    });

    renderRecordSelection();
  }

  function populateForm(item) {
    nameInput.value = item.name;
    slugInput.value = item.slug;
    statusInput.value = item.status;
    displayOrderInput.value = String(item.displayOrder);
    descriptionInput.value = item.shortDescription || "";
  }

  function startEdit(categoryId, shouldScroll) {
    const item = state.items.find((entry) => entry.categoryId === categoryId);
    if (!item) {
      return;
    }

    state.selectedCategoryId = categoryId;
    populateForm(item);
    updateModeUi();
    animateFormPanel();
    highlightActionButtons();
    setFeedback(feedback, `Đang chỉnh sửa category "${item.name}".`, "is-muted");

    if (shouldScroll) {
      formPanel?.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }

  function clearSelection() {
    state.selectedCategoryId = null;
    resetFormFields();
    updateModeUi();
    setFeedback(feedback, "Form đã trở về chế độ tạo mới.", "is-muted");
  }

  function readFormPayload() {
    return {
      name: nameInput.value.trim(),
      slug: slugInput.value.trim(),
      status: statusInput.value,
      displayOrder: Number.parseInt(displayOrderInput.value || "0", 10),
      shortDescription: descriptionInput.value.trim(),
    };
  }

  function validatePayload(payload) {
    if (!payload.name) {
      return "Tên category không được để trống.";
    }

    if (Number.isNaN(payload.displayOrder) || payload.displayOrder < 0) {
      return "Thứ tự hiển thị phải là số nguyên từ 0 trở lên.";
    }

    return null;
  }

  async function requestJson(url, options) {
    const response = await fetch(url, {
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
      },
      ...options,
    });

    const contentType = response.headers.get("content-type") || "";
    const data = contentType.includes("application/json") ? await response.json() : null;

    if (!response.ok) {
      throw new Error(data?.message || "Không thể xử lý yêu cầu category.");
    }

    return data;
  }

  async function loadCategories(showMessage) {
    setBusy(true);
    try {
      const data = await requestJson("/api/admin/categories", {
        method: "GET",
      });

      state.summary = data.summary;
      state.items = data.items;

      if (
        state.selectedCategoryId &&
        !state.items.some((item) => item.categoryId === state.selectedCategoryId)
      ) {
        state.selectedCategoryId = null;
      }

      renderSummary();
      renderList();
      updateModeUi();

      if (!state.selectedCategoryId) {
        resetFormFields();
      }

      if (showMessage) {
        setFeedback(listFeedback, "Danh sách category đã được làm mới từ API.", "is-success");
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : "Không thể tải danh sách category.";
      setFeedback(listFeedback, message, "is-error");
    } finally {
      setBusy(false);
    }
  }

  async function createCategory() {
    const payload = readFormPayload();
    const validationMessage = validatePayload(payload);
    if (validationMessage) {
      setFeedback(feedback, validationMessage, "is-error");
      return;
    }

    setBusy(true);
    try {
      await requestJson("/api/admin/categories", {
        method: "POST",
        body: JSON.stringify(payload),
      });

      state.selectedCategoryId = null;
      setFeedback(feedback, `Đã thêm category "${payload.name}".`, "is-success");
      await loadCategories(true);
      resetFormFields();
    } catch (error) {
      const message = error instanceof Error ? error.message : "Không thể thêm category.";
      setFeedback(feedback, message, "is-error");
    } finally {
      setBusy(false);
    }
  }

  async function updateCategory() {
    if (!state.selectedCategoryId) {
      setFeedback(feedback, "Hãy chọn category cần sửa trước khi cập nhật.", "is-error");
      return;
    }

    const payload = readFormPayload();
    const validationMessage = validatePayload(payload);
    if (validationMessage) {
      setFeedback(feedback, validationMessage, "is-error");
      return;
    }

    setBusy(true);
    try {
      await requestJson(`/api/admin/categories/${state.selectedCategoryId}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });

      setFeedback(feedback, `Đã cập nhật category "${payload.name}".`, "is-success");
      await loadCategories(true);

      const updatedItem = state.items.find((item) => item.categoryId === state.selectedCategoryId);
      if (updatedItem) {
        populateForm(updatedItem);
        updateModeUi();
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : "Không thể cập nhật category.";
      setFeedback(feedback, message, "is-error");
    } finally {
      setBusy(false);
    }
  }

  async function deleteSelectedCategory() {
    if (!state.selectedCategoryId) {
      setFeedback(feedback, "Hãy chọn category cần xóa trước.", "is-error");
      return;
    }

    const selectedItem = state.items.find((item) => item.categoryId === state.selectedCategoryId);
    if (!selectedItem) {
      setFeedback(feedback, "Không tìm thấy category đang chọn.", "is-error");
      return;
    }

    const isConfirmed = window.confirm(`Bạn có chắc muốn xóa category "${selectedItem.name}" không?`);
    if (!isConfirmed) {
      return;
    }

    setBusy(true);
    try {
      await requestJson(`/api/admin/categories/${state.selectedCategoryId}`, {
        method: "DELETE",
      });

      setFeedback(feedback, `Đã xóa category "${selectedItem.name}".`, "is-success");
      state.selectedCategoryId = null;
      await loadCategories(true);
      resetFormFields();
    } catch (error) {
      const message = error instanceof Error ? error.message : "Không thể xóa category.";
      setFeedback(feedback, message, "is-error");
    } finally {
      setBusy(false);
    }
  }

  function formatDateTime(value, short = false) {
    if (!value) {
      return short ? "--" : "Chưa có dữ liệu";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return short ? "--" : "Chưa có dữ liệu";
    }

    return new Intl.DateTimeFormat("vi-VN", {
      hour: "2-digit",
      minute: "2-digit",
      day: "2-digit",
      month: "2-digit",
      year: short ? undefined : "numeric",
    }).format(date);
  }

  function padNumber(value) {
    return String(value).padStart(2, "0");
  }

  filterKeyword?.addEventListener("input", () => {
    renderList();
  });

  filterStatus?.addEventListener("change", () => {
    renderList();
  });

  createButton?.addEventListener("click", createCategory);
  updateButton?.addEventListener("click", updateCategory);
  deleteButton?.addEventListener("click", deleteSelectedCategory);

  resetButton?.addEventListener("click", () => {
    resetFormFields();
    setFeedback(feedback, "Đã làm mới nội dung input trong form category.", "is-muted");
  });

  clearSelectionButton?.addEventListener("click", clearSelection);

  if (root.dataset.startMode === "create") {
    setFeedback(feedback, "Bạn đang ở chế độ tạo category mới.", "is-muted");
  }

  loadCategories(false);
})();
