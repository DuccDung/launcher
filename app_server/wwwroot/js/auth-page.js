const searchParams = new URLSearchParams(window.location.search);

const authState = {
  overlayStage: null,
  verifyEmail: "",
  resetEmail: "",
  redirectUrl: searchParams.get("returnUrl") || document.body.dataset.authRedirect || "/",
};

const overlay = document.getElementById("authFlowOverlay");
const overlayStages = Array.from(document.querySelectorAll("[data-auth-stage]"));
const openButtons = Array.from(document.querySelectorAll("[data-auth-open]"));
const closeButtons = Array.from(document.querySelectorAll("[data-auth-close]"));
const focusButtons = Array.from(document.querySelectorAll("[data-auth-focus]"));
const viewButtons = Array.from(document.querySelectorAll("[data-auth-view]"));
const globalBadge = document.getElementById("authGlobalBadge");
const mobileLayout = window.matchMedia("(max-width: 991.98px)");

const loginCard = document.getElementById("authLoginCard");
const registerCard = document.getElementById("authRegisterCard");

const loginForm = document.getElementById("loginForm");
const registerForm = document.getElementById("registerForm");
const verifyOtpForm = document.getElementById("verifyOtpForm");
const forgotPasswordForm = document.getElementById("forgotPasswordForm");
const resetPasswordForm = document.getElementById("resetPasswordForm");

const loginMessage = document.getElementById("loginMessage");
const registerMessage = document.getElementById("registerMessage");
const verifyMessage = document.getElementById("verifyMessage");
const forgotMessage = document.getElementById("forgotMessage");
const resetMessage = document.getElementById("resetMessage");

const verifyEmailInput = document.getElementById("verifyEmail");
const verifyEmailText = document.getElementById("verifyEmailText");
const verifyExpiryText = document.getElementById("verifyExpiryText");
const resendOtpButton = document.getElementById("resendOtpButton");

const resetEmailInput = document.getElementById("resetEmail");
const resetEmailText = document.getElementById("resetEmailText");
const resetExpiryText = document.getElementById("resetExpiryText");
const resetPasswordBlock = document.getElementById("resetPasswordBlock");
const resendResetOtpButton = document.getElementById("resendResetOtpButton");
const passwordToggles = Array.from(document.querySelectorAll("[data-password-toggle]"));

function setBadge(label) {
  const span = globalBadge?.querySelector("span");
  if (span) {
    span.textContent = label;
  }
}

function clearMessage(element) {
  if (!element) return;
  element.hidden = true;
  element.textContent = "";
  element.classList.remove("is-success", "is-error");
}

function setMessage(element, message, isSuccess) {
  if (!element) return;
  element.hidden = false;
  element.textContent = message;
  element.classList.remove("is-success", "is-error");
  element.classList.add(isSuccess ? "is-success" : "is-error");
}

function clearAllMessages() {
  [loginMessage, registerMessage, verifyMessage, forgotMessage, resetMessage].forEach(
    clearMessage
  );
}

function focusFirstField(container) {
  const field = container?.querySelector("input:not([type='hidden']), button");
  if (!field) return;

  window.requestAnimationFrame(() => {
    field.focus({ preventScroll: true });
  });
}

function setCardView(target) {
  const activeTarget = target === "register" ? "register" : "login";

  if (loginCard) {
    loginCard.classList.toggle("is-active", activeTarget === "login");
  }

  if (registerCard) {
    registerCard.classList.toggle("is-active", activeTarget === "register");
  }

  viewButtons.forEach((button) => {
    const isActive = button.dataset.authView === activeTarget;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", isActive ? "true" : "false");
  });
}

