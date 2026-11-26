<script setup>
import { computed, inject, onMounted, ref } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { usuario, micros, cargarMicros, cambiarOcupacion, actualizandoMicro, irA } = inject('rutabol')
const miUnidad = ref(null)
const cargando = ref(false)
const error = ref('')
const estado = computed(() => micros.value.find(m => Number(m.id) === Number(miUnidad.value?.id)) || miUnidad.value)
const permitido = computed(() => usuario.value?.rol === 'Conductor')
async function actualizar() {
  if (!permitido.value) return
  cargando.value = true
  error.value = ''
  try {
    const r = await fetch('/api/ubicaciones/mis-unidades', { credentials: 'include' })
    const datos = await r.json().catch(() => ([]))
    if (!r.ok) throw new Error(datos.mensaje || `Error HTTP ${r.status}`)
    miUnidad.value = Array.isArray(datos) ? (datos[0] || null) : null
    await cargarMicros()
  } catch (e) { error.value = e.message }
  finally { cargando.value = false }
}
async function mover(cambio) {
  if (!estado.value || !permitido.value) return
  await cambiarOcupacion(estado.value, cambio)
  await actualizar()
}
onMounted(actualizar)
</script>

<template>
  <section v-if="permitido" class="modulo-micros">
    <div class="modulo-encabezado"><div><h2><AppIcon name="bus"/> Mi unidad asignada</h2><p>Panel exclusivo del conductor</p></div><button type="button" class="boton-actualizar" @click="actualizar"><AppIcon name="refresh-cw"/> Actualizar</button></div>
    <p v-if="cargando">Consultando tu asignación...</p>
    <p v-if="error" class="error">{{error}}</p>
    <div v-if="!cargando && !miUnidad" class="info-banner"><AppIcon name="info"/><div><strong>Aún no tienes un micro asignado.</strong><p>Un administrador debe asociar tu cuenta a una línea y número interno desde Administración → Centro de gestión.</p></div></div>
    <div v-if="miUnidad && estado" class="conductor-tarjeta">
      <p class="conductor-eyebrow">UNIDAD AUTORIZADA</p>
      <h2>Línea {{miUnidad.linea}}</h2>
      <h3>Interno {{miUnidad.numeroInterno}}</h3>
      <p>Estado: <strong>{{estado.estado}}</strong></p>
      <div class="conductor-cifra">{{estado.pasajerosActuales}} <span>/ {{estado.capacidadMaxima}} pasajeros</span></div>
      <div class="conductor-barra"><div :style="{width: `${Math.min(100,100 * estado.pasajerosActuales / Math.max(1,estado.capacidadMaxima))}%`}"></div></div>
      <div class="conductor-acciones"><button class="boton-subir" type="button" :disabled="actualizandoMicro !== null || estado.pasajerosActuales >= estado.capacidadMaxima" @click="mover(1)">+ Subir pasajero</button><button type="button" class="boton-bajar" :disabled="actualizandoMicro !== null || estado.pasajerosActuales <= 0" @click="mover(-1)">− Bajar pasajero</button></div>
      <button class="boton-principal" type="button" @click="irA('ubicaciones')"><AppIcon name="navigation"/> Compartir mi GPS actual</button>
      <p class="conductor-nota">No puedes modificar tarifas ni seleccionar ubicaciones manuales. El GPS se activa únicamente con tu autorización.</p>
    </div>
  </section>
</template>
<style scoped>
.conductor-tarjeta{border:1px solid var(--borde);border-radius:14px;background:var(--superficie);padding:26px;display:grid;gap:13px;max-width:660px}
.conductor-eyebrow{font-size:11px;font-weight:800;letter-spacing:.13em;color:var(--verde)}
.conductor-tarjeta h2{font-size:32px;margin:0}.conductor-tarjeta h3{margin:0;font-size:19px;color:var(--secundario)}
.conductor-cifra{font-size:42px;font-weight:800}.conductor-cifra span{font-size:16px;color:var(--secundario)}
.conductor-barra{height:12px;border-radius:20px;background:var(--borde);overflow:hidden}.conductor-barra>div{background:var(--verde);height:100%;transition:width .2s}
.conductor-acciones{display:flex;gap:12px;flex-wrap:wrap}.conductor-acciones button{padding:12px 16px;border:0;border-radius:10px;cursor:pointer}.conductor-acciones button:disabled{opacity:.5;cursor:not-allowed}
.conductor-nota{color:var(--secundario);font-size:12px}
</style>
