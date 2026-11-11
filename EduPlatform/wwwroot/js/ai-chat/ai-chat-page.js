// «ԱԲ զրուցարան» էջ՝ ձախ մաս (նոր զրույց, ԱԲ ագենտներ, ամրացված և նախկին զրույցներ) + զրույցի բացում։
// Զրույցի ինքը՝ ai-chat.js
document.addEventListener('DOMContentLoaded', function () {
    const page = document.querySelector('[data-aic-page]');
    if (!page || !window.EduAiChat) return;

    const cfg = window.__aiChat;
    const base = cfg.urls.base;
    const t = function (k) { return (cfg.i18n && cfg.i18n[k]) || k; };

    const side = document.getElementById('aicSide');
    const titleEl = page.querySelector('[data-aic-title]');
    const pinnedSection = page.querySelector('[data-aic-pinned-section]');
    const pinnedList = page.querySelector('[data-aic-pinned]');
    const historyList = page.querySelector('[data-aic-history]');
    const emptyNote = page.querySelector('[data-aic-side-empty]');
    const defaultTitle = titleEl.textContent;

    let activeId = null;

    const chat = window.EduAiChat.init(page.querySelector('[data-aic]'), {
        onState: function (s) {
            const wasNew = activeId !== s.conversationId;
            activeId = s.conversationId;
            titleEl.textContent = s.title || s.agentName || defaultTitle;

            const url = new URL(window.location.href);
            if (s.conversationId) url.searchParams.set('c', s.conversationId); else url.searchParams.delete('c');
            window.history.replaceState(null, '', url);

            // նոր զրույց ստեղծվեց կամ պատասխանն ավարտվեց՝ ցուցակը թարմացվում է (վերջին զրույցը՝ վերևում)
            markActive();
            const streaming = chat && chat.isStreaming();
            if (s.conversationId && (wasNew || !streaming)) refreshList();
        }
    });

    // ── Ժամանակի ցուցադրում ───────────────────────────────────────────────

    function relTime(iso) {
        const d = new Date(iso);
        if (isNaN(d)) return '';
        const now = new Date();
        const lang = window.EduAiChat.lang();

        if (now - d < 60 * 1000) return t('now');

        const startOf = function (x) { return new Date(x.getFullYear(), x.getMonth(), x.getDate()).getTime(); };
        const days = Math.round((startOf(now) - startOf(d)) / 86400000);

        if (days === 0) return d.toLocaleTimeString(lang, { hour: '2-digit', minute: '2-digit' });
        if (days === 1) return t('yesterday');
        return d.toLocaleDateString(lang, { day: 'numeric', month: 'short' });
    }

    // ── Ցուցակ ────────────────────────────────────────────────────────────

    function convEl(c) {
        const item = document.createElement('div');
        item.className = 'aic-conv';
        item.dataset.id = c.id;

        const main = document.createElement('button');
        main.type = 'button';
        main.className = 'aic-conv-main';

        const title = document.createElement('div');
        title.className = 'aic-conv-title';
        if (c.isPinned) {
            const pin = document.createElement('i');
            pin.className = 'bi bi-pin-angle-fill';
            title.appendChild(pin);
        }
        title.appendChild(document.createTextNode(c.title));

        const preview = document.createElement('div');
        preview.className = 'aic-conv-preview';
        preview.textContent = c.preview || '';

        const time = document.createElement('div');
        time.className = 'aic-conv-time';
        time.textContent = relTime(c.updatedAt);

        main.appendChild(title);
        main.appendChild(preview);
        main.appendChild(time);
        main.addEventListener('click', function () { openConv(c.id); });

        const menuBtn = document.createElement('button');
        menuBtn.type = 'button';
        menuBtn.className = 'aic-conv-menu-btn';
        menuBtn.innerHTML = '<i class="bi bi-three-dots"></i>';

        const menu = document.createElement('div');
        menu.className = 'aic-conv-menu';
        menu.hidden = true;
        menu.innerHTML =
            '<button type="button" data-act="pin"><i class="bi ' + (c.isPinned ? 'bi-pin-angle-fill' : 'bi-pin-angle') + '"></i></button>' +
            '<button type="button" data-act="rename"><i class="bi bi-pencil"></i></button>' +
            '<button type="button" data-act="delete" class="is-danger"><i class="bi bi-trash"></i></button>';
        menu.querySelector('[data-act="pin"]').append(' ' + (c.isPinned ? t('unpin') : t('pin')));
        menu.querySelector('[data-act="rename"]').append(' ' + t('rename'));
        menu.querySelector('[data-act="delete"]').append(' ' + t('delete'));

        menuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            const open = menu.hidden;
            closeMenus();
            menu.hidden = !open;
        });

        menu.addEventListener('click', async function (e) {
            const btn = e.target.closest('[data-act]');
            if (!btn) return;
            e.stopPropagation();
            menu.hidden = true;

            if (btn.dataset.act === 'pin') {
                await post('/Conversations/' + c.id + '/Pin', { pinned: !c.isPinned });
                refreshList();
            } else if (btn.dataset.act === 'rename') {
                const name = window.prompt(t('renamePrompt'), c.title);
                if (name && name.trim()) {
                    await post('/Conversations/' + c.id + '/Rename', { title: name.trim() });
                    if (activeId === c.id) titleEl.textContent = name.trim();
                    refreshList();
                }
            } else if (btn.dataset.act === 'delete') {
                if (!window.confirm(t('confirmDelete'))) return;
                await post('/Conversations/' + c.id + '/Delete');
                if (activeId === c.id) chat.reset(null);
                refreshList();
            }
        });

        item.appendChild(main);
        item.appendChild(menuBtn);
        item.appendChild(menu);
        return item;
    }

    function post(path, body) {
        return fetch(base + path, {
            method: 'POST',
            credentials: 'same-origin',
            headers: body ? { 'Content-Type': 'application/json' } : undefined,
            body: body ? JSON.stringify(body) : undefined
        });
    }

    function closeMenus() {
        page.querySelectorAll('.aic-conv-menu').forEach(function (m) { m.hidden = true; });
    }
    document.addEventListener('click', closeMenus);

    function markActive() {
        page.querySelectorAll('.aic-conv').forEach(function (n) {
            n.classList.toggle('is-active', activeId != null && String(activeId) === n.dataset.id);
        });
    }

    async function refreshList() {
        let data;
        try {
            const res = await fetch(base + '/Conversations', { credentials: 'same-origin' });
            if (!res.ok) return;
            data = await res.json();
        } catch (e) { return; }

        pinnedList.innerHTML = '';
        historyList.innerHTML = '';
        (data.pinned || []).forEach(function (c) { pinnedList.appendChild(convEl(c)); });
        (data.recent || []).forEach(function (c) { historyList.appendChild(convEl(c)); });

        pinnedSection.hidden = !(data.pinned && data.pinned.length);
        emptyNote.hidden = !!((data.pinned && data.pinned.length) || (data.recent && data.recent.length));
        markActive();
    }

    function openConv(id) {
        chat.open(id);
        side.classList.remove('is-open');
    }

    // ── Ձախ մասի կոճակներ ─────────────────────────────────────────────────

    page.querySelector('[data-aic-new]').addEventListener('click', function () {
        chat.reset(null);
        side.classList.remove('is-open');
    });

    const agentsToggle = page.querySelector('[data-aic-agents-toggle]');
    const agentsBox = page.querySelector('[data-aic-agents]');
    agentsToggle.addEventListener('click', function () {
        agentsBox.hidden = !agentsBox.hidden;
        agentsToggle.classList.toggle('is-open', !agentsBox.hidden);
    });

    agentsBox.querySelectorAll('[data-aic-agent-key]').forEach(function (b) {
        b.addEventListener('click', function () {
            chat.reset(b.getAttribute('data-aic-agent-key'));
            side.classList.remove('is-open');
        });
    });

    // փոքր էկրան՝ ձախ մասը բացվում/փակվում է
    page.querySelector('[data-aic-side-toggle]').addEventListener('click', function () {
        side.classList.toggle('is-open');
    });

    // ── Սկզբնական բեռնում ─────────────────────────────────────────────────

    refreshList();
    const initial = parseInt(page.dataset.initial, 10);
    if (initial) chat.open(initial);
});
