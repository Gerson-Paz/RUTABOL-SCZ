<script setup>
import { inject } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { oscuro, pantalla, nombre, correo, password, usuario, sesion, mensaje, error, cargando, mostrarPassword, tarifas, historialTarifas, cargandoTarifas, guardandoTarifa, mensajeTarifas, errorTarifas, mostrarHistorial, nuevaTarifa, micros, cargandoMicros, guardandoMicro, errorMicros, mensajeMicros, actualizandoMicro, nuevoMicro, solicitudNueva, fotoNueva, solicitudesMias, pendientesRevision, microsPasajeId, misPasajes, resultadoPasaje, guardandoSolicitud, procesandoPasaje, errorCategoria, avisoCategoria, errorPasaje, cargarMicros, registrarMicro, cambiarOcupacion, seleccionarFoto, cargarSolicitudes, enviarSolicitudCategoria, revisarSolicitudCategoria, cargarMisPasajes, simularPasaje, cargarTarifas, registrarTarifa, cargarHistorialTarifas, alternarHistorialTarifas, cambiarTema, limpiar, cambiarPantalla, solicitar, registrar, iniciarSesion, consultarSesion, cerrarSesion , irA } = inject('rutabol')
</script>
<template>
          <section class="modulo-tarifas">
            <div class="modulo-encabezado">
              <h4><AppIcon name="ticket" /> Simulación de pasajes</h4>
              <p>Calcula el monto desde la tarifa vigente, sin cobrar dinero real.</p>
            </div>
            <form class="formulario-tarifa" @submit.prevent="simularPasaje">
              <div class="campo">
                <label for="micro-pasaje">Selecciona un micro</label>
                <select id="micro-pasaje" v-model="microsPasajeId" required>
                  <option value="" disabled>Selecciona un micro</option>
                  <option v-for="m in micros" :key="m.id" :value="String(m.id)">
                    Línea {{ m.linea }} — Interno {{ m.numeroInterno }}
                  </option>
                </select>
              </div>
              <button class="boton-principal" :disabled="procesandoPasaje" type="submit">
                {{ procesandoPasaje ? 'Calculando...' : 'Simular pasaje (sin pago real)' }}
              </button>
            </form>
            <div v-if="resultadoPasaje" class="tarifa-item">
              <div><strong>Comprobante de simulación #{{ resultadoPasaje.id }}</strong>
                <p>{{ resultadoPasaje.categoriaAplicada }} · {{ resultadoPasaje.estado }}</p>
                <small>{{ resultadoPasaje.codigoComprobante }}</small>
              </div>
              <div class="tarifa-precio">Bs {{ Number(resultadoPasaje.monto).toFixed(2) }}</div>
            </div>
            <h4>Mis simulaciones recientes</h4>
            <p v-if="misPasajes.length === 0">Todavía no tienes simulaciones.</p>
            <div v-for="p in misPasajes" :key="p.id" class="tarifa-item">
              <div><strong>#{{ p.id }} · {{ p.categoriaAplicada }}</strong>
              <p>Micro #{{ p.microId }} · Simulado (no pagado)</p></div>
              <div class="tarifa-precio">Bs {{ Number(p.monto).toFixed(2) }}</div>
            </div>
            <p v-if="errorPasaje" class="error">{{ errorPasaje }}</p>
          </section>


</template>
