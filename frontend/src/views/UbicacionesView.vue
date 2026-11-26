<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import AppIcon from '../components/AppIcon.vue'
import { inject } from 'vue'

const { usuario } = inject('rutabol')
const mapaElemento = ref(null)
const gpsUsuario = ref(null)
const modoPunto = ref('ninguno') // gps | manual | ninguno
const eligiendoPunto = ref(false)
const cerca = ref([])
const ubicaciones = ref([])
const unidades = ref([])
const seleccionado = ref('')
const mensaje = ref('')
const error = ref('')
const ocupado = ref(false)
const transmitiendo = ref(false)
const radioKm = ref(5)
let mapa = null
let capaMicros = null
let marcadorUsuario = null
let circuloUsuario = null
let L = null
let temporizador = null
let actualizacionGPS = null
let destruido = false
let primeraVista = true
const puedeTransmitir = computed(() => ['Administrador','Conductor'].includes(usuario.value?.rol))
const puedeElegirPin = computed(() => usuario.value?.rol !== 'Conductor')
const esConductor = computed(() => usuario.value?.rol === 'Conductor')
const unidadAsignada = computed(() => unidades.value[0] || null)

async function obtenerJson(ruta, opciones) {
  const r = await fetch(ruta, { credentials: 'include', ...opciones })
  const cuerpo = await r.json().catch(() => ({}))
  if (!r.ok) throw new Error(cuerpo.mensaje || (r.status === 403 ? 'No tienes permisos para realizar esta acción.' : `Error HTTP ${r.status}`))
  return cuerpo
}

function cargarHojaEstilo() {
  if (document.querySelector('link[data-rutabol-leaflet]')) return
  const link = document.createElement('link')
  link.rel = 'stylesheet'
  link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'
  link.dataset.rutabolLeaflet = '1'
  document.head.appendChild(link)
}
function cargarBibliotecaMapa() {
  if (window.L) return Promise.resolve(window.L)
  if (window.__rutabolLeafletCargando) return window.__rutabolLeafletCargando
  cargarHojaEstilo()
  window.__rutabolLeafletCargando = new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'
    script.onload = () => window.L ? resolve(window.L) : reject(new Error('Biblioteca del mapa no disponible.'))
    script.onerror = () => reject(new Error('No se pudo cargar el mapa. Comprueba tu conexión a Internet.'))
    document.head.appendChild(script)
  }).catch(e => { window.__rutabolLeafletCargando = null; throw e })
  return window.__rutabolLeafletCargando
}

async function iniciarMapa() {
  try {
    L = await cargarBibliotecaMapa()
    await nextTick()
    if (destruido || !mapaElemento.value) return
    mapa = L.map(mapaElemento.value, { scrollWheelZoom: false }).setView([-17.7833, -63.1821], 12)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19, attribution: '© OpenStreetMap contributors'
    }).addTo(mapa)
    capaMicros = L.layerGroup().addTo(mapa)
    mapa.on('click', async (evento) => {
      if (!puedeElegirPin.value || !eligiendoPunto.value || destruido) return
      eligiendoPunto.value = false
      modoPunto.value = 'manual'
      gpsUsuario.value = { lat: evento.latlng.lat, lon: evento.latlng.lng }
      error.value = ''; mensaje.value = 'Punto seleccionado. Buscando micros cerca del marcador.'
      primeraVista = false
      await consultarCercanos()
      if (!destruido) dibujarMapa()
    })
    dibujarMapa()
    setTimeout(() => { if (mapa) mapa.invalidateSize() }, 150)
  } catch (e) { if (!destruido) error.value = e.message }
}

