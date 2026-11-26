<script setup>
import { ref, computed, onMounted } from 'vue'
import AppIcon from '../components/AppIcon.vue'

const emit = defineEmits(['actualizar-micros'])
const lineas = ref([])
const micros = ref([])
const cargando = ref(false)
const procesando = ref(false)
const error = ref('')
const exito = ref('')
const codigoNuevo = ref('')
const codigoElegido = ref('')
const internoNuevo = ref('')
const capacidadNueva = ref(25)
const editando = ref(null)
const internoEditar = ref('')
const capacidadEditar = ref(25)

const activos = computed(() => micros.value.filter(m => m.activo !== false))
const inactivos = computed(() => micros.value.filter(m => m.activo === false))

function internosDe(linea) {
  return micros.value.filter(m => m.lineaId === linea.id || (!m.lineaId && m.linea?.trim().toUpperCase() === linea.codigo))
}

async function llamar(ruta, opciones = {}) {
  const r = await fetch(ruta, { credentials: 'include', ...opciones })
  const datos = await r.json().catch(() => ({}))
  if (!r.ok) throw new Error(datos.mensaje || `Error HTTP ${r.status}`)
  return datos
}
function jsonMetodo(metodo, datos) {
  return { method: metodo, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(datos) }
}
async function cargar() {
  cargando.value = true
  error.value = ''
  try {
    const [ls, ms] = await Promise.all([
      llamar('/api/lineas/todas'), llamar('/api/micros/todos')
    ])
    lineas.value = ls
    micros.value = ms
    if (!codigoElegido.value && lineas.value.some(l => l.activa)) {
      codigoElegido.value = lineas.value.find(l => l.activa).codigo
    }
    emit('actualizar-micros')
  } catch (e) { error.value = e.message }
  finally { cargando.value = false }
}
async function ejecutar(accion, mensaje = '') {
  if (procesando.value) return
  procesando.value = true
  error.value = ''
  exito.value = ''
  try {
    const respuesta = await accion()
    await cargar()
    exito.value = respuesta?.mensaje || mensaje || 'Operación completada.'
  } catch (e) { error.value = e.message }
  finally { procesando.value = false }
}
function crearLinea() {
  if (!codigoNuevo.value.trim()) return
  ejecutar(async () => {
    const res = await llamar('/api/lineas', jsonMetodo('POST', { codigo: codigoNuevo.value }))
    codigoElegido.value = codigoNuevo.value.trim().toUpperCase()
    codigoNuevo.value = ''
    return res
  })
}
function sincronizar() {
  if (!confirm('¿Vincular los micros que ya existen con sus líneas? Esta acción conserva los datos.')) return
  ejecutar(() => llamar('/api/lineas/sincronizar', { method: 'POST' }))
}
function crearMicro() {
  if (!codigoElegido.value || !internoNuevo.value.trim()) return
  ejecutar(async () => {
    const r = await llamar('/api/micros', jsonMetodo('POST', {
      linea: codigoElegido.value, numeroInterno: internoNuevo.value,
      capacidadMaxima: Number(capacidadNueva.value)
    }))
    internoNuevo.value = ''
    capacidadNueva.value = 25
    return { mensaje: `Interno ${r.numeroInterno || 'registrado'} correctamente.` }
  })
}
function iniciarEdicion(m) {
  editando.value = m.id
  internoEditar.value = m.numeroInterno
  capacidadEditar.value = m.capacidadMaxima
}
function guardarEdicion(m) {
  ejecutar(async () => {
    const r = await llamar(`/api/micros/${m.id}/editar`, jsonMetodo('PUT', {
      numeroInterno: internoEditar.value, capacidadMaxima: Number(capacidadEditar.value)
    }))
    editando.value = null
    return r
  })
}
function cambiarEstadoMicro(m) {
  const activar = m.activo === false
  const verbo = activar ? 'reactivar' : 'retirar'
  if (!confirm(`¿Deseas ${verbo} el interno ${m.numeroInterno} de la línea ${m.linea}?\nEl historial de pasajes se conservará.`)) return
  ejecutar(() => llamar(`/api/micros/${m.id}/estado`, jsonMetodo('PUT', { activo: activar })))
}
function cambiarEstadoLinea(l) {
  const activar = !l.activa
  if (!confirm(`¿Deseas ${activar ? 'reactivar' : 'desactivar'} la línea ${l.codigo}?\nNo se eliminará su historial.`)) return
  ejecutar(() => llamar(`/api/lineas/${l.id}/estado`, jsonMetodo('PUT', { activa: activar })))
}
onMounted(cargar)
</script>

