<script setup>
import { ref, onMounted, onUnmounted, provide, computed } from 'vue'
import AppIcon from './components/AppIcon.vue'
import AppSidebar from './components/AppSidebar.vue'
import InicioView from './views/InicioView.vue'
import MicrosView from './views/MicrosView.vue'
import TarifasView from './views/TarifasView.vue'
import CategoriasView from './views/CategoriasView.vue'
import PasajesView from './views/PasajesView.vue'
import PerfilView from './views/PerfilView.vue'
import AdminView from './views/AdminView.vue' 

const oscuro = ref(localStorage.getItem('rutabol-tema') === 'oscuro')
const pantalla = ref('login')
const nombre = ref('')
const correo = ref('')
const password = ref('')
const usuario = ref(null)
const sesion = ref(null)
const mensaje = ref('')
const error = ref('')
const cargando = ref(false)
const mostrarPassword = ref(false)

const tarifas = ref([])
const historialTarifas = ref([])
const cargandoTarifas = ref(false)
const guardandoTarifa = ref(false)
const mensajeTarifas = ref('')
const errorTarifas = ref('')
const mostrarHistorial = ref(false)

const nuevaTarifa = ref({
  nombre: '',
  monto: ''
})

const micros = ref([])
const cargandoMicros = ref(false)
const guardandoMicro = ref(false)
const errorMicros = ref('')
const mensajeMicros = ref('')

// id del micro que se está actualizando (null = ninguno)
const actualizandoMicro = ref(null)

const nuevoMicro = ref({
  linea: '',
  numeroInterno: '',
  capacidadMaxima: 25
})

async function cargarMicros() {
  cargandoMicros.value = true
  errorMicros.value = ''

  try {
    const respuesta = await fetch('/api/micros', {
      credentials: 'include'
    })

    if (!respuesta.ok) {
      throw new Error('No se pudieron consultar los micros.')
    }

    micros.value = await respuesta.json()
    return true
  } catch (e) {
    errorMicros.value = e.message
    return false
  } finally {
    cargandoMicros.value = false
  }
}

async function registrarMicro() {
  errorMicros.value = ''
  mensajeMicros.value = ''
  guardandoMicro.value = true

  try {
    const respuesta = await fetch('/api/micros', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        linea: nuevoMicro.value.linea,
        numeroInterno: nuevoMicro.value.numeroInterno,
        capacidadMaxima: Number(nuevoMicro.value.capacidadMaxima)
      })
    })

    const texto = await respuesta.text()

    let resultado = {}

    if (texto) {
      try {
        resultado = JSON.parse(texto)
      } catch {
        throw new Error('El servidor devolvió una respuesta no válida.')
      }
    }

    if (!respuesta.ok) {
      if (respuesta.status === 401) {
        throw new Error(
          'Tu sesión ha expirado. Cierra sesión e ingresa nuevamente.'
        )
      }

      if (respuesta.status === 403) {
        throw new Error(
          'No tienes permisos de administrador para registrar micros.'
        )
      }

      throw new Error(resultado.mensaje || `Error HTTP ${respuesta.status}`)
    }

    nuevoMicro.value = {
      linea: '',
      numeroInterno: '',
      capacidadMaxima: 25
    }

    const listaActualizada = await cargarMicros()
    mensajeMicros.value = listaActualizada
      ? 'Micro registrado correctamente.'
      : 'Micro registrado, pero no se pudo actualizar el listado.'
  } catch (e) {
    errorMicros.value = e.message
  } finally {
    guardandoMicro.value = false
  }
}

async function cambiarOcupacion(micro, cambio) {
  errorMicros.value = ''
  mensajeMicros.value = ''

  if (actualizandoMicro.value !== null) return

  actualizandoMicro.value = micro.id

  try {
    const respuesta = await fetch(`/api/micros/${micro.id}/movimiento`, {
      method: 'PUT',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        cambio: cambio
      })
    })

    const texto = await respuesta.text()
    let resultado = {}

    if (texto) {
      try {
        resultado = JSON.parse(texto)
      } catch {
        throw new Error('Respuesta no válida del servidor.')
      }
    }

    if (!respuesta.ok) {
      if (respuesta.status === 401) {
        throw new Error('Debes iniciar sesión nuevamente.')
      }

      if (respuesta.status === 403) {
        throw new Error('No tienes permisos para modificar pasajeros.')
      }

      throw new Error(
        resultado.mensaje ||
        `Error HTTP ${respuesta.status}`
      )
    }

    const listaActualizada = await cargarMicros()
    mensajeMicros.value = listaActualizada
      ? (resultado.mensaje || 'Ocupación actualizada.')
      : 'Movimiento guardado, pero no se pudo actualizar el listado.'
  } catch (e) {
    await cargarMicros()
    errorMicros.value = e.message
  } finally {
    actualizandoMicro.value = null
  }
}

// Solicitudes de categorías y pasajes de demostración
const solicitudNueva = ref({ categoria: '', codigo: '', correoInstitucional: '' })
const fotoNueva = ref(null)
const solicitudesMias = ref([])
const pendientesRevision = ref([])
const microsPasajeId = ref('')
const misPasajes = ref([])
const resultadoPasaje = ref(null)
const guardandoSolicitud = ref(false)
const procesandoPasaje = ref(false)
const errorCategoria = ref('')
const avisoCategoria = ref('')
const errorPasaje = ref('')

function seleccionarFoto(evento) {
  fotoNueva.value = evento.target.files?.[0] || null
}

async function cargarSolicitudes() {
  try {
    const r = await fetch('/api/categorias/mis-solicitudes', { credentials: 'include' })
    if (r.ok) solicitudesMias.value = await r.json()
    if (usuario.value?.rol === 'Administrador') {
      const admin = await fetch('/api/categorias/pendientes', { credentials: 'include' })
      if (admin.ok) pendientesRevision.value = await admin.json()
    }
  } catch (e) { errorCategoria.value = e.message }
}