function focusCard(target, { shouldScroll = true, shouldFocus = true } = {}) {
  setCardView(target);

  const card = target === "register" ? registerCard : loginCard;
  if (!card) return;

  if (shouldScroll) {
    card.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  if (shouldFocus) {
    focusFirstField(card);
  }
}

function openOverlay(stage) {
  authState.overlayStage = stage;
  clearAllMessages();

  overlayStages.forEach((panel) => {
    panel.classList.toggle("is-active", panel.dataset.authStage === stage);
  });

  if (overlay) {
    overlay.hidden = false;
  }

  document.body.classList.add("auth-flow-open");

  const labels = {
    verify: "Đang chờ OTP",
    forgot: "Khôi phục mật khẩu",
  };

  setBadge(labels[stage] || "Sẵn sàng");
  focusFirstField(overlayStages.find((panel) => panel.dataset.authStage === stage));
}

function closeOverlay() {
  authState.overlayStage = null;

  overlayStages.forEach((panel) => {
    panel.classList.remove("is-active");
  });

  if (overlay) {
    overlay.hidden = true;
  }

  document.body.classList.remove("auth-flow-open");
  setBadge("Sẵn sàng");
}

async function parseJson(response) {
  const contentType = response.headers.get("content-type") || "";
  if (!contentType.includes("application/json")) {
    return null;
  }

  return response.json();
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  const data = await parseJson(response);
  return { response, data };
}

function normalizeError(data, fallback) {
  return data?.message || fallback;
}

function isChallengePayload(data) {
  return Boolean(data && typeof data.email === "string" && typeof data.purpose === "string");
}

function describeExpiry(expiresAtUtc) {
  const date = new Date(expiresAtUtc);
  if (Number.isNaN(date.getTime())) {
    return "OTP có hiệu lực trong thời gian ngắn";
  }

  const minutes = Math.max(1, Math.round((date.getTime() - Date.now()) / 60000));
  return `Hết hạn sau khoảng ${minutes} phút`;
}

function handleChallenge(challenge) {
  if (!challenge) return;

  if (challenge.purpose === "password_reset") {
    if (!(resetEmailInput && resetEmailText && resetExpiryText && resetPasswordBlock)) {
      setMessage(loginMessage, challenge.message || "Cần OTP khôi phục mật khẩu để tiếp tục.", false);
      return;
    }

    authState.resetEmail = challenge.email || "";
    resetEmailInput.value = authState.resetEmail;
    resetEmailText.textContent = authState.resetEmail || "Chưa có email nào được chọn";
    resetExpiryText.textContent = challenge.emailDispatched
      ? describeExpiry(challenge.expiresAtUtc)
      : challenge.message || "OTP reset đã được tạo nhưng chưa gửi được email.";
    resetPasswordBlock.hidden = false;
    openOverlay("forgot");
    setMessage(
      forgotMessage,
      challenge.message || "Mã OTP khôi phục đã được tạo.",
      challenge.emailDispatched !== false
    );
    return;
  }

  if (!(verifyEmailInput && verifyEmailText && verifyExpiryText)) {
    setMessage(loginMessage, challenge.message || "Tài khoản cần xác thực email để tiếp tục.", false);
    return;
  }

  authState.verifyEmail = challenge.email || "";
  verifyEmailInput.value = authState.verifyEmail;
  verifyEmailText.textContent = authState.verifyEmail || "Chưa có email nào được chọn";
  verifyExpiryText.textContent = challenge.emailDispatched
    ? describeExpiry(challenge.expiresAtUtc)
    : challenge.message || "OTP đã được tạo nhưng chưa gửi được email.";
  openOverlay("verify");
  setMessage(
    verifyMessage,
    challenge.message || "Mã OTP xác thực đã được tạo.",
    challenge.emailDispatched !== false
  );
}

function redirectAfterSuccess() {
  window.setTimeout(() => {
    window.location.href = authState.redirectUrl;
  }, 1200);
}

function setPasswordToggleState(button, input) {
  const icon = button.querySelector("i");
  const isVisible = input.type === "text";

  button.classList.toggle("is-active", isVisible);
  button.setAttribute("aria-label", isVisible ? "Ẩn mật khẩu" : "Hiện mật khẩu");

  if (icon) {
    icon.className = isVisible ? "bi bi-eye-slash" : "bi bi-eye";
  }
}

openButtons.forEach((button) => {
  button.addEventListener("click", () => {
    const stage = button.dataset.authOpen;
    if (stage) {
      openOverlay(stage);
    }
  });
});

closeButtons.forEach((button) => {
  button.addEventListener("click", closeOverlay);
});

focusButtons.forEach((button) => {
  button.addEventListener("click", () => {
    const target = button.dataset.authFocus;
    if (target) {
      closeOverlay();
      focusCard(target, { shouldScroll: !mobileLayout.matches });
    }
  });
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && authState.overlayStage) {
    closeOverlay();
  }
});

