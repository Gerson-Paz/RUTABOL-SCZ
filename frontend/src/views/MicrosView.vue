<script setup>
import { inject, ref, computed, onMounted } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { oscuro, pantalla, nombre, correo, password, usuario, sesion, mensaje, error, cargando, mostrarPassword, tarifas, historialTarifas, cargandoTarifas, guardandoTarifa, mensajeTarifas, errorTarifas, mostrarHistorial, nuevaTarifa, micros, cargandoMicros, guardandoMicro, errorMicros, mensajeMicros, actualizandoMicro, nuevoMicro, solicitudNueva, fotoNueva, solicitudesMias, pendientesRevision, microsPasajeId, misPasajes, resultadoPasaje, guardandoSolicitud, procesandoPasaje, errorCategoria, avisoCategoria, errorPasaje, cargarMicros, registrarMicro, cambiarOcupacion, seleccionarFoto, cargarSolicitudes, enviarSolicitudCategoria, revisarSolicitudCategoria, cargarMisPasajes, simularPasaje, cargarTarifas, registrarTarifa, cargarHistorialTarifas, alternarHistorialTarifas, cambiarTema, limpiar, cambiarPantalla, solicitar, registrar, iniciarSesion, consultarSesion, cerrarSesion , irA } = inject('rutabol')

// Solo se habilitan botones en las unidades asignadas al conductor.
const unidadesAsignadas = ref([])
const cargandoAsignacion = ref(false)
const errorAsignacion = ref('')
const esConductor = computed(() => usuario.value?.rol === 'Conductor')
const idsAsignados = computed(() => new Set(unidadesAsignadas.value.map(x => Number(x.id))))
function puedeOperarMicro(id) {
  return usuario.value?.rol === 'Administrador' || (esConductor.value && idsAsignados.value.has(Number(id)))
}
async function cargarUnidadesAsignadas() {
  if (!esConductor.value) return
  cargandoAsignacion.value = true
  errorAsignacion.value = ''
  try {
    const r = await fetch('/api/ubicaciones/mis-unidades', { credentials: 'include' })
    const data = await r.json().catch(() => ({}))
    if (!r.ok) throw new Error(data.mensaje || 'No se pudieron consultar tus unidades asignadas.')
    unidadesAsignadas.value = data
  } catch (e) { errorAsignacion.value = e.message }
  finally { cargandoAsignacion.value = false }
}
onMounted(cargarUnidadesAsignadas)
</script>
<template>
          <div class="modulo-micros">

            <div class="modulo-encabezado">
              <div>
                <h4><AppIcon name="bus" /> Gestión de micros</h4>
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

            <div v-if="esConductor" class="modulo-encabezado" style="margin:14px 0">
              <div><strong>Mi unidad asignada</strong>
                <p v-if="cargandoAsignacion">Cargando asignaciones...</p>
                <p v-else-if="errorAsignacion" class="error">{{ errorAsignacion }}</p>
                <p v-else-if="!unidadesAsignadas.length">Todavía no tienes una unidad asignada. El administrador debe asignarte una línea y un interno.</p>
                <p v-else>Tu unidad es Línea {{ unidadesAsignadas[0]?.linea }} · Interno {{ unidadesAsignadas[0]?.numeroInterno }}. Solo puedes operar esa unidad.</p>
              </div><button type="button" class="boton-actualizar" @click="cargarUnidadesAsignadas">Actualizar asignaciones</button>
            </div>
            <div class="lista-micros">
              <div class="lista-encabezado">
                <h4>Unidades registradas</h4>
                <button
                  type="button"
                  class="boton-actualizar"
                  @click="cargarMicros"
                >
                  <AppIcon name="refresh-cw" /> Actualizar
                </button>
              </div>

              <p v-if="cargandoMicros">Consultando unidades...</p>

              <p v-else-if="!errorMicros && micros.length === 0">
                Todavía no hay micros registrados.
              </p>

              <div
                v-for="micro in micros"
                :key="micro.id"
                class="micro-item"
              >
                <div class="micro-icono"><AppIcon name="bus" /></div>

                <div class="micro-informacion">
                  <strong>Línea {{ micro.linea }}</strong>

                  <span>
                    Número interno: {{ micro.numeroInterno }}
                  </span>

                  <span>
                    Capacidad: {{ micro.capacidadMaxima }} pasajeros
                  </span>
                </div>

                <span
                  class="micro-estado"
                  :class="{
                    'estado-lleno':
                      micro.pasajerosActuales >= micro.capacidadMaxima
                  }"
                >
                  {{ micro.estado }}
                </span>

                <div class="ocupacion-contenedor">

                  <div class="ocupacion-titulo">
                    <strong>Ocupación actual</strong>

                    <strong>
                      {{ micro.pasajerosActuales }} /
                      {{ micro.capacidadMaxima }}
                    </strong>
                  </div>

                  <div class="ocupacion-barra">
                    <div
                      class="ocupacion-progreso"
                      :class="{
                        'progreso-lleno':
                          micro.pasajerosActuales >= micro.capacidadMaxima
                      }"
                      :style="{
                        width: (
                          micro.pasajerosActuales /
                          micro.capacidadMaxima * 100
                        ) + '%'
                      }"
                    ></div>
                  </div>

                  <p class="ocupacion-espacios">
                    {{
                      micro.pasajerosActuales >= micro.capacidadMaxima
                        ? 'Micro lleno. No hay espacios disponibles.'
                        : (micro.capacidadMaxima - micro.pasajerosActuales)
                          + ' espacios disponibles'
                    }}
                  </p>

                  <div
                    v-if="
                      puedeOperarMicro(micro.id)
                    "
                    class="ocupacion-botones"
                  >
                    <button
                      type="button"
                      class="boton-subir"
                      :disabled="
                        actualizandoMicro !== null ||
                        micro.pasajerosActuales >= micro.capacidadMaxima
                      "
                      @click="cambiarOcupacion(micro, 1)"
                    >
                      + Subir pasajero
                    </button>

                    <button
                      type="button"
                      class="boton-bajar"
                      :disabled="
                        actualizandoMicro !== null ||
                        micro.pasajerosActuales <= 0
                      "
                      @click="cambiarOcupacion(micro, -1)"
                    >
                      − Bajar pasajero
                    </button>
                  </div>

                </div>
              </div>
            </div>

            <p v-if="mensajeMicros" class="mensaje">
              {{ mensajeMicros }}
            </p>

            <p v-if="errorMicros" class="error">
              {{ errorMicros }}
            </p>

          </div>


</template>
