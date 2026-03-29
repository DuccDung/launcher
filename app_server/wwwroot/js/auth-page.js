const mobileMenuToggle = document.getElementById("mobileMenuToggle");
const mobileDrawer = document.getElementById("mobileDrawer");
const mobileOverlay = document.getElementById("mobileOverlay");
const closeDrawer = document.getElementById("closeDrawer");
const ambientGlow = document.querySelector(".ambient-glow");
const motionPreference = window.matchMedia("(prefers-reduced-motion: reduce)");
let ambientGlowFrame = null;

function openDrawer() {
    if (!mobileDrawer || !mobileOverlay) return;
    mobileDrawer.classList.add("active");
    mobileOverlay.classList.add("active");
    mobileDrawer.setAttribute("aria-hidden", "false");
    mobileMenuToggle?.setAttribute("aria-expanded", "true");
    document.body.style.overflow = "hidden";
}

function closeMenu() {
    if (!mobileDrawer || !mobileOverlay) return;
    mobileDrawer.classList.remove("active");
    mobileOverlay.classList.remove("active");
    mobileDrawer.setAttribute("aria-hidden", "true");
    mobileMenuToggle?.setAttribute("aria-expanded", "false");
    document.body.style.overflow = "";
}

mobileMenuToggle?.addEventListener("click", openDrawer);
closeDrawer?.addEventListener("click", closeMenu);
mobileOverlay?.addEventListener("click", closeMenu);
document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeMenu();
});

function updateAmbientGlow(x, y, scale, opacity) {
    if (!ambientGlow) return;
    ambientGlow.style.setProperty("--glow-x", `${x}px`);
    ambientGlow.style.setProperty("--glow-y", `${y}px`);
    ambientGlow.style.setProperty("--glow-scale", scale.toFixed(3));
    ambientGlow.style.setProperty("--glow-opacity", opacity.toFixed(3));
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
        const driftX = (Math.sin(t * 1.12) * window.innerWidth * 0.075 + Math.cos(t * 0.42) * window.innerWidth * 0.028) * motionFactor;
        const driftY = (Math.cos(t * 0.88) * window.innerHeight * 0.095 + Math.sin(t * 0.36) * window.innerHeight * 0.032) * motionFactor;
        const scale = 1.04 + (Math.sin(t * 1.88) * 0.12 + Math.cos(t * 0.98) * 0.05) * motionFactor;
        const opacity = 0.32 + ((((Math.sin(t * 1.72 - 0.5) + 1) * 0.5) * 0.2) + (((Math.cos(t * 0.64) + 1) * 0.5) * 0.05)) * motionFactor;

        updateAmbientGlow(centerX + driftX, centerY + driftY, scale, opacity);
        ambientGlowFrame = window.requestAnimationFrame(animate);
    };

    ambientGlowFrame = window.requestAnimationFrame(animate);
}

startAmbientGlow();

function setMessage(element, message, isSuccess) {
    if (!element) return;
    element.textContent = message;
    element.classList.remove("is-success", "is-error");
    element.classList.add(isSuccess ? "is-success" : "is-error");
}

async function parseResponse(response) {
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
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
    });

    const data = await parseResponse(response);
    return { response, data };
}

const loginForm = document.getElementById("loginForm");
const registerForm = document.getElementById("registerForm");
const loginMessage = document.getElementById("loginMessage");
const registerMessage = document.getElementById("registerMessage");

loginForm?.addEventListener("submit", async (event) => {
    event.preventDefault();

    const email = document.getElementById("authLoginEmail")?.value.trim();
    const password = document.getElementById("authLoginPassword")?.value ?? "";

    if (!email || !password) {
        setMessage(loginMessage, "Vui lòng nhập email và mật khẩu.", false);
        return;
    }

    setMessage(loginMessage, "Đang đăng nhập...", true);

    try {
        const { response, data } = await postJson("/api/auth/login", { email, password });

        if (!response.ok) {
            setMessage(loginMessage, data?.message || "Đăng nhập thất bại.", false);
            return;
        }

        setMessage(loginMessage, "Đăng nhập thành công.", true);
    } catch {
        setMessage(loginMessage, "Không thể kết nối tới máy chủ.", false);
    }
});

registerForm?.addEventListener("submit", async (event) => {
    event.preventDefault();

    const email = document.getElementById("authRegisterEmail")?.value.trim();
    const password = document.getElementById("authRegisterPassword")?.value ?? "";
    const confirmPassword = document.getElementById("authRegisterConfirm")?.value ?? "";
    const displayName = document.getElementById("authRegisterDisplayName")?.value.trim();

    if (!email || !password) {
        setMessage(registerMessage, "Vui lòng nhập email và mật khẩu.", false);
        return;
    }

    if (password !== confirmPassword) {
        setMessage(registerMessage, "Mật khẩu nhập lại không khớp.", false);
        return;
    }

    setMessage(registerMessage, "Đang tạo tài khoản...", true);

    try {
        const { response, data } = await postJson("/api/auth/register", {
            email,
            password,
            displayName
        });

        if (!response.ok) {
            setMessage(registerMessage, data?.message || "Đăng ký thất bại.", false);
            return;
        }

        setMessage(registerMessage, "Đăng ký thành công.", true);
        registerForm.reset();
    } catch {
        setMessage(registerMessage, "Không thể kết nối tới máy chủ.", false);
    }
});
