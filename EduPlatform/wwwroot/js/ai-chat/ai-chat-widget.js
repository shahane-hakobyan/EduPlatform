// «ԱԲ զրուցարան»՝ լողացող կոճակ + փոքր պատուհան բոլոր էջերում։ Զրույցի ինքը՝ ai-chat.js
document.addEventListener('DOMContentLoaded', function () {
    const widget = document.getElementById('aicWidget');
    if (!widget || !window.EduAiChat) return;

    const panel = document.getElementById('aicWidgetPanel');
    const fab = document.getElementById('aicFab');
    const fullLink = document.getElementById('aicWidgetFull');
    const fullBase = fullLink.getAttribute('href');
    const KEY_CONV = 'aicWidgetConv';
    const KEY_OPEN = 'aicWidgetOpen';

    let chat = null;

    function store(key, value) {
        try { if (value == null || value === '') sessionStorage.removeItem(key); else sessionStorage.setItem(key, value); } catch (e) { }
    }
    function load(key) {
        try { return sessionStorage.getItem(key); } catch (e) { return null; }
    }

    function ensureChat() {
        if (chat) return chat;

        chat = window.EduAiChat.init(panel.querySelector('[data-aic]'), {
            onState: function (s) {
                store(KEY_CONV, s.conversationId);
                fullLink.href = s.conversationId ? fullBase + '?c=' + s.conversationId : fullBase;
            }
        });

        const saved = parseInt(load(KEY_CONV), 10);
        if (saved) chat.open(saved);
        return chat;
    }

    function setOpen(open) {
        panel.hidden = !open;
        fab.classList.toggle('is-open', open);
        fab.querySelector('i').className = open ? 'bi bi-x-lg' : 'bi bi-chat-dots';
        store(KEY_OPEN, open ? '1' : '');
        if (open) {
            ensureChat();
            const input = panel.querySelector('[data-aic-input]');
            if (input) input.focus();
        }
    }

    fab.addEventListener('click', function () { setOpen(panel.hidden); });
    document.getElementById('aicWidgetClose').addEventListener('click', function () { setOpen(false); });
    document.getElementById('aicWidgetNew').addEventListener('click', function () { ensureChat().reset(null); });

    // նախորդ էջում պատուհանը բաց էր՝ բացում ենք նորից
    if (load(KEY_OPEN) === '1') setOpen(true);
});
