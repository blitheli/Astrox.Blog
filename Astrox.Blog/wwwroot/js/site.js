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
