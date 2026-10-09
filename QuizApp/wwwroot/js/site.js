/* =========================================================================
   Quiz – "Brætspilsbordet"
   Al bevægelse er progressiv: uden JavaScript virker alle sider som før
   (hele formularerne er i DOM'en), og med JavaScript lægges spillet ovenpå.
   ========================================================================= */
(function () {
  'use strict';

  var mqReduce = window.matchMedia('(prefers-reduced-motion: reduce)');
  function reduced() { return mqReduce.matches; }
  function each(list, fn) { Array.prototype.forEach.call(list, fn); }

  /* ---------------- Scroll-reveal ---------------- */
  function initReveal() {
    var els = document.querySelectorAll('[data-reveal]');
    if (!els.length) return;
    if (reduced() || !('IntersectionObserver' in window)) {
      each(els, function (el) { el.classList.add('is-in'); });
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add('is-in'); io.unobserve(e.target); }
      });
    }, { rootMargin: '0px 0px -6% 0px', threshold: 0.06 });
    each(els, function (el) { io.observe(el); });
  }

  /* ---------------- Kinetisk overskrift ---------------- */
  function initKinetic() {
    each(document.querySelectorAll('[data-kinetic]'), function (el) {
      var text = el.textContent;
      if (!text || reduced()) return;
      var frag = document.createDocumentFragment();
      var i = 0;
      text.split(/(\s+)/).forEach(function (part) {
        if (!part) return;
        if (/^\s+$/.test(part)) {
          var sp = document.createElement('span');
          sp.className = 'k-space';
          sp.textContent = part;
          frag.appendChild(sp);
          return;
        }
        var word = document.createElement('span');
        word.style.display = 'inline-block';
        word.style.whiteSpace = 'nowrap';
        Array.from(part).forEach(function (ch) {
          var s = document.createElement('span');
          s.className = 'k-letter';
          s.setAttribute('aria-hidden', 'true');
          s.style.setProperty('--i', String(i++));
          s.textContent = ch;
          word.appendChild(s);
        });
        frag.appendChild(word);
      });
      el.setAttribute('aria-label', text);
      el.textContent = '';
      el.appendChild(frag);
    });
  }

  /* ---------------- Parallax på svævende klodser ---------------- */
  function initFloaties() {
    var stage = document.querySelector('[data-floaties]');
    if (!stage || reduced()) return;
    var items = stage.querySelectorAll('.floaty');
    var raf = null, tx = 0, ty = 0;
    function apply() {
      raf = null;
      each(items, function (el) {
        var depth = parseFloat(el.getAttribute('data-depth') || '1');
        el.style.setProperty('--px', (tx * depth).toFixed(1) + 'px');
        el.style.setProperty('--py', (ty * depth).toFixed(1) + 'px');
      });
    }
    window.addEventListener('pointermove', function (e) {
      if (e.pointerType === 'touch') return;
      tx = (e.clientX / window.innerWidth - 0.5) * 30;
      ty = (e.clientY / window.innerHeight - 0.5) * 22;
      if (!raf) raf = window.requestAnimationFrame(apply);
    }, { passive: true });
  }

  /* ---------------- Huskespil: flip på touch ---------------- */
  function initFlip() {
    if (!window.matchMedia('(hover: none)').matches) return;
    each(document.querySelectorAll('.flip'), function (a) {
      a.addEventListener('click', function (e) {
        if (!a.classList.contains('is-flipped')) {
          e.preventDefault();
          a.classList.add('is-flipped');
        }
      });
    });
  }

  /* ---------------- Brætspilssti + ét spørgsmål ad gangen ---------------- */
  function initStepper() {
    var shell = document.querySelector('[data-stepper]');
    if (!shell) return;
    var form = document.getElementById('quizForm');
    var steps = Array.prototype.slice.call(shell.querySelectorAll('.q-step'));
    if (!steps.length) return;

    var board = document.querySelector('.board');
    var cells = board ? Array.prototype.slice.call(board.querySelectorAll('.board-cell')) : [];
    var pawn = document.querySelector('.board-pawn');
    var backBtn = document.querySelector('[data-step-back]');
    var nextBtn = document.querySelector('[data-step-next]');
    var submitBtn = document.querySelector('[data-step-submit]');
    var counter = document.querySelector('[data-step-count]');
    var live = document.querySelector('[data-step-live]');
    var contactIdx = steps.length - 1;
    var idx = 0;

    function label(i) {
      return i === contactIdx ? 'Dine oplysninger' : 'Spørgsmål ' + (i + 1) + ' af ' + contactIdx;
    }

    function placePawn() {
      if (!pawn || !cells.length) return;
      var cell = cells[Math.min(idx, cells.length - 1)];
      var x = cell.offsetLeft + cell.offsetWidth / 2 - pawn.offsetWidth / 2;
      pawn.style.setProperty('--pawn-x', x + 'px');
      // Felter kan bryde over flere raekker - saa skal brikken ogsaa flyttes lodret.
      pawn.style.setProperty('--pawn-y', cell.offsetTop + 'px');
    }

    function hasAnswer(step) {
      if (step.getAttribute('data-touched') === 'true') return true;
      if (step.querySelector('input[type="radio"]:checked, input[type="checkbox"]:checked')) return true;
      var inputs = step.querySelectorAll('input[type="text"], input[type="number"]');
      for (var i = 0; i < inputs.length; i++) {
        if (inputs[i].value.trim()) return true;
      }
      var sels = step.querySelectorAll('select');
      for (var j = 0; j < sels.length; j++) {
        if (sels[j].value) return true;
      }
      return false;
    }

    function paintCells() {
      cells.forEach(function (cell, i) {
        var step = steps[i];
        cell.classList.toggle('is-done', !!step && hasAnswer(step));
        cell.classList.toggle('is-current', i === idx);
        if (i === idx) { cell.setAttribute('aria-current', 'step'); }
        else { cell.removeAttribute('aria-current'); }
      });
    }

    function render(animate) {
      shell.setAttribute('data-dir', shell.getAttribute('data-dir') || 'fwd');
      steps.forEach(function (s, i) {
        var on = i === idx;
        if (on && !animate) {
          s.style.animation = 'none';
          window.requestAnimationFrame(function () { s.style.animation = ''; });
        }
        s.classList.toggle('is-active', on);
      });
      if (backBtn) backBtn.hidden = idx === 0;
      if (nextBtn) nextBtn.hidden = idx === contactIdx;
      if (submitBtn) submitBtn.hidden = idx !== contactIdx;
      if (counter) {
        counter.innerHTML = idx === contactIdx
          ? 'Sidste felt &middot; <b>dine oplysninger</b>'
          : 'Spørgsmål <b>' + (idx + 1) + '</b> af ' + contactIdx;
      }
      if (live) live.textContent = label(idx);
      paintCells();
      placePawn();
    }

    function goTo(target, dir) {
      target = Math.max(0, Math.min(steps.length - 1, target));
      if (target === idx) return;
      shell.setAttribute('data-dir', dir || (target > idx ? 'fwd' : 'back'));
      idx = target;
      render(true);
      var head = document.querySelector('.game-top') || shell;
      var top = head.getBoundingClientRect().top + window.pageYOffset - 96;
      window.scrollTo({ top: top, behavior: reduced() ? 'auto' : 'smooth' });
      var focusable = steps[idx].querySelector('input, select, textarea, button');
      if (focusable) {
        window.setTimeout(function () { focusable.focus({ preventScroll: true }); }, 260);
      }
    }

    function showError(show) {
      var err = document.querySelector('[data-error="name"]');
      var name = document.getElementById('ParticipantName');
      if (err) err.classList.toggle('is-shown', !!show);
      if (name) name.classList.toggle('is-invalid', !!show);
      if (show && name) name.focus();
      return !show;
    }

    function contactOk() {
      var name = document.getElementById('ParticipantName');
      return !!name && name.value.trim().length > 0;
    }

    if (form) {
      form.addEventListener('change', paintCells);
      form.addEventListener('input', function (e) {
        paintCells();
        if (e.target && e.target.id === 'ParticipantName' && e.target.value.trim()) showError(false);
      });

      form.addEventListener('submit', function (e) {
        if (!contactOk()) {
          e.preventDefault();
          goTo(contactIdx, 'fwd');
          showError(true);
        }
      });
    }

    if (backBtn) backBtn.addEventListener('click', function () { goTo(idx - 1, 'back'); });
    if (nextBtn) nextBtn.addEventListener('click', function () { goTo(idx + 1, 'fwd'); });

    cells.forEach(function (cell, i) {
      cell.addEventListener('click', function () {
        goTo(i, i > idx ? 'fwd' : 'back');
      });
    });

    document.addEventListener('keydown', function (e) {
      if (e.altKey || e.ctrlKey || e.metaKey) return;
      var t = e.target;
      if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) return;
      if (e.key === 'ArrowRight') { goTo(idx + 1, 'fwd'); }
      else if (e.key === 'ArrowLeft') { goTo(idx - 1, 'back'); }
    });

    window.addEventListener('resize', placePawn);

    render(false);
    // Første gang: pege på det rigtige felt uden at rulle brugeren væk.
    shell.setAttribute('data-dir', 'fwd');
  }

  /* ---------------- Rækkefølge: FLIP-animation ---------------- */
  function flipMove(list, mutate) {
    var items = Array.prototype.slice.call(list.children);
    var first = items.map(function (el) { return el.getBoundingClientRect().top; });
    mutate();
    if (reduced()) return;
    items.forEach(function (el, i) {
      var dy = first[i] - el.getBoundingClientRect().top;
      if (!dy) return;
      el.style.transition = 'none';
      el.style.transform = 'translateY(' + dy + 'px)';
      window.requestAnimationFrame(function () {
        el.style.transition = 'transform .38s cubic-bezier(.22,.8,.28,1)';
        el.style.transform = '';
      });
    });
  }

  function renumber(list) {
    var n = 0;
    Array.prototype.forEach.call(list.children, function (li) {
      n++;
      var pos = li.querySelector('.pos');
      if (pos) pos.textContent = String(n);
    });
  }

  function initOrdering() {
    each(document.querySelectorAll('ul.ordering'), function (list) {
      var hidden = document.getElementById(list.getAttribute('data-target'));
      function sync() {
        if (hidden) {
          hidden.value = Array.prototype.map.call(list.querySelectorAll('li'), function (li) {
            return li.getAttribute('data-id');
          }).join(',');
        }
        renumber(list);
      }
      list.addEventListener('click', function (e) {
        var btn = e.target.closest ? e.target.closest('button') : null;
        if (!btn || !list.contains(btn)) return;
        var li = btn.closest('li');
        if (!li) return;
        var shell = list.closest('.q-step');
        if (shell) shell.setAttribute('data-touched', 'true');
        flipMove(list, function () {
          if (btn.classList.contains('move-up') && li.previousElementSibling) {
            list.insertBefore(li, li.previousElementSibling);
          } else if (btn.classList.contains('move-down') && li.nextElementSibling) {
            list.insertBefore(li.nextElementSibling, li);
          }
        });
        sync();
        if (btn.classList.contains('move-up')) btn.focus();
      });
      sync();
    });
  }

  /* ---------------- Resultat: ring + optælling ---------------- */
  function initResult() {
    var ring = document.querySelector('[data-ring]');
    if (!ring) return;
    var circle = ring.querySelector('.value');
    var numEl = document.querySelector('[data-count]');
    var pct = parseFloat(ring.getAttribute('data-ring')) || 0;
    if (circle) {
      var r = parseFloat(circle.getAttribute('r')) || 54;
      var circ = 2 * Math.PI * r;
      circle.style.setProperty('--circ', String(circ));
      var apply = function () { circle.style.strokeDashoffset = String(circ * (1 - pct / 100)); };
      if (reduced()) { apply(); }
      else { window.requestAnimationFrame(function () { window.requestAnimationFrame(apply); }); }
    }
    if (!numEl) return;
    var target = parseInt(numEl.getAttribute('data-count'), 10) || 0;
    if (reduced()) { numEl.textContent = String(target); return; }
    var start = null, dur = 1300;
    function frame(ts) {
      if (start === null) start = ts;
      var p = Math.min(1, (ts - start) / dur);
      var eased = 1 - Math.pow(1 - p, 3);
      numEl.textContent = String(Math.round(target * eased));
      if (p < 1) window.requestAnimationFrame(frame);
    }
    window.requestAnimationFrame(frame);
  }

  /* ---------------- Konfetti (egen canvas, ingen biblioteker) ------------ */
  function initConfetti() {
    var canvas = document.getElementById('confetti');
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    var particles = [], raf = null;
    var colors = ['#F2B01E', '#FF6B6B', '#2FA86B', '#3D6BF5', '#9B5DE5', '#2F5BEA', '#C98A4B'];

    function resize() {
      var dpr = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.floor(window.innerWidth * dpr);
      canvas.height = Math.floor(window.innerHeight * dpr);
      canvas.style.width = window.innerWidth + 'px';
      canvas.style.height = window.innerHeight + 'px';
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
    resize();
    window.addEventListener('resize', resize);

    function burst(x, y, n, spread) {
      if (reduced()) return;
      for (var i = 0; i < n; i++) {
        var a = (Math.random() - 0.5) * spread;
        var speed = 6 + Math.random() * 9;
        particles.push({
          x: x, y: y,
          vx: Math.sin(a) * speed,
          vy: -Math.cos(a) * speed - 3,
          w: 7 + Math.random() * 8,
          h: 9 + Math.random() * 9,
          rot: Math.random() * Math.PI,
          vr: (Math.random() - 0.5) * 0.32,
          color: colors[(Math.random() * colors.length) | 0],
          life: 1
        });
      }
      if (!raf) raf = window.requestAnimationFrame(tick);
    }

    function tick() {
      raf = null;
      ctx.clearRect(0, 0, window.innerWidth, window.innerHeight);
      for (var i = particles.length - 1; i >= 0; i--) {
        var p = particles[i];
        p.vy += 0.24;
        p.vx *= 0.985;
        p.vy *= 0.985;
        p.x += p.vx;
        p.y += p.vy;
        p.rot += p.vr;
        p.life -= 0.0055;
        if (p.life <= 0 || p.y > window.innerHeight + 60) { particles.splice(i, 1); continue; }
        ctx.save();
        ctx.globalAlpha = Math.max(0, Math.min(1, p.life));
        ctx.translate(p.x, p.y);
        ctx.rotate(p.rot);
        ctx.fillStyle = p.color;
        ctx.fillRect(-p.w / 2, -p.h / 2, p.w, p.h);
        ctx.restore();
      }
      if (particles.length) raf = window.requestAnimationFrame(tick);
    }

    var api = {
      celebrate: function () {
        var w = window.innerWidth, h = window.innerHeight;
        burst(w * 0.18, h * 0.86, 70, 1.5);
        burst(w * 0.82, h * 0.86, 70, 1.5);
        window.setTimeout(function () { burst(w * 0.5, h * 0.7, 50, 2.6); }, 240);
      }
    };
    window.QuizKonfetti = api;

    each(document.querySelectorAll('[data-confetti-again]'), function (btn) {
      btn.addEventListener('click', function () { api.celebrate(); });
    });

    if (!reduced() && document.querySelector('[data-celebrate]')) {
      window.setTimeout(api.celebrate, 420);
    }
    document.addEventListener('visibilitychange', function () {
      if (document.hidden && raf) { window.cancelAnimationFrame(raf); raf = null; }
    });
  }

  /* ---------------- Start ---------------- */
  function boot() {
    initReveal();
    initKinetic();
    initFloaties();
    initFlip();
    initStepper();
    initOrdering();
    initResult();
    initConfetti();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