function pintarEtiqueta(micro) {
  const caja = document.createElement('div')
  caja.style.padding = '3px'
  const titulo = document.createElement('strong')
  titulo.textContent = `Línea ${micro.linea} · Interno ${micro.numeroInterno}`
  caja.appendChild(titulo)
  const detalle = document.createElement('div')
  detalle.textContent = `${micro.pasajerosActuales}/${micro.capacidadMaxima} pasajeros · ${micro.estado}`
  caja.appendChild(detalle)
  return caja
}
function dibujarMapa() {
  if (!L || !mapa || !capaMicros) return
  capaMicros.clearLayers()
  const cercanosIds = new Set(cerca.value.map(m => m.microId))
  for (const micro of ubicaciones.value) {
    const cercano = gpsUsuario.value && cercanosIds.has(micro.microId)
    const icono = L.divIcon({
      className: '',
      html: `<div style="width:30px;height:30px;border-radius:50%;border:3px solid white;background:${cercano ? '#19875b' : '#336b9b'};color:white;display:flex;align-items:center;justify-content:center;box-shadow:0 3px 12px #0005;font-size:18px">●</div>`,
      iconSize: [30,30], iconAnchor: [15,15]
    })
    L.marker([micro.latitud, micro.longitud], { icon: icono }).bindPopup(pintarEtiqueta(micro)).addTo(capaMicros)
  }
  if (marcadorUsuario) { mapa.removeLayer(marcadorUsuario); marcadorUsuario = null }
  if (circuloUsuario) { mapa.removeLayer(circuloUsuario); circuloUsuario = null }
  if (gpsUsuario.value) {
    const { lat, lon } = gpsUsuario.value
    circuloUsuario = L.circle([lat, lon], { radius: radioKm.value * 1000, weight: 2, color: '#19875b', fillOpacity: .055 }).addTo(mapa)
    marcadorUsuario = L.circleMarker([lat, lon], { radius: 8, weight: 3, color: '#fff', fillColor: '#19875b', fillOpacity: 1 }).addTo(mapa).bindTooltip(modoPunto.value === 'manual' ? 'Punto de búsqueda elegido' : 'Tu ubicación aproximada')
    if (primeraVista) { mapa.setView([lat,lon], 13); primeraVista = false }
  } else if (primeraVista && ubicaciones.value.length) {
    const m = ubicaciones.value[0]; mapa.setView([m.latitud, m.longitud], 13); primeraVista = false
  }
}
async function cargarUnidades() {
  if (!puedeTransmitir.value) return
  try {
    unidades.value = await obtenerJson('/api/ubicaciones/mis-unidades')
    if (!unidades.value.some(x => String(x.id) === String(seleccionado.value))) seleccionado.value = unidades.value[0]?.id ? String(unidades.value[0].id) : ''
  } catch (e) { error.value = e.message }
}
async function actualizar() {
  try {
    const lista = await obtenerJson('/api/ubicaciones/micros')
    if (destruido) return
    ubicaciones.value = lista
    await consultarCercanos()
    dibujarMapa()
  } catch (e) { if (!destruido) error.value = e.message }
}
async function consultarCercanos() {
  if (!gpsUsuario.value) { cerca.value = []; return }
  const {lat, lon} = gpsUsuario.value
  try {
    cerca.value = await obtenerJson(`/api/ubicaciones/cercanos?lat=${encodeURIComponent(lat)}&lon=${encodeURIComponent(lon)}&radioKm=${radioKm.value}`)
  } catch (e) { error.value = e.message }
}
function posicionActual() {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) return reject(new Error('Este navegador no admite geolocalización.'))
    navigator.geolocation.getCurrentPosition(
      p => resolve({lat: p.coords.latitude, lon: p.coords.longitude, precisionMetros: p.coords.accuracy}),
      e => reject(new Error(e.code === 1 ? 'Permiso de ubicación denegado. Habilítalo en el navegador.' : 'No se pudo obtener la ubicación. Revisa el GPS y los permisos.')),
      { enableHighAccuracy: true, timeout: 12000, maximumAge: 5000 }
    )
  })
}
async function localizarme() {
  ocupado.value = true; error.value = ''; mensaje.value = ''
  try {
    gpsUsuario.value = await posicionActual()
    modoPunto.value = 'gps'
    eligiendoPunto.value = false
    primeraVista = true
    await consultarCercanos()
    dibujarMapa()
    mensaje.value = 'Ubicación obtenida. La búsqueda incluye solo micros con GPS reciente.'
  } catch (e) { error.value = e.message }
  finally { ocupado.value = false }
}
function elegirPunto() {
  if (!puedeElegirPin.value) return
  if (!mapa) { error.value = 'El mapa todavía no está disponible.'; return }
  eligiendoPunto.value = !eligiendoPunto.value
  error.value = ''
  mensaje.value = eligiendoPunto.value
    ? 'Haz clic en cualquier lugar del mapa para colocar tu punto de búsqueda.'
    : 'Selección de punto cancelada.'
}
async function enviarUbicacion() {
  if (!seleccionado.value) { error.value = 'Selecciona una unidad asignada.'; return }
  if (actualizacionGPS) return
  actualizacionGPS = true
  try {
    const p = await posicionActual()
    await obtenerJson(`/api/ubicaciones/micros/${seleccionado.value}`, {
      method: 'PUT', headers: { 'Content-Type':'application/json' },
      body: JSON.stringify({ latitud: p.lat, longitud: p.lon, precisionMetros: p.precisionMetros })
    })
    if (!destruido) { mensaje.value = 'GPS enviado correctamente.'; await actualizar() }
  } catch (e) { if (!destruido) { error.value = e.message; detenerTransmision() } }
  finally { actualizacionGPS = false }
}
async function iniciarTransmision() {
  if (!seleccionado.value) { error.value = 'Selecciona primero un micro.'; return }
  error.value = ''; mensaje.value = ''
  await enviarUbicacion()
  if (error.value || destruido) return
  transmitiendo.value = true
  temporizador = setInterval(enviarUbicacion, 30000)
  mensaje.value = 'Compartiendo ubicación cada 30 segundos mientras esta página permanezca abierta.'
}
function detenerTransmision() {
  if (temporizador) clearInterval(temporizador)
  temporizador = null
  transmitiendo.value = false
}
function cambioUnidad() { if (transmitiendo.value) detenerTransmision() }
onMounted(async () => {
  await iniciarMapa()
  if (destruido) return
  await Promise.all([actualizar(), cargarUnidades()])
})
onUnmounted(() => { destruido = true; detenerTransmision(); if (mapa) { mapa.remove(); mapa = null } })
</script>

