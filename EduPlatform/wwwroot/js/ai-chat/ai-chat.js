// «ԱԲ զրուցարան»՝ զրույցի ընդհանուր տրամաբանությունը (էջ + լողացող պատուհան)։
// Նշագրումը՝ Views/Shared/AiChat/_AiChatBody.cshtml, թարգմանությունները՝ window.__aiChat (_AiChatStrings.cshtml)։
//
// const chat = EduAiChat.init(rootElement, {
//     onState: function (state) { }     // { conversationId, title, agentKey }՝ զրույցը ստեղծվելիս/բացվելիս/մաքրվելիս
// })  →  { open(id), reset(agentKey), currentId(), isStreaming() }
window.EduAiChat = (function () {
    'use strict';

    const ERROR_MARKER = '\u001eERROR';
    const MAX_FILES = 5;
    const MAX_BYTES = 10 * 1024 * 1024;
    const ALLOWED_EXT = ['.pdf', '.png', '.jpg', '.jpeg', '.gif', '.webp', '.txt', '.md', '.csv', '.json', '.docx', '.xlsx', '.pptx'];

    function cfg() { return window.__aiChat || { urls: { base: '' }, i18n: {} }; }
    function t(key) { return (cfg().i18n && cfg().i18n[key]) || key; }
    function lang() { return (document.documentElement.lang || 'hy').slice(0, 2); }

    function esc(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    // Markdown → անվտանգ HTML (marked + DOMPurify); եթե գրադարանները չբեռնվեցին՝ պարզ տեքստ
    function renderMarkdown(text) {
        if (window.marked && window.DOMPurify) {
            return window.DOMPurify.sanitize(window.marked.parse(text, { gfm: true, breaks: true }));
        }
        return '<p>' + esc(text).replace(/\n/g, '<br>') + '</p>';
    }

    function fmtSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return Math.round(bytes / 1024) + ' KB';
        return (bytes / 1024 / 1024).toFixed(1) + ' MB';
    }

    function fmtTime(iso) {
        const d = iso ? new Date(iso) : new Date();
        if (isNaN(d)) return '';
        return d.toLocaleTimeString(lang(), { hour: '2-digit', minute: '2-digit' });
    }

    function stamp() {
        const d = new Date();
        const p = function (n) { return String(n).padStart(2, '0'); };
        return d.getFullYear() + p(d.getMonth() + 1) + p(d.getDate()) + '-' + p(d.getHours()) + p(d.getMinutes());
    }

    function make(tag, className, html) {
        const el = document.createElement(tag);
        if (className) el.className = className;
        if (html !== undefined) el.innerHTML = html;
        return el;
    }

    function copyText(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text);
        }
        return new Promise(function (resolve) {
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            try { document.execCommand('copy'); } catch (e) { }
            ta.remove();
            resolve();
        });
    }

    function init(root, options) {
        options = options || {};
        const urls = cfg().urls;

        const q = function (sel) { return root.querySelector(sel); };
        const el = {
            messages: q('[data-aic-messages]'),
            empty: q('[data-aic-empty]'),
            thread: q('[data-aic-thread]'),
            input: q('[data-aic-input]'),
            send: q('[data-aic-send]'),
            attach: q('[data-aic-attach]'),
            mic: q('[data-aic-mic]'),
            file: q('[data-aic-file]'),
            attachments: q('[data-aic-attachments]'),
            pill: q('[data-aic-agent-pill]'),
            pillName: q('[data-aic-agent-name]'),
            pillClear: q('[data-aic-agent-clear]')
        };

        const agentNames = {};
        root.querySelectorAll('.aic-chip[data-aic-agent-key]').forEach(function (b) {
            agentNames[b.getAttribute('data-aic-agent-key')] = b.textContent.trim();
        });

        const state = {
            conversationId: null,
            title: '',
            agentKey: null,
            agentLocked: false,
            pending: [],       // { file, kind, tr: { text } }
            streaming: false,
            abort: null,
            recorder: null
        };

        function emitState() {
            if (options.onState) {
                options.onState({ conversationId: state.conversationId, title: state.title, agentKey: state.agentKey, agentName: state.agentKey ? agentNames[state.agentKey] : null });
            }
        }

        // ── Օգնական UI ────────────────────────────────────────────────────

        function showEmpty(show) { el.empty.style.display = show ? '' : 'none'; }

        function nearBottom() {
            const m = el.messages;
            return m.scrollHeight - m.scrollTop - m.clientHeight < 120;
        }

        function scrollBottom(force) {
            if (force || nearBottom()) el.messages.scrollTop = el.messages.scrollHeight;
        }

        function autosize() {
            el.input.style.height = 'auto';
            el.input.style.height = Math.min(el.input.scrollHeight, 160) + 'px';
        }

        function updateSendState() {
            const hasContent = el.input.value.trim().length > 0 || state.pending.length > 0;
            const icon = el.send.querySelector('i');

            if (state.streaming) {
                el.send.disabled = false;
                el.send.classList.add('is-stop');
                el.send.title = t('stop');
                if (icon) icon.className = 'bi bi-stop-fill';
            } else {
                el.send.disabled = !hasContent || !!state.recorder;
                el.send.classList.remove('is-stop');
                el.send.title = t('send');
                if (icon) icon.className = 'bi bi-send';
            }
        }

        function setAgent(key, locked) {
            state.agentKey = key || null;
            state.agentLocked = !!locked;
            if (state.agentKey && agentNames[state.agentKey]) {
                el.pill.hidden = false;
                el.pillName.textContent = agentNames[state.agentKey];
                el.pillClear.style.display = state.agentLocked ? 'none' : '';
            } else {
                el.pill.hidden = true;
            }
        }

        // ── Հաղորդագրությունների նկարում ──────────────────────────────────

        function attachmentsHtml(list, linked) {
            return (list || []).map(function (a) {
                const icon = a.kind === 'audio' ? 'bi-mic' : 'bi-file-earmark-text';
                const inner = '<i class="bi ' + icon + '"></i><span class="aic-att-name">' + esc(a.fileName) + '</span><span class="aic-att-size">' + fmtSize(a.sizeBytes || 0) + '</span>';
                if (linked && a.id) {
                    return '<a class="aic-att" target="_blank" rel="noopener" href="' + urls.base + '/Attachments/' + a.id + '">' + inner + '</a>';
                }
                return '<span class="aic-att">' + inner + '</span>';
            }).join('');
        }

        function fillUserBubble(wrap) {
            const bubble = wrap.querySelector('.aic-bubble');
            bubble.classList.remove('is-editing');
            bubble.innerHTML =
                '<div class="aic-text"></div>' +
                '<div class="aic-atts">' + attachmentsHtml(wrap._atts, !!wrap._linked) + '</div>' +
                '<div class="aic-time"></div>';
            bubble.querySelector('.aic-text').textContent = wrap._text;
            bubble.querySelector('.aic-text').style.display = wrap._text ? '' : 'none';
            bubble.querySelector('.aic-time').textContent = fmtTime(wrap._createdAt) + (wrap._edited ? ' · ' + t('edited') : '');

            const actions = wrap.querySelector('.aic-actions');
            actions.style.display = wrap._text ? '' : 'none';
        }

        function userEl(m) {
            const wrap = make('div', 'aic-msg aic-msg-user');
            wrap.dataset.id = m.id || '';
            wrap._text = m.text || '';
            wrap._atts = m.attachments || [];
            wrap._linked = !!m.id;
            wrap._createdAt = m.createdAt || new Date().toISOString();
            wrap._edited = !!m.editedAt;

            wrap.innerHTML =
                '<div class="aic-msg-main">' +
                '<div class="aic-actions">' +
                '<button type="button" data-act="copy" title="' + esc(t('copy')) + '"><i class="bi bi-clipboard"></i></button>' +
                '<button type="button" data-act="edit" title="' + esc(t('edit')) + '"><i class="bi bi-pencil"></i></button>' +
                '</div>' +
                '<div class="aic-bubble"></div>' +
                '</div>' +
                '<div class="aic-avatar aic-avatar-user"><i class="bi bi-person"></i></div>';
            fillUserBubble(wrap);
            return wrap;
        }

        function assistantEl(m) {
            const wrap = make('div', 'aic-msg aic-msg-assistant');
            wrap.dataset.id = m.id || '';
            wrap._text = m.text || '';
            wrap.innerHTML =
                '<div class="aic-avatar aic-avatar-bot"><i class="bi bi-robot"></i></div>' +
                '<div class="aic-msg-main">' +
                '<div class="aic-bubble"><div class="aic-md"></div><div class="aic-time"></div></div>' +
                '<div class="aic-actions"><button type="button" data-act="copy" title="' + esc(t('copy')) + '"><i class="bi bi-clipboard"></i></button></div>' +
                '</div>';
            paintAssistant(wrap, wrap._text, false);
            if (m.createdAt) wrap.querySelector('.aic-time').textContent = fmtTime(m.createdAt);
            return wrap;
        }

        function paintAssistant(wrap, text, typing) {
            const md = wrap.querySelector('.aic-md');
            wrap._text = text;
            if (!text && typing) {
                md.innerHTML = '<span class="aic-typing"><i></i><i></i><i></i></span>';
            } else {
                md.innerHTML = renderMarkdown(text);
                md.querySelectorAll('a').forEach(function (a) { a.target = '_blank'; a.rel = 'noopener noreferrer'; });
            }
        }

        function clearThread() {
            el.thread.innerHTML = '';
        }

        // ── Գործողություններ՝ պատճենել, խմբագրել ──────────────────────────

        el.thread.addEventListener('click', function (e) {
            const btn = e.target.closest('[data-act]');
            if (!btn) return;
            const wrap = btn.closest('.aic-msg');
            if (!wrap) return;

            if (btn.dataset.act === 'copy') {
                copyText(wrap._text || '').then(function () {
                    const icon = btn.querySelector('i');
                    icon.className = 'bi bi-check2';
                    btn.title = t('copied');
                    setTimeout(function () { icon.className = 'bi bi-clipboard'; btn.title = t('copy'); }, 1400);
                });
            } else if (btn.dataset.act === 'edit') {
                startEdit(wrap);
            }
        });

        function startEdit(wrap) {
            if (state.streaming || wrap.classList.contains('is-editing')) return;
            const id = wrap.dataset.id;
            if (!id) return;

            const bubble = wrap.querySelector('.aic-bubble');
            bubble.classList.add('is-editing');
            bubble.innerHTML =
                '<textarea class="aic-edit-input" maxlength="8000"></textarea>' +
                '<div class="aic-edit-actions">' +
                '<button type="button" class="aic-edit-cancel"></button>' +
                '<button type="button" class="aic-edit-save"></button>' +
                '</div>';

            const ta = bubble.querySelector('textarea');
            ta.value = wrap._text;
            bubble.querySelector('.aic-edit-cancel').textContent = t('cancel');
            bubble.querySelector('.aic-edit-save').textContent = t('save');

            const grow = function () { ta.style.height = 'auto'; ta.style.height = Math.min(ta.scrollHeight, 260) + 'px'; };
            ta.addEventListener('input', grow);
            grow();
            ta.focus();
            ta.setSelectionRange(ta.value.length, ta.value.length);

            bubble.querySelector('.aic-edit-cancel').addEventListener('click', function () { fillUserBubble(wrap); });
            bubble.querySelector('.aic-edit-save').addEventListener('click', function () {
                const newText = ta.value.trim();
                if (!newText) return;

                // խմբագրվածից հետո եղած հաղորդագրությունները հանվում են (պատասխանը նորից կստեղծվի)
                let next = wrap.nextElementSibling;
                while (next) { const n = next.nextElementSibling; next.remove(); next = n; }

                wrap._text = newText;
                wrap._edited = true;
                fillUserBubble(wrap);

                runStream(urls.base + '/Messages/' + id + '/Edit', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ text: newText })
                }, null);
            });
        }

        // ── Պատասխանի հոսք ────────────────────────────────────────────────

        function visible(text) {
            const i = text.indexOf(ERROR_MARKER);
            return i < 0 ? text : text.slice(0, i);
        }

        async function runStream(url, init, userWrap) {
            state.streaming = true;
            updateSendState();
            showEmpty(false);

            const ctrl = new AbortController();
            state.abort = ctrl;

            let bot = null;
            let full = '';
            let raf = 0;
            let limitMessage = '';
            const paint = function () {
                raf = 0;
                if (bot) { paintAssistant(bot, visible(full), true); scrollBottom(false); }
            };

            try {
                const res = await fetch(url, Object.assign({ signal: ctrl.signal, credentials: 'same-origin' }, init));

                // օրական սահմանաչափը լրացել է՝ հաղորդագրությունը չի պահվել
                if (res.status === 429) {
                    let info = {};
                    try { info = await res.json(); } catch (e) { }
                    if (userWrap) userWrap.remove();
                    limitMessage = t('limitReached').replace('{0}', info.limit || '');
                    return;
                }

                if (!res.ok || !res.body) throw new Error('HTTP ' + res.status);

                const reader = res.body.getReader();
                const decoder = new TextDecoder('utf-8');
                let buf = '';
                let metaDone = false;

                while (true) {
                    const r = await reader.read();
                    if (r.done) break;
                    buf += decoder.decode(r.value, { stream: true });

                    if (!metaDone) {
                        const nl = buf.indexOf('\n');
                        if (nl < 0) continue;

                        const meta = JSON.parse(buf.slice(0, nl));
                        buf = buf.slice(nl + 1);
                        metaDone = true;

                        state.conversationId = meta.conversationId;
                        state.title = meta.title || state.title;
                        if (meta.agentKey) setAgent(meta.agentKey, true);

                        if (userWrap && meta.userMessage) {
                            userWrap.dataset.id = meta.userMessage.id;
                            userWrap._atts = meta.userMessage.attachments || [];
                            userWrap._linked = true;
                            fillUserBubble(userWrap);
                        }

                        bot = assistantEl({ id: meta.assistantMessageId, text: '' });
                        paintAssistant(bot, '', true);
                        el.thread.appendChild(bot);
                        scrollBottom(true);
                        emitState();
                    }

                    if (metaDone && buf) {
                        full += buf;
                        buf = '';
                        if (!raf) raf = requestAnimationFrame(paint);
                    }
                }

                buf += decoder.decode();
                if (metaDone && buf) full += buf;
            } catch (err) {
                if (err.name !== 'AbortError') full += ERROR_MARKER;
            } finally {
                if (raf) cancelAnimationFrame(raf);

                const failed = full.indexOf(ERROR_MARKER) >= 0;
                if (bot) {
                    paintAssistant(bot, visible(full), false);
                    bot.querySelector('.aic-time').textContent = fmtTime(new Date().toISOString());
                    if (failed) {
                        bot.querySelector('.aic-bubble').appendChild(make('div', 'aic-error', '<i class="bi bi-exclamation-circle"></i> ' + esc(t('error'))));
                    }
                } else if (limitMessage) {
                    const note = make('div', 'aic-limit', '<i class="bi bi-hourglass-split"></i> ' + esc(limitMessage));
                    el.thread.appendChild(note);
                    if (!el.thread.querySelector('.aic-msg')) showEmpty(true);
                } else if (failed) {
                    // պատասխանի հոսքը չսկսվեց (օր․ սերվերի սխալ)՝ նշում ենք օգտատիրոջ հաղորդագրության տակ
                    const note = assistantEl({ id: '', text: '' });
                    note.querySelector('.aic-md').innerHTML = '';
                    note.querySelector('.aic-bubble').appendChild(make('div', 'aic-error', '<i class="bi bi-exclamation-circle"></i> ' + esc(t('error'))));
                    el.thread.appendChild(note);
                }

                state.streaming = false;
                state.abort = null;
                updateSendState();
                scrollBottom(true);
                emitState();
            }
        }

        // ── Ուղարկել ──────────────────────────────────────────────────────

        async function send() {
            if (state.streaming) { if (state.abort) state.abort.abort(); return; }
            if (state.recorder) return;

            const text = el.input.value.trim();
            const pend = state.pending.slice();
            if (!text && !pend.length) return;

            const wrap = userEl({
                id: '',
                text: text,
                createdAt: new Date().toISOString(),
                attachments: pend.map(function (p) { return { kind: p.kind, fileName: p.file.name, sizeBytes: p.file.size }; })
            });
            el.thread.appendChild(wrap);
            showEmpty(false);
            scrollBottom(true);

            const fd = new FormData();
            if (state.conversationId) fd.append('ConversationId', state.conversationId);
            else if (state.agentKey) fd.append('AgentKey', state.agentKey);
            fd.append('Text', text);
            pend.forEach(function (p) { fd.append('Attachments', p.file, p.file.name); });
            fd.append('AttachmentMeta', JSON.stringify(pend.map(function (p) {
                return { kind: p.kind, transcript: p.kind === 'audio' ? ((p.tr && p.tr.text) || '').trim() : '' };
            })));

            el.input.value = '';
            state.pending = [];
            renderPending();
            autosize();
            updateSendState();

            await runStream(urls.base + '/Send', { method: 'POST', body: fd }, wrap);
        }

        // ── Կցված ֆայլեր ──────────────────────────────────────────────────

        function renderPending() {
            el.attachments.innerHTML = '';
            state.pending.forEach(function (p, i) {
                const chip = make('span', 'aic-pending');
                chip.innerHTML =
                    '<i class="bi ' + (p.kind === 'audio' ? 'bi-mic' : 'bi-file-earmark-text') + '"></i>' +
                    '<span class="aic-att-name">' + esc(p.file.name) + '</span>' +
                    '<span class="aic-att-size">' + fmtSize(p.file.size) + '</span>' +
                    '<button type="button" aria-label="x"><i class="bi bi-x"></i></button>';
                chip.querySelector('button').addEventListener('click', function () {
                    state.pending.splice(i, 1);
                    renderPending();
                    updateSendState();
                });
                el.attachments.appendChild(chip);
            });
        }

        function addPending(file, kind, tr) {
            if (state.pending.length >= MAX_FILES) { alert(t('tooManyFiles')); return null; }
            if (file.size > MAX_BYTES) { alert(t('fileTooBig')); return null; }
            const entry = { file: file, kind: kind, tr: tr || null };
            state.pending.push(entry);
            renderPending();
            updateSendState();
            return entry;
        }

        function addFiles(list) {
            Array.from(list).forEach(function (f) {
                const ext = (f.name.match(/\.[^.]+$/) || [''])[0].toLowerCase();
                if (ALLOWED_EXT.indexOf(ext) < 0) { alert(t('badType') + ' (' + f.name + ')'); return; }
                addPending(f, 'file', null);
            });
        }

        el.attach.addEventListener('click', function () { el.file.click(); });
        el.file.addEventListener('change', function () { addFiles(el.file.files); el.file.value = ''; });

        // նկարը clipboard-ից տեղադրելը (Ctrl+V)
        el.input.addEventListener('paste', function (e) {
            const files = e.clipboardData && e.clipboardData.files;
            if (files && files.length) {
                e.preventDefault();
                addFiles(files);
            }
        });

        // ── Ձայնագրություն ────────────────────────────────────────────────

        function recLang() {
            return { hy: 'hy-AM', ru: 'ru-RU', en: 'en-US' }[lang()] || 'hy-AM';
        }

        function setMicState(active) {
            el.mic.classList.toggle('is-recording', active);
            el.mic.title = active ? t('stopRecord') : t('record');
            el.mic.querySelector('i').className = active ? 'bi bi-stop-circle' : 'bi bi-mic';
        }

        async function toggleRecording() {
            if (state.recorder) { stopRecording(); return; }
            if (state.streaming) return;
            if (!navigator.mediaDevices || !window.MediaRecorder) { alert(t('micDenied')); return; }

            let stream;
            try { stream = await navigator.mediaDevices.getUserMedia({ audio: true }); }
            catch (e) { alert(t('micDenied')); return; }

            const mime = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4'].find(function (m) {
                return window.MediaRecorder.isTypeSupported && window.MediaRecorder.isTypeSupported(m);
            });
            const rec = new MediaRecorder(stream, mime ? { mimeType: mime } : undefined);
            const chunks = [];
            const tr = { text: '' };   // խոսքի ճանաչման տեքստը (Claude-ին տրվում է տեքստով, քանի որ աուդիո չի ընդունում)

            rec.ondataavailable = function (e) { if (e.data && e.data.size) chunks.push(e.data); };
            rec.onstop = function () {
                stream.getTracks().forEach(function (track) { track.stop(); });
                const type = rec.mimeType || 'audio/webm';
                const ext = type.indexOf('mp4') >= 0 ? '.m4a' : '.webm';
                const file = new File([new Blob(chunks, { type: type })], 'recording-' + stamp() + ext, { type: type });
                addPending(file, 'audio', tr);
            };

            // բրաուզերի խոսքի ճանաչում (Chrome/Edge)՝ ձայնագրության հետ զուգահեռ
            const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
            let recog = null;
            if (SR) {
                try {
                    recog = new SR();
                    recog.lang = recLang();
                    recog.continuous = true;
                    recog.interimResults = false;
                    recog.onresult = function (ev) {
                        for (let i = ev.resultIndex; i < ev.results.length; i++) {
                            if (ev.results[i].isFinal) tr.text += ev.results[i][0].transcript + ' ';
                        }
                    };
                    recog.onerror = function () { };
                    recog.onend = function () {
                        if (state.recorder && state.recorder.active) { try { recog.start(); } catch (e) { } }
                    };
                    recog.start();
                } catch (e) { recog = null; }
            }

            state.recorder = { rec: rec, recog: recog, active: true };
            rec.start();
            setMicState(true);
            updateSendState();
        }

        function stopRecording() {
            const r = state.recorder;
            if (!r) return;
            r.active = false;
            try { if (r.recog) r.recog.stop(); } catch (e) { }
            try { r.rec.stop(); } catch (e) { }
            state.recorder = null;
            setMicState(false);
            updateSendState();
        }

        el.mic.addEventListener('click', toggleRecording);

        // ── Գրելու դաշտ ───────────────────────────────────────────────────

        el.input.addEventListener('input', function () { autosize(); updateSendState(); });
        el.input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
                e.preventDefault();
                if (!el.send.disabled && !state.streaming) send();
            }
        });
        el.send.addEventListener('click', send);

        // ԱԲ ագենտ՝ դատարկ էկրանի կոճակներով
        root.querySelectorAll('.aic-chip[data-aic-agent-key]').forEach(function (b) {
            b.addEventListener('click', function () {
                setAgent(b.getAttribute('data-aic-agent-key'), false);
                emitState();
                el.input.focus();
            });
        });
        el.pillClear.addEventListener('click', function () { setAgent(null, false); emitState(); });

        // ── Արտաքին API ───────────────────────────────────────────────────

        async function open(id) {
            if (state.streaming) return;
            const res = await fetch(urls.base + '/Conversations/' + id, { credentials: 'same-origin' });
            if (!res.ok) { reset(null); return; }
            const c = await res.json();

            clearThread();
            state.conversationId = c.id;
            state.title = c.title;
            setAgent(c.agentKey, true);
            showEmpty(c.messages.length === 0);

            c.messages.forEach(function (m) {
                el.thread.appendChild(m.role === 'user'
                    ? userEl({ id: m.id, text: m.text, createdAt: m.createdAt, editedAt: m.editedAt, attachments: m.attachments })
                    : assistantEl({ id: m.id, text: m.text, createdAt: m.createdAt }));
            });

            scrollBottom(true);
            emitState();
        }

        function reset(agentKey) {
            if (state.streaming && state.abort) state.abort.abort();
            if (state.recorder) stopRecording();

            clearThread();
            state.conversationId = null;
            state.title = '';
            state.pending = [];
            renderPending();
            el.input.value = '';
            autosize();
            setAgent(agentKey || null, false);
            showEmpty(true);
            updateSendState();
            emitState();
            el.input.focus();
        }

        // սկզբնական վիճակ
        el.input.placeholder = t('placeholder');
        showEmpty(true);
        updateSendState();

        return {
            open: open,
            reset: reset,
            currentId: function () { return state.conversationId; },
            isStreaming: function () { return state.streaming; }
        };
    }

    return { init: init, fmtTime: fmtTime, lang: lang, t: t };
})();