async function enviarSolicitudCategoria() {
  if (guardandoSolicitud.value) return
  errorCategoria.value = ''
  avisoCategoria.value = ''
  if (!fotoNueva.value) {
    errorCategoria.value = 'Adjunta una foto JPG o PNG de prueba.'
    return
  }
  guardandoSolicitud.value = true
  try {
    const datos = new FormData()
    datos.append('Categoria', solicitudNueva.value.categoria)
    datos.append('Codigo', solicitudNueva.value.codigo)
    datos.append('CorreoInstitucional', solicitudNueva.value.correoInstitucional)
    datos.append('Foto', fotoNueva.value)
    const r = await fetch('/api/categorias/solicitar', { method: 'POST', credentials: 'include', body: datos })
    const respuesta = await r.json().catch(() => ({}))
    if (!r.ok) throw new Error(respuesta.mensaje || `Error HTTP ${r.status}`)
    avisoCategoria.value = respuesta.mensaje
    solicitudNueva.value = { categoria: '', codigo: '', correoInstitucional: '' }
    fotoNueva.value = null
    await cargarSolicitudes()
  } catch (e) { errorCategoria.value = e.message }
  finally { guardandoSolicitud.value = false }
}

async function revisarSolicitudCategoria(id, estado) {
  errorCategoria.value = ''
  try {
    const r = await fetch(`/api/categorias/${id}/revisar`, {
      method: 'PUT', credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ estado })
    })
    const respuesta = await r.json().catch(() => ({}))
    if (!r.ok) throw new Error(respuesta.mensaje || `Error HTTP ${r.status}`)
    avisoCategoria.value = `Solicitud ${estado.toLowerCase()}.`
    await cargarSolicitudes()
  } catch (e) { errorCategoria.value = e.message }
}

async function cargarMisPasajes() {
  try {
    const r = await fetch('/api/pasajes/mios', { credentials: 'include' })
    if (r.ok) misPasajes.value = await r.json()
  } catch (e) { errorPasaje.value = e.message }
}

async function simularPasaje() {
  errorPasaje.value = ''
  resultadoPasaje.value = null
  if (procesandoPasaje.value) return
  procesandoPasaje.value = true
  try {
    const r = await fetch('/api/pasajes/simular', {
      method: 'POST', credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ microId: Number(microsPasajeId.value) })
    })
    const respuesta = await r.json().catch(() => ({}))
    if (!r.ok) throw new Error(respuesta.mensaje || `Error HTTP ${r.status}`)
    resultadoPasaje.value = respuesta
    await cargarMisPasajes()
  } catch (e) { errorPasaje.value = e.message }
  finally { procesandoPasaje.value = false }
}

async function cargarTarifas() {
  cargandoTarifas.value = true
  errorTarifas.value = ''

  try {
    const respuesta = await fetch('/api/tarifas', {
      credentials: 'include'
    })

    if (!respuesta.ok) {
      throw new Error('No se pudieron consultar las tarifas.')
    }

    tarifas.value = await respuesta.json()
    return true
  } catch (e) {
    errorTarifas.value = e.message
    return false
  } finally {
    cargandoTarifas.value = false
  }
}

async function registrarTarifa() {
  if (guardandoTarifa.value) return

  errorTarifas.value = ''
  mensajeTarifas.value = ''

  const nombre = nuevaTarifa.value.nombre.trim()
  const montoTexto = String(nuevaTarifa.value.monto).trim().replace(',', '.')

  if (!nombre) {
    errorTarifas.value = 'Selecciona un tipo de tarifa.'
    return
  }

  if (!/^(?:\d+)(?:\.\d{1,2})?$/.test(montoTexto)) {
    errorTarifas.value = 'Escribe un precio con máximo dos decimales.'
    return
  }

  const monto = Number(montoTexto)
  if (!Number.isFinite(monto) || monto < 0.01 || monto > 9999.99) {
    errorTarifas.value = 'El precio debe estar entre Bs 0,01 y Bs 9.999,99.'
    return
  }

  const tarifaActual = tarifas.value.find(t =>
    t.nombre.toLocaleLowerCase('es') === nombre.toLocaleLowerCase('es')
  )

  if (tarifaActual && Number(tarifaActual.monto) === monto) {
    errorTarifas.value = 'Esta tarifa ya tiene ese precio. No hay cambios que guardar.'
    return
  }

  guardandoTarifa.value = true

  try {
    const respuesta = await fetch('/api/tarifas', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nombre, monto })
    })

    const resultado = await respuesta.json().catch(() => ({}))

    if (!respuesta.ok) {
      if (respuesta.status === 401) throw new Error('Vuelve a iniciar sesión.')
      if (respuesta.status === 403) throw new Error('Solo un administrador puede registrar tarifas.')
      if (resultado.errors) {
        throw new Error(Object.values(resultado.errors).flat().join(' '))
      }
      throw new Error(resultado.mensaje || `Error HTTP ${respuesta.status}`)
    }

    nuevaTarifa.value = { nombre: '', monto: '' }
    const actualizado = await cargarTarifas()
    mensajeTarifas.value = actualizado
      ? 'Tarifa guardada correctamente.'
      : 'Tarifa guardada, pero no se pudo actualizar la lista.'

    if (mostrarHistorial.value) await cargarHistorialTarifas()
  } catch (e) {
    errorTarifas.value = e.message
  } finally {
    guardandoTarifa.value = false
  }
}

async function cargarHistorialTarifas() {
  errorTarifas.value = ''

  try {
    const respuesta = await fetch('/api/tarifas/historial', {
      credentials: 'include'
    })

    if (!respuesta.ok) {
      if (respuesta.status === 401 || respuesta.status === 403) {
        throw new Error('No tienes acceso al historial de tarifas.')
      }
      throw new Error('No se pudo cargar el historial.')
    }

    historialTarifas.value = await respuesta.json()
  } catch (e) {
    errorTarifas.value = e.message
  }
}

async function alternarHistorialTarifas() {
  mostrarHistorial.value = !mostrarHistorial.value
  if (mostrarHistorial.value) await cargarHistorialTarifas()
}

function cambiarTema() {
  oscuro.value = !oscuro.value
  localStorage.setItem('rutabol-tema', oscuro.value ? 'oscuro' : 'claro')
}

function limpiar() {
  mensaje.value = ''
  error.value = ''
}

function cambiarPantalla(valor) {
  pantalla.value = valor
  password.value = ''
  limpiar()
}

async function solicitar(ruta, metodo = 'GET', datos = null) {
  const opciones = {
    method: metodo,
    credentials: 'include',
    headers: {}
  }

  if (datos !== null) {
    opciones.headers['Content-Type'] = 'application/json'
    opciones.body = JSON.stringify(datos)
  }

  const respuesta = await fetch('/api/auth/' + ruta, opciones)

  const resultado = await respuesta.json().catch(() => ({}))

  if (!respuesta.ok) {
    if (resultado.errors) {
      throw new Error(Object.values(resultado.errors).flat().join(' '))
    }
    throw new Error(resultado.mensaje || 'Error HTTP ' + respuesta.status)
  }

  return resultado
}

