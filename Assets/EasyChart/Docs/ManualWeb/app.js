(function(){
  const state = {
    chapters: [],
    activeId: null,
    search: '',
    expandedGroups: new Set(),
    lang: 'zh'
  };

  const GROUP_STORAGE_KEY = 'easychart_manual_nav_groups_v2';
  const LANG_STORAGE_KEY = 'easychart_manual_lang_v1';

  const GROUPS = [
    { key: 'workflow', title: '概览' },
    { key: 'editor_ui', title: '编辑界面说明' },
    { key: 'charts', title: 'Series详细配置' },
    { key: 'reference', title: '配置项参考' },
    { key: 'other', title: '其他' }
  ];

  const I18N = {
    zh: {
      searchPlaceholder: '搜索章节...',
      onThisPage: '本页目录',
      noHeadings: '没有标题',
      noChapters: '未找到任何手册章节。请将 Markdown 放到 Assets/EasyChart/Docs/Manual',
      groups: {
        workflow: '概览',
        editor_ui: '编辑界面说明',
        charts: 'Series详细配置',
        reference: '配置项参考',
        other: '其他'
      }
    },
    en: {
      searchPlaceholder: 'Search chapters...',
      onThisPage: 'On this page',
      noHeadings: 'No headings',
      noChapters: 'No manual chapters found. Put Markdown under Assets/EasyChart/Docs/Manual',
      groups: {
        workflow: 'Overview',
        editor_ui: 'Editor UI',
        charts: 'Series',
        reference: 'Reference',
        other: 'Other'
      }
    }
  };

  function t(){
    const lang = state.lang || 'zh';
    return I18N[lang] || I18N.zh;
  }

  function loadLang(){
    try{
      const raw = localStorage.getItem(LANG_STORAGE_KEY);
      if(raw === 'en' || raw === 'zh') state.lang = raw;
    }catch(_){
    }
  }

  function saveLang(lang){
    try{
      localStorage.setItem(LANG_STORAGE_KEY, lang);
    }catch(_){
    }
  }

  function getManualDataForLang(lang){
    if(lang === 'en' && window.EASYCHART_MANUAL_EN) return window.EASYCHART_MANUAL_EN;
    if(lang === 'zh' && window.EASYCHART_MANUAL_ZH) return window.EASYCHART_MANUAL_ZH;
    return window.EASYCHART_MANUAL || {};
  }

  function ensureManualDataForLang(lang, done){
    try{
      const has = (lang === 'en') ? !!window.EASYCHART_MANUAL_EN : !!window.EASYCHART_MANUAL_ZH;
      if(has){ done(); return; }
      const s = document.createElement('script');
      s.src = `./manual-data.${lang}.js?t=${Date.now()}`;
      s.onload = done;
      s.onerror = done;
      document.body.appendChild(s);
    }catch(_){
      done();
    }
  }

  function setChaptersFromManualData(manual){
    const data = (manual && manual.chapters) ? manual.chapters : [];
    state.chapters = data.map(ch => {
      const id = ch.id || '';
      return {
        id: id,
        relPath: ch.relPath || '',
        title: ch.title || id,
        content: ch.content || ''
      };
    });
    state.chapters.sort(compareChapters);

    if(state.activeId){
      const exists = state.chapters.some(c => c.id === state.activeId);
      if(!exists && state.chapters.length > 0) state.activeId = state.chapters[0].id;
    }
  }

  function getNavConfig(){
    try{
      const nav = window.EASYCHART_MANUAL_NAV;
      if(!nav || !Array.isArray(nav.groups)) return null;
      const groups = nav.groups
        .filter(g => g && typeof g.key === 'string' && typeof g.title === 'string' && Array.isArray(g.items))
        .map(g => ({ key: g.key, title: g.title, items: g.items.filter(x => typeof x === 'string') }));
      if(groups.length <= 0) return null;
      return { groups };
    }catch(_){
      return null;
    }

  }

  function normalizeNavGroups(groups){
    const dict = t();
    return (groups || []).map(g => {
      const title = (dict.groups && dict.groups[g.key]) ? dict.groups[g.key] : (g.title || g.key);
      return { key: g.key, title, items: g.items };
    });
  }


  function loadExpandedGroups(){
    try{
      const raw = localStorage.getItem(GROUP_STORAGE_KEY);
      if(!raw) return;
      const arr = JSON.parse(raw);
      if(Array.isArray(arr)){
        state.expandedGroups = new Set(arr.filter(x => typeof x === 'string'));
      }
    }catch(_){
    }
  }

  function saveExpandedGroups(){
    try{
      localStorage.setItem(GROUP_STORAGE_KEY, JSON.stringify(Array.from(state.expandedGroups)));
    }catch(_){
    }
  }

  function ensureDefaultExpanded(){
    if(state.expandedGroups.size > 0) return;
    const nav = getNavConfig();
    const groups = (nav && nav.groups) ? nav.groups : GROUPS;
    for(let i=0;i<groups.length;i++){
      const g = groups[i];
      if(!g || !g.key) continue;
      if(g.key === 'other') continue;
      state.expandedGroups.add(g.key);
    }
  }

  function escapeHtml(s){
    return (s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
  }

  function escapeAttr(s){
    return escapeHtml(s).replace(/"/g,'&quot;');
  }

  function slugify(text){
    const s = (text||'').toLowerCase().trim();
    return s
      .replace(/[^a-z0-9\s-_]/g,'')
      .replace(/[\s_]+/g,'-')
      .replace(/-+/g,'-') || 'h';
  }

  function getFileName(relPath){
    return (relPath || '').replace(/\\/g,'/').replace(/.*\//,'');
  }

  function getNavTitle(ch){
    const t = (ch && ch.title) ? String(ch.title) : '';
    return t.replace(/^\s*\d+\s*-\s*/,'').trim() || t;
  }

  function getChapterNumber(relPath){
    const name = getFileName(relPath);
    const m = name.match(/^(\d+)[-_]/);
    if(!m) return Number.POSITIVE_INFINITY;
    const n = parseInt(m[1], 10);
    return Number.isFinite(n) ? n : Number.POSITIVE_INFINITY;
  }

  function compareChapters(a, b){
    const an = getChapterNumber(a.relPath);
    const bn = getChapterNumber(b.relPath);
    if(an !== bn) return an - bn;
    const at = getNavTitle(a).toLowerCase();
    const bt = getNavTitle(b).toLowerCase();
    if(at < bt) return -1;
    if(at > bt) return 1;
    return (a.id || '').localeCompare((b.id || ''), undefined, { sensitivity: 'base' });
  }

  function compareChaptersInGroup(groupKey, a, b){
    return compareChapters(a, b);
  }

  function getGroupKey(ch){
    const n = getChapterNumber(ch.relPath);
    if(n >= 0 && n <= 1) return 'workflow';
    if(n === 2 || (n >= 20 && n <= 29)) return 'editor_ui';
    if(n >= 10 && n <= 19) return 'charts';
    if(n >= 3 && n <= 9) return 'reference';
    return 'other';
  }

  function buildGroups(chapters){
    const nav = getNavConfig();
    if(nav && nav.groups){
      const groupDefs = normalizeNavGroups(nav.groups);
      const map = new Map();
      const idToGroup = new Map();
      const groupOrder = [];

      groupDefs.forEach(g => {
        map.set(g.key, { key: g.key, title: g.title, children: [] });
        groupOrder.push(g.key);
        for(let i=0;i<g.items.length;i++){
          const id = g.items[i];
          if(!idToGroup.has(id)) idToGroup.set(id, { key: g.key, index: i });
        }
      });

      const otherKey = map.has('other') ? 'other' : null;
      if(!otherKey){
        map.set('other', { key: 'other', title: '其他', children: [] });
        groupOrder.push('other');
      }

      chapters.forEach(ch => {
        const hit = idToGroup.get(ch.id);
        const key = hit ? hit.key : 'other';
        if(!map.has(key)) map.set(key, { key, title: key, children: [] });
        map.get(key).children.push(ch);
      });

      groupDefs.forEach(g => {
        const orderMap = new Map();
        for(let i=0;i<g.items.length;i++) orderMap.set(g.items[i], i);
        const grp = map.get(g.key);
        if(!grp) return;
        grp.children.sort((a, b) => {
          const ai = orderMap.has(a.id) ? orderMap.get(a.id) : Number.POSITIVE_INFINITY;
          const bi = orderMap.has(b.id) ? orderMap.get(b.id) : Number.POSITIVE_INFINITY;
          if(ai !== bi) return ai - bi;
          return compareChapters(a, b);
        });
      });

      const otherGrp = map.get('other');
      if(otherGrp) otherGrp.children.sort((a, b) => compareChaptersInGroup('other', a, b));
      return groupOrder.map(k => map.get(k)).filter(g => g && g.children.length > 0);
    }

    const map = new Map();
    const fallbackGroups = normalizeNavGroups(GROUPS);
    fallbackGroups.forEach(g => map.set(g.key, { key: g.key, title: g.title, children: [] }));
    chapters.forEach(ch => {
      const key = getGroupKey(ch);
      if(!map.has(key)) map.set(key, { key, title: key, children: [] });
      map.get(key).children.push(ch);
    });
    map.forEach(g => g.children.sort((a, b) => compareChaptersInGroup(g.key, a, b)));
    return fallbackGroups.map(g => map.get(g.key)).filter(g => g && g.children.length > 0);
  }

  function inlineToHtml(text){
    if(!text) return '';
    let out = escapeHtml(text);

    out = out.replace(/`([^`]+)`/g, (m, g1) => `<code class="inline">${g1}</code>`);
    out = out.replace(/\*\*([^*]+)\*\*/g, (m, g1) => `<strong>${g1}</strong>`);
    out = out.replace(/(^|[^*])\*([^*]+)\*([^*]|$)/g, (m, p1, g1, p2) => `${p1}<em>${g1}</em>${p2}`);

    out = out.replace(/\[(?<t>[^\]]+)\]\((?<u>[^\)]+)\)/g, (m, _1, _2, _3, _4, groups) => {
      const t = groups.t;
      let u = groups.u;
      u = u.replace(/\\/g,'/');

      if(u.endsWith('.md')){
        const id = u.replace(/.*\//,'').replace(/\.md$/,'');
        return `<a href="#/${encodeURIComponent(id)}">${t}</a>`;
      }

      const mdAnchor = u.match(/([^#]+)\.md#(.+)$/);
      if(mdAnchor){
        const id = mdAnchor[1].replace(/.*\//,'');
        const anchor = mdAnchor[2];
        return `<a href="#/${encodeURIComponent(id)}#${encodeURIComponent(anchor)}">${t}</a>`;
      }

      if(u.startsWith('#')){
        return `<a href="${escapeAttr(u)}">${t}</a>`;
      }

      return `<a href="${escapeAttr(u)}" target="_blank" rel="noreferrer">${t}</a>`;
    });
    return out;
  }

  function markdownToHtml(md){
    if(!md) return '';
    const lines = md.replace(/\r\n/g,'\n').replace(/\r/g,'\n').split('\n');

    let inCode = false;
    let inUl = false;
    let inOl = false;
    let para = '';

    const parts = [];

    function flushPara(){
      if(!para.trim()) return;
      parts.push(`<p>${inlineToHtml(para.trim())}</p>`);
      para = '';
    }

    function closeLists(){
      if(inUl){ parts.push('</ul>'); inUl = false; }
      if(inOl){ parts.push('</ol>'); inOl = false; }
    }

    for(let i=0;i<lines.length;i++){
      const raw = lines[i] || '';
      const t = raw.replace(/\s+$/,'');

      if(t.trimStart().startsWith('```')){
        if(!inCode){
          flushPara();
          closeLists();
          inCode = true;
          parts.push('<pre><code>');
        }else{
          inCode = false;
          parts.push('</code></pre>');
        }
        continue;
      }

      if(inCode){
        parts.push(escapeHtml(t) + '\n');
        continue;
      }

      if(!t.trim()){
        flushPara();
        closeLists();
        continue;
      }

      if(t.startsWith('>')){
        flushPara();
        closeLists();
        const qt = t.replace(/^>\s?/, '').trim();
        parts.push(`<blockquote>${inlineToHtml(qt)}</blockquote>`);
        continue;
      }

      if(t.startsWith('---')){
        flushPara();
        closeLists();
        parts.push('<hr/>');
        continue;
      }

      if(t.startsWith('#')){
        flushPara();
        closeLists();
        let level = 0;
        while(level < t.length && t[level] === '#') level++;
        level = Math.max(1, Math.min(3, level));
        const text = t.slice(level).trim();
        const id = slugify(text);
        parts.push(`<h${level} id="${id}">${inlineToHtml(text)}</h${level}>`);
        continue;
      }

      if(t.startsWith('- ') || t.startsWith('* ')){
        flushPara();
        if(inOl){ parts.push('</ol>'); inOl = false; }
        if(!inUl){ parts.push('<ul>'); inUl = true; }
        parts.push(`<li>${inlineToHtml(t.slice(2).trim())}</li>`);
        continue;
      }

      const m = t.match(/^(\d+)\.\s+(.+)$/);
      if(m){
        flushPara();
        if(inUl){ parts.push('</ul>'); inUl = false; }
        if(!inOl){ parts.push('<ol>'); inOl = true; }
        parts.push(`<li>${inlineToHtml(m[2].trim())}</li>`);
        continue;
      }

      para += (para ? ' ' : '') + t.trim();
    }

    flushPara();
    closeLists();
    if(inCode) parts.push('</code></pre>');

    return parts.join('');
  }

  function buildTocFromContent(root){
    const toc = [];
    const hs = root.querySelectorAll('h1, h2, h3');
    hs.forEach(h => {
      const level = Number(h.tagName.slice(1));
      const title = h.textContent || '';
      const id = h.getAttribute('id') || '';
      if(!id) return;
      toc.push({level, title, id});
    });
    return toc;
  }

  function render(){
    const nav = document.getElementById('nav');
    const content = document.getElementById('content');
    const tocEl = document.getElementById('toc');
    const dict = t();
    const search = (state.search || '').toLowerCase().trim();

    const searchInput = document.getElementById('search');
    if(searchInput) searchInput.placeholder = dict.searchPlaceholder;

    nav.innerHTML = '';
    const visible = state.chapters.filter(ch => {
      if(!search) return true;
      return (ch.title||'').toLowerCase().includes(search) || getNavTitle(ch).toLowerCase().includes(search) || (ch.relPath||'').toLowerCase().includes(search);
    });

    const groups = buildGroups(visible);
    const forceExpand = !!search;

    groups.forEach(g => {
      const header = document.createElement('button');
      header.type = 'button';
      header.className = 'nav-group-header';

      const toggle = document.createElement('span');
      toggle.className = 'nav-group-toggle';
      header.appendChild(toggle);

      const title = document.createElement('span');
      title.className = 'nav-group-title';
      title.textContent = g.title;
      header.appendChild(title);

      const expanded = forceExpand || state.expandedGroups.has(g.key);
      header.setAttribute('data-expanded', expanded ? 'true' : 'false');

      header.addEventListener('click', () => {
        const nowExpanded = !(header.getAttribute('data-expanded') === 'true');
        header.setAttribute('data-expanded', nowExpanded ? 'true' : 'false');
        if(nowExpanded) state.expandedGroups.add(g.key);
        else state.expandedGroups.delete(g.key);
        saveExpandedGroups();
        render();
      });

      const groupWrap = document.createElement('div');
      groupWrap.className = 'nav-group';
      groupWrap.appendChild(header);

      const children = document.createElement('div');
      children.className = 'nav-children';
      children.style.display = expanded ? 'block' : 'none';

      g.children.forEach(ch => {
        const a = document.createElement('a');
        a.className = 'nav-item' + (ch.id === state.activeId ? ' active' : '');
        a.href = `#/${encodeURIComponent(ch.id)}`;
        a.textContent = getNavTitle(ch);
        children.appendChild(a);
      });

      groupWrap.appendChild(children);
      nav.appendChild(groupWrap);
    });

    const active = state.chapters.find(c => c.id === state.activeId) || visible[0] || state.chapters[0];
    if(!active){
      content.innerHTML = '<div class="notice">' + escapeHtml(dict.noChapters) + '</div>';
      tocEl.innerHTML = '';
      return;
    }

    if(active.id !== state.activeId){
      state.activeId = active.id;
      location.hash = `#/${encodeURIComponent(active.id)}`;
      return;
    }

    const html = markdownToHtml(active.content);
    content.innerHTML = html;

    const toc = buildTocFromContent(content);
    tocEl.innerHTML = '';
    if(toc.length > 0){
      const title = document.createElement('div');
      title.className = 'toc-title';
      title.textContent = dict.onThisPage;
      tocEl.appendChild(title);

      toc.forEach(it => {
        const a = document.createElement('a');
        a.href = `#/${encodeURIComponent(active.id)}#${encodeURIComponent(it.id)}`;
        a.textContent = it.title;
        a.style.paddingLeft = (it.level - 1) * 10 + 'px';
        tocEl.appendChild(a);
      });
    }else{
      tocEl.innerHTML = '<div class="toc-title">' + escapeHtml(dict.onThisPage) + '</div><div class="notice">' + escapeHtml(dict.noHeadings) + '</div>';
    }

    const anchor = decodeURIComponent((location.hash.split('#')[2] || '').trim());
    if(anchor){
      const el = document.getElementById(anchor);
      if(el) el.scrollIntoView({block:'start'});
    }else{
      window.scrollTo(0,0);
    }
  }

  function syncFromHash(){
    const h = location.hash || '';
    const m = h.match(/^#\/([^#]+)/);
    if(m && m[1]){
      state.activeId = decodeURIComponent(m[1]);
    }
  }

  function init(){
    loadLang();

    try{
      const req = window.EASYCHART_MANUAL_OPEN || {};
      const hasHash = !!(location.hash && location.hash.length > 1);
      if(!hasHash && req && req.chapterId){
        location.hash = `#/${encodeURIComponent(req.chapterId)}` + (req.anchor ? `#${encodeURIComponent(req.anchor)}` : '');
      }
    }catch(_){
    }

    setChaptersFromManualData(getManualDataForLang(state.lang));
    loadExpandedGroups();
    ensureDefaultExpanded();

    const langSelect = document.getElementById('lang');
    if(langSelect){
      langSelect.value = (state.lang === 'en') ? 'en' : 'zh';
      langSelect.addEventListener('change', () => {
        state.lang = (langSelect.value === 'en') ? 'en' : 'zh';
        saveLang(state.lang);
        ensureManualDataForLang(state.lang, () => {
          setChaptersFromManualData(getManualDataForLang(state.lang));
          syncFromHash();
          render();
        });
      });
    }

    const searchInput = document.getElementById('search');
    if(searchInput){
      searchInput.addEventListener('input', () => {
        state.search = searchInput.value || '';
        render();
      });
    }

    window.addEventListener('hashchange', () => {
      syncFromHash();
      render();
    });

    syncFromHash();
    if(!state.activeId && state.chapters.length > 0) state.activeId = state.chapters[0].id;
    render();
  }

  if(document.readyState === 'loading'){
    document.addEventListener('DOMContentLoaded', init);
  }else{
    init();
  }
})();
