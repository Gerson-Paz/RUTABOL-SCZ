<script setup>
import { computed, inject, onMounted, ref, watch } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { irA } = inject('rutabol')

const correoNuevo = ref('')
const conductores = ref([])
const micros = ref([])
const seleccionadoId = ref('')
const lineaElegida = ref('')
const microElegido = ref('')
const cargando = ref(false)
const guardando = ref(false)
const habilitando = ref(false)
const error = ref('')
const mensaje = ref('')

const conductorActual = computed(() => conductores.value.find(c => String(c.id) === seleccionadoId.value) || null)
const lineas = computed(() => [...new Set(micros.value.map(m => m.linea))].sort((a,b) => a.localeCompare(b,'es',{numeric:true})))
const internos = computed(() => micros.value.filter(m => m.linea === lineaElegida.value))
const asignados = computed(() => new Map(conductores.value.filter(c => c.unidad).map(c => [Number(c.unidad.microId), c])))

function seleccionarConductor() {
  lineaElegida.value = conductorActual.value?.unidad?.linea || ''
  microElegido.value = conductorActual.value?.unidad?.microId ? String(conductorActual.value.unidad.microId) : ''
}
watch(seleccionadoId, seleccionarConductor)
watch(lineaElegida, nueva => {
  if (microElegido.value && !micros.value.some(m => String(m.id) === microElegido.value && m.linea === nueva)) microElegido.value = ''
})

async function pedir(ruta, opciones = {}) {
  const r = await fetch(ruta, { credentials: 'include', ...opciones })
  const body = await r.json().catch(() => ({}))
  if (!r.ok) throw new Error(body.mensaje || `Error HTTP ${r.status}`)
  return body
}
async function actualizar() {
  if (cargando.value) return
  cargando.value = true; error.value = ''
  try {
    const [lista, unidades] = await Promise.all([pedir('/api/conductores'), pedir('/api/micros')])
    conductores.value = Array.isArray(lista) ? lista : []
    micros.value = Array.isArray(unidades) ? unidades : []
    if (!conductores.value.some(c => String(c.id) === seleccionadoId.value)) {
      seleccionadoId.value = conductores.value[0] ? String(conductores.value[0].id) : ''
    }
    seleccionarConductor()
  } catch (e) { error.value = e.message }
  finally { cargando.value = false }
}
async function habilitar() {
  if (habilitando.value || !correoNuevo.value.trim()) return
  habilitando.value = true; error.value = ''; mensaje.value = ''
  try {
    const r = await pedir('/api/conductores/habilitar', {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ correo: correoNuevo.value.trim() })
    })
    mensaje.value = r.mensaje
    correoNuevo.value = ''
    await actualizar()
    if (r.id) seleccionadoId.value = String(r.id)
  } catch (e) { error.value = e.message }
  finally { habilitando.value = false }
}
async function guardarAsignacion() {
  if (!conductorActual.value || guardando.value) return
  const id = microElegido.value ? Number(microElegido.value) : null
  const destino = micros.value.find(m => Number(m.id) === id)
  const pregunta = id ? `¿Asignar Línea ${destino?.linea}, Interno ${destino?.numeroInterno} a ${conductorActual.value.correo}?` : `¿Dejar a ${conductorActual.value.correo} sin unidad asignada?`
  if (!window.confirm(pregunta)) return
  guardando.value = true; error.value = ''; mensaje.value = ''
  try {
    const r = await pedir(`/api/conductores/${conductorActual.value.id}/unidad`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ microId: id })
    })
    mensaje.value = r.mensaje
    await actualizar()
  } catch (e) { error.value = e.message }
  finally { guardando.value = false }
}
onMounted(actualizar)
</script>

