const tickets = [
  { id:'#041', title:'Error al iniciar sesión en el portal', user:'Sara Corona', desc:'El usuario reporta que al intentar ingresar al portal interno con sus credenciales, el sistema muestra un error 401 sin mensaje claro. El problema ocurre desde hace 2 horas.', priority:'Urgente', status:'Abierto', tech:'Jorge López', techInit:'JL', techColor:'av-blue', date:'hace 1h', msgs:[{from:'Carlos M.',init:'CM',color:'av-amber',text:'Hola, no puedo entrar al portal desde esta mañana. Me sale error 401.',time:'9:05 AM'},{from:'Jorge L.',init:'JL',color:'av-blue',text:'Hola Carlos, ¿puedes decirme desde qué dispositivo intentas entrar?',time:'9:12 AM'}]},
  { id:'#040', title:'Impresora sin conexión en piso 3', user:'Ary Olvera', desc:'La impresora HP LaserJet del piso 3 no responde desde ayer.', priority:'Media', status:'En progreso', tech:'Sara Andrade', techInit:'SA', techColor:'av-teal', date:'hace 3h', msgs:[{from:'Marta R.',init:'MR',color:'av-purple',text:'La impresora del piso 3 no imprime nada.',time:'7:30 AM'},{from:'Sara A.',init:'SA',color:'av-teal',text:'Ya voy para allá.',time:'7:45 AM'}]},
  { id:'#039', title:'Solicitud de acceso a carpeta compartida', user:'Luis Pérez', desc:'El usuario necesita acceso a la carpeta /proyectos/2025.', priority:'Baja', status:'Resuelto', tech:'Ana Méndez', techInit:'AM', techColor:'av-purple', date:'hace 5h', msgs:[{from:'Luis P.',init:'LP',color:'av-amber',text:'Necesito acceso a la carpeta de proyectos 2025.',time:'6:00 AM'}]},
  { id:'#038', title:'Pantalla azul al abrir Excel', user:'Pedro González', desc:'Al usuario le aparece BSOD cada vez que intenta abrir archivos .xlsx grandes.', priority:'Urgente', status:'Abierto', tech:'Raúl Vargas', techInit:'RV', techColor:'av-amber', date:'hace 6h', msgs:[{from:'Pedro G.',init:'PG',color:'av-teal',text:'Me sale pantalla azul cada que abro Excel.',time:'5:00 AM'}]},
];

const priorityBadge = p => p==='Urgente'?'badge-urgent':p==='Media'?'badge-medium':'badge-low';
const statusBadge = s => s==='Abierto'?'badge-open':s==='En progreso'?'badge-progress':'badge-closed';

function renderRow(t, tbody) {
  const tr = document.createElement('tr');
  tr.innerHTML = `
    <td class="t-ticket-id">${t.id}</td>
    <td><div class="t-title">${t.title}</div><div class="t-user">${t.user}</div></td>
    <td><span class="badge ${priorityBadge(t.priority)}">${t.priority}</span></td>
    <td><span class="badge ${statusBadge(t.status)}">${t.status}</span></td>
    <td><div class="tech-chip"><div class="avatar ${t.techColor}" style="width:26px;height:26px;font-size:10px">${t.techInit}</div><span>${t.tech.split(' ')[0]}</span></div></td>
    <td class="t-date">${t.date}</td>
    <td><button class="btn btn-ghost row-action" style="padding:5px 10px;font-size:12px" onclick="openTicket('${t.id}')">Ver →</button></td>`;
  tbody.appendChild(tr);
}

function renderTables(filter='todos') {
  const recent = document.getElementById('recent-tickets');
  const all = document.getElementById('all-tickets');
  if(recent) recent.innerHTML='';
  if(all) all.innerHTML='';
  tickets.forEach((t,i) => {
    if(recent && i < 4) renderRow(t, recent);
    if(all && (filter==='todos' || t.status===filter)) renderRow(t, all);
  });
}

function filterTickets(f, el) {
  document.querySelectorAll('.filter-chip').forEach(c=>c.classList.remove('active'));
  el.classList.add('active');
  renderTables(f);
}