passwordToggles.forEach((button) => {
  const inputId = button.dataset.passwordToggle;
  const input = inputId ? document.getElementById(inputId) : null;

  if (!(input instanceof HTMLInputElement)) {
    return;
  }

  setPasswordToggleState(button, input);

  button.addEventListener("click", () => {
    input.type = input.type === "password" ? "text" : "password";
    setPasswordToggleState(button, input);
    input.focus({ preventScroll: true });
    const length = input.value.length;
    input.setSelectionRange(length, length);
  });
});

loginForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearAllMessages();

  const email = document.getElementById("authLoginEmail")?.value.trim();
  const password = document.getElementById("authLoginPassword")?.value || "";

  if (!email || !password) {
    setMessage(loginMessage, "Vui lòng nhập đầy đủ email và mật khẩu.", false);
    return;
  }

  setMessage(loginMessage, "Đang kiểm tra đăng nhập...", true);

  try {
    const { response, data } = await postJson("/api/auth/login", { email, password });

    if (!response.ok) {
      setMessage(loginMessage, normalizeError(data, "Đăng nhập thất bại."), false);
      return;
    }

    if (isChallengePayload(data)) {
      handleChallenge(data);
      return;
    }

    setBadge("Đăng nhập thành công");
    setMessage(loginMessage, "Đăng nhập thành công. Đang chuyển hướng...", true);
    redirectAfterSuccess();
  } catch {
    setMessage(loginMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

registerForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearAllMessages();

  const displayName = document.getElementById("authRegisterDisplayName")?.value.trim() || "";
  const email = document.getElementById("authRegisterEmail")?.value.trim();
  const password = document.getElementById("authRegisterPassword")?.value || "";
  const confirmPassword = document.getElementById("authRegisterConfirm")?.value || "";

  if (!displayName) {
    setMessage(registerMessage, "Hãy nhập tên hiển thị để tiếp tục.", false);
    return;
  }

  if (!email || !password) {
    setMessage(registerMessage, "Vui lòng nhập đầy đủ email và mật khẩu.", false);
    return;
  }

  if (password.length < 6) {
    setMessage(registerMessage, "Mật khẩu phải có ít nhất 6 ký tự.", false);
    return;
  }

  if (password !== confirmPassword) {
    setMessage(registerMessage, "Mật khẩu xác nhận chưa khớp.", false);
    return;
  }

  setMessage(registerMessage, "Đang tạo tài khoản...", true);

  try {
    const { response, data } = await postJson("/api/auth/register", {
      displayName,
      phone: null,
      email,
      password,
    });

    if (!response.ok) {
      setMessage(registerMessage, normalizeError(data, "Đăng ký thất bại."), false);
      return;
    }

    handleChallenge(data);
  } catch {
    setMessage(registerMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

verifyOtpForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearMessage(verifyMessage);

  const email = verifyEmailInput?.value.trim() || "";
  const otp = document.getElementById("authVerifyOtp")?.value.trim();

  if (!email || !otp) {
    setMessage(verifyMessage, "Vui lòng nhập mã OTP để xác thực.", false);
    return;
  }

  setMessage(verifyMessage, "Đang xác thực OTP...", true);

  try {
    const { response, data } = await postJson("/api/auth/verify-email-otp", {
      email,
      otp,
    });

    if (!response.ok) {
      setMessage(verifyMessage, normalizeError(data, "Xác thực OTP thất bại."), false);
      return;
    }

    setBadge("Email đã xác thực");
    setMessage(verifyMessage, "Xác thực thành công. Đang chuyển hướng...", true);
    redirectAfterSuccess();
  } catch {
    setMessage(verifyMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

resendOtpButton?.addEventListener("click", async () => {
  clearMessage(verifyMessage);

  const email = verifyEmailInput?.value.trim() || "";
  if (!email) {
    setMessage(verifyMessage, "Chưa có email nào để gửi lại OTP.", false);
    return;
  }

  setMessage(verifyMessage, "Đang gửi lại OTP...", true);

  try {
    const { response, data } = await postJson("/api/auth/resend-email-otp", { email });

    if (!response.ok) {
      setMessage(verifyMessage, normalizeError(data, "Không thể gửi lại OTP."), false);
      return;
    }

    handleChallenge(data);
  } catch {
    setMessage(verifyMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

forgotPasswordForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearMessage(forgotMessage);

  const email = document.getElementById("authForgotEmail")?.value.trim();
  if (!email) {
    setMessage(forgotMessage, "Vui lòng nhập email cần khôi phục.", false);
    return;
  }

  setMessage(forgotMessage, "Đang tạo OTP khôi phục...", true);

  try {
    const { response, data } = await postJson("/api/auth/forgot-password", { email });

    if (!response.ok) {
      setMessage(forgotMessage, normalizeError(data, "Không thể gửi mã khôi phục."), false);
      return;
    }

    handleChallenge(data);
  } catch {
    setMessage(forgotMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

resetPasswordForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearMessage(resetMessage);

  const email = resetEmailInput?.value.trim() || "";
  const otp = document.getElementById("authResetOtp")?.value.trim();
  const password = document.getElementById("authResetPassword")?.value || "";
  const confirmPassword = document.getElementById("authResetConfirm")?.value || "";

  if (!email || !otp || !password) {
    setMessage(resetMessage, "Hãy điền đủ OTP và mật khẩu mới.", false);
    return;
  }

  if (password.length < 6) {
    setMessage(resetMessage, "Mật khẩu mới phải có ít nhất 6 ký tự.", false);
    return;
  }

  if (password !== confirmPassword) {
    setMessage(resetMessage, "Mật khẩu xác nhận chưa khớp.", false);
    return;
  }

  setMessage(resetMessage, "Đang đặt lại mật khẩu...", true);

  try {
    const { response, data } = await postJson("/api/auth/reset-password", {
      email,
      otp,
      password,
    });

    if (!response.ok) {
      setMessage(resetMessage, normalizeError(data, "Không thể đặt lại mật khẩu."), false);
      return;
    }

    setBadge("Đã cấp lại truy cập");
    setMessage(resetMessage, "Mật khẩu đã được cập nhật. Đang chuyển hướng...", true);
    redirectAfterSuccess();
  } catch {
    setMessage(resetMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

resendResetOtpButton?.addEventListener("click", async () => {
  clearMessage(resetMessage);

  const email = resetEmailInput?.value.trim() || "";
  if (!email) {
    setMessage(resetMessage, "Chưa có email nào để gửi lại OTP.", false);
    return;
  }

  setMessage(resetMessage, "Đang gửi lại OTP...", true);

  try {
    const { response, data } = await postJson("/api/auth/forgot-password", { email });

    if (!response.ok) {
      setMessage(resetMessage, normalizeError(data, "Không thể gửi lại OTP."), false);
      return;
    }

    handleChallenge(data);
    setMessage(
      resetMessage,
      data?.message || "OTP reset mới đã được tạo.",
      data?.emailDispatched !== false
    );
  } catch {
    setMessage(resetMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

setBadge("Sẵn sàng");
setCardView("login");

if (searchParams.get("mode") === "forgot") {
  openOverlay("forgot");
} else if (searchParams.get("mode") === "register") {
  focusCard("register", { shouldScroll: false, shouldFocus: false });
}