<template>
  <section class="admin-info">
    <div class="section-head"><div class="heading-icon"><AppIcon name="settings"/></div><div><p class="eyebrow">Administración</p><h2>Centro de gestión</h2><p>Control de líneas, unidades, tarifas y conductores autorizados.</p></div></div>
    <div class="access-grid">
      <button type="button" class="access-card" @click="irA('lineas')"><AppIcon name="bus"/><strong>Administrar líneas e internos</strong><span>Registrar, editar y retirar unidades conservando el historial</span></button>
      <button type="button" class="access-card" @click="irA('micros')"><AppIcon name="bus"/><strong>Gestionar micros</strong><span>Registro y control de ocupación</span></button>
      <button type="button" class="access-card" @click="irA('tarifas')"><AppIcon name="wallet"/><strong>Gestionar tarifas</strong><span>Versiones y tarifas vigentes</span></button>
      <button type="button" class="access-card" @click="irA('categorias')"><AppIcon name="graduation-cap"/><strong>Revisar categorías</strong><span>Validación de documentos de pasajeros</span></button>
    </div>

    <section class="ad-conductores">
      <div class="ad-encabezado"><div><p class="eyebrow">Control de permisos</p><h3>Gestión de conductores</h3><p>Solo el administrador puede asignar o cambiar una unidad.</p></div><button type="button" class="boton-actualizar" @click="actualizar" :disabled="cargando"><AppIcon name="refresh-cw"/> {{cargando?'Cargando...':'Actualizar'}}</button></div>

      <div class="ad-habilitar">
        <h4>Habilitar una cuenta como conductor</h4>
        <p>Primero registra la cuenta como pasajero. Después escribe su correo para convertirla en conductor.</p>
        <form class="ad-fila" @submit.prevent="habilitar">
          <input v-model="correoNuevo" type="email" required maxlength="254" placeholder="conductor1.rutabol@example.com" autocomplete="off" />
          <button class="boton-principal" type="submit" :disabled="habilitando">{{ habilitando ? 'Habilitando...' : 'Habilitar conductor' }}</button>
        </form>
      </div>

      <div class="ad-distribucion">
        <div class="ad-lista">
          <h4>Conductores registrados ({{conductores.length}})</h4>
          <p v-if="!cargando && !conductores.length" class="ad-mutado">Todavía no hay conductores. Habilita una cuenta para comenzar.</p>
          <button v-for="c in conductores" :key="c.id" type="button" class="ad-conductor" :class="{'ad-seleccionado':seleccionadoId===String(c.id)}" @click="seleccionadoId=String(c.id)">
            <span class="ad-conductor-nombre">{{c.nombreCompleto}}</span>
            <span class="ad-correo">{{c.correo}}</span>
            <span class="ad-estado" :class="{'ad-alerta':c.cantidadAsignaciones>1}">{{c.cantidadAsignaciones>1 ? 'Revisar asignaciones anteriores' : c.unidad ? `Línea ${c.unidad.linea} · Interno ${c.unidad.numeroInterno}${c.unidad.activo?'':' (retirado)'}` : 'Sin unidad asignada'}}</span>
          </button>
        </div>
        <form v-if="conductorActual" class="ad-form" @submit.prevent="guardarAsignacion">
          <h4>Asignar línea e interno</h4>
          <p><strong>{{conductorActual.nombreCompleto}}</strong></p><p class="ad-mutado">{{conductorActual.correo}}</p>
          <div class="ad-asignacion-vigente">Asignación actual: <strong>{{conductorActual.unidad ? `Línea ${conductorActual.unidad.linea} · Interno ${conductorActual.unidad.numeroInterno}` : 'Ninguna'}}</strong></div>
          <label>Línea<select v-model="lineaElegida"><option value="">Sin línea</option><option v-for="l in lineas" :key="l" :value="l">Línea {{l}}</option></select></label>
          <label>Interno<select v-model="microElegido" :disabled="!lineaElegida"><option value="">{{lineaElegida ? 'Sin unidad asignada' : 'Selecciona una línea'}}</option><option v-for="m in internos" :key="m.id" :value="String(m.id)" :disabled="asignados.has(Number(m.id)) && asignados.get(Number(m.id)).id !== conductorActual.id">Interno {{m.numeroInterno}}{{asignados.has(Number(m.id)) && asignados.get(Number(m.id)).id !== conductorActual.id ? ' — asignado a otro conductor' : ''}}</option></select></label>
          <p class="ad-mutado">Un conductor solo puede operar una unidad. Al cambiarla, su anterior asignación se libera. Para desasignarlo, elige «Sin unidad asignada».</p>
          <button class="boton-principal" type="submit" :disabled="guardando">{{guardando ? 'Guardando...' : 'Guardar asignación'}}</button>
        </form>
        <div v-else class="ad-form"><p class="ad-mutado">Selecciona un conductor de la lista para asignarle su unidad.</p></div>
      </div>
      <p v-if="error" class="error">{{error}}</p><p v-if="mensaje" class="ad-ok">{{mensaje}}</p>
      <p class="ad-mutado">Si el conductor ya estaba conectado, debe cerrar sesión y volver a ingresar después de habilitar su rol. Su unidad aparecerá en «Mi unidad asignada».</p>
    </section>
    <div class="info-banner"><AppIcon name="info"/><div><strong>Historial conservado</strong><p>Desactivar un interno no elimina sus pasajes ni sus registros anteriores. La asignación se administra exclusivamente desde este centro.</p></div></div>
  </section>
</template>

<style scoped>
.ad-conductores{margin:22px 0;display:grid;gap:18px;border:1px solid var(--borde);background:var(--superficie);border-radius:14px;padding:22px}
.ad-encabezado{display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap}.ad-encabezado h3{margin:3px 0 6px;font-size:22px}.ad-encabezado p,.ad-mutado,.ad-habilitar p{color:var(--secundario);font-size:13px;margin:4px 0}
.ad-habilitar{background:var(--verde-suave);border-radius:12px;padding:17px;display:grid;gap:10px}.ad-habilitar h4,.ad-lista h4,.ad-form h4{margin:0}
.ad-fila{display:flex;align-items:center;gap:10px;flex-wrap:wrap}.ad-fila input{flex:1;min-width:240px;padding:12px;background:var(--input);color:var(--texto);border:1px solid var(--borde);border-radius:9px}.ad-fila button{min-height:42px}
.ad-distribucion{display:grid;grid-template-columns:minmax(220px,1fr) minmax(260px,1fr);gap:16px}.ad-lista,.ad-form{border:1px solid var(--borde);border-radius:12px;padding:17px;display:grid;gap:13px;align-content:start;min-width:0}
.ad-conductor{text-align:left;display:grid;gap:5px;padding:12px;border:1px solid var(--borde);border-radius:10px;background:var(--input);color:var(--texto);cursor:pointer;overflow-wrap:anywhere}.ad-conductor:hover,.ad-seleccionado{border-color:var(--verde);background:var(--verde-suave)}
.ad-conductor-nombre{font-weight:800}.ad-correo,.ad-estado{color:var(--secundario);font-size:12px}.ad-alerta{color:#b45b18}
.ad-form p{margin:0}.ad-form label{display:grid;gap:6px;font-size:13px;font-weight:700}.ad-form select{padding:12px;min-width:0;border:1px solid var(--borde);border-radius:9px;background:var(--input);color:var(--texto)}.ad-asignacion-vigente{font-size:13px;border:1px solid var(--borde);border-radius:9px;padding:12px}
.ad-ok{border-radius:8px;background:var(--verde-suave);padding:12px}.ad-estado{font-weight:600}
@media(max-width:760px){.ad-distribucion{grid-template-columns:1fr}.ad-conductores{padding:14px}}
</style>