async function registrar() {
  limpiar()
  cargando.value = true

  try {
    const resultado = await solicitar('registro', 'POST', {
      nombreCompleto: nombre.value,
      correo: correo.value,
      password: password.value
    })

    pantalla.value = 'login'
    password.value = ''
    mensaje.value = resultado.mensaje
  } catch (e) {
    error.value = e.message
  } finally {
    cargando.value = false
  }
}

async function iniciarSesion() {
  limpiar()
  cargando.value = true

  try {
    const resultado = await solicitar('login', 'POST', {
      correo: correo.value,
      password: password.value
    })

    usuario.value = resultado.usuario
    pantalla.value = 'panel'
    password.value = ''
    mensaje.value = resultado.mensaje
    await consultarSesion()
    await cargarMicros()
    await cargarTarifas()
    await cargarSolicitudes()
    await cargarMisPasajes()
  } catch (e) {
    error.value = e.message
  } finally {
    cargando.value = false
  }
}

async function consultarSesion() {
  try {
    sesion.value = await solicitar('sesion')
  } catch {
    sesion.value = null
  }
}

async function cerrarSesion() {
  limpiar()
  cargando.value = true

  try {
    await solicitar('logout', 'POST')

    usuario.value = null
    sesion.value = null
    micros.value = []
    tarifas.value = []
    historialTarifas.value = []
    solicitudesMias.value = []
    pendientesRevision.value = []
    misPasajes.value = []
    resultadoPasaje.value = null
    fotoNueva.value = null
    errorCategoria.value = ''
    avisoCategoria.value = ''
    errorPasaje.value = ''

    mensajeMicros.value = ''
    errorMicros.value = ''
    mensajeTarifas.value = ''
    errorTarifas.value = ''
    mostrarHistorial.value = false

    password.value = ''
    pantalla.value = 'login'
    mensaje.value = 'Sesión cerrada correctamente.'
  } catch (e) {
    error.value = e.message
  } finally {
    cargando.value = false
  }
}

onMounted(async () => {
  try {
    usuario.value = await solicitar('perfil')
    pantalla.value = 'panel'
    await consultarSesion()
    await cargarMicros()
    await cargarTarifas()
    await cargarSolicitudes()
    await cargarMisPasajes()
  } catch {
    usuario.value = null
  }
})

