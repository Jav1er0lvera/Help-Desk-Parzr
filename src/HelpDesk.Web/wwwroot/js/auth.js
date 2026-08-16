function togglePass(id) {
    const inp = document.getElementById(id);
    inp.type = inp.type === 'password' ? 'text' : 'password';
}

function checkStrength(val) {
    const bar = document.getElementById('strength-bar');
    const hint = document.getElementById('strength-hint');
    if (!val) { bar.style.width = '0%'; hint.textContent = 'Escribe una contraseña'; return; }
    let score = 0;
    if (val.length >= 8) score++;
    if (/[A-Z]/.test(val)) score++;
    if (/[0-9]/.test(val)) score++;
    if (/[^A-Za-z0-9]/.test(val)) score++;
    const levels = [
        { w: '25%', bg: '#EF4444', txt: 'Muy débil' },
        { w: '50%', bg: '#F59E0B', txt: 'Débil' },
        { w: '75%', bg: '#3B82F6', txt: 'Buena' },
        { w: '100%', bg: '#10B981', txt: 'Muy segura' },
    ];
    const l = levels[score - 1] || levels[0];
    bar.style.width = l.w;
    bar.style.background = l.bg;
    hint.textContent = l.txt;
}

function showToast(msg) {
    const t = document.getElementById('toast');
    document.getElementById('toast-msg').textContent = msg;
    t.classList.add('show');
    setTimeout(() => t.classList.remove('show'), 3000);
}

// Validación de cliente antes de enviar el form de registro al servidor.
// El registro real (guardar en BD) lo hace el AccountController.
function validateRegisterClientSide() {
    const pass = document.getElementById('reg-pass').value;
    const pass2 = document.getElementById('reg-pass2').value;
    const passMatchErr = document.getElementById('pass-match-err');
    passMatchErr.classList.remove('show');

    if (pass.length < 8) {
        passMatchErr.textContent = 'La contraseña debe tener al menos 8 caracteres.';
        passMatchErr.classList.add('show');
        return false;
    }
    if (pass !== pass2) {
        passMatchErr.textContent = 'Las contraseñas no coinciden.';
        passMatchErr.classList.add('show');
        return false;
    }
    return true;
}
