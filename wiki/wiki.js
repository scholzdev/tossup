
(function () {
  var base = document.documentElement.dataset.base || '', assets = document.documentElement.dataset.assets || '';
  var q = document.getElementById('q'), box = document.getElementById('results');
  if (q) {
    q.addEventListener('input', function () {
      var t = q.value.trim().toLowerCase();
      if (!t) { box.style.display = 'none'; return; }
      var hits = SEARCH_INDEX.filter(function (e) { return (e.name + ' ' + (e.alt || '')).toLowerCase().indexOf(t) >= 0; }).slice(0, 12);
      box.innerHTML = hits.map(function (e) { return '<a href="' + base + e.url + '"><img src="' + base + e.icon + '" alt=""><span>' + e.name + '</span><small>' + e.type + '</small></a>'; }).join('') || '<a>No results</a>';
      box.style.display = 'block';
    });
    document.addEventListener('click', function (e) { if (!box.contains(e.target) && e.target !== q) box.style.display = 'none'; });
  }
  var table = document.getElementById('coin-table');
  if (table) {
    var state = {rarity: null, char: null, text: ''};
    function apply() {
      table.querySelectorAll('tbody tr').forEach(function (r) {
        var ok = (!state.rarity || r.dataset.rarity === state.rarity) && (!state.char || r.dataset.chars.split(' ').indexOf(state.char) >= 0) && (!state.text || r.dataset.name.indexOf(state.text) >= 0);
        r.style.display = ok ? '' : 'none';
      });
    }
    document.querySelectorAll('.filters button[data-rarity]').forEach(function (b) { b.onclick = function () { var on = b.classList.toggle('on'); document.querySelectorAll('.filters button[data-rarity]').forEach(function (o) { if (o !== b) o.classList.remove('on'); }); state.rarity = on ? b.dataset.rarity : null; apply(); }; });
    document.querySelectorAll('.filters button[data-char]').forEach(function (b) { b.onclick = function () { var on = b.classList.toggle('on'); document.querySelectorAll('.filters button[data-char]').forEach(function (o) { if (o !== b) o.classList.remove('on'); }); state.char = on ? b.dataset.char : null; apply(); }; });
    document.getElementById('coinq').oninput = function (e) { state.text = e.target.value.trim().toLowerCase(); apply(); };
    document.getElementById('clear').onclick = function () { state = {rarity: null, char: null, text: ''}; document.getElementById('coinq').value = ''; document.querySelectorAll('.filters button').forEach(function (b) { b.classList.remove('on'); }); apply(); };
  }
})();
