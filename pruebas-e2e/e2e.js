// Pruebas de punta a punta de los sprints 5 y 6 (RF09, RF10, RNF01, RNF03, RNF06, RNF08)
// en Chrome (motor Blink) y WebKit (motor de Safari), en tamaño celular y escritorio.
// Requisitos: la aplicación corriendo con el perfil https (https://localhost:7090), SQL Server Express
// y `npm install` en esta carpeta (más `npx playwright-core install webkit` la primera vez).
// Uso: node e2e.js   -> capturas en ./shots y resultados en ./resultado.json
const pw = require('playwright-core');
const { chromium } = pw;
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const BASE = 'https://localhost:7090';
const SHOTS = path.join(__dirname, 'shots');
// Para no tener que pasar por el panel del administrador, el conductor de prueba se aprueba directo en la base.
fs.mkdirSync(SHOTS, { recursive: true });
const resultados = [];
const tiempos = {};

function ok(nombre, cond, detalle = '') {
  resultados.push({ nombre, ok: !!cond, detalle });
  console.log(`${cond ? 'OK ' : 'FALLA'} ${nombre} ${detalle}`);
}

function sql(q) {
  const salida = execSync(`sqlcmd -S localhost\\SQLEXPRESS -E -b -I -d VuelvoSeguroDb -Q "${q}"`, { stdio: 'pipe' }).toString();
  if (!salida.includes('1 rows affected')) throw new Error('SQL: ' + salida);
}

async function medir(page, nombre, url) {
  const t0 = Date.now();
  await page.goto(BASE + url, { waitUntil: 'load' });
  const ms = Date.now() - t0;
  (tiempos[nombre] ||= []).push(ms);
  return ms;
}

async function sinScrollHorizontal(page) {
  return page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth);
}

async function registrarPasajero(page, sufijo) {
  await page.goto(BASE + '/Cuenta/Registro');
  const f = '#formPasajero ';
  await page.fill(f + '[name="Pasajero.Nombre"]', 'Lucía');
  await page.fill(f + '[name="Pasajero.Apellido"]', 'Gómez');
  await page.fill(f + '[name="Pasajero.Dni"]', '4' + sufijo);
  await page.fill(f + '[name="Pasajero.FechaNacimiento"]', '1995-04-12');
  await page.fill(f + '[name="Pasajero.Email"]', `pasajero${sufijo}@prueba.test`);
  await page.fill(f + '[name="Pasajero.Telefono"]', '1155550000');
  await page.fill(f + '[name="Pasajero.Password"]', 'clave1234');
  await page.fill(f + '[name="Pasajero.ConfirmarPassword"]', 'clave1234');
  await page.fill(f + '[name="Pasajero.Direccion"]', 'Av. Rivadavia 4520, CABA');
  await page.fill(f + '[name="Pasajero.Vehiculo.Marca"]', 'Volkswagen');
  await page.fill(f + '[name="Pasajero.Vehiculo.Modelo"]', 'Gol Trend');
  await page.fill(f + '[name="Pasajero.Vehiculo.Patente"]', 'AA' + sufijo.slice(-3) + 'BB');
  await page.fill(f + '[name="Pasajero.Vehiculo.VencimientoSeguro"]', '2027-12-31');
  // Sin aceptar los términos el formulario no se envía.
  await page.click(f + 'button[type="submit"]');
  await page.waitForTimeout(400);
  ok('[' + page.context().browser().browserType().name() + '] RNF08 registro sin aceptar términos no avanza', page.url().includes('/Cuenta/Registro'));
  await page.check(f + '[name="Pasajero.AceptaTerminos"]');
  await Promise.all([page.waitForNavigation(), page.click(f + 'button[type="submit"]')]);
  ok('[' + page.context().browser().browserType().name() + '] RNF08 registro aceptando términos entra al inicio del pasajero', page.url().includes('/Pasajero'), page.url());
}

async function registrarConductor(page, sufijo) {
  await page.goto(BASE + '/Cuenta/Registro?rol=conductor');
  const f = '#formConductor ';
  await page.fill(f + '[name="Conductor.Nombre"]', 'Martín');
  await page.fill(f + '[name="Conductor.Apellido"]', 'Pérez');
  await page.fill(f + '[name="Conductor.Dni"]', '3' + sufijo);
  await page.fill(f + '[name="Conductor.FechaNacimiento"]', '1988-09-03');
  await page.fill(f + '[name="Conductor.Email"]', `conductor${sufijo}@prueba.test`);
  await page.fill(f + '[name="Conductor.Telefono"]', '1166660000');
  await page.fill(f + '[name="Conductor.Password"]', 'clave1234');
  await page.fill(f + '[name="Conductor.ConfirmarPassword"]', 'clave1234');
  await page.check(f + '[name="Conductor.AceptaTerminos"]');
  await Promise.all([page.waitForNavigation(), page.click(f + 'button[type="submit"]')]);
  sql(`UPDATE Users SET EstadoUsuarioId=3 WHERE Email='conductor${sufijo}@prueba.test'`);
}

