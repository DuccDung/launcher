function setAuthFeedback(element, message, isSuccess) {
  if (!element) return;

  element.hidden = false;
  element.textContent = message;
  element.classList.remove("is-success", "is-error");
  element.classList.add(isSuccess ? "is-success" : "is-error");
}

function clearAuthFeedback(element) {
  if (!element) return;

  element.hidden = true;
  element.textContent = "";
  element.classList.remove("is-success", "is-error");
}

async function parseAuthResponse(response) {
  const contentType = response.headers.get("content-type") || "";
  if (!contentType.includes("application/json")) {
    return null;
  }

  return response.json();
}

async function postAuthJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(payload)
  });

  const data = await parseAuthResponse(response);
  return { response, data };
}

const loginForm = document.getElementById("loginForm");
const registerForm = document.getElementById("registerForm");
const loginMessage = document.getElementById("loginMessage");
const registerMessage = document.getElementById("registerMessage");

loginForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearAuthFeedback(loginMessage);

  const email = document.getElementById("authLoginEmail")?.value.trim();
  const password = document.getElementById("authLoginPassword")?.value ?? "";

  if (!email || !password) {
    setAuthFeedback(loginMessage, "Vui lòng nhập email và mật khẩu.", false);
    return;
  }

  setAuthFeedback(loginMessage, "Đang đăng nhập...", true);

  try {
    const { response, data } = await postAuthJson("/api/auth/login", {
      email,
      password
    });

    if (!response.ok) {
      setAuthFeedback(loginMessage, data?.message || "Đăng nhập thất bại.", false);
      return;
    }

    setAuthFeedback(loginMessage, "Đăng nhập thành công.", true);
  } catch {
    setAuthFeedback(loginMessage, "Không thể kết nối tới máy chủ.", false);
  }
});

registerForm?.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearAuthFeedback(registerMessage);

  const email = document.getElementById("authRegisterEmail")?.value.trim();
  const password = document.getElementById("authRegisterPassword")?.value ?? "";
  const confirmPassword = document.getElementById("authRegisterConfirm")?.value ?? "";

  if (!email || !password) {
    setAuthFeedback(registerMessage, "Vui lòng nhập email và mật khẩu.", false);
    return;
  }

  if (password !== confirmPassword) {
    setAuthFeedback(registerMessage, "Mật khẩu nhập lại không khớp.", false);
    return;
  }

  setAuthFeedback(registerMessage, "Đang tạo tài khoản...", true);

  try {
    const { response, data } = await postAuthJson("/api/auth/register", {
      email,
      password
    });

    if (!response.ok) {
      setAuthFeedback(registerMessage, data?.message || "Đăng ký thất bại.", false);
      return;
    }

    setAuthFeedback(registerMessage, "Đăng ký thành công.", true);
    registerForm.reset();
  } catch {
    setAuthFeedback(registerMessage, "Không thể kết nối tới máy chủ.", false);
  }
});