function openTicket(id) {
  const t = tickets.find(x=>x.id===id);
  if(!t) return;
  document.getElementById('detail-breadcrumb').textContent = `Tickets / ${t.id} — ${t.title}`;
  document.getElementById('detail-title').textContent = t.title;
  document.getElementById('detail-meta').innerHTML = `<span class="badge ${statusBadge(t.status)}">${t.status}</span><span class="badge ${priorityBadge(t.priority)}">${t.priority}</span>`;
  document.getElementById('detail-desc').textContent = t.desc;
  document.getElementById('det-status').innerHTML = `<span class="badge ${statusBadge(t.status)}">${t.status}</span>`;
  document.getElementById('det-priority').innerHTML = `<span class="badge ${priorityBadge(t.priority)}">${t.priority}</span>`;
  document.getElementById('det-user').textContent = t.user;
  document.getElementById('det-date').textContent = t.date;
  document.getElementById('det-status-select').value = t.status;
  const chat = document.getElementById('chat-messages');
  chat.innerHTML='';
  t.msgs.forEach(m => {
    const d = document.createElement('div');
    d.className = 'msg';
    d.innerHTML = `<div class="avatar ${m.color}" style="width:30px;height:30px;font-size:11px;flex-shrink:0">${m.init}</div><div class="msg-content"><div class="msg-author">${m.from}</div><div class="msg-text">${m.text}</div><div class="msg-time">${m.time}</div></div>`;
    chat.appendChild(d);
  });
  chat.scrollTop = chat.scrollHeight;
  showView('ticket-detail', null);
}

function sendMsg() {
  const input = document.getElementById('chat-input');
  const val = input.value.trim();
  if(!val) return;
  const chat = document.getElementById('chat-messages');
  const d = document.createElement('div');
  d.className = 'msg me';
  const now = new Date().toLocaleTimeString('es-MX',{hour:'2-digit',minute:'2-digit'});
  d.innerHTML = `<div class="msg-content"><div class="msg-author">Tú</div><div class="msg-text">${val}</div><div class="msg-time">${now}</div></div>`;
  chat.appendChild(d);
  chat.scrollTop = chat.scrollHeight;
  input.value = '';
  showToast('Comentario enviado ✓');
}

function updateStatus(val) {
  showToast(`Estado actualizado a "${val}" ✓`);
}

function showView(name, navEl) {
  document.querySelectorAll('.view').forEach(v=>v.classList.remove('active'));
  document.getElementById('view-'+name).classList.add('active');
  if(navEl) {
    document.querySelectorAll('.nav-item').forEach(n=>n.classList.remove('active'));
    navEl.classList.add('active');
  }
  const titles = {dashboard:'Dashboard', tickets:'Tickets', 'ticket-detail':'Detalle de ticket', metrics:'Métricas', users:'Usuarios'};
  document.getElementById('page-title').textContent = titles[name]||name;
}

let toastTimer;
function showToast(msg) {
  const t = document.getElementById('toast');
  document.getElementById('toast-msg').textContent = msg;
  t.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(()=>t.classList.remove('show'), 2500);
}

function openModal() { document.getElementById('modal').classList.add('show'); }
function closeModal() { document.getElementById('modal').classList.remove('show'); }

let ticketCount = 42;
function createTicket() {
  const title = document.getElementById('new-title').value.trim();
  if(!title) { alert('Escribe un título para el ticket.'); return; }
  const priority = document.getElementById('new-priority').value;
  const tech = document.getElementById('new-tech').value;
  const desc = document.getElementById('new-desc').value || 'Sin descripción adicional.';
  ticketCount++;
  tickets.unshift({ id:`#0${ticketCount}`, title, user:'Tú', desc, priority, status:'Abierto', tech, techInit:tech.split(' ').map(w=>w[0]).join(''), techColor:'av-blue', date:'ahora', msgs:[] });
  renderTables();
  closeModal();
  document.getElementById('new-title').value='';
  document.getElementById('new-desc').value='';
  showToast('Ticket creado exitosamente ✓');
}

document.addEventListener('DOMContentLoaded', () => {
  document.getElementById('modal').addEventListener('click', e => { if(e.target===e.currentTarget) closeModal(); });
  renderTables();
});