<template>
<div class="lineas-fase13">
  <div class="section-head"><div class="heading-icon"><AppIcon name="bus"/></div>
    <div><p class="eyebrow">FASE 13 · ADMINISTRACIÓN</p><h2>Líneas e internos</h2>
    <p>Organiza cada línea por separado. Retirar una unidad no borra su historial.</p></div>
  </div>
  <div class="lineas-resumen">
    <article><AppIcon name="route"/><div><span>Líneas registradas</span><strong>{{ lineas.length }}</strong></div></article>
    <article><AppIcon name="bus"/><div><span>Internos activos</span><strong>{{ activos.length }}</strong></div></article>
    <article><AppIcon name="info"/><div><span>Internos retirados</span><strong>{{ inactivos.length }}</strong></div></article>
  </div>
  <div class="lineas-acciones">
    <button type="button" @click="cargar" :disabled="cargando || procesando"><AppIcon name="refresh-cw"/> Actualizar</button>
    <button type="button" @click="sincronizar" :disabled="procesando"><AppIcon name="route"/> Sincronizar micros existentes</button>
  </div>
  <p v-if="error" class="error" role="alert">{{ error }}</p>
  <p v-if="exito" class="mensaje" role="status">{{ exito }}</p>

  <div class="lineas-columnas">
    <section class="lineas-tarjeta">
      <h3>Crear una línea</h3>
      <form @submit.prevent="crearLinea" class="lineas-formulario">
        <label for="linea-nueva">Código de la línea</label>
        <input id="linea-nueva" v-model="codigoNuevo" maxlength="20" placeholder="Ej. 17" required/>
        <button type="submit" class="boton-principal" :disabled="procesando || !codigoNuevo.trim()"><AppIcon name="plus"/> Registrar línea</button>
      </form>
    </section>
    <section class="lineas-tarjeta">
      <h3>Crear un interno</h3>
      <form @submit.prevent="crearMicro" class="lineas-formulario">
        <label for="linea-elegida">Línea</label>
        <select id="linea-elegida" v-model="codigoElegido" required>
          <option value="" disabled>Seleccionar línea</option>
          <option v-for="l in lineas.filter(x => x.activa)" :key="l.id" :value="l.codigo">Línea {{ l.codigo }}</option>
        </select>
        <label for="numero-interno">Número interno</label>
        <input id="numero-interno" v-model="internoNuevo" maxlength="20" placeholder="Ej. 101" required/>
        <label for="capacidad-interno">Capacidad (18 a 25)</label>
        <input id="capacidad-interno" v-model.number="capacidadNueva" type="number" min="18" max="25" required/>
        <button type="submit" class="boton-principal" :disabled="procesando || !codigoElegido"><AppIcon name="plus"/> Registrar interno</button>
      </form>
    </section>
  </div>
  <div class="lineas-listado">
    <h3>Líneas registradas</h3>
    <p v-if="cargando">Cargando líneas e internos...</p>
    <p v-else-if="!lineas.length" class="lineas-ayuda">No se han registrado líneas. Pulsa «Sincronizar micros existentes» si ya tenías unidades.</p>
    <article v-for="l in lineas" :key="l.id" class="lineas-tarjeta linea-registro">
      <header class="linea-titulo"><div><strong>Línea {{ l.codigo }}</strong><span :class="l.activa ? 'estado-activo' : 'estado-inactivo'">{{ l.activa ? 'Activa' : 'Inactiva' }}</span></div>
        <button type="button" class="lineas-boton" :disabled="procesando" @click="cambiarEstadoLinea(l)">{{ l.activa ? 'Desactivar línea' : 'Reactivar línea' }}</button>
      </header>
      <p class="lineas-ayuda">{{ internosDe(l).length }} internos registrados. Las unidades retiradas permanecen en el historial.</p>
      <div v-if="!internosDe(l).length" class="lineas-vacio">Esta línea no tiene internos; puedes registrarlos sin crear otra línea.</div>
      <div v-for="m in internosDe(l)" :key="m.id" class="linea-interno">
        <div v-if="editando !== m.id" class="interno-datos">
          <strong><AppIcon name="bus"/> Interno {{ m.numeroInterno }}</strong>
          <span>{{ m.pasajerosActuales }} / {{ m.capacidadMaxima }} pasajeros</span>
          <span :class="m.activo === false ? 'estado-inactivo' : 'estado-activo'">{{ m.activo === false ? 'Retirado' : m.estado }}</span>
        </div>
        <form v-else class="lineas-edicion" @submit.prevent="guardarEdicion(m)">
          <label>Número <input v-model="internoEditar" maxlength="20" required/></label>
          <label>Capacidad <input v-model.number="capacidadEditar" type="number" min="18" max="25" required/></label>
          <button type="submit" class="lineas-boton" :disabled="procesando">Guardar</button>
          <button type="button" class="lineas-boton" @click="editando=null">Cancelar</button>
        </form>
        <div v-if="editando !== m.id" class="interno-botones">
          <button type="button" class="lineas-boton" @click="iniciarEdicion(m)" :disabled="procesando">Editar</button>
          <button type="button" class="lineas-boton" @click="cambiarEstadoMicro(m)" :disabled="procesando" :class="{'lineas-peligro':m.activo !== false}">{{ m.activo === false ? 'Reactivar' : 'Retirar' }}</button>
        </div>
      </div>
    </article>
  </div>
  <div class="lineas-nota"><AppIcon name="shield"/> Retirar es una desactivación reversible. Los datos de micros y pasajes existentes no se borran.</div>