<template>
  <section class="geo-modulo">
    <div class="geo-encabezado"><div><h2><AppIcon name="map-pin" :size="23"/> Micros cercanos</h2><p>Ubicaciones GPS reportadas recientemente por unidades autorizadas.</p></div><button class="geo-boton secundario" @click="actualizar"><AppIcon name="refresh-cw" :size="17"/> Actualizar</button></div>
    <div class="geo-informacion"><AppIcon name="info" :size="18"/><span>Solo aparecen ubicaciones actualizadas en los últimos 15 minutos. El radio máximo permitido es de 5 km. El mapa necesita Internet.</span></div>
    <div class="geo-acciones"><button class="geo-boton" :disabled="ocupado" @click="localizarme"><AppIcon name="navigation" :size="18"/> {{ocupado ? 'Localizando...' : 'Usar mi ubicación'}}</button><button v-if="puedeElegirPin" class="geo-boton secundario" :aria-pressed="eligiendoPunto" @click="elegirPunto"><AppIcon name="map-pin" :size="18"/> {{ eligiendoPunto ? 'Cancelar selección' : 'Elegir punto en el mapa' }}</button><label class="geo-radio">Radio de búsqueda <select v-model.number="radioKm" @change="consultarCercanos().then(dibujarMapa)"><option :value="1">1 km</option><option :value="3">3 km</option><option :value="5">5 km</option></select></label></div>
    <p v-if="error" class="geo-error">{{error}}</p><p v-if="mensaje" class="geo-exito">{{mensaje}}</p>
    <div ref="mapaElemento" class="geo-mapa" :class="{ 'geo-mapa-eligiendo': eligiendoPunto }" role="region" aria-label="Mapa interactivo de micros en Santa Cruz"></div>
    <p class="geo-ayuda">Puntos verdes: micros cercanos al punto elegido. Puntos azules: demás micros con ubicación reciente. Puedes elegir otro punto cuando quieras. La ubicación puede variar según la señal del dispositivo. <a href="https://www.openstreetmap.org/" target="_blank" rel="noopener noreferrer">Abrir OpenStreetMap</a>.</p>
    <div class="geo-titulo-lista"><h3>{{gpsUsuario ? `Micros a ${radioKm} km o menos ${modoPunto === 'manual' ? 'del punto elegido' : 'de ti'} (${cerca.length})` : `Micros con GPS reciente (${ubicaciones.length})`}}</h3></div>
    <div class="geo-lista"><div v-for="m in (gpsUsuario ? cerca : ubicaciones)" :key="m.microId" class="geo-unidad"><strong>Línea {{m.linea}} · Interno {{m.numeroInterno}}</strong><span>{{m.pasajerosActuales}}/{{m.capacidadMaxima}} pasajeros · {{m.estado}}</span><span v-if="m.distanciaKm !== undefined">{{m.distanciaKm.toFixed(2)}} km de distancia</span><small>Actualizado: {{new Date(m.actualizadoEn).toLocaleString('es-BO')}}</small></div><p v-if="(gpsUsuario ? cerca : ubicaciones).length===0" class="geo-vacio">No hay micros con ubicación reciente {{gpsUsuario ? 'en este radio' : ''}}. Un administrador o conductor asignado debe activar la transmisión.</p></div>
    <section v-if="puedeTransmitir" class="geo-panel"><h3>Publicar ubicación de una unidad</h3><p>Tu navegador compartirá su ubicación únicamente cuando lo actives, mientras permanezcas en esta sección.</p><div class="geo-form"><label v-if="esConductor">Tu unidad asignada<strong style="padding:10px;border:1px solid var(--borde);border-radius:8px">{{ unidadAsignada ? `Línea ${unidadAsignada.linea} / Interno ${unidadAsignada.numeroInterno}` : 'Sin asignación' }}</strong></label><label v-else>Unidad a ubicar<select v-model="seleccionado" :disabled="transmitiendo" @change="cambioUnidad"><option value="">Selecciona un micro</option><option v-for="m in unidades" :key="m.id" :value="String(m.id)">Línea {{m.linea}} / Interno {{m.numeroInterno}}</option></select></label><button v-if="!transmitiendo" class="geo-boton" @click="iniciarTransmision">Iniciar ubicación GPS</button><button v-else class="geo-boton peligro" @click="detenerTransmision">Detener transmisión</button></div><p v-if="!unidades.length">No tienes unidades asignadas. Solicita al administrador una unidad desde Administración → Centro de gestión.</p>

    </section>
  </section>
