window.forgeCodeEditor = {
    attach: (textareaId, highlightId) => {
        const textarea = document.getElementById(textareaId);
        const highlight = document.getElementById(highlightId);
        const code = highlight?.querySelector("code");
        if (!textarea || !code) {
            return;
        }

        const render = () => {
            code.textContent = textarea.value;
            Prism.highlightElement(code);
        };

        const syncScroll = () => {
            highlight.scrollTop = textarea.scrollTop;
            highlight.scrollLeft = textarea.scrollLeft;
        };

        if (!textarea.dataset.forgeAttached) {
            textarea.dataset.forgeAttached = "1";
            textarea.addEventListener("input", render);
            textarea.addEventListener("scroll", syncScroll);
            textarea.addEventListener("keydown", (event) => {
                if (event.key !== "Tab") {
                    return;
                }

                event.preventDefault();
                const start = textarea.selectionStart;
                const end = textarea.selectionEnd;
                textarea.setRangeText("    ", start, end, "end");
                textarea.dispatchEvent(new Event("input", { bubbles: true }));
            });
        }

        render();
        syncScroll();
    }
};