// Cambiamos de seccion sin perder los formularios ni los datos
const secciones = ['inicio', 'micros', 'tarifas', 'categorias', 'pasajes', 'perfil', 'admin']
function seccionDesdeUrl() {
  const ruta = window.location.hash.replace(/^#\/?/, '').split('/')[0]
  return secciones.includes(ruta) ? ruta : 'inicio'
}
const seccionActual = ref(seccionDesdeUrl())
function actualizarSeccion() {
  seccionActual.value = seccionDesdeUrl()
}
const vistaActual = computed(() => {
  if (seccionActual.value === 'admin' && usuario.value?.rol !== 'Administrador') return 'inicio'
  return seccionActual.value
})
function irA(seccion) {
  if (!secciones.includes(seccion)) return
  window.location.hash = `#/${seccion}`
  seccionActual.value = seccion
  window.scrollTo({ top: 0, behavior: 'smooth' })
}
provide('rutabol', {
  oscuro,
  pantalla,
  nombre,
  correo,
  password,
  usuario,
  sesion,
  mensaje,
  error,
  cargando,
  mostrarPassword,
  tarifas,
  historialTarifas,
  cargandoTarifas,
  guardandoTarifa,
  mensajeTarifas,
  errorTarifas,
  mostrarHistorial,
  nuevaTarifa,
  micros,
  cargandoMicros,
  guardandoMicro,
  errorMicros,
  mensajeMicros,
  actualizandoMicro,
  nuevoMicro,
  solicitudNueva,
  fotoNueva,
  solicitudesMias,
  pendientesRevision,
  microsPasajeId,
  misPasajes,
  resultadoPasaje,
  guardandoSolicitud,
  procesandoPasaje,
  errorCategoria,
  avisoCategoria,
  errorPasaje,
  cargarMicros,
  registrarMicro,
  cambiarOcupacion,
  seleccionarFoto,
  cargarSolicitudes,
  enviarSolicitudCategoria,
  revisarSolicitudCategoria,
  cargarMisPasajes,
  simularPasaje,
  cargarTarifas,
  registrarTarifa,
  cargarHistorialTarifas,
  alternarHistorialTarifas,
  cambiarTema,
  limpiar,
  cambiarPantalla,
  solicitar,
  registrar,
  iniciarSesion,
  consultarSesion,
  cerrarSesion,
  irA
})

onMounted(() => window.addEventListener('hashchange', actualizarSeccion))
onUnmounted(() => window.removeEventListener('hashchange', actualizarSeccion))

const titulos = {inicio:'Inicio',micros:'Micros y líneas',tarifas:'Tarifas',categorias:'Categorías',pasajes:'Pasajes',perfil:'Mi perfil',admin:'Administración'}
const menuAbierto = ref(false)
function alNavegar(destino) { irA(destino); menuAbierto.value = false }
</script>
<template>
<div class="app" :class="{oscuro}">
  <template v-if="pantalla !== 'panel'">
    <header class="public-header"><div class="brand-public"><div class="brand-logo"><AppIcon name="bus" :size="27"/></div><strong>RUTABOL<span>-SCZ</span></strong></div><button class="theme-switch" @click="cambiarTema"><AppIcon :name="oscuro ? 'sun' : 'moon'"/><span>{{oscuro?'Modo claro':'Modo oscuro'}}</span></button></header>
    <main class="login-page">
      <section class="login-hero"><span class="hero-tag"><AppIcon name="shield" :size="16"/> PLATAFORMA ACADÉMICA</span><h1>Tu ciudad, <em>más conectada.</em></h1><p>Una forma más clara de consultar micros, tarifas y categorías de pasajeros en Santa Cruz.</p><div class="login-highlights"><span><AppIcon name="bus"/> Unidades registradas</span><span><AppIcon name="wallet"/> Tarifas vigentes</span><span><AppIcon name="shield"/> Acceso seguro</span></div></section>
      <section class="login-card">
          <div class="tarjeta-icono"><AppIcon name="bus" /></div>

          <h3>
            {{ pantalla === 'login'
              ? '¡Bienvenido de nuevo!'
              : 'Únete a RutaBol' }}
          </h3>

          <p class="subtitulo">
            {{ pantalla === 'login'
              ? 'Inicia sesión para acceder a tu cuenta.'
              : 'Crea tu cuenta de pasajero para comenzar.' }}
          </p>

          <div class="tabs">
            <button
              :class="{ activo: pantalla === 'login' }"
              @click="cambiarPantalla('login')"
            >
              Iniciar sesión
            </button>

            <button
              :class="{ activo: pantalla === 'registro' }"
              @click="cambiarPantalla('registro')"
            >
              Registrarse
            </button>
          </div>

          <form
            @submit.prevent="
              pantalla === 'login' ? iniciarSesion() : registrar()
            "
          >
            <div v-if="pantalla === 'registro'" class="campo">
              <label for="nombre">Nombre completo</label>
              <input
                id="nombre"
                v-model="nombre"
                type="text"
                minlength="3"
                maxlength="150"
                placeholder="Ej. Juan Pérez"
                autocomplete="name"
                required
              />
            </div>

            <div class="campo">
              <label for="correo">Correo electrónico</label>
              <input
                id="correo"
                v-model="correo"
                type="email"
                placeholder="correo@ejemplo.com"
                autocomplete="email"
                required
              />
            </div>

            <div class="campo">
              <label for="password">Contraseña</label>

              <div class="password-contenedor">
                <input
                  id="password"
                  v-model="password"
                  :type="mostrarPassword ? 'text' : 'password'"
                  placeholder="Ingresa tu contraseña"
                  :autocomplete="pantalla === 'login'
                    ? 'current-password'
                    : 'new-password'"
                  :minlength="pantalla === 'registro' ? 8 : undefined"
                  required
                />

                <button
                  type="button"
                  class="ver-password"
                  @click="mostrarPassword = !mostrarPassword"
                >
                  {{ mostrarPassword ? 'Ocultar' : 'Ver' }}
                </button>
              </div>
            </div>

            <button
              class="boton-principal"
              type="submit"
              :disabled="cargando"
            >
              {{ cargando ? 'Procesando...' :
                pantalla === 'login'
                  ? 'Iniciar sesión'
                  : 'Crear cuenta' }}
              <span>→</span>
            </button>
          </form>

          <p class="ayuda">
            {{ pantalla === 'login'
              ? '¿Todavía no tienes una cuenta?'
              : '¿Ya tienes una cuenta?' }}

            <button
              class="enlace"
              @click="cambiarPantalla(
                pantalla === 'login' ? 'registro' : 'login'
              )"
            >
              {{ pantalla === 'login'
                ? 'Regístrate aquí'
                : 'Inicia sesión' }}
            </button>
          </p>


      <p v-if="mensaje" class="mensaje">{{mensaje}}</p><p v-if="error" class="error">{{error}}</p>
      </section>
    </main>
  </template>
  <template v-else>
    <div class="dashboard-layout">
      <AppSidebar :active="vistaActual" :user="usuario" :mobile-open="menuAbierto" @navigate="alNavegar" @close="menuAbierto=false" @logout="cerrarSesion"/>
      <div class="mobile-shade" v-if="menuAbierto" @click="menuAbierto=false"></div>
      <div class="dashboard-content">
        <header class="dashboard-topbar"><div class="topbar-start"><button class="hamburger" @click="menuAbierto=!menuAbierto" aria-label="Abrir menú"><AppIcon name="menu"/></button><div><p class="eyebrow">RUTABOL-SCZ / {{ titulos[vistaActual] || 'Inicio' }}</p><strong>{{titulos[vistaActual] || 'Inicio'}}</strong></div></div><div class="topbar-end"><span class="role-tag">{{usuario?.rol}}</span><button class="theme-switch" @click="cambiarTema" :title="oscuro?'Cambiar a modo claro':'Cambiar a modo oscuro'"><AppIcon :name="oscuro?'sun':'moon'"/><span>{{oscuro?'Claro':'Oscuro'}}</span></button><button class="topbar-profile" @click="irA('perfil')" title="Mi perfil">{{usuario?.nombreCompleto?.charAt(0)?.toUpperCase()}}</button></div></header>
        <main class="view-content">
          <InicioView v-if="vistaActual==='inicio'" />
          <MicrosView v-else-if="vistaActual==='micros'" />
          <TarifasView v-else-if="vistaActual==='tarifas'" />
          <CategoriasView v-else-if="vistaActual==='categorias'" />
          <PasajesView v-else-if="vistaActual==='pasajes'" />
          <PerfilView v-else-if="vistaActual==='perfil'" />
          <AdminView v-else-if="vistaActual==='admin'" />
          <p v-if="mensaje" class="mensaje message-global">{{mensaje}}</p><p v-if="error" class="error message-global">{{error}}</p>
        </main>
        <footer class="dashboard-footer">RUTABOL-SCZ · Proyecto académico — Tecnología Web I · UPDS</footer>
      </div>
    </div>
  </template>
</div>
</template>
<style>

:root {
  font-family: 'Segoe UI', Arial, sans-serif;
  font-synthesis: none;
}

* {
  box-sizing: border-box;
}

body {
  margin: 0;
}

button, input {
  font: inherit;
}

button {
  cursor: pointer;
}

.app {
  --fondo: #f5f9f6;
  --superficie: #ffffff;
  --texto: #34453b;
  --secundario: #718078;
  --verde: #19875b;
  --verde-hover: #116c48;
  --verde-suave: #e6f4eb;
  --borde: #dee8e1;
  --input: #ffffff;
  --tabs: #f1f5f2;
  --sombra: 0 18px 50px rgba(30, 65, 45, 0.07);

  background: var(--fondo);
  color: var(--texto);
  min-height: 100vh;
  transition: background 0.25s, color 0.25s;
}

.app.oscuro {
  --fondo: #151d19;
  --superficie: #232e27;
  --texto: #e1e9e3;
  --secundario: #a0aea4;
  --verde: #258b60;
  --verde-hover: #1b704d;
  --verde-suave: #304a3a;
  --borde: #3b4b41;
  --input: #29372e;
  --tabs: #1a241e;
  --sombra: 0 18px 50px rgba(0, 0, 0, 0.15);
}

.navbar {
  height: 85px;
  background: var(--superficie);
  border-bottom: 1px solid var(--borde);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 7%;
}

