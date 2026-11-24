
<script setup>
import { ref, onMounted } from 'vue'

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

const micros = ref([])
const cargandoMicros = ref(false)
const guardandoMicro = ref(false)
const errorMicros = ref('')
const mensajeMicros = ref('')

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
  } catch (e) {
    errorMicros.value = e.message
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
        throw new Error(
          'El servidor devolvió una respuesta no válida.'
        )
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

      throw new Error(
        resultado.mensaje ||
        `Error HTTP ${respuesta.status}`
      )
    }

    mensajeMicros.value = 'Micro registrado correctamente.'

    nuevoMicro.value = {
      linea: '',
      numeroInterno: '',
      capacidadMaxima: 25
    }

    await cargarMicros()

  } catch (e) {
    errorMicros.value = e.message
  } finally {
    guardandoMicro.value = false
  }
}



function cambiarTema() {
  oscuro.value = !oscuro.value
  localStorage.setItem(
    'rutabol-tema',
    oscuro.value ? 'oscuro' : 'claro'
  )
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
      throw new Error(
        Object.values(resultado.errors).flat().join(' ')
      )
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
  } catch {
    usuario.value = null
  }
})
</script>

<template>
  <div class="app" :class="{ oscuro }">

    <header class="navbar">
      <div class="marca">
        <div class="marca-icono">🚌</div>
        <div>
          <h1>RUTABOL<span>-SCZ</span></h1>
          <p>Movilidad inteligente</p>
        </div>
      </div>

      <div class="navbar-derecha">
        <span class="etiqueta-superior">Santa Cruz, Bolivia</span>

        <button
          class="tema-boton"
          @click="cambiarTema"
          :aria-label="oscuro ? 'Activar modo claro' : 'Activar modo oscuro'"
          :title="oscuro ? 'Modo claro' : 'Modo oscuro'"
        >
          {{ oscuro ? '☀️' : '🌙' }}
          <span>{{ oscuro ? 'Modo claro' : 'Modo oscuro' }}</span>
        </button>
      </div>
    </header>

    <main class="contenido">

      <section class="hero">
        <div class="hero-etiqueta">
          <span class="punto"></span>
          SISTEMA DE TRANSPORTE PÚBLICO
        </div>

        <h2>
          Una nueva forma<br />
          de <span>moverte por<br />tu ciudad.</span>
        </h2>

        <p class="hero-descripcion">
          RUTABOL-SCZ conecta pasajeros y transporte
          urbano en una sola plataforma. Más información,
          mayor seguridad y una mejor experiencia de viaje.
        </p>

        <div class="beneficios">
          <div class="beneficio">
            <div class="beneficio-icono">🚌</div>
            <div>
              <strong>Transporte organizado</strong>
              <p>Información de líneas y unidades</p>
            </div>
          </div>

          <div class="beneficio">
            <div class="beneficio-icono">🛡️</div>
            <div>
              <strong>Acceso seguro</strong>
              <p>Protección de cuentas y sesiones</p>
            </div>
          </div>

          <div class="beneficio">
            <div class="beneficio-icono">📍</div>
            <div>
              <strong>Movilidad conectada</strong>
              <p>Gestión inteligente del transporte</p>
            </div>
          </div>
        </div>

        <div class="hero-nota">
          Plataforma académica en desarrollo
        </div>
      </section>

      <section class="tarjeta">
        <template v-if="pantalla !== 'panel'">

          <div class="tarjeta-icono">🚌</div>

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

        </template>

        <template v-else>

          <div class="panel-superior">
            <div class="avatar">
              {{ usuario?.nombreCompleto?.charAt(0).toUpperCase() }}
            </div>
            <span class="estado">● Sesión activa</span>
          </div>

          <h3>¡Hola, {{ usuario?.nombreCompleto }}!</h3>

          <p class="subtitulo">
            Bienvenido al panel principal de RUTABOL-SCZ.
          </p>

          <div class="panel-datos">
            <div class="dato">
              <span>Nombre completo</span>
              <strong>{{ usuario?.nombreCompleto }}</strong>
            </div>

            <div class="dato">
              <span>Correo electrónico</span>
              <strong>{{ usuario?.correo }}</strong>
            </div>

            <div class="dato">
              <span>Rol de usuario</span>
              <strong>{{ usuario?.rol }}</strong>
            </div>

            <div class="dato">
              <span>Autenticación</span>
              <strong class="verde">
                {{ sesion?.autenticado
                  ? '✓ Autenticado'
                  : 'Verificando...' }}
              </strong>
            </div>

            <div class="dato">
              <span>Nombre almacenado en sesión</span>
              <strong>{{ sesion?.nombreGuardado || 'Sin datos' }}</strong>
            </div>

            <div class="dato">
              <span>Último acceso (UTC)</span>
              <strong>{{ sesion?.ultimoAcceso || 'Sin datos' }}</strong>
            </div>
          </div>

          <button class="boton-secundario" @click="consultarSesion">
            ↻ Actualizar sesión
          </button>


