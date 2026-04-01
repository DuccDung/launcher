const authState = {
  stage: "login",
  verifyEmail: "",
  resetEmail: "",
  redirectUrl:
    new URLSearchParams(window.location.search).get("returnUrl") ||
    document.body.dataset.authRedirect ||
    "/",
};

const stageButtons = Array.from(document.querySelectorAll("[data-auth-target]"));
const stagePanels = Array.from(document.querySelectorAll("[data-auth-stage]"));
const globalBadge = document.getElementById("authGlobalBadge");

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

function hasStage(stage) {
  return stagePanels.some((panel) => panel.dataset.authStage === stage);
}

function openStage(stage) {
  const nextStage = hasStage(stage)
    ? stage
    : hasStage(authState.stage)
      ? authState.stage
      : "login";

  authState.stage = nextStage;

  stageButtons.forEach((button) => {
    button.classList.toggle("is-active", button.dataset.authTarget === nextStage);
  });

  stagePanels.forEach((panel) => {
    panel.classList.toggle("is-active", panel.dataset.authStage === nextStage);
  });

  const labels = {
    login: "Sẵn sàng đăng nhập",
    register: "Tạo tài khoản mới",
    verify: "Đang chờ OTP",
    forgot: "Khôi phục mật khẩu",
  };

  setBadge(labels[nextStage] || "Sẵn sàng");
}

function setMessage(element, message, isSuccess) {
  if (!element) return;
  element.hidden = false;
  element.textContent = message;
  element.classList.remove("is-success", "is-error");
  element.classList.add(isSuccess ? "is-success" : "is-error");
}

function clearMessage(element) {
  if (!element) return;
  element.hidden = true;
  element.textContent = "";
  element.classList.remove("is-success", "is-error");
}

function clearAllMessages() {
  [loginMessage, registerMessage, verifyMessage, forgotMessage, resetMessage].forEach(
    clearMessage
  );
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

function showLoginOnlyFallback(message, badgeLabel) {
  if (badgeLabel) {
    setBadge(badgeLabel);
  }

  openStage("login");
  setMessage(loginMessage, message, false);
}

function handleChallenge(challenge) {
  if (!challenge) return;

  if (challenge.purpose === "password_reset") {
    if (!(resetEmailInput && resetEmailText && resetExpiryText && resetPasswordBlock && hasStage("forgot"))) {
      showLoginOnlyFallback(
        challenge.message || "Tài khoản này cần OTP khôi phục mật khẩu để tiếp tục.",
        "Cần khôi phục mật khẩu"
      );
      return;
    }

    authState.resetEmail = challenge.email || "";
    resetEmailInput.value = authState.resetEmail;
    resetEmailText.textContent = authState.resetEmail || "Chưa có email nào được chọn";
    resetExpiryText.textContent = challenge.emailDispatched
      ? describeExpiry(challenge.expiresAtUtc)
      : challenge.message || "OTP reset đã được tạo nhưng chưa gửi được email.";
    resetPasswordBlock.hidden = false;
    openStage("forgot");
    setMessage(
      forgotMessage,
      challenge.message || "Mã OTP khôi phục đã được tạo.",
      challenge.emailDispatched !== false
    );
    return;
  }

  if (!(verifyEmailInput && verifyEmailText && verifyExpiryText && hasStage("verify"))) {
    showLoginOnlyFallback(
      challenge.message || "Tài khoản chưa xác thực email và cần OTP để tiếp tục.",
      "Cần xác thực email"
    );
    return;
  }

  authState.verifyEmail = challenge.email || "";
  verifyEmailInput.value = authState.verifyEmail;
  verifyEmailText.textContent = authState.verifyEmail || "Chưa có email nào được chọn";
  verifyExpiryText.textContent = challenge.emailDispatched
    ? describeExpiry(challenge.expiresAtUtc)
    : challenge.message || "OTP đã được tạo nhưng chưa gửi được email.";
  openStage("verify");
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

stageButtons.forEach((button) => {
  button.addEventListener("click", () => {
    openStage(button.dataset.authTarget || "login");
  });
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
  const phone = document.getElementById("authRegisterPhone")?.value.trim() || "";
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

  setMessage(registerMessage, "Đang tạo tài khoản và phát OTP...", true);

  try {
    const { response, data } = await postJson("/api/auth/register", {
      displayName,
      phone: phone || null,
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
    setMessage(resetMessage, "Chưa có email nào để gửi lại OTP reset.", false);
    return;
  }

  setMessage(resetMessage, "Đang gửi lại OTP reset...", true);

  try {
    const { response, data } = await postJson("/api/auth/forgot-password", { email });

    if (!response.ok) {
      setMessage(resetMessage, normalizeError(data, "Không thể gửi lại OTP reset."), false);
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

openStage("login");
