(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const esc = value => String(value ?? '').replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));
  const statusName = value => ({InCollection:'В колекция',ForTrade:'За размяна',Traded:'Разменен',PendingApproval:'Чака оценител',OnAuction:'На търг',Sold:'Продаден'}[value] || value);
  const roleName = value => ({Collector:'Колекционер',Evaluator:'Оценител',Admin:'Администратор'}[value] || value);
  const money = value => new Intl.NumberFormat('bg-BG',{style:'currency',currency:'EUR'}).format(Number(value||0));
  const mascotStyles = [
    {id:'classic',name:'Класически колекционер',coat:'#2474b9',trim:'#8cdbff',bag:'#f0a84f'},
    {id:'curator',name:'Музеен куратор',coat:'#644a91',trim:'#d4b4ff',bag:'#e6c878'},
    {id:'adventurer',name:'Ловец на находки',coat:'#267b68',trim:'#a4efcb',bag:'#bd7547'},
    {id:'neon',name:'Неонов стил',coat:'#bd477f',trim:'#ffb2df',bag:'#60dbea'}
  ];
  function mascotSvg(styleId) {
    const s=mascotStyles.find(x=>x.id===styleId)||mascotStyles[0];
    return `<svg class="mascot-illustration" viewBox="0 0 160 180" role="img" aria-label="Кликс — уличен ракун колекционер">
      <defs><linearGradient id="fur-${s.id}" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#d2d1cb"/><stop offset="1" stop-color="#777a80"/></linearGradient><linearGradient id="hoodie-${s.id}" x1="0" y1="0" x2="1" y2="1"><stop stop-color="${s.trim}"/><stop offset=".42" stop-color="${s.coat}"/><stop offset="1" stop-color="#222127"/></linearGradient></defs>
      <ellipse cx="80" cy="169" rx="34" ry="6" fill="#020b18" opacity=".56"/>
      <!-- New character: a cheeky raccoon in a streetwear hoodie and high tops. -->
      <path d="M55 120 38 124l-8 16q-3 7 3 9l8-1 11-15 9 1Zm50 0 16 5 8 17q3 7-3 9l-8-1-11-15-8 1Z" fill="url(#hoodie-${s.id})" stroke="#17171b" stroke-width="4" stroke-linejoin="round"/>
      <path d="M60 115q20-12 40 0l14 38-10 12H55l-10-12Z" fill="url(#hoodie-${s.id})" stroke="#17171b" stroke-width="4" stroke-linejoin="round"/>
      <path d="M67 119q13 10 26 0l-4 25H71Z" fill="#27262c" stroke="#17171b" stroke-width="2.5"/><path d="M72 139h16" stroke="${s.trim}" stroke-width="3" stroke-linecap="round"/>
      <path d="M60 155h40l-2 12H62Z" fill="#29282e" stroke="#17171b" stroke-width="3"/><path d="M55 164q9-4 17 1l-2 6H47q-4-2 0-5Zm50 0q-9-4-17 1l2 6h23q4-2 0-5Z" fill="#242329" stroke="#101014" stroke-width="3" stroke-linejoin="round"/><path d="m49 167 14 2m37-2 14 2" stroke="#f5d56e" stroke-width="2"/>
      <path d="M98 121q11 4 16 13l-7 20-16-5Z" fill="${s.bag}" stroke="#17171b" stroke-width="3"/><path d="m98 137 12 4-3 9-12-4Z" fill="#ffe9a8" stroke="#17171b" stroke-width="2"/>
      <path d="M57 122 80 141l22-19" fill="none" stroke="#f4d785" stroke-width="2.5"/><path d="m78 140 2-4 2 4v10l-2 3-2-3Z" fill="#f5d56e" stroke="#17171b" stroke-width="1.5"/>
      <g class="toy-head">
        <!-- round ears and plush raccoon head -->
        <path d="M48 55 43 30q-1-7 6-4l20 16Zm44-13 18-16q6-5 7 3l-2 27Z" fill="#8f9296" stroke="#17171b" stroke-width="4" stroke-linejoin="round"/><path d="m49 33 13 11-12 4Zm60 0L97 45l12 3Z" fill="#bd7777"/>
        <path d="M48 53q3-29 32-33 31 2 34 34l-4 42q-5 22-30 25-25-3-29-25Z" fill="url(#fur-${s.id})" stroke="#17171b" stroke-width="4"/>
        <!-- unmistakable raccoon mask -->
        <path d="M48 70q18-14 32-3 14-11 33 2l-5 18q-14 10-28-1-13 11-27 1Z" fill="#343238" stroke="#17171b" stroke-width="2.5"/>
        <ellipse class="toy-eye" cx="65" cy="77" rx="4" ry="5" fill="#fff4d8"/><ellipse class="toy-eye" cx="96" cy="77" rx="4" ry="5" fill="#fff4d8"/><circle cx="66" cy="78" r="2.3" fill="#17171b"/><circle cx="95" cy="78" r="2.3" fill="#17171b"/>
        <path d="M75 89q5-5 10 0l-2 5h-6Z" fill="#211e24" stroke="#17171b" stroke-width="1.5"/><path d="M71 101q9 7 18 0" fill="none" stroke="#29232a" stroke-width="3" stroke-linecap="round"/><path d="M60 96q5 3 10 0m20 0q5 3 10 0" fill="none" stroke="#f5e6d2" stroke-width="2"/>
        <!-- tilted snapback cap -->
        <path d="M48 55q3-24 30-29 23-3 35 16l-5 15q-19-10-35-1Z" fill="#222127" stroke="#111115" stroke-width="4" stroke-linejoin="round"/><path d="M50 52q22-12 49-6l18 8q3 3-1 6-39-13-65 1Z" fill="${s.coat}" stroke="#17171b" stroke-width="3" stroke-linejoin="round"/><path d="m81 34-2 11" stroke="${s.trim}" stroke-width="4" stroke-linecap="round"/><circle cx="79" cy="31" r="3" fill="#f5d56e"/>
      </g>
      <!-- prize card with a comic burst -->
      <path d="M23 109q0-4 4-3l19 7q3 1 2 5l-5 24q-1 4-4 3l-19-7q-3-1-2-5Z" fill="#fff1d1" stroke="#17171b" stroke-width="3"/><path d="m26 116 15 5-3 16-15-5Z" fill="${s.coat}" stroke="#17171b" stroke-width="2"/><path d="m33 119 2 4 4 1-4 2-2 4-2-4-4-1 4-2Z" fill="#f5d56e"/>
      <path class="mascot-star" d="m132 32 3 7 7-2-4 7 6 5-8 1-2 8-4-7-8 1 5-6-4-7 8 3Z" fill="#ffdb55" stroke="#17171b" stroke-width="2"/>
    </svg>`;
  }
  let selectedMascotId = 'classic';  let me = null, role = null, profile = null, view = 'market', authMode = 'login', categories = [], items = [], toastTimer, conversationTimer = null, notificationTimer = null;
  const rarityLevels = [{id:'common',name:'Обикновена',color:'#aaa'}, {id:'rare',name:'Рядка',color:'#42b7e8'}, {id:'epic',name:'Епична',color:'#b975f4'}, {id:'legendary',name:'Легендарна',color:'#f3ca4f'}];
  let collectionExtras = {rarities:{},showcase:[],daily:{date:'',visited:[],login:false}};
  function dateKey(){const d=new Date();return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;}
  function extrasKey(){return `collection-extras:${me?.sub||'guest'}`;}
  function loadExtras(){try{const saved=JSON.parse(localStorage.getItem(extrasKey())||'{}');collectionExtras={...collectionExtras,...saved,rarities:saved.rarities||{},showcase:saved.showcase||[],daily:saved.daily||{date:'',visited:[],login:false}};}catch{collectionExtras={rarities:{},showcase:[],daily:{date:'',visited:[],login:false}};}const today=dateKey();if(collectionExtras.daily.date!==today)collectionExtras.daily={date:today,visited:[],login:true};else collectionExtras.daily.login=true;saveExtras();}
  function saveExtras(){localStorage.setItem(extrasKey(),JSON.stringify(collectionExtras));}
  function avatarMarkup(url, name, extra = '') { const initial = esc((name || '?').trim().charAt(0).toLocaleUpperCase('bg')); return `<span class="avatar ${extra}"><span>${initial}</span>${url ? `<img src="${esc(url)}" alt="" onerror="this.remove()">` : ''}</span>`; }

  function identity() {
    if (!api.token) return null;
    try { const part = api.token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/'); return JSON.parse(decodeURIComponent(escape(atob(part)))); }
    catch { api.token = null; return null; }
  }
  function notify(message, error = false) {
    const box = $('#toast'); box.textContent = message; box.className = `toast show${error ? ' error' : ''}`;
    clearTimeout(toastTimer); toastTimer = setTimeout(() => { box.className = 'toast'; }, 3500);
  }
  async function safe(task) { try { return await task(); } catch (e) { notify(e.message || 'Възникна грешка.', true); return undefined; } }
  function renderMascot() {
    let style=mascotStyles.find(x=>x.id===selectedMascotId)||mascotStyles[0];selectedMascotId=style.id;
    $('#mascotArt').dataset.kind=style.id;$('#mascotArt').setAttribute('aria-label',`Кликс — ${style.name}`);$('#mascotArt').innerHTML=mascotSvg(style.id);
    $('#mascotCaption').textContent=`Кликс · ${style.name}`;
    $('#mascotMessage').querySelector('b').textContent='Рядка находка? Остави я на мен.';
    $('#mascotOptions').innerHTML=mascotStyles.map(x=>`<button type="button" class="mascot-option ${x.id===style.id?'selected':''}" data-mascot="${x.id}" aria-pressed="${x.id===style.id}"><span class="style-swatch" style="--style-color:${x.coat};--style-trim:${x.trim}"></span><b>${esc(x.name)}</b></button>`).join('');
  }  function start() {
    clearInterval(notificationTimer); notificationTimer = null;
    me = identity(); role = me?.role || null; loadExtras();
    selectedMascotId = localStorage.getItem(`collection-mascot:${me?.sub||'guest'}`) || 'classic'; renderMascot();
    $('#authView').hidden = !!me; $('#appView').hidden = !me; $('#mainNav').hidden = !me; $('#userMenu').hidden = !me;
    if (!me) return;
    $('#userName').textContent = me.name || roleName(role); $('#userAvatar').innerHTML = avatarMarkup(null, me.name || 'К');
    $('#adminNav').hidden = role !== 'Admin'; $('#reviewNav').hidden = role !== 'Evaluator'; $('#auctionsNav').hidden = role === 'Evaluator'; $('#newItemButton').hidden = role !== 'Collector';
    $$('.nav-link[data-view="mine"],.nav-link[data-view="dossier"],.nav-link[data-view="trades"],.nav-link[data-view="messages"]').forEach(b => b.hidden = role !== 'Collector');
    $('#communityCount').textContent = '—';
    safe(async()=>{profile=await api.get('/api/profile/me');$('#userName').textContent=profile.displayName;$('#userAvatar').innerHTML=avatarMarkup(profile.avatarUrl,profile.displayName);});
    loadCategories().then(() => loadView('market'));
    if (role === 'Collector') refreshTradeBadge();
    refreshNotificationBadge();
    notificationTimer = setInterval(() => { refreshNotificationBadge(); if (view === 'notifications') renderNotifications(); }, 30000);
  }
  async function loadCategories() {
    categories = await api.get('/api/categories');
    const select = $('#categoryFilter');
    select.innerHTML = '<option value="">Всички категории</option>' + categories.map(x => `<option value="${x.id}">${esc(x.name)}</option>`).join('');
  }
  function selectView(next) {
    view = next; $$('.nav-link').forEach(b => b.classList.toggle('active', b.dataset.view === next));
    $('#filterBar').hidden = !['market','mine','review','auctions'].includes(next); $('#adminSummary').hidden = next !== 'admin';
    $('#dashboardSummary').hidden = next !== 'market';
    $('#pageTitle').innerHTML = ({market:'Не просто вещи.<br><em>Малки открития.</em>',auctions:'Редки находки.<br><em>Големи наддавания.</em>',mine:'Твоите истории.<br><em>В една колекция.</em>',dossier:'Твоето колекционерско<br><em>досие.</em>',trades:'Добрата находка<br><em>намира нов дом.</em>',messages:'Разговори между<br><em>колекционери.</em>',notifications:'Всичко важно.<br><em>На едно място.</em>',profile:'Твоят профил.<br><em>Твоята история.</em>',admin:'Зад кулисите<br><em>на общността.</em>',review:'Стойността си личи<br><em>в детайлите.</em>'})[next];
    $('#pageIntro').textContent = ({market:'Намери следващото попълнение за своята колекция.',auctions:'Разгледай активните търгове и наддавай за следващата си находка.',mine:'Подреди, допълни и пусни предмет за размяна или търг.',dossier:'Следи напредъка си, подреди витрината и отключи нови значки.',trades:'Прегледай предложенията и договорените размени.',messages:'Пиши директно на хората, с които споделяш интерес.',notifications:'Оферти, наддавания и съобщения от колекционерския клуб.',profile:'Покажи на общността какво събираш и какво те вдъхновява.',admin:'Управлявай категориите и следи активността.',review:'Одобри предметите, които са заявени за търг.'})[next];
    $('#pageEyebrow').innerHTML = `<i></i> ${next==='admin'?'АДМИНИСТРАТИВНО УПРАВЛЕНИЕ':next==='review'?'ПРОВЕРКА НА ПРЕДМЕТИ':next==='profile'?'КОЛЕКЦИОНЕРСКИ ПРОФИЛ':next==='notifications'?'ИЗВЕСТИЯ ОТ КЛУБА':next==='auctions'?'АКТИВНИ АУКЦИОНИ':'ДИГИТАЛЕН КОЛЕКЦИОНЕРСКИ КЛУБ'}`;
    loadView(next);
  }
  async function loadView(next = view) {
    view = next; $('#content').innerHTML = '<div class="loading">Подготвяме колекцията…</div>';
    if (['market','mine','review','auctions'].includes(next)) return loadItems(next);
    if (next === 'trades') return renderTrades();
    if (next === 'dossier') return renderDossier();
    if (next === 'messages') return renderContacts();
    if (next === 'notifications') return renderNotifications();
    if (next === 'profile') return renderProfile();
    if (next === 'admin') return renderAdmin();
  }
  async function loadItems(which) {
    const params = new URLSearchParams(); const search = $('#searchInput').value.trim(); const cat = $('#categoryFilter').value; const status = $('#statusFilter').value;
    if (search) params.set('search', search); if (cat) params.set('categoryId', cat); if (status) params.set('status', status);
    if (which === 'review') params.set('status','PendingApproval');
    if (which === 'auctions') params.set('status','OnAuction');
    const data = await safe(() => api.get(`/api/items?${params}`)); if (!data) return;
    items = which === 'mine' ? data.filter(x => x.ownerId === Number(me.sub)) : data;
    $('#resultCount').textContent = `${items.length} ${items.length === 1 ? 'предмет' : 'предмета'}`;
    $('#communityCount').textContent = data.length;
    $('#dashboardSummary').hidden = which !== 'market';
    if(which==='market') $('#dashboardSummary').innerHTML=[['Предмети',data.length],['В колекции',data.filter(x=>x.status==='InCollection').length],['Активни търгове',data.filter(x=>x.status==='OnAuction').length],['Категории',categories.length]].map(([label,value])=>`<div class="summary-card"><span>${label}</span><b>${value}</b></div>`).join('');
    if (!items.length) {
      const text = which==='mine'?'Колекцията ти започва с една находка.':which==='auctions'?'В момента няма активни аукциони. Провери отново по-късно или разгледай таблото.':'Тук засега няма предмети. Опитай с друга категория или търсене.';
      $('#content').innerHTML = `<div class="empty-state"><span>✳</span><h3>${which==='mine'?'Още няма добавени предмети.':which==='auctions'?'Няма активни аукциони засега.':'Не открихме съвпадение.'}</h3><p>${text}</p>${which==='mine'?'<button class="button primary" data-new>Добави първия предмет</button>':''}</div>`;
      return;
    }
    if(which==='mine'){
      $('#content').innerHTML=`<div class="collection-table-wrap"><table class="collection-table"><thead><tr><th>Предмет</th><th>Категория</th><th>Статус</th><th>Добавен</th><th>Описание</th><th>Действия</th></tr></thead><tbody>${items.map(x=>`<tr data-item="${x.id}"><td><div class="collection-item">${x.imageUrl?`<img src="${esc(x.imageUrl)}" alt="" loading="lazy">`:avatarMarkup(null,x.name,'collection-thumb')}<span><b>${esc(x.name)}</b><small>${avatarMarkup(x.ownerAvatarUrl,x.ownerName,'row-owner-avatar')} ${esc(x.ownerName)}</small></span></div></td><td>${esc(x.category)}</td><td><span class="status-pill ${x.status==='PendingApproval'?'pending':''}">${esc(statusName(x.status))}</span>${x.status==='PendingApproval'?'<small class="collection-status-note">Изпратен за одобрение от оценител</small>':''}</td><td>${new Date(x.createdAt).toLocaleDateString('bg-BG')}</td><td class="collection-description">${esc(x.description||'—')}</td><td><div class="collection-actions">${x.status==='InCollection'?`<button class="button secondary" data-action="edit" data-id="${x.id}">Редактирай</button><button class="button ghost" data-action="trade-list" data-id="${x.id}">＋ За размяна</button><button class="button primary" data-action="auction-request" data-id="${x.id}">↑ Заяви търг</button>`:x.status==='ForTrade'?`<button class="button secondary" data-action="withdraw-trade" data-id="${x.id}">Оттегли размяната</button>`:x.status==='PendingApproval'?`<button class="button secondary" data-action="withdraw-auction" data-id="${x.id}">Оттегли заявката</button>`:''}<button class="button ghost" data-action="details" data-id="${x.id}">Детайли</button></div></td></tr>`).join('')}</tbody></table></div>`;
      return;
    }
    $('#content').innerHTML = items.map(x => `<article class="item-card" data-item="${x.id}"><div class="item-image">${x.imageUrl?`<img src="${esc(x.imageUrl)}" alt="${esc(x.name)}" loading="lazy">`:`<div class="placeholder">${esc((x.name||'К').charAt(0))}</div>`}<span class="item-tag ${x.status==='OnAuction'?'auction':x.status==='ForTrade'?'trade':''}">${esc(statusName(x.status))}</span><span class="image-shade"></span></div><div class="item-body"><span class="item-category">${esc(x.category)}</span><h3>${esc(x.name)}</h3><p>${esc((x.description||'Предмет с история, готов за ново приключение.').slice(0,105))}</p><div class="item-meta"><span class="item-owner">${avatarMarkup(x.ownerAvatarUrl,x.ownerName,'owner-avatar')}<span>${esc(x.ownerName)}</span></span><b class="item-price">${x.status==='OnAuction'&&x.currentBid!=null?money(x.currentBid):x.auctionEndsAt?'До '+new Date(x.auctionEndsAt).toLocaleDateString('bg-BG'):''}</b></div></div></article>`).join('');
  }
  async function renderDossier(){
    const [all,trades]=await Promise.all([safe(()=>api.get('/api/items')),safe(()=>api.get('/api/trades'))]);if(!all)return;
    const owned=all.filter(x=>x.ownerId===Number(me.sub)&&!['Traded','Sold'].includes(x.status));
    const ownedByCategory=categories.map(c=>({name:c.name,count:owned.filter(x=>x.category===c.name).length}));
    const covered=ownedByCategory.filter(x=>x.count>0).length,coverage=categories.length?Math.round(covered/categories.length*100):0;
    const validIds=new Set(owned.map(x=>x.id)),showcase=collectionExtras.showcase.map(Number).filter(id=>validIds.has(id));if(showcase.length!==collectionExtras.showcase.length){collectionExtras.showcase=showcase;saveExtras();}const featured=owned.filter(x=>showcase.includes(x.id));
    const today=collectionExtras.daily,visited=today.visited.length,missionRows=[['Влез в клуба','Посети колекцията си днес.',today.login?1:0,1],['Разгледай 3 находки','Отвори детайлите на различни предмети.',Math.min(visited,3),3],['Подреди витрината','Избери поне един предмет за показване.',featured.length?1:0,1]];
    const activeTrades=(trades||[]).filter(t=>t.status==='Accepted').length;
    const badgeRows=[['Първа находка',owned.length>=1,'Добави първия предмет в колекцията.'],['Пълен сак',owned.length>=5,'Събери 5 активни предмета.'],['Разнообразен вкус',covered>=3,'Събери предмети от 3 категории.'],['Улична сделка',activeTrades>=1,'Завърши първата си размяна.'],['Легендарен улов',Object.values(collectionExtras.rarities).includes('legendary'),'Маркирай предмет като легендарен.']];
    const xp=badgeRows.filter(x=>x[1]).length*100+missionRows.filter(x=>x[2]>=x[3]).length*25,level=Math.floor(xp/250)+1,xpProgress=(xp%250)/250*100;
    const leaderboardMap=new Map();all.filter(x=>!['Traded','Sold'].includes(x.status)).forEach(x=>{const key=x.ownerId;const row=leaderboardMap.get(key)||{id:key,name:x.ownerName,avatar:x.ownerAvatarUrl,count:0};row.count++;leaderboardMap.set(key,row);});
    const leaders=[...leaderboardMap.values()].sort((a,b)=>b.count-a.count||a.name.localeCompare(b.name,'bg')).slice(0,5);
    const itemMarkup=x=>`<article class="dossier-item ${showcase.includes(x.id)?'is-featured':''}" data-item="${x.id}"><div class="dossier-image">${x.imageUrl?`<img src="${esc(x.imageUrl)}" alt="" loading="lazy">`:`<span>${esc((x.name||'К').charAt(0))}</span>`}<b class="rarity-tag rarity-${collectionExtras.rarities[x.id]||'common'}">${rarityLevels.find(r=>r.id===(collectionExtras.rarities[x.id]||'common')).name}</b></div><div class="dossier-item-copy"><b>${esc(x.name)}</b><small>${esc(x.category)}</small><label class="rarity-picker">Рядкост<select data-rarity="${x.id}" aria-label="Рядкост на ${esc(x.name)}">${rarityLevels.map(r=>`<option value="${r.id}" ${(collectionExtras.rarities[x.id]||'common')===r.id?'selected':''}>${r.name}</option>`).join('')}</select></label><button class="button ${showcase.includes(x.id)?'primary':'secondary'}" data-action="toggle-showcase" data-id="${x.id}">${showcase.includes(x.id)?'★ Във витрината':'＋ Добави във витрината'}</button></div></article>`;
    $('#content').innerHTML=`<section class="dossier-panel dossier-progress"><div class="dossier-panel-heading"><div><span class="item-category">КОЛЕКЦИОНЕРСКИ ПЪТ</span><h2>Албумът ти</h2><p>${covered} от ${categories.length} категории имат поне една находка.</p></div><strong>${coverage}%</strong></div><div class="collector-level"><b>НИВО ${level}</b><span>${xp} УЛИЧНИ ТОЧКИ · още ${250-(xp%250)} до следващото ниво</span><div class="mission-track"><span style="width:${xpProgress}%"></span></div></div><div class="dossier-progress-track"><span style="width:${coverage}%"></span></div><div class="album-categories">${ownedByCategory.map(c=>`<div class="album-category ${c.count?'is-filled':''}"><span>${c.count?'✦':'·'}</span><b>${esc(c.name)}</b><small>${c.count?`${c.count} ${c.count===1?'предмет':'предмета'}`:'Още няма находка'}</small></div>`).join('')}</div></section>
      <section class="dossier-panel"><div class="dossier-panel-heading"><div><span class="item-category">ЗАДАЧИ ЗА ДНЕС</span><h2>Квартални мисии</h2></div><span class="mission-count">${missionRows.filter(x=>x[2]>=x[3]).length}/${missionRows.length}</span></div><div class="mission-list">${missionRows.map(([title,desc,progress,total])=>`<article class="mission-row ${progress>=total?'is-done':''}"><span class="mission-check">${progress>=total?'✓':'·'}</span><div><b>${title}</b><small>${desc}</small><div class="mission-track"><span style="width:${Math.min(100,progress/total*100)}%"></span></div></div><strong>${progress}/${total}</strong></article>`).join('')}</div></section>
      <section class="dossier-panel"><div class="dossier-panel-heading"><div><span class="item-category">ТВОИТЕ ОТЛИЧИЯ</span><h2>Значки</h2></div><span class="mission-count">${badgeRows.filter(x=>x[1]).length}/${badgeRows.length}</span></div><div class="badge-shelf">${badgeRows.map(([name,earned,desc])=>`<article class="collector-badge ${earned?'is-earned':'is-locked'}"><span>${earned?'✦':'?'}</span><b>${name}</b><small>${desc}</small></article>`).join('')}</div></section>
      <section class="dossier-panel"><div class="dossier-panel-heading"><div><span class="item-category">ИЗБРАНИ НАХОДКИ</span><h2>Твоята витрина</h2><p>Покажи най-любимите си предмети.</p></div><span class="mission-count">${featured.length}/3</span></div><div class="showcase-grid">${featured.length?featured.slice(0,3).map(itemMarkup).join(''):'<p class="dossier-empty">Избери до три находки от албума си и ги добави тук.</p>'}</div><h3 class="dossier-subhead">Албум с предмети</h3><div class="showcase-grid">${owned.length?owned.map(itemMarkup).join(''):'<p class="dossier-empty">Още нямаш предмети в албума. Добави първата си находка от таблото.</p>'}</div><p class="dossier-note">Редкостта и витрината се пазят локално в този браузър.</p></section>
      <section class="dossier-panel"><div class="dossier-panel-heading"><div><span class="item-category">КЛАСАЦИЯ НА КВАРТАЛА</span><h2>Активни колекционери</h2></div></div><ol class="collector-leaderboard">${leaders.map((x,i)=>`<li class="${x.id===Number(me.sub)?'is-you':''}"><span class="rank-number">${String(i+1).padStart(2,'0')}</span>${avatarMarkup(x.avatar,x.name,'contact-avatar')}<b>${esc(x.name)}${x.id===Number(me.sub)?' · ТИ':''}</b><span>${x.count} ${x.count===1?'находка':'находки'}</span></li>`).join('')||'<li class="dossier-empty">Класацията ще се появи след първите находки.</li>'}</ol></section>`;
  }
  async function openItem(id) {
    const x = await safe(() => api.get(`/api/items/${id}`)); if (!x) return;
    const today=dateKey();if(collectionExtras.daily.date!==today)collectionExtras.daily={date:today,visited:[],login:true};if(!collectionExtras.daily.visited.includes(Number(id)))collectionExtras.daily.visited.push(Number(id));saveExtras();
    const [activity, owner] = await Promise.all([safe(() => api.get(`/api/items/${id}/activity`)),safe(() => api.get(`/api/users/${x.ownerId}/profile`))]);
    const history = activity || {offers:[],bids:[]};
    const mine = x.ownerId === Number(me.sub); let actions = '';
    if (role==='Collector' && mine && x.status==='InCollection') actions = `<button class="button secondary" data-action="edit" data-id="${id}">Редактирай</button><button class="button ghost" data-action="trade-list" data-id="${id}">Пусни за размяна</button><button class="button ghost" data-action="auction-request" data-id="${id}">Заяви търг</button><button class="button danger" data-action="delete" data-id="${id}">Изтрий</button>`;
    else if(role==='Collector' && mine && x.status==='ForTrade') actions=`<button class="button secondary" data-action="withdraw-trade" data-id="${id}">Оттегли от размяна</button>`;
    else if(role==='Collector' && mine && x.status==='PendingApproval') actions=`<button class="button secondary" data-action="withdraw-auction" data-id="${id}">Оттегли заявката за търг</button>`;
    else if(role==='Collector'&&!mine&&x.status==='ForTrade') actions=`<button class="button primary" data-action="propose" data-id="${id}">Предложи размяна</button><button class="button ghost" data-action="message" data-user="${x.ownerId}">Пиши колекционера</button>`;
    else if(role==='Collector'&&!mine&&x.status==='OnAuction') actions=`<button class="button primary" data-action="bid" data-id="${id}" data-current-bid="${x.currentBid??''}" data-minimum-bid="${x.minimumNextBid??0.52}">Наддай</button><button class="button ghost" data-action="message" data-user="${x.ownerId}">Пиши колекционера</button>`;
    else if(role==='Collector'&&!mine) actions=`<button class="button ghost" data-action="message" data-user="${x.ownerId}">Пиши колекционера</button>`;
    if(role==='Evaluator'&&x.status==='PendingApproval') actions=`<button class="button primary" data-action="approve" data-id="${id}">Одобри търга</button><button class="button ghost" data-action="reject" data-id="${id}">Върни в колекция</button>`;
    const offerHtml=history.offers.map(o=>`<div class="activity-row"><span><b>${esc(o.senderName||'Предложение')}</b><small>Предлага „${esc(o.offeredItem)}“ · ${esc(o.status)}</small></span>${role==='Collector'&&mine&&o.status==='Pending'?`<button class="button secondary" data-action="accept-trade" data-id="${o.id}">Приеми</button>`:''}</div>`).join('');
    const bids=history.bids.map(b=>`<div class="activity-row"><span>${esc(b.bidder)}<small>${new Date(b.createdAt).toLocaleString('bg-BG')}</small></span><b>${money(b.amount)}</b></div>`).join('');
    const contact=x.contactPhone||x.deliveryAddress?`<div class="activity"><h3>Данни за предаване</h3>${x.contactPhone?`<p>Телефон: ${esc(x.contactPhone)}</p>`:''}${x.deliveryAddress?`<p>Адрес: ${esc(x.deliveryAddress)}</p>`:''}</div>`:'';
    $('#dialogContent').innerHTML=`<div class="detail-layout"><div class="detail-photo">${x.imageUrl?`<img src="${esc(x.imageUrl)}" alt="${esc(x.name)}">`:`<div class="placeholder">${esc(x.name.charAt(0))}</div>`}</div><div class="detail-copy"><span class="item-category">${esc(x.category)} · ${esc(statusName(x.status))}</span><h2>${esc(x.name)}</h2><p>${esc(x.description||'Няма описание за предмета.')}</p><p><b>Състояние:</b> ${esc(x.condition||'Не е посочено')}</p><div class="seller-card">${avatarMarkup(owner?.avatarUrl||x.ownerAvatarUrl,x.ownerName,'seller-avatar')}<div><b>${esc(owner?.displayName||x.ownerName)}</b><small>${esc(owner?.location||'Член на общността')}</small>${owner?.bio?`<p>${esc(owner.bio)}</p>`:''}${owner?.website?`<a class="profile-link" href="${esc(owner.website)}" target="_blank" rel="noopener noreferrer">Личен сайт ↗</a>`:''}</div></div><div class="detail-owner">Добавен ${new Date(x.createdAt).toLocaleDateString('bg-BG')}</div><div class="detail-actions">${actions}</div>${x.auctionEndsAt?`<div class="activity"><h3>Търгът приключва</h3><p>${new Date(x.auctionEndsAt).toLocaleString('bg-BG')} · Текущо: ${x.currentBid!=null?money(x.currentBid):'без наддавания'}</p>${x.status==='OnAuction'?`<p>Минимално следващо наддаване: <b>${money(x.minimumNextBid??0.52)}</b></p>`:''}</div>`:''}${offerHtml?`<div class="activity"><h3>Предложения за размяна</h3>${offerHtml}</div>`:''}${bids?`<div class="activity"><h3>Последни наддавания</h3>${bids}</div>`:''}${contact}</div></div>`;
    $('#itemDialog').showModal();
  }
  function showForm(title, subtitle, content) { $('#formContent').innerHTML=`<div class="form-head"><span class="eyebrow"><i></i> КОЛЕКЦИОНЕРСКИ СВЯТ / РАБОТНО МЯСТО</span><h2>${title}</h2><p>${subtitle}</p></div><div class="dialog-form">${content}</div>`; $('#formDialog').showModal(); }
  function itemForm(existing = {}) {
    const isEdit=!!existing.id;
    const contactFields=isEdit?'<p class="muted">Телефонът и адресът се пазят скрити и не се променят от този екран.</p>':'<label>Адрес на доставка<input name="deliveryAddress" maxlength="300" placeholder="Пази се скрит до завършена сделка"></label><label>Телефон<input name="phone" maxlength="40" placeholder="Пази се скрит до завършена сделка"></label>';
    const fields=`<form id="itemForm" class="form-stack"><label>Име на предмета<input name="name" required maxlength="120" value="${esc(existing.name||'')}" placeholder="Например: първо издание на албум"></label><label>Категория<select name="categoryId" required>${categories.map(c=>`<option value="${c.id}" ${existing.category===c.name?'selected':''}>${esc(c.name)}</option>`).join('')}</select></label><label>Описание<textarea name="description" maxlength="1200" placeholder="История, особености или откъде е находката">${esc(existing.description||'')}</textarea></label><label>Състояние<input name="condition" maxlength="40" value="${esc(existing.condition||'')}" placeholder="Много добро, запазена опаковка…"></label>${contactFields}<label>Снимка на предмета<input name="imageFile" type="file" accept="image/png,image/jpeg,image/webp"><small>Качи снимка от устройството си · до 5 MB</small></label><label>Или използвай адрес на снимка<input name="imageUrl" maxlength="500" value="${esc(existing.imageUrl||'')}" placeholder="https://…"></label><button class="button primary" type="submit">${isEdit?'Запази промените':'Добави в колекцията'} <span>→</span></button></form>`;
    showForm(isEdit?'Редактирай предмета':'Добави нова находка','Детайлите помагат на другите колекционери да оценят предмета.',fields);
    $('#itemForm').addEventListener('submit',async e=>{e.preventDefault();const f=new FormData(e.currentTarget);const data=Object.fromEntries(f);data.categoryId=Number(data.categoryId);const file=f.get('imageFile');if(file?.size){const uploaded=await safe(()=>uploadImage(file));if(!uploaded)return;data.imageUrl=uploaded;}delete data.imageFile;const result=await safe(()=>isEdit?api.put(`/api/items/${existing.id}`,data):api.post('/api/items',data));if(result){$('#formDialog').close();$('#itemDialog').close();notify(isEdit?'Промените са запазени.':'Предметът е добавен.');loadView(view);}});
  }
  async function openNewItemForm(){if(!categories.length){const loaded=await safe(()=>api.get('/api/categories'));if(!loaded?.length){notify('Не успях да заредя категориите. Провери връзката и опитай отново.',true);return;}categories=loaded;}itemForm();}
  async function action(name,id,userId,button) {
    if(name==='notification-read'){await api.post(`/api/notifications/${id}/read`);await refreshNotificationBadge();return renderNotifications();}
    if(name==='notifications-read-all'){await api.post('/api/notifications/read-all');await refreshNotificationBadge();return renderNotifications();}
    if(name==='notification-open'){await api.post(`/api/notifications/${id}/read`);await refreshNotificationBadge();return openItem(Number(button?.dataset.itemId));}
    if(name==='notification-messages'){await api.post(`/api/notifications/${id}/read`);await refreshNotificationBadge();return selectView('messages');}
    if(name==='toggle-showcase'){const selected=collectionExtras.showcase.map(Number);if(selected.includes(id))collectionExtras.showcase=selected.filter(x=>x!==id);else{if(selected.length>=3){notify('Витрината побира до 3 находки.',true);return;}collectionExtras.showcase=[...selected,id];}saveExtras();notify(selected.includes(id)?'Находката е премахната от витрината.':'Находката е добавена във витрината!');return renderDossier();}
    if(name==='details')return openItem(id);
    if(name==='edit'){const x=await api.get(`/api/items/${id}`);return itemForm(x);}
    if(name==='delete'){if(!confirm('Да изтрием ли този предмет?'))return;await api.delete(`/api/items/${id}`);$('#itemDialog').close();notify('Предметът е изтрит.');return loadView(view);}
    if(name==='trade-list'||name==='auction-request') {await api.post(`/api/items/${id}/${name==='trade-list'?'offer-for-trade':'request-auction'}`);$('#itemDialog').close();notify(name==='trade-list'?'Предметът вече е обявен за размяна.':'Заявката е изпратена към оценител. Предметът остава в колекцията ти със статус „Чака оценител“.');return loadView(view);}
    if(name==='withdraw-trade'||name==='withdraw-auction'){await api.post(`/api/items/${id}/${name}`);if($('#itemDialog').open)$('#itemDialog').close();notify(name==='withdraw-trade'?'Предметът е върнат в колекцията.':'Заявката за търг е оттеглена. Предметът е върнат в колекцията.');return loadView(view);}
    if(name==='approve'||name==='reject'){await api.post(`/api/items/${id}/approval`,{approve:name==='approve'});$('#itemDialog').close();notify(name==='approve'?'Търгът е одобрен за 7 дни.':'Предметът е върнат в колекцията.');return loadView(view);}
    if(name==='accept-trade'){await api.post(`/api/trades/${id}/accept`);$('#itemDialog').close();notify('Размяната е приета и двата предмета смениха собственика си.');await refreshTradeBadge();return loadView(view);}
    if(name==='propose'){return tradeForm(id);}
    if(name==='bid'){
      const minimum=Number(button?.dataset.minimumBid||0.52),current=button?.dataset.currentBid?Number(button.dataset.currentBid):null;
      $('#itemDialog').close();
      showForm('Твоето наддаване',`${current==null?'Все още няма оферти.':`Текущата най-висока оферта е ${money(current)}.`} Всяка следваща оферта трябва да е поне с 5% по-висока.`,
        `<form id="bidForm" class="form-stack"><div class="bid-minimum"><span>Минимална следваща оферта</span><b>${money(minimum)}</b></div><label>Твоята оферта (€)<input name="amount" type="number" min="${minimum.toFixed(2)}" step="0.01" value="${minimum.toFixed(2)}" inputmode="decimal" required></label><button class="button primary" type="submit">Потвърди наддаването →</button></form>`);
      $('#bidForm').addEventListener('submit',async e=>{e.preventDefault();const amount=Number(new FormData(e.currentTarget).get('amount'));const result=await safe(()=>api.post(`/api/items/${id}/bids`,{amount}));if(result){$('#formDialog').close();notify(`Наддаването от ${money(result.amount)} е записано.`);return openItem(id);}});
      return;
    }
    if(name==='message'){return openConversation(Number(userId));}
  }
  async function tradeForm(wantedId) {
    const mine=await api.get('/api/items?status=InCollection'); const own=mine.filter(x=>x.ownerId===Number(me.sub));
    if(!own.length)return notify('Добави предмет със статус „В колекция“, който да предложиш.',true);
    showForm('Предложи размяна','Избери предмет от своята колекция. Адресът и телефонът остават скрити до приемането.',`<form id="tradeForm" class="form-stack"><label>Твоят предмет<select name="offeredItemId">${own.map(x=>`<option value="${x.id}">${esc(x.name)} · ${esc(x.category)}</option>`).join('')}</select></label><label>Бележка към колекционера<textarea name="note" maxlength="500" placeholder="Мога да добавя още детайли или снимки…"></textarea></label><button class="button primary">Изпрати предложение →</button></form>`);
    $('#tradeForm').addEventListener('submit',async e=>{e.preventDefault();const f=new FormData(e.currentTarget);const result=await safe(()=>api.post('/api/trades',{wantedItemId:wantedId,offeredItemId:Number(f.get('offeredItemId')),note:f.get('note')}));if(result){$('#formDialog').close();$('#itemDialog').close();notify('Предложението е изпратено.');refreshTradeBadge();}});
  }
  async function refreshTradeBadge(){const rows=await safe(()=>api.get('/api/trades'));if(!rows)return;const n=rows.filter(x=>x.status==='Pending'&&x.recipientId===Number(me.sub)).length;$('#tradeBadge').textContent=n;$('#tradeBadge').hidden=!n;}
  async function refreshNotificationBadge(){let result;try{result=await api.get('/api/notifications/unread-count');}catch{return;}const badge=$('#notificationBadge');badge.textContent=result.count>99?'99+':result.count;badge.hidden=result.count===0;}
  async function renderNotifications(){
    const rows=await safe(()=>api.get('/api/notifications'));if(!rows)return;
    const unread=rows.filter(n=>!n.isRead).length;
    $('#content').innerHTML=`<section class="notification-page"><header class="notification-page-head"><div><h2>Известия</h2><p>${unread?`${unread} непрочетени ${unread===1?'известие':'известия'}`:'Всичко е прегледано.'}</p></div>${unread?'<button class="button secondary" data-action="notifications-read-all">Прочети всички</button>':''}</header>${rows.length?`<div class="notification-list">${rows.map(n=>`<article class="notification-row ${n.isRead?'':'is-unread'}"><span class="notification-mark" aria-hidden="true"></span><div class="notification-copy"><b>${esc(n.title)}</b><p>${n.actorName?`${esc(n.actorName)} · `:''}${esc(n.body)}</p><time datetime="${esc(n.createdAt)}">${new Date(n.createdAt).toLocaleString('bg-BG')}</time></div><div class="notification-actions">${n.itemId?`<button class="button ghost" data-action="notification-open" data-id="${n.id}" data-item-id="${n.itemId}">Към предмета</button>`:n.type==='message'?`<button class="button ghost" data-action="notification-messages" data-id="${n.id}">Към съобщенията</button>`:''}${!n.isRead?`<button class="button secondary" data-action="notification-read" data-id="${n.id}">Прочетено</button>`:''}</div></article>`).join('')}</div>`:'<div class="empty-state notification-empty"><h3>Засега е тихо.</h3><p>Тук ще се появят нови предложения, наддавания и съобщения.</p></div>'}</section>`;
  }
  async function renderTrades(){const rows=await safe(()=>api.get('/api/trades'));if(!rows)return;const meId=Number(me.sub);$('#content').innerHTML=rows.length?rows.map(t=>`<article class="trade-row"><div><h3>${esc(t.wantedItem)} <span style="color:#b1a99d">↔</span> ${esc(t.offeredItem)}</h3><p>${t.senderId===meId?'Твое предложение':'Получено предложение'} · ${t.note?esc(t.note):'Без бележка'} · ${new Date(t.createdAt).toLocaleDateString('bg-BG')}</p></div><div style="display:flex;gap:10px;align-items:center"><span class="status-pill ${t.status==='Pending'?'pending':''}">${esc(t.status==='Pending'?'Чака отговор':t.status==='Accepted'?'Приета':t.status==='Declined'?'Отказана':'Отменена')}</span>${t.status==='Pending'&&t.recipientId===meId?`<button class="button primary" data-action="accept-trade" data-id="${t.id}">Приеми размяната</button>`:''}</div></article>`).join(''):'<div class="empty-state"><span>↔</span><h3>Още няма предложения.</h3><p>Отвори предмет „За размяна“ и изпрати първото предложение.</p></div>';}
  async function renderContacts(){
    const rows=await safe(()=>api.get('/api/messages/contacts'));if(!rows)return;
    $('#content').innerHTML=`<section class="people-finder"><div><span class="item-category">НОВ РАЗГОВОР</span><h2>Намери колекционер</h2><p>Потърси по име, биография или град и започни нов разговор.</p></div><label class="people-search"><span>⌕</span><input id="peopleSearchInput" type="search" minlength="2" placeholder="Например: Борис или Пловдив" autocomplete="off"></label><div id="peopleSearchResults" class="people-results" aria-live="polite"></div></section><section class="conversation-list"><div class="conversation-list-head"><h2>Последни разговори</h2><span>${rows.length}</span></div>${rows.length?rows.map(c=>`<article class="message-row" data-contact="${c.id}"><div class="message-person">${avatarMarkup(c.avatarUrl,c.name,'contact-avatar')}<div><h3>${esc(c.name)}</h3><p>Последен разговор · ${new Date(c.lastMessage).toLocaleString('bg-BG')}</p></div></div><button class="button secondary" data-action="message" data-user="${c.id}" data-person-name="${esc(c.name)}">Отвори разговора →</button></article>`).join(''):'<div class="empty-state"><span>✉</span><h3>Все още нямаш разговори.</h3><p>Потърси колекционер по име и изпрати първото съобщение.</p></div>'}</section>`;
    let timer;$('#peopleSearchInput').addEventListener('input',e=>{clearTimeout(timer);const query=e.target.value.trim();if(query.length<2){$('#peopleSearchResults').innerHTML=query?'<p class="search-hint">Въведи поне 2 символа.</p>':'';return;}$('#peopleSearchResults').innerHTML='<p class="search-hint">Търсим колекционери…</p>';timer=setTimeout(()=>searchPeople(query),250);});
  }
  async function searchPeople(query){const rows=await safe(()=>api.get(`/api/users/search?query=${encodeURIComponent(query)}`));const target=$('#peopleSearchResults');if(!rows||!target)return;target.innerHTML=rows.length?rows.map(person=>`<article class="person-result">${avatarMarkup(person.avatarUrl,person.displayName,'contact-avatar')}<div class="person-result-info"><b>${esc(person.displayName)}</b><small>${esc(person.location||'Колекционер в общността')}</small></div><button class="button secondary" data-action="message" data-user="${person.id}" data-person-name="${esc(person.displayName)}" data-person-avatar="${esc(person.avatarUrl||'')}" data-person-location="${esc(person.location||'')}">Изпрати съобщение →</button></article>`).join(''):'<p class="search-hint">Не намерихме колекционер с това търсене.</p>';}
  async function uploadImage(file) { const form=new FormData(); form.append('file',file); const result=await api.request('/api/media/upload',{method:'POST',body:form}); return result.url; }
  async function renderProfile(){
    profile=await safe(()=>api.get('/api/profile/me')); if(!profile)return;
    const mine=await safe(()=>api.get('/api/items'))||[]; const mineNow=mine.filter(x=>x.ownerId===Number(me.sub)&&!['Traded','Sold'].includes(x.status)); const count=mineNow.length; const profileFeatured=mineNow.filter(x=>collectionExtras.showcase.map(Number).includes(x.id)).slice(0,3);
    const showcaseHtml=`<section class="profile-showcase"><div><span class="item-category">ЛИЧНА ВИТРИНА</span><h3>Избрани находки</h3></div>${profileFeatured.length?`<div class="profile-featured-grid">${profileFeatured.map(x=>`<article class="profile-featured-item">${x.imageUrl?`<img src="${esc(x.imageUrl)}" alt="" loading="lazy">`:`<span class="profile-featured-placeholder">${esc((x.name||'К').charAt(0))}</span>`}<div><b>${esc(x.name)}</b><small>${esc(x.category)}</small></div></article>`).join('')}</div>`:'<p class="dossier-empty">Избери находки от „Досие“, за да ги покажеш тук.</p>'}</section>`;
    $('#content').innerHTML=`<section class="profile-page"><div class="profile-cover"><div class="profile-identity">${avatarMarkup(profile.avatarUrl,profile.displayName,'profile-avatar')}<div><span class="profile-role">${esc(roleName(profile.role))} · ЧЛЕН ОТ ${new Date(profile.memberSince).toLocaleDateString('bg-BG',{month:'long',year:'numeric'})}</span><h2>${esc(profile.displayName)}</h2><p>${esc(profile.location||'Още не е добавено местоположение')}</p></div></div><div class="profile-count"><b>${count}</b><span>предмета в колекцията</span></div></div>${showcaseHtml}<div class="profile-edit"><div><span class="item-category">ЛИЧНО ПРЕДСТАВЯНЕ</span><h3>Разкажи за себе си</h3><p>Профилът ти помага на останалите колекционери да те опознаят.</p></div><form id="profileForm" class="profile-form"><label>Име<input name="displayName" required minlength="2" maxlength="80" value="${esc(profile.displayName)}"></label><label>Биография<textarea name="bio" maxlength="500" rows="4" placeholder="Какво събираш? Коя е любимата ти находка?">${esc(profile.bio||'')}</textarea></label><div class="profile-form-row"><label>Град<input name="location" maxlength="80" value="${esc(profile.location||'')}" placeholder="Например: София"></label><label>Уебсайт<input name="website" type="url" maxlength="200" value="${esc(profile.website||'')}" placeholder="https://…"></label></div><label>Профилна снимка<input name="avatarFile" type="file" accept="image/png,image/jpeg,image/webp"><small>JPG, PNG или WebP · до 5 MB</small></label><div class="profile-preview">${avatarMarkup(profile.avatarUrl,profile.displayName,'profile-preview-avatar')}<span>Текуща профилна снимка</span></div><button class="button primary" type="submit">Запази профила <span>→</span></button></form></div></section>`;
    $('#profileForm').addEventListener('submit',async e=>{e.preventDefault();const form=new FormData(e.currentTarget);let avatarUrl=profile.avatarUrl;const file=form.get('avatarFile');if(file?.size){const uploaded=await safe(()=>uploadImage(file));if(!uploaded)return;avatarUrl=uploaded;}const updated=await safe(()=>api.put('/api/profile/me',{displayName:form.get('displayName'),bio:form.get('bio'),location:form.get('location'),website:form.get('website'),avatarUrl}));if(updated){profile=updated;$('#userName').textContent=profile.displayName;$('#userAvatar').innerHTML=avatarMarkup(profile.avatarUrl,profile.displayName);notify('Профилът е обновен.');renderProfile();}});
  }
  async function openConversation(otherId,personHint=null){const contacts=await api.get('/api/messages/contacts');let person=contacts.find(x=>x.id===otherId)||personHint;if(!person)person=await safe(()=>api.get(`/api/users/${otherId}/profile`));const history=await api.get(`/api/messages/${otherId}`);showForm(person?.displayName||person?.name||'Разговор','Съобщенията се пазят само за двамата участници.',`${person?.avatarUrl?`<div class="conversation-person">${avatarMarkup(person.avatarUrl,person.displayName||person.name,'contact-avatar')}<b>${esc(person.displayName||person.name)}</b>${person.location?`<small>${esc(person.location)}</small>`:''}</div>`:''}<div id="messageWindow" class="message-window">${history.map(m=>`<div class="message-bubble ${m.senderId===Number(me.sub)?'mine':''}">${esc(m.text)}<small>${new Date(m.sentAt).toLocaleString('bg-BG')}</small></div>`).join('')||'<p class="muted">Започни разговора с кратко съобщение.</p>'}</div><form id="messageForm" class="form-stack"><label>Ново съобщение<textarea name="text" required maxlength="2000" placeholder="Здравей! Интересувам се от тази находка…"></textarea></label><button class="button primary">Изпрати съобщението →</button></form>`);
    clearInterval(conversationTimer);
    conversationTimer=setInterval(async()=>{if(!$('#formDialog').open)return;try{const latest=await api.get(`/api/messages/${otherId}`);const win=$('#messageWindow');if(win)win.innerHTML=latest.map(m=>`<div class="message-bubble ${m.senderId===Number(me.sub)?'mine':''}">${esc(m.text)}<small>${new Date(m.sentAt).toLocaleString('bg-BG')}</small></div>`).join('')||'<p class="muted">Започни разговора с кратко съобщение.</p>';if(win)win.scrollTop=win.scrollHeight;}catch{clearInterval(conversationTimer);}},3000);
    $('#messageForm').addEventListener('submit',async e=>{e.preventDefault();const f=new FormData(e.currentTarget);const result=await safe(()=>api.post('/api/messages',{recipientId:otherId,text:f.get('text')}));if(result){e.currentTarget.reset();$('#formDialog').close();notify('Съобщението е изпратено.');if(view==='messages')renderContacts();}});
  }
  async function renderAdmin(){const [summary,cats]=await Promise.all([api.get('/api/admin/summary'),loadCategories()]);$('#adminSummary').innerHTML=[['Профили',summary.users],['Предмети',summary.items],['Активни търгове',summary.auctions],['Чакащи размени',summary.pendingTrades]].map(x=>`<div class="summary-card"><span>${x[0]}</span><b>${x[1]}</b></div>`).join('');$('#content').innerHTML=`<article class="trade-row"><div><h3>Категории на колекциите</h3><p>Категория може да се изключи, след като няма свързани предмети.</p></div><button class="button primary" data-action="new-category">＋ Нова категория</button></article>${cats.map(c=>`<article class="trade-row"><div><h3>${esc(c.name)}</h3><p>Категория №${c.id}</p></div><button class="button danger" data-action="delete-category" data-id="${c.id}">Премахни</button></article>`).join('')}<article class="trade-row"><div><h3>Приключи изтеклите търгове</h3><p>Победителят се определя от най-високото наддаване.</p></div><button class="button secondary" data-action="close-auctions">Обработи изтеклите</button></article>`;}
  async function adminAction(name,id){if(name==='new-category'){showForm('Нова категория','Добави нов тип предмет към общия каталог.',`<form id="categoryForm" class="form-stack"><label>Име<input name="name" required minlength="2" maxlength="60" placeholder="Например: значки"></label><button class="button primary">Добави категория →</button></form>`);$('#categoryForm').addEventListener('submit',async e=>{e.preventDefault();const name=new FormData(e.currentTarget).get('name');const ok=await safe(()=>api.post('/api/admin/categories',{name}));if(ok){$('#formDialog').close();renderAdmin();}});}
    if(name==='delete-category'){if(!confirm('Да премахнем ли тази категория?'))return;await api.delete(`/api/admin/categories/${id}`);notify('Категорията е премахната.');return renderAdmin();}
    if(name==='close-auctions'){const result=await safe(()=>api.post('/api/auctions/close-due'));if(result){notify(`Приключени търгове: ${result.closed}`);return renderAdmin();}}
  }

  $('#authForm').addEventListener('submit',async e=>{e.preventDefault();const f=new FormData(e.currentTarget);let result;
    if(authMode==='login')result=await safe(()=>api.post('/api/auth/login',{email:f.get('email'),password:f.get('password')}));
    else {const role=f.get('role');result=await safe(()=>api.post(role==='Evaluator'?'/api/auth/register-evaluator':'/api/auth/register-collector',{email:f.get('email'),displayName:f.get('displayName'),password:f.get('password')}));}
    if(result){api.token=result.token;start();notify(authMode==='login'?'Добре дошъл обратно.':'Профилът ти е създаден.');}
  });
  const authPassword=$('#authForm [name=password]'), passwordToggle=$('#passwordToggle');
  const setPasswordVisibility=visible=>{authPassword.type=visible?'text':'password';passwordToggle.setAttribute('aria-pressed',String(visible));passwordToggle.setAttribute('aria-label',visible?'Скрий паролата':'Покажи паролата');passwordToggle.querySelector('[data-password-icon="hidden"]').hidden=visible;passwordToggle.querySelector('[data-password-icon="visible"]').hidden=!visible;};
  passwordToggle.addEventListener('click',()=>{setPasswordVisibility(authPassword.type==='password');authPassword.focus({preventScroll:true});});
  $$('.auth-tab').forEach(b=>b.addEventListener('click',()=>{authMode=b.dataset.auth;setPasswordVisibility(false);$$('.auth-tab').forEach(x=>x.classList.toggle('selected',x===b));const reg=authMode==='register';$('#nameField').hidden=!reg;$('#roleField').hidden=!reg;$('#authTitle').textContent=reg?'Започни своята колекция.':'Радваме се, че се върна.';$('#authSubtitle').textContent=reg?'Създай профил и намери своята общност.':'Влез в пространството на колекционерите.';$('#authSubmit').innerHTML=reg?'Създай профил <span>→</span>':'Влез в профила <span>→</span>';authPassword.autocomplete=reg?'new-password':'current-password';}));
  $('#logoutButton').addEventListener('click',()=>{api.token=null;start();});
  $('#mascotButton').addEventListener('click',()=>{const widget=$('#mascotWidget');const open=!widget.classList.contains('is-open');widget.classList.toggle('is-open',open);$('#mascotMessage').setAttribute('aria-hidden',String(!open));$('#mascotButton').setAttribute('aria-expanded',String(open));});
  $('#mascotPickerToggle').addEventListener('click',()=>{$('#mascotPicker').hidden=!$('#mascotPicker').hidden;});
  $('#mascotPickerClose').addEventListener('click',()=>$('#mascotPicker').hidden=true);
  $('#mascotOptions').addEventListener('click',e=>{const option=e.target.closest('[data-mascot]');if(!option)return;selectedMascotId=option.dataset.mascot;localStorage.setItem(`collection-mascot:${me?.sub||'guest'}`,selectedMascotId);renderMascot();$('#mascotPicker').hidden=true;notify(`Кликс смени стила си: ${mascotStyles.find(x=>x.id===selectedMascotId).name}.`);});
  window.addEventListener('auth:expired',start);
  $$('.nav-link').forEach(b=>b.addEventListener('click',()=>selectView(b.dataset.view)));
  $('#newItemButton').type='button';
  $('#newItemButton').addEventListener('click',openNewItemForm);
  $('#searchInput').addEventListener('keydown',e=>{if(e.key==='Enter')loadView(view);});
  $('#categoryFilter').addEventListener('change',()=>loadView(view)); $('#statusFilter').addEventListener('change',()=>loadView(view));
  document.addEventListener('click',async e=>{
    const item=e.target.closest('[data-item]'); if(item&&!e.target.closest('[data-action],select'))return openItem(Number(item.dataset.item));
    const create=e.target.closest('[data-new]');if(create)return openNewItemForm();
    const close=e.target.closest('[data-close]');if(close){close.closest('dialog').close();return;}
    const actionButton=e.target.closest('[data-action]');if(!actionButton)return;
    const {action:act,id,user}=actionButton.dataset;
    if(view==='admin')return safe(()=>adminAction(act,Number(id)));
    if(act==='message'){const person=actionButton.dataset.personName?{id:Number(user),displayName:actionButton.dataset.personName,avatarUrl:actionButton.dataset.personAvatar||null,location:actionButton.dataset.personLocation||null}:null;return safe(()=>openConversation(Number(user),person));}
    return safe(()=>action(act,Number(id),user,actionButton));
  });
  document.addEventListener('change',e=>{const picker=e.target.closest('[data-rarity]');if(!picker)return;collectionExtras.rarities[picker.dataset.rarity]=picker.value;saveExtras();notify(`Рядкостта е зададена: ${rarityLevels.find(x=>x.id===picker.value)?.name||'Обикновена'}.`);renderDossier();});
  $('#itemDialog').addEventListener('click',e=>{if(e.target===$('#itemDialog'))$('#itemDialog').close();});
  $('#formDialog').addEventListener('close',()=>clearInterval(conversationTimer));
  start();
})();