.marca {
  display: flex;
  align-items: center;
  gap: 13px;
}

.marca-icono {
  width: 48px;
  height: 48px;
  background: var(--verde);
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 13px;
  font-size: 25px;
}

.marca h1 {
  font-size: 21px;
  margin: 0;
  letter-spacing: 0.3px;
}

.marca h1 span {
  color: var(--verde);
}

.marca p {
  margin: 4px 0 0;
  color: var(--secundario);
  font-size: 12px;
}

.navbar-derecha {
  display: flex;
  align-items: center;
  gap: 20px;
}

.etiqueta-superior {
  font-size: 13px;
  color: var(--secundario);
}

.tema-boton {
  border: 1px solid var(--borde);
  background: var(--superficie);
  color: var(--texto);
  padding: 11px 15px;
  border-radius: 10px;
  display: flex;
  gap: 8px;
  align-items: center;
  font-size: 13px;
  font-weight: 600;
}

.tema-boton:hover {
  background: var(--verde-suave);
}

.contenido {
  max-width: 1190px;
  margin: 0 auto;
  padding: 75px 35px 90px;
  display: grid;
  grid-template-columns: 1fr 440px;
  align-items: center;
  gap: 90px;
}

.hero-etiqueta {
  display: inline-flex;
  gap: 9px;
  align-items: center;
  color: var(--verde);
  background: var(--verde-suave);
  font-size: 11px;
  font-weight: 700;
  padding: 11px 15px;
  border-radius: 50px;
  letter-spacing: 1px;
}

.punto {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--verde);
}

.hero h2 {
  font-size: clamp(36px, 4.3vw, 57px);
  letter-spacing: -2px;
  line-height: 1.13;
  margin: 25px 0;
  font-weight: 750;
}

.hero h2 span {
  color: var(--verde);
}

.hero-descripcion {
  font-size: 16px;
  color: var(--secundario);
  line-height: 1.9;
  max-width: 510px;
}

.beneficios {
  display: flex;
  flex-direction: column;
  gap: 22px;
  margin-top: 38px;
}

.beneficio {
  display: flex;
  align-items: center;
  gap: 15px;
}

.beneficio-icono {
  background: var(--verde-suave);
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
}

.beneficio strong {
  font-size: 14px;
  font-weight: 650;
}

.beneficio p {
  font-size: 12px;
  color: var(--secundario);
  margin: 5px 0 0;
}

.hero-nota {
  margin-top: 45px;
  font-size: 12px;
  color: var(--secundario);
}

.tarjeta {
  background: var(--superficie);
  border: 1px solid var(--borde);
  border-radius: 20px;
  padding: 36px;
  box-shadow: var(--sombra);
}

.tarjeta-icono {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 55px;
  height: 55px;
  background: var(--verde-suave);
  border-radius: 15px;
  font-size: 27px;
  margin-bottom: 24px;
}

.tarjeta h3 {
  font-size: 25px;
  margin: 0 0 8px;
  letter-spacing: -0.5px;
}

.subtitulo {
  color: var(--secundario);
  font-size: 13px;
  line-height: 1.6;
  margin: 0 0 25px;
}

.tabs {
  display: flex;
  background: var(--tabs);
  border-radius: 11px;
  padding: 5px;
  margin-bottom: 25px;
}

.tabs button {
  flex: 1;
  padding: 12px;
  border-radius: 8px;
  border: none;
  background: transparent;
  color: var(--secundario);
  font-size: 13px;
  font-weight: 650;
}

.tabs button.activo {
  background: var(--superficie);
  color: var(--verde);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.campo {
  margin-bottom: 19px;
}

.campo label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 9px;
}

.campo input {
  width: 100%;
  padding: 14px 15px;
  border: 1px solid var(--borde);
  border-radius: 9px;
  background: var(--input);
  color: var(--texto);
  outline: none;
  font-size: 14px;
}

.campo input::placeholder {
  color: var(--secundario);
  opacity: 0.75;
}

.campo input:focus {
  border-color: var(--verde);
  box-shadow: 0 0 0 3px rgba(25, 135, 91, 0.12);
}

.password-contenedor {
  position: relative;
}

.password-contenedor input {
  padding-right: 83px;
}

.ver-password {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  border: none;
  background: transparent;
  color: var(--verde);
  font-size: 12px;
  font-weight: 700;
  padding: 8px;
}

.boton-principal {
  margin-top: 10px;
  width: 100%;
  background: var(--verde);
  border: none;
  color: white;
  padding: 16px;
  border-radius: 10px;
  font-weight: 700;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 15px;
}

.boton-principal:hover {
  background: var(--verde-hover);
}

