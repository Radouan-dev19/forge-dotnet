(() => {
    const STORAGE_KEY = "forge-theme";
    const media = window.matchMedia("(prefers-color-scheme: dark)");

    const systemTheme = () => (media.matches ? "dark" : "light");

    const apply = (theme) => {
        document.documentElement.dataset.theme = theme;
        document.documentElement.setAttribute("data-bs-theme", theme);
    };

    const stored = () => {
        try {
            return localStorage.getItem(STORAGE_KEY);
        } catch {
            return null;
        }
    };

    apply(stored() ?? systemTheme());

    media.addEventListener("change", () => {
        if (!stored()) {
            apply(systemTheme());
        }
    });

    document.addEventListener("click", (event) => {
        if (!event.target.closest("#theme-toggle")) {
            return;
        }

        const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
        apply(next);
        try {
            localStorage.setItem(STORAGE_KEY, next);
        } catch {
            // Stockage indisponible (navigation privée, quota) : le thème reste appliqué pour la session en cours.
        }
    });
})();
