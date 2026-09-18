(() => {
  const toggle = document.getElementById("navToggle");
  const nav = document.getElementById("siteNav");
  if (toggle && nav) {
    toggle.addEventListener("click", () => {
      const open = nav.classList.toggle("is-open");
      toggle.setAttribute("aria-expanded", open ? "true" : "false");
    });
  }
})();

(() => {
  const KEY = "astrox-theme";
  const root = document.documentElement;
  const btn = document.getElementById("themeToggle");

  const resolveTheme = () => {
    try {
      const stored = localStorage.getItem(KEY);
      if (stored === "light" || stored === "dark") return stored;
    } catch (_) { /* ignore */ }
    return window.matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark";
  };

  const applyTheme = (theme) => {
    root.setAttribute("data-theme", theme);
    if (btn) {
      btn.setAttribute(
        "aria-label",
        theme === "dark" ? "切换为明亮主题" : "切换为暗黑主题"
      );
    }
  };

  applyTheme(resolveTheme());

  if (btn) {
    btn.addEventListener("click", () => {
      const next = root.getAttribute("data-theme") === "light" ? "dark" : "light";
      try {
        localStorage.setItem(KEY, next);
      } catch (_) { /* ignore */ }
      applyTheme(next);
    });
  }
})();

(() => {
  const links = [...document.querySelectorAll(".toc-link")];
  if (!links.length) return;

  const items = links
    .map((a) => {
      const href = a.getAttribute("href") || "";
      const id = decodeURIComponent(href.replace(/^#/, ""));
      return { a, el: id ? document.getElementById(id) : null };
    })
    .filter((x) => x.el);

  if (!items.length) return;

  const sync = () => {
    let current = items[0];
    for (const item of items) {
      if (item.el.getBoundingClientRect().top <= 96) current = item;
    }
    for (const item of items) {
      item.a.classList.toggle("is-active", item === current);
    }
  };

  document.addEventListener("scroll", sync, { passive: true });
  sync();
})();