.boton-principal span {
  font-size: 19px;
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.ayuda {
  text-align: center;
  font-size: 12px;
  color: var(--secundario);
  margin: 25px 0 0;
}

.enlace {
  color: var(--verde);
  border: none;
  background: none;
  font-weight: 700;
  padding: 3px;
}

.panel-superior {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 22px;
}

.avatar {
  width: 55px;
  height: 55px;
  background: var(--verde);
  color: white;
  border-radius: 15px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  font-weight: 700;
}

.estado {
  font-size: 12px;
  color: var(--verde);
  background: var(--verde-suave);
  padding: 9px 12px;
  border-radius: 20px;
  font-weight: 700;
}

.panel-datos {
  border: 1px solid var(--borde);
  border-radius: 12px;
  padding: 5px 17px;
  margin-bottom: 17px;
}

.dato {
  padding: 14px 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
  border-bottom: 1px solid var(--borde);
}

.dato:last-child {
  border-bottom: none;
}

.dato span {
  color: var(--secundario);
  font-size: 12px;
}

.dato strong {
  font-size: 13px;
  font-weight: 650;
  overflow-wrap: anywhere;
}

.verde {
  color: var(--verde);
}

.boton-secundario,
.boton-salir {
  width: 100%;
  padding: 13px;
  border-radius: 9px;
  font-weight: 650;
  margin-top: 10px;
}

.boton-secundario {
  background: var(--verde-suave);
  color: var(--verde);
  border: none;
}

.boton-salir {
  background: #b83e47;
  color: white;
  border: none;
}

.boton-salir:hover {
  background: #9b2f38;
}

.mensaje,
.error {
  border-radius: 9px;
  padding: 13px;
  margin-top: 20px;
  font-size: 13px;
}

.mensaje {
  background: var(--verde-suave);
  color: var(--verde);
}

.error {
  background: #fce8e8;
  color: #b83e47;
}

footer {
  display: flex;
  justify-content: space-between;
  border-top: 1px solid var(--borde);
  padding: 24px 7%;
  color: var(--secundario);
  font-size: 12px;
}

@media (max-width: 900px) {
  .contenido {
    grid-template-columns: 1fr;
    gap: 40px;
    padding: 40px 22px;
  }

  .hero h2 {
    font-size: 40px;
  }

  .tarjeta {
    max-width: 520px;
    width: 100%;
    margin: 0 auto;
  }
}

@media (max-width: 520px) {
  .navbar {
    padding: 0 18px;
  }

  .etiqueta-superior {
    display: none;
  }

  .tema-boton span {
    display: none;
  }

  .hero h2 {
    font-size: 34px;
  }

  .tarjeta {
    padding: 25px 20px;
  }

  footer {
    flex-direction: column;
    gap: 8px;
    text-align: center;
  }
}

.modulo-micros {
  margin-top: 28px;
  border-top: 1px solid var(--borde);
  padding-top: 25px;
}

.modulo-encabezado h4,
.formulario-micro h4,
.lista-encabezado h4 {
  margin: 0 0 8px;
  font-size: 16px;
  color: var(--texto);
}

.modulo-encabezado p {
  color: var(--secundario);
  font-size: 12px;
}

.estadisticas-micros {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin: 22px 0;
}

.estadistica-micro {
  padding: 17px;
  border-radius: 12px;
  background: var(--verde-suave);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.estadistica-micro span {
  color: var(--secundario);
  font-size: 12px;
}

.estadistica-micro strong {
  color: var(--verde);
  font-size: 28px;
}

.formulario-micro {
  border: 1px solid var(--borde);
  padding: 20px;
  border-radius: 12px;
  margin-bottom: 25px;
}

.lista-encabezado {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 15px;
}

.boton-actualizar {
  background: var(--verde-suave);
  color: var(--verde);
  border: none;
  border-radius: 8px;
  padding: 9px 12px;
  font-size: 12px;
  font-weight: 600;
}

.micro-item {
  display: flex;
  flex-wrap: wrap; /* necesario para que el bloque de ocupación pase a otra fila */
  align-items: center;
  gap: 12px;
  border: 1px solid var(--borde);
  border-radius: 12px;
  padding: 14px;
  margin-bottom: 12px;
}

.micro-icono {
  background: var(--verde-suave);
  padding: 12px;
  border-radius: 10px;
  font-size: 22px;
}

.micro-informacion {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 5px;
  min-width: 0;
}

.micro-informacion strong {
  color: var(--texto);
  font-size: 13px;
}

.micro-informacion span {
  color: var(--secundario);
  font-size: 11px;
}

.micro-estado {
  color: var(--verde);
  font-size: 11px;
  font-weight: 700;
  white-space: nowrap;
}

@media (max-width: 520px) {
  .estadisticas-micros {
    grid-template-columns: 1fr;
  }

  .micro-item {
    flex-wrap: wrap;
  }
}

/* ===== Ocupación de micros ===== */
.ocupacion-contenedor {
  width: 100%;
  margin-top: 12px;
  padding-top: 15px;
  border-top: 1px solid var(--borde);
}

.ocupacion-titulo {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 10px;
  color: var(--texto);
  font-size: 13px;
}

.ocupacion-barra {
  width: 100%;
  height: 12px;
  border-radius: 20px;
  background: var(--verde-suave);
  overflow: hidden;
}

.ocupacion-progreso {
  height: 100%;
  background: var(--verde);
  border-radius: 20px;
  transition: width 0.3s ease;
}

.progreso-lleno {
  background: #d46b42;
}

.ocupacion-espacios {
  font-size: 12px;
  color: var(--secundario);
  margin: 10px 0 15px;
}

.ocupacion-botones {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

.boton-subir,
.boton-bajar {
  padding: 12px 8px;
  border-radius: 9px;
  border: none;
  font-size: 12px;
  font-weight: 700;
}

.boton-subir {
  background: var(--verde);
  color: white;
}

.boton-subir:hover:not(:disabled) {
  background: var(--verde-hover);
}

.boton-bajar {
  background: var(--verde-suave);
  color: var(--verde);
  border: 1px solid var(--borde);
}

.estado-lleno {
  color: #d46b42;
}

.ocupacion-botones button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.modulo-tarifas {
  margin-top: 28px;
  border-top: 1px solid var(--borde);
  padding-top: 25px;
}

.tarifa-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px;
  margin-bottom: 12px;
  border: 1px solid var(--borde);
  border-radius: 12px;
}

.tarifa-item strong {
  color: var(--texto);
  font-size: 14px;
}

.tarifa-item p {
  color: var(--secundario);
  font-size: 12px;
  margin: 6px 0 0;
}

.tarifa-precio {
  color: var(--verde);
  font-size: 19px;
  font-weight: 750;
  white-space: nowrap;
}

.formulario-tarifa {
  border: 1px solid var(--borde);
  border-radius: 12px;
  padding: 20px;
  margin: 20px 0 25px;
}

.formulario-tarifa h4,
.historial-tarifas h4 {
  color: var(--texto);
  margin: 0 0 18px;
  font-size: 15px;
}

.formulario-tarifa select {
  width: 100%;
  padding: 14px 15px;
  border: 1px solid var(--borde);
  border-radius: 9px;
  color: var(--texto);
  background: var(--input);
  font: inherit;
}

.nota-tarifa {
  color: var(--secundario);
  font-size: 12px;
  line-height: 1.6;
}

.boton-historial {
  width: 100%;
  margin-top: 12px;
}

.historial-tarifas {
  border-top: 1px solid var(--borde);
  margin-top: 18px;
  padding-top: 18px;
}

.historial-tarifas small {
  color: var(--secundario);
  display: block;
  margin-top: 6px;
  font-size: 11px;
}

.revision-item {
  border: 1px solid var(--borde);
  border-radius: 12px;
  padding: 14px;
  margin: 12px 0;
}
.revision-item p { color: var(--secundario); font-size: 12px; }
.revision-item a, .tarifa-item a { color: var(--verde); font-size: 12px; }
.revision-item .ocupacion-botones { margin-top: 12px; }


/* El rediseño reutiliza los controles de cada módulo */
.app{--fondo:#f4f7f5;--superficie:#ffffff;--texto:#1e3429;--secundario:#698074;--verde:#147d53;--verde-hover:#0d6241;--verde-suave:#e6f4eb;--borde:#d9e6df;--input:#ffffff;--tabs:#edf4ef;--sombra:0 12px 35px rgba(14,49,29,.06);--icon-color:#147d53;background:var(--fondo);color:var(--texto);min-height:100vh;transition:background .2s,color .2s}
.app.oscuro{--fondo:#101915;--superficie:#1c2821;--texto:#f0f5f1;--secundario:#a8b8ad;--verde:#239466;--verde-hover:#1c8055;--verde-suave:#2c4939;--borde:#34463a;--input:#25352a;--tabs:#18251d;--sombra:0 16px 40px rgba(0,0,0,.16);--icon-color:#f2f7f3}
.ui-icon{color:var(--icon-color);flex-shrink:0;vertical-align:middle}
.brand-logo{width:48px;height:48px;display:grid;place-items:center;border-radius:15px;background:var(--verde-suave)}
.brand-logo .ui-icon{color:var(--icon-color)}
.public-header{max-width:1350px;margin:auto;display:flex;justify-content:space-between;align-items:center;padding:25px 4%}
.brand-public{display:flex;align-items:center;gap:12px;font-weight:900;letter-spacing:.04em}.brand-public span{color:var(--verde)}
.theme-switch{display:flex;align-items:center;gap:9px;background:var(--superficie);color:var(--texto);border:1px solid var(--borde);padding:11px 13px;border-radius:12px;font-size:13px;font-weight:650}
.login-page{max-width:1180px;min-height:calc(100vh - 105px);margin:auto;display:grid;grid-template-columns:minmax(0,1.15fr) minmax(320px,430px);align-items:center;gap:9%;padding:25px 32px 70px}
.login-hero h1{font-size:clamp(45px,5.5vw,78px);line-height:1.03;letter-spacing:-.055em;margin:25px 0}.login-hero h1 em{font-style:normal;color:var(--verde)}.login-hero>p{font-size:17px;line-height:1.8;color:var(--secundario);max-width:540px}
.hero-tag{display:inline-flex;align-items:center;gap:9px;color:var(--verde);background:var(--verde-suave);padding:10px 13px;border-radius:30px;font-size:11px;font-weight:800;letter-spacing:.08em}
.login-highlights{display:flex;gap:12px;flex-wrap:wrap;margin-top:35px}.login-highlights span{display:flex;gap:8px;align-items:center;padding:12px;background:var(--superficie);border:1px solid var(--borde);border-radius:12px;color:var(--secundario);font-size:12px}
.login-card{background:var(--superficie);padding:34px;border-radius:20px;border:1px solid var(--borde);box-shadow:var(--sombra)}
.dashboard-layout{display:grid;grid-template-columns:250px minmax(0,1fr);min-height:100vh}
.sidebar{height:100vh;position:sticky;top:0;display:flex;flex-direction:column;background:var(--superficie);border-right:1px solid var(--borde);padding:24px 14px 18px;z-index:50}
.side-brand{display:flex;gap:11px;align-items:center;padding:2px 10px 36px}.side-brand strong{font-size:15px;letter-spacing:.03em}.side-brand strong span{color:var(--verde)}.side-brand small{display:block;font-size:11px;color:var(--secundario);margin-top:5px}
.nav-label{color:var(--secundario);font-size:10px;font-weight:800;letter-spacing:.14em;margin:0 15px 12px}.nav-secondary{margin-top:28px}
.side-links{display:flex;flex-direction:column;gap:4px}.side-links a{color:var(--secundario);text-decoration:none;display:flex;align-items:center;gap:13px;padding:13px;border-radius:12px;font-weight:650;font-size:13px;transition:background .15s}.side-links a .ui-icon{opacity:.8}.side-links a:hover{background:var(--verde-suave);color:var(--texto)}.side-links a.nav-active{color:var(--verde);background:var(--verde-suave)}.nav-arrow{margin-left:auto;opacity:.45}
.side-bottom{margin-top:auto;padding-top:20px;border-top:1px solid var(--borde)}.side-account{display:flex;gap:9px;align-items:center;text-decoration:none;color:var(--texto);padding:9px 7px;border-radius:10px;min-width:0}.side-account>div:last-child{min-width:0}.side-account strong{font-size:12px;display:block;overflow:hidden;white-space:nowrap;text-overflow:ellipsis}.side-account small{color:var(--secundario);font-size:11px}.avatar-mini,.topbar-profile{background:var(--verde-suave);color:var(--texto);width:37px;height:37px;flex-shrink:0;display:grid;place-items:center;border-radius:11px;font-weight:800}
.sidebar-logout{border:0;background:transparent;display:flex;align-items:center;gap:11px;color:var(--secundario);padding:12px;cursor:pointer;width:100%;text-align:left;font-size:13px}.sidebar-logout:hover{color:#e77c7c}
.dashboard-content{min-width:0;display:flex;flex-direction:column;min-height:100vh}
.dashboard-topbar{height:78px;display:flex;align-items:center;justify-content:space-between;gap:16px;padding:0 3.8%;border-bottom:1px solid var(--borde);background:var(--superficie);position:sticky;top:0;z-index:20}
.topbar-start,.topbar-end{display:flex;align-items:center;gap:14px}.topbar-start strong{font-size:16px}.eyebrow{font-size:10px;color:var(--verde);letter-spacing:.13em;font-weight:800;text-transform:uppercase;margin:0 0 6px}.role-tag{color:var(--verde);background:var(--verde-suave);border-radius:10px;font-size:11px;font-weight:700;padding:9px 12px}.topbar-profile{border:0;cursor:pointer}.hamburger,.mobile-close{display:none}
.view-content{width:min(1100px,100%);padding:34px 38px 55px;margin:0 auto;flex:1}.dashboard-footer{font-size:11px;color:var(--secundario);padding:20px 38px;border-top:1px solid var(--borde);text-align:right}
.view-content .modulo-micros,.view-content .modulo-tarifas,.view-content .perfil-wrap,.view-content .admin-info{margin:0;background:var(--superficie);border:1px solid var(--borde);border-radius:19px;padding:26px;box-shadow:var(--sombra);max-width:none}
.view-content .modulo-micros,.view-content .modulo-tarifas{border-top:1px solid var(--borde)}.view-content .formulario-micro,.view-content .formulario-tarifa{background:var(--fondo)}
.view-content .micro-item,.view-content .tarifa-item{background:var(--superficie)}
.view-content .micro-icono,.view-content .tarjeta-icono{display:grid;place-items:center}.view-content .micro-icono{width:52px;height:52px}
.view-content .micro-estado{border-radius:12px;padding:6px 9px;background:var(--verde-suave)}.view-content .estado-lleno{color:#e58a5c;background:rgba(229,138,92,.12)}
.view-content a{color:var(--verde)}
.view-content .estadisticas-micros{grid-template-columns:repeat(2,minmax(0,1fr))}
.view-content .panel-datos{display:grid;grid-template-columns:1fr 1fr;gap:0 25px}
.view-content .panel-datos .dato:nth-last-child(2){border-bottom:0}
.view-content .dato{min-width:0}
.view-content .formulario-tarifa h4{margin-top:0}
.view-content .modulo-encabezado{display:flex;align-items:center;gap:10px;margin-bottom:18px}
.view-content .modulo-encabezado h4{font-size:22px;letter-spacing:-.03em}
.view-content .modulo-encabezado p{font-size:13px}
.home-view{display:flex;flex-direction:column;gap:23px}.welcome-banner{border-radius:20px;background:linear-gradient(114deg,#176442,#0d2e22);padding:37px;display:flex;align-items:center;justify-content:space-between;color:#fff;min-height:225px;overflow:hidden;position:relative}.welcome-banner .eyebrow{color:#a3e8c3}.welcome-banner h2{font-size:clamp(26px,3.2vw,40px);line-height:1.12;letter-spacing:-.04em;max-width:570px;margin:12px 0}.welcome-banner p:not(.eyebrow){color:#d1e9d9;font-size:13px;max-width:500px;line-height:1.6}.welcome-banner button{display:inline-flex;align-items:center;gap:9px;background:white;border:none;color:#155b3d;font-size:12px;font-weight:800;padding:12px 16px;border-radius:10px;margin-top:12px}.welcome-banner button .ui-icon{color:#155b3d}.home-bus{opacity:.15;transform:rotate(-12deg) scale(5);margin-right:55px}.home-bus .ui-icon{color:white}
.stats-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px}.stat-card{display:flex;align-items:center;gap:13px;border:1px solid var(--borde);border-radius:15px;padding:20px;background:var(--superficie)}.stat-card>.ui-icon{color:var(--verde)}.stat-card span{font-size:11px;color:var(--secundario);display:block;margin-bottom:7px}.stat-card strong{font-size:27px;font-weight:800;line-height:1}
.home-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}.summary-card{border:1px solid var(--borde);border-radius:17px;background:var(--superficie);padding:22px;min-width:0}.summary-title{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:16px}.summary-title h3{font-size:16px;margin:0}.text-link{border:0;background:none;color:var(--verde);display:flex;align-items:center;gap:5px;font-size:12px;font-weight:800}.summary-row{display:flex;align-items:center;gap:11px;border-top:1px solid var(--borde);padding:13px 0}.summary-row>div:nth-child(2){min-width:0;flex:1}.summary-row strong{font-size:12px}.summary-row p{font-size:11px;color:var(--secundario);margin:6px 0 0}.line-pill{background:var(--verde-suave);padding:8px;border-radius:10px}.state-pill{font-size:10px;color:var(--verde);background:var(--verde-suave);padding:6px;border-radius:8px;white-space:nowrap}.state-full{color:#db895e}.money{color:var(--verde);white-space:nowrap}.empty-state{color:var(--secundario);font-size:13px;padding:20px 0}
.access-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px}.access-card{border:1px solid var(--borde);padding:20px;border-radius:14px;background:var(--fondo);color:var(--texto);display:flex;gap:13px;flex-direction:column;align-items:flex-start;text-align:left}.access-card:hover{border-color:var(--verde)}.access-card strong{font-size:14px}.access-card span{font-size:12px;color:var(--secundario);line-height:1.6}.section-head{display:flex;align-items:center;gap:15px;margin-bottom:28px}.heading-icon{width:55px;height:55px;display:grid;place-items:center;background:var(--verde-suave);border-radius:15px}.section-head h2{font-size:25px;margin:0}.section-head p:last-child{color:var(--secundario);font-size:13px}.info-banner{display:flex;align-items:flex-start;gap:13px;border:1px solid var(--borde);border-radius:12px;padding:16px;margin-top:20px;background:var(--verde-suave)}.info-banner p{font-size:12px;line-height:1.65;color:var(--secundario)}
.message-global{margin-top:22px}.mobile-shade{display:none}
@media(max-width:1100px){.dashboard-layout{grid-template-columns:220px minmax(0,1fr)}.sidebar{padding-left:10px;padding-right:10px}.view-content{padding:25px 24px}.stats-grid{grid-template-columns:repeat(2,1fr)}}
@media(max-width:780px){.dashboard-layout{display:block}.sidebar{position:fixed;left:0;top:0;width:265px;transform:translateX(-110%);transition:transform .22s}.sidebar.sidebar-open{transform:translateX(0)}.mobile-shade{display:block;position:fixed;inset:0;background:rgba(0,0,0,.55);z-index:40}.hamburger,.mobile-close{display:grid;place-items:center;border:0;background:transparent;color:var(--texto)}.mobile-close{margin-left:auto}.dashboard-topbar{padding:0 18px}.role-tag{display:none}.view-content{padding:20px 14px}.dashboard-footer{text-align:center}.login-page{grid-template-columns:1fr;gap:28px}.login-hero{text-align:center}.login-hero>p{margin:auto}.login-highlights{justify-content:center}.home-grid{grid-template-columns:1fr}.welcome-banner{padding:26px}}
@media(max-width:510px){.view-content .modulo-micros,.view-content .modulo-tarifas,.view-content .perfil-wrap,.view-content .admin-info{padding:18px 13px}.stats-grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}.stat-card{padding:13px;gap:8px}.stat-card strong{font-size:23px}.stat-card span{font-size:10px}.access-grid{grid-template-columns:1fr}.view-content .estadisticas-micros{grid-template-columns:1fr}.view-content .panel-datos{grid-template-columns:1fr}.view-content .panel-datos .dato{border-bottom:1px solid var(--borde)}.welcome-banner h2{font-size:27px}.home-bus{display:none}.topbar-end .theme-switch span{display:none}.login-page{padding:15px}.login-card{padding:24px}.login-highlights{gap:8px}.login-highlights span{font-size:10px}.summary-row{gap:7px}}

</style>