</template>

<style scoped>
.geo-modulo{display:grid;gap:17px;max-width:1180px}.geo-encabezado,.geo-acciones,.geo-titulo-lista{display:flex;gap:15px;align-items:center;justify-content:space-between;flex-wrap:wrap}.geo-encabezado h2{margin:0 0 5px;font-size:25px;display:flex;align-items:center;gap:10px}.geo-encabezado p,.geo-panel p,.geo-ayuda{color:var(--secundario);margin:4px 0;font-size:13px}.geo-boton{background:var(--verde);color:white;border:0;border-radius:9px;display:inline-flex;align-items:center;justify-content:center;gap:9px;padding:11px 15px;font-size:13px;font-weight:700;cursor:pointer}.geo-boton:disabled{opacity:.6;cursor:not-allowed}.geo-boton.secundario{background:var(--verde-suave);color:var(--texto);border:1px solid var(--borde)}.geo-boton.peligro{background:#a63c3c}.geo-informacion{border:1px solid var(--borde);background:var(--verde-suave);border-radius:9px;display:flex;gap:10px;align-items:center;padding:12px 14px;font-size:13px}.geo-radio{font-size:13px;font-weight:600;display:flex;align-items:center;gap:8px}.geo-radio select,.geo-form select,.geo-form input{background:var(--input);color:var(--texto);padding:10px;border:1px solid var(--borde);border-radius:8px;min-width:150px}.geo-mapa-eligiendo{cursor:crosshair}.geo-mapa-eligiendo :deep(.leaflet-container),.geo-mapa-eligiendo :deep(.leaflet-interactive){cursor:crosshair!important}.geo-mapa{height:410px;min-height:300px;background:#dbe7e0;border:1px solid var(--borde);border-radius:12px;z-index:0;overflow:hidden}.geo-ayuda a{color:var(--verde)}.geo-lista{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:10px}.geo-unidad{padding:14px;border:1px solid var(--borde);border-radius:10px;background:var(--superficie);display:grid;gap:7px;font-size:13px}.geo-unidad span,.geo-unidad small,.geo-vacio{color:var(--secundario)}.geo-panel{border:1px solid var(--borde);border-radius:12px;padding:20px;background:var(--superficie);display:grid;gap:15px}.geo-panel h3,.geo-panel h4,.geo-titulo-lista h3{margin:0}.geo-form{display:flex;gap:10px;align-items:end;flex-wrap:wrap}.geo-form label{display:grid;gap:5px;font-size:13px;font-weight:600}.geo-error,.geo-exito{margin:0;padding:10px;border-radius:8px;font-size:13px}.geo-error{background:#fbe4e4;color:#a13a3a}.geo-exito{background:var(--verde-suave);color:var(--texto)}.geo-vacio{font-size:13px;padding:16px}.geo-titulo-lista{margin-top:7px}
@media(max-width:650px){.geo-mapa{height:325px}.geo-encabezado h2{font-size:21px}.geo-form{align-items:stretch;flex-direction:column}.geo-form label,.geo-form select,.geo-form input,.geo-form button{width:100%}}
</style>
