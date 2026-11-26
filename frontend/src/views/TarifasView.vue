<script setup>
import { inject } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { oscuro, pantalla, nombre, correo, password, usuario, sesion, mensaje, error, cargando, mostrarPassword, tarifas, historialTarifas, cargandoTarifas, guardandoTarifa, mensajeTarifas, errorTarifas, mostrarHistorial, nuevaTarifa, micros, cargandoMicros, guardandoMicro, errorMicros, mensajeMicros, actualizandoMicro, nuevoMicro, solicitudNueva, fotoNueva, solicitudesMias, pendientesRevision, microsPasajeId, misPasajes, resultadoPasaje, guardandoSolicitud, procesandoPasaje, errorCategoria, avisoCategoria, errorPasaje, cargarMicros, registrarMicro, cambiarOcupacion, seleccionarFoto, cargarSolicitudes, enviarSolicitudCategoria, revisarSolicitudCategoria, cargarMisPasajes, simularPasaje, cargarTarifas, registrarTarifa, cargarHistorialTarifas, alternarHistorialTarifas, cambiarTema, limpiar, cambiarPantalla, solicitar, registrar, iniciarSesion, consultarSesion, cerrarSesion , irA } = inject('rutabol')
</script>
<template>
          <section class="modulo-tarifas">
            <div class="modulo-encabezado">
              <h4><AppIcon name="wallet" /> Tarifas de pasajes</h4>
              <p>Consulta los precios registrados en RUTABOL-SCZ</p>
            </div>

            <form
              v-if="usuario?.rol === 'Administrador'"
              class="formulario-tarifa"
              @submit.prevent="registrarTarifa"
            >
              <h4>Registrar o actualizar tarifa</h4>

              <div class="campo">
                <label for="nombre-tarifa">Tipo de tarifa</label>
                <select id="nombre-tarifa" v-model="nuevaTarifa.nombre" required>
                  <option value="" disabled>Selecciona una categoría</option>
                  <option value="General">General</option>
                  <option value="Universitaria">Universitaria</option>
                  <option value="Secundaria">Secundaria</option>
                  <option value="Adulto mayor">Adulto mayor</option>
                  <option value="Persona con discapacidad">Persona con discapacidad</option>
                  <option value="Primaria y menores">Primaria y menores</option>
                </select>
              </div>

              <div class="campo">
                <label for="monto-tarifa">Precio en bolivianos (Bs)</label>
                <input
                  id="monto-tarifa"
                  v-model="nuevaTarifa.monto"
                  type="text"
                  inputmode="decimal"
                  placeholder="Ej. 3.50"
                  required
                />
              </div>

              <p class="nota-tarifa">
                Los precios deben basarse en tarifas autorizadas.
                Si cambias un importe, la versión anterior quedará en el historial.
              </p>

              <button
                type="submit"
                class="boton-principal"
                :disabled="guardandoTarifa"
              >
                {{ guardandoTarifa ? 'Guardando...' : 'Guardar tarifa' }}
              </button>
            </form>

            <div class="lista-encabezado">
              <h4>Tarifas vigentes</h4>
              <button type="button" class="boton-actualizar" @click="cargarTarifas">
                <AppIcon name="refresh-cw" /> Actualizar
              </button>
            </div>

            <p v-if="cargandoTarifas">Consultando tarifas...</p>
            <p v-else-if="!errorTarifas && tarifas.length === 0">
              Todavía no hay tarifas registradas.
            </p>

            <div v-for="tarifa in tarifas" :key="tarifa.id" class="tarifa-item">
              <div>
                <strong>{{ tarifa.nombre }}</strong>
                <p>Tarifa vigente</p>
              </div>
              <div class="tarifa-precio">
                Bs {{ Number(tarifa.monto).toFixed(2) }}
              </div>
            </div>

            <div v-if="usuario?.rol === 'Administrador'">
              <button
                type="button"
                class="boton-actualizar boton-historial"
                @click="alternarHistorialTarifas"
              >
                {{ mostrarHistorial ? 'Ocultar historial' : 'Ver historial de tarifas' }}
              </button>

              <div v-if="mostrarHistorial" class="historial-tarifas">
                <h4>Historial de tarifas</h4>
                <p v-if="historialTarifas.length === 0 && !errorTarifas">
                  Todavía no hay cambios registrados.
                </p>
                <div
                  v-for="tarifa in historialTarifas"
                  :key="tarifa.id"
                  class="tarifa-item"
                >
                  <div>
                    <strong>{{ tarifa.nombre }}</strong>
                    <p>{{ tarifa.vigente ? 'Vigente' : 'Versión anterior' }}</p>
                    <small>
                      Desde: {{ new Date(tarifa.fechaInicio).toLocaleDateString('es-BO') }}
                    </small>
                  </div>
                  <div class="tarifa-precio">Bs {{ Number(tarifa.monto).toFixed(2) }}</div>
                </div>
              </div>
            </div>

            <p v-if="mensajeTarifas" class="mensaje">{{ mensajeTarifas }}</p>
            <p v-if="errorTarifas" class="error">{{ errorTarifas }}</p>
          </section>


</template>