<div class="modulo-micros">

  <div class="modulo-encabezado">
    <div>
      <h4>🚌 Gestión de micros</h4>
      <p>Unidades registradas en RUTABOL-SCZ</p>
    </div>
  </div>

  <div class="estadisticas-micros">
    <div class="estadistica-micro">
      <span>Total de micros</span>
      <strong>{{ micros.length }}</strong>
    </div>

    <div class="estadistica-micro">
      <span>Pasajeros registrados a bordo</span>
      <strong>
        {{ micros.reduce((total, m) =>
          total + m.pasajerosActuales, 0) }}
      </strong>
    </div>
  </div>

  <form
    v-if="usuario?.rol === 'Administrador'"
    class="formulario-micro"
    @submit.prevent="registrarMicro"
  >
    <h4>Registrar nuevo micro</h4>

    <div class="campo">
      <label>Línea</label>
      <input
        v-model="nuevoMicro.linea"
        type="text"
        maxlength="20"
        placeholder="Ej. 17"
        required
      />
    </div>

    <div class="campo">
      <label>Número interno</label>
      <input
        v-model="nuevoMicro.numeroInterno"
        type="text"
        maxlength="20"
        placeholder="Ej. 101"
        required
      />
    </div>

    <div class="campo">
      <label>Capacidad máxima</label>
      <input
        v-model.number="nuevoMicro.capacidadMaxima"
        type="number"
        min="18"
        max="25"
        required
      />
    </div>

    <button
      type="submit"
      class="boton-principal"
      :disabled="guardandoMicro"
    >
      {{ guardandoMicro ? 'Guardando...' : 'Registrar micro' }}
    </button>
  </form>

  <div class="lista-micros">
    <div class="lista-encabezado">
      <h4>Unidades disponibles</h4>
      <button
        type="button"
        class="boton-actualizar"
        @click="cargarMicros"
      >
        ↻ Actualizar
      </button>
    </div>

    <p v-if="cargandoMicros">Consultando unidades...</p>

    <p v-else-if="micros.length === 0">
      Todavía no hay micros registrados.
    </p>

    <div
      v-for="micro in micros"
      :key="micro.id"
      class="micro-item"
    >
      <div class="micro-icono">🚌</div>

      <div class="micro-informacion">
        <strong>Línea {{ micro.linea }}</strong>
        <span>Interno: {{ micro.numeroInterno }}</span>
        <span>
          Ocupación: {{ micro.pasajerosActuales }} /
          {{ micro.capacidadMaxima }}
        </span>
      </div>

      <span class="micro-estado">{{ micro.estado }}</span>
    </div>
  </div>

  <p v-if="mensajeMicros" class="mensaje">
    {{ mensajeMicros }}
  </p>

  <p v-if="errorMicros" class="error">
    {{ errorMicros }}
  </p>

</div>


          <button
            class="boton-salir"
            :disabled="cargando"
            @click="cerrarSesion"
          >
            Cerrar sesión
          </button>

        </template>

        <div v-if="mensaje" class="mensaje">
          ✓ {{ mensaje }}
        </div>

        <div v-if="error" class="error">
          {{ error }}
        </div>
      </section>
    </main>

    <footer>
      <span>© RUTABOL-SCZ · Tecnología Web I</span>
      <span>Proyecto académico · UPDS</span>
    </footer>

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

</style>