</div>
</template>

<style scoped>
.lineas-fase13 {display:grid;gap:22px;max-width:1200px}
.lineas-resumen {display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px}
.lineas-resumen article,.lineas-tarjeta {border:1px solid var(--borde);background:var(--superficie);border-radius:14px;padding:20px}
.lineas-resumen article {display:flex;align-items:center;gap:12px}
.lineas-resumen article :deep(svg){color:var(--verde)}
.lineas-resumen article div {display:grid;gap:3px}.lineas-resumen article span {font-size:.83rem;color:var(--secundario)}
.lineas-resumen article strong {font-size:1.6rem}
.lineas-acciones,.interno-botones {display:flex;flex-wrap:wrap;gap:8px}
.lineas-acciones button,.lineas-boton {background:var(--tabs);border:1px solid var(--borde);border-radius:9px;padding:9px 12px;color:var(--texto);display:inline-flex;align-items:center;justify-content:center;gap:8px;cursor:pointer}
.lineas-acciones button:disabled,.lineas-boton:disabled{opacity:.5;cursor:default}
.lineas-columnas {display:grid;grid-template-columns:1fr 1fr;gap:16px}
.lineas-tarjeta h3,.lineas-listado h3{margin:0 0 16px;font-size:1.1rem}
.lineas-formulario {display:grid;gap:9px}
.lineas-formulario label,.lineas-edicion label {font-size:.85rem;color:var(--secundario)}
.lineas-formulario input,.lineas-formulario select,.lineas-edicion input {background:var(--input);color:var(--texto);border:1px solid var(--borde);border-radius:9px;padding:12px;min-width:0;width:100%}
.lineas-formulario .boton-principal{display:flex;align-items:center;justify-content:center;gap:10px;margin-top:5px}
.lineas-listado{display:grid;gap:12px}.linea-registro{padding:18px}
.linea-titulo{display:flex;justify-content:space-between;gap:10px;align-items:center}.linea-titulo>div{display:flex;align-items:center;flex-wrap:wrap;gap:10px}
.estado-activo,.estado-inactivo{display:inline-block;border-radius:12px;padding:4px 8px;font-size:.77rem;font-weight:600;background:var(--verde-suave);color:var(--verde)}
.estado-inactivo{color:var(--secundario);background:var(--tabs)}
.lineas-ayuda,.lineas-vacio{font-size:.86rem;color:var(--secundario)}
.linea-interno{border-top:1px solid var(--borde);padding:13px 0;display:flex;justify-content:space-between;align-items:center;gap:14px}
.interno-datos{display:flex;align-items:center;flex-wrap:wrap;gap:12px;font-size:.9rem}
.interno-datos strong{display:inline-flex;align-items:center;gap:7px}
.lineas-edicion{display:flex;align-items:end;gap:8px;flex-wrap:wrap;width:100%}
.lineas-edicion label{min-width:120px;flex:1}
.lineas-peligro{border-color:#ba6b6b;color:#cb6868}
.lineas-nota{display:flex;align-items:center;gap:8px;color:var(--secundario);font-size:.83rem}
@media(max-width:800px){.lineas-columnas{grid-template-columns:1fr}.lineas-resumen{grid-template-columns:1fr}.linea-interno{align-items:start;flex-direction:column}}
</style>
