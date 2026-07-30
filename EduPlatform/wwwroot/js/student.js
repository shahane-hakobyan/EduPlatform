(function () {
  var header = document.querySelector(".site-header");
  if (header) {
    var updateHeaderShadow = function () {
      header.classList.toggle("is-scrolled", window.scrollY > 8);
    };
    updateHeaderShadow();
    window.addEventListener("scroll", updateHeaderShadow, { passive: true });
  }

  var revealTargets = document.querySelectorAll(".reveal, .reveal-stagger");
  if (!revealTargets.length) return;

  if (!("IntersectionObserver" in window)) {
    revealTargets.forEach(function (el) { el.classList.add("in-view"); });
    return;
  }

  var observer = new IntersectionObserver(
    function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add("in-view");
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.15, rootMargin: "0px 0px -60px 0px" }
  );

  revealTargets.forEach(function (el) { observer.observe(el); });
})();

(function () {
  var toggle = document.getElementById("langSwitchToggle");
  var menu = document.getElementById("langSwitchMenu");
  if (!toggle || !menu) return;

  toggle.addEventListener("click", function (e) {
    e.stopPropagation();
    var isOpen = menu.classList.toggle("show");
    toggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
  });

  document.addEventListener("click", function (e) {
    if (!menu.contains(e.target) && e.target !== toggle) {
      menu.classList.remove("show");
      toggle.setAttribute("aria-expanded", "false");
    }
  });
})();

(function () {
  var root = document.getElementById("teamCarousel");
  if (!root) return;

  var textSlides = root.querySelectorAll(".carousel-slide-content");
  var imgSlides = root.querySelectorAll(".carousel-slide-img");
  var dots = root.querySelectorAll("#teamDots span");
  var prevBtn = document.getElementById("teamPrevBtn");
  var nextBtn = document.getElementById("teamNextBtn");
  if (!textSlides.length || !prevBtn || !nextBtn) return;

  var total = textSlides.length;
  var current = 0;
  var animating = false;
  var LEAVE_MS = 180;
  var ENTER_MS = 350;
  var slidesWrap = root.querySelector(".team-carousel-slides");

  function stabilizeHeight() {
    if (!slidesWrap) return;
    slidesWrap.style.minHeight = "";
    var tallest = 0;
    textSlides.forEach(function (slide) {
      var wasActive = slide.classList.contains("is-active");
      if (!wasActive) slide.style.display = "block";
      tallest = Math.max(tallest, slide.offsetHeight);
      if (!wasActive) slide.style.display = "";
    });
    slidesWrap.style.minHeight = tallest + "px";
  }

  stabilizeHeight();
  window.addEventListener("resize", stabilizeHeight);

  function goTo(targetIndex) {
    var next = ((targetIndex % total) + total) % total;
    if (animating || next === current) return;
    animating = true;

    var outgoingText = textSlides[current];
    var outgoingImg = imgSlides[current];
    outgoingText.classList.remove("is-active");
    outgoingText.classList.add("is-leaving");
    outgoingImg.classList.remove("is-active");

    window.setTimeout(function () {
      outgoingText.classList.remove("is-leaving");

      var incomingText = textSlides[next];
      incomingText.classList.add("is-active", "is-entering");
      imgSlides[next].classList.add("is-active");

      // Force a reflow so removing "is-entering" transitions instead of jumping.
      void incomingText.offsetWidth;
      incomingText.classList.remove("is-entering");

      dots.forEach(function (dot, i) {
        dot.classList.toggle("active", i === next);
      });

      current = next;
      window.setTimeout(function () { animating = false; }, ENTER_MS);
    }, LEAVE_MS);
  }

  prevBtn.addEventListener("click", function (e) {
    e.preventDefault();
    goTo(current - 1);
  });
  nextBtn.addEventListener("click", function (e) {
    e.preventDefault();
    goTo(current + 1);
  });
  dots.forEach(function (dot, i) {
    dot.addEventListener("click", function () { goTo(i); });
  });
})();