async function publicar(page, origen) {
  await page.goto(BASE + '/Pasajero');
  await page.fill('#origen', origen);
  await Promise.all([page.waitForNavigation(), page.click('form[action*="Publicar"] button[type="submit"]')]);
  return Number(page.url().split('/').pop());
}

async function cancelar(page, rol, id, motivo, otro) {
  await page.goto(`${BASE}/${rol}/Cancelar/${id}`);
  if (otro) {
    await page.check('#motivoOtro');
    await page.fill('#otroMotivo', otro);
  } else {
    await page.check(`input[name="motivo"][value="${motivo}"]`);
  }
  await Promise.all([page.waitForNavigation(), page.click('#formCancelar button[type="submit"]')]);
}

async function correr(canal) {
  const sufijo = String(Date.now()).slice(-7);
  const motor = canal === 'chrome' ? 'chromium' : canal;
  const browser = await pw[motor].launch(canal === 'chrome' ? { channel: 'chrome' } : {});
  const movil = motor === 'firefox' ? {} : { isMobile: true };
  const ctxP = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, hasTouch: true, ...movil });
  const ctxC = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1280, height: 800 } });
  const p = await ctxP.newPage();
  const c = await ctxC.newPage();
  const pre = `[${canal}]`;

  // Términos y condiciones públicos.
  await p.goto(BASE + '/Home/Terminos');
  ok(`${pre} RNF08 página de términos visible sin sesión`, (await p.textContent('h4')).includes('Términos'));
  ok(`${pre} RNF06 términos sin scroll horizontal en celular`, await sinScrollHorizontal(p));
  if (canal === 'chrome') await p.screenshot({ path: path.join(SHOTS, 'terminos-celular.png'), fullPage: false });

  await registrarPasajero(p, sufijo);
  await registrarConductor(c, sufijo);

  // 1) El pasajero cancela un viaje Publicado con "Otro motivo".
  let id = await publicar(p, 'Bar La Esquina, Av. Corrientes 1800');
  await p.goto(`${BASE}/Pasajero/Cancelar/${id}`);
  ok(`${pre} RF09 pantalla de cancelación sin scroll horizontal`, await sinScrollHorizontal(p));
  if (canal === 'chrome') await p.screenshot({ path: path.join(SHOTS, 'cancelar-celular.png'), fullPage: true });
  // Sin motivo no se envía.
  await p.click('#formCancelar button[type="submit"]');
  await p.waitForTimeout(300);
  ok(`${pre} RF09 sin motivo no se cancela`, p.url().includes('/Cancelar/'));
  await cancelar(p, 'Pasajero', id, null, 'Me vinieron a buscar unos amigos');
  ok(`${pre} RF09 pasajero cancela viaje publicado`, (await p.content()).includes('Viaje cancelado') && (await p.content()).includes('Me vinieron a buscar unos amigos'));

  // 2) El conductor acepta y luego cancela; el pasajero lo ve.
  id = await publicar(p, 'Bar El Faro, Av. Cabildo 2100');
  await c.goto(BASE + '/Conductor/Disponibles');
  await Promise.all([c.waitForNavigation(), c.click(`form[action$="/Aceptar/${id}"] button`)]);
  ok(`${pre} RF07 conductor acepta el viaje`, c.url().includes(`/Conductor/Viaje/${id}`));
  if (canal === 'chrome') await c.screenshot({ path: path.join(SHOTS, 'conductor-viaje-escritorio.png') });
  await cancelar(c, 'Conductor', id, 'Tuve un imprevisto');
  ok(`${pre} RF09 conductor cancela viaje aceptado`, c.url().includes('/Conductor/Disponibles'));
  await p.goto(`${BASE}/Pasajero/Viaje/${id}`);
  const txt = await p.content();
  ok(`${pre} RF09 el pasajero ve que el conductor canceló y el motivo`, txt.includes('Tu conductor canceló el viaje') && txt.includes('Tuve un imprevisto'));
  if (canal === 'chrome') await p.screenshot({ path: path.join(SHOTS, 'cancelado-por-conductor-celular.png'), fullPage: true });

  // 3) Un viaje En curso no se puede cancelar; se termina normalmente.
  id = await publicar(p, 'Club Social, Av. Libertador 5000');
  await c.goto(BASE + '/Conductor/Disponibles');
  await Promise.all([c.waitForNavigation(), c.click(`form[action$="/Aceptar/${id}"] button`)]);
  await Promise.all([c.waitForNavigation(), c.click('form[action*="Iniciar"] button')]);
  ok(`${pre} RF09 sin botón de cancelar para el conductor en curso`, (await c.locator('a[href*="/Cancelar/"]').count()) === 0);
  await p.goto(`${BASE}/Pasajero/Viaje/${id}`);
  ok(`${pre} RF09 sin botón de cancelar para el pasajero en curso`, (await p.locator('a[href*="/Cancelar/"]').count()) === 0);
  await p.goto(`${BASE}/Pasajero/Cancelar/${id}`);
  ok(`${pre} RF09 entrar a la URL de cancelar en curso lo rechaza`, p.url().includes(`/Pasajero/Viaje/${id}`) && (await p.content()).includes('Un viaje en curso no se puede cancelar.'));
  await Promise.all([c.waitForNavigation(), c.click('form[action*="Finalizar"] button')]);

  // 4) Historiales.
  await medir(p, 'Historial pasajero', '/Pasajero/Historial');
  const itemsP = await p.locator('.historial .list-group-item').count();
  ok(`${pre} RF10 historial del pasajero muestra los 3 viajes`, itemsP === 3, `(${itemsP})`);
  ok(`${pre} RF10 historial del pasajero sin scroll horizontal en celular`, await sinScrollHorizontal(p));
  if (canal === 'chrome') await p.screenshot({ path: path.join(SHOTS, 'historial-pasajero-celular.png'), fullPage: true });
  await medir(c, 'Historial conductor', '/Conductor/Historial');
  const itemsC = await c.locator('.historial .list-group-item').count();
  ok(`${pre} RF10 historial del conductor muestra sus 2 viajes`, itemsC === 2, `(${itemsC})`);
  ok(`${pre} RF10 la navegación del conductor tiene Historial`, (await c.locator('.nav-superior a[href="/Conductor/Historial"]').count()) === 1);
  if (canal === 'chrome') await c.screenshot({ path: path.join(SHOTS, 'historial-conductor-escritorio.png'), fullPage: true });

  // 5) Seguridad: un pasajero no puede ver el historial ni cancelar como conductor.
  await p.goto(BASE + '/Conductor/Historial');
  ok(`${pre} RNF03 el pasajero no entra al historial del conductor`, p.url().includes('AccesoDenegado'));

  // 6) Tiempos de carga (RNF01).
  for (let i = 0; i < 5; i++) {
    await medir(p, 'Inicio de sesión', '/Cuenta/Login');
    await medir(p, 'Inicio pasajero', '/Pasajero');
    await medir(p, 'Historial pasajero', '/Pasajero/Historial');
    await medir(c, 'Viajes disponibles', '/Conductor/Disponibles');
    await medir(c, 'Historial conductor', '/Conductor/Historial');
    await medir(p, 'Términos', '/Home/Terminos');
  }

  await browser.close();
}

(async () => {
  try {
    await correr('chrome');
    await correr('webkit');
    // El administrador ve el motivo en el listado de viajes.
    const browser = await chromium.launch({ channel: 'chrome' });
    const a = await (await browser.newContext({ ignoreHTTPSErrors: true })).newPage();
    await a.goto(BASE + '/Cuenta/Login');
    await a.fill('[name="Email"]', 'admin@vuelvoseguro.com');
    await a.fill('[name="Password"]', 'Admin123!');
    await Promise.all([a.waitForNavigation(), a.click('button[type="submit"]')]);
    await medir(a, 'Viajes (administrador)', '/Admin/Viajes');
    ok('RF09 el administrador ve el motivo de las cancelaciones', (await a.content()).includes('Tuve un imprevisto'));
    await browser.close();
  } catch (e) {
    console.error(e);
    ok('Ejecución sin errores', false, e.message);
  }
  const resumen = {};
  for (const [k, v] of Object.entries(tiempos)) {
    const s = [...v].sort((x, y) => x - y);
    resumen[k] = { n: v.length, promedio: Math.round(v.reduce((x, y) => x + y, 0) / v.length), max: s[s.length - 1] };
  }
  console.log(JSON.stringify(resumen, null, 1));
  const fallas = resultados.filter(r => !r.ok).length;
  console.log(`TOTAL ${resultados.length} casos, ${fallas} fallas`);
  fs.writeFileSync(path.join(__dirname, 'resultado.json'), JSON.stringify({ resultados, resumen }, null, 1));
})();
