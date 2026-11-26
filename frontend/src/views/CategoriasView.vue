<script setup>
import { inject } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { oscuro, pantalla, nombre, correo, password, usuario, sesion, mensaje, error, cargando, mostrarPassword, tarifas, historialTarifas, cargandoTarifas, guardandoTarifa, mensajeTarifas, errorTarifas, mostrarHistorial, nuevaTarifa, micros, cargandoMicros, guardandoMicro, errorMicros, mensajeMicros, actualizandoMicro, nuevoMicro, solicitudNueva, fotoNueva, solicitudesMias, pendientesRevision, microsPasajeId, misPasajes, resultadoPasaje, guardandoSolicitud, procesandoPasaje, errorCategoria, avisoCategoria, errorPasaje, cargarMicros, registrarMicro, cambiarOcupacion, seleccionarFoto, cargarSolicitudes, enviarSolicitudCategoria, revisarSolicitudCategoria, cargarMisPasajes, simularPasaje, cargarTarifas, registrarTarifa, cargarHistorialTarifas, alternarHistorialTarifas, cambiarTema, limpiar, cambiarPantalla, solicitar, registrar, iniciarSesion, consultarSesion, cerrarSesion , irA } = inject('rutabol')
</script>
<template>
          <section class="modulo-tarifas">
            <div class="modulo-encabezado">
              <h4><AppIcon name="graduation-cap" /> Solicitud de categoría preferencial</h4>
              <p>Revisión manual de documentación. No es una validación oficial de RUDE.</p>
            </div>
            <form class="formulario-tarifa" @submit.prevent="enviarSolicitudCategoria">
              <div class="campo">
                <label for="categoria-solicitud">Categoría solicitada</label>
                <select id="categoria-solicitud" v-model="solicitudNueva.categoria" required>
                  <option disabled value="">Selecciona una categoría</option>
                  <option>Universitaria</option>
                  <option>Secundaria</option>
                  <option>Primaria y menores</option>
                  <option>Adulto mayor</option>
                  <option>Persona con discapacidad</option>
                </select>
              </div>
              <div class="campo">
                <label for="codigo-solicitud">Código o referencia (RUDE, universitario o documento)</label>
                <input id="codigo-solicitud" v-model.trim="solicitudNueva.codigo" maxlength="50" required />
              </div>
              <div class="campo" v-if="solicitudNueva.categoria === 'Universitaria'">
                <label for="correo-institucional">Correo universitario @upds.edu.bo</label>
                <input id="correo-institucional" type="email" v-model.trim="solicitudNueva.correoInstitucional" required />
              </div>
              <div class="campo">
                <label for="foto-solicitud">Foto JPG o PNG (máximo 2 MB)</label>
                <input id="foto-solicitud" type="file" accept="image/jpeg,image/png" @change="seleccionarFoto" required />
              </div>
              <p class="nota-tarifa">Para la demostración usa solo imágenes y datos ficticios o autorizados.</p>
              <button class="boton-principal" :disabled="guardandoSolicitud" type="submit">
                {{ guardandoSolicitud ? 'Enviando...' : 'Solicitar revisión' }}
              </button>
            </form>
            <h4>Mis solicitudes</h4>
            <div v-for="s in solicitudesMias" :key="s.id" class="tarifa-item">
              <div><strong>{{ s.categoria }}</strong><p>{{ s.estado }}</p></div>
              <a :href="`/api/categorias/${s.id}/foto`" target="_blank" rel="noopener noreferrer">Ver foto</a>
            </div>
            <div v-if="usuario?.rol === 'Administrador'" class="formulario-tarifa">
              <h4>Solicitudes pendientes de revisar</h4>
              <p v-if="pendientesRevision.length === 0">Sin solicitudes pendientes.</p>
              <div v-for="s in pendientesRevision" :key="s.id" class="revision-item">
                <strong>{{ s.categoria }} — Usuario #{{ s.usuarioId }}</strong>
                <p>Código: {{ s.codigo }} · {{ s.correoInstitucional || 'Sin correo institucional' }}</p>
                <a :href="`/api/categorias/${s.id}/foto`" target="_blank" rel="noopener noreferrer">Ver documentación</a>
                <div class="ocupacion-botones">
                  <button type="button" class="boton-subir" @click="revisarSolicitudCategoria(s.id, 'Aprobada')">Aprobar</button>
                  <button type="button" class="boton-bajar" @click="revisarSolicitudCategoria(s.id, 'Rechazada')">Rechazar</button>
                </div>
              </div>
            </div>
            <p v-if="avisoCategoria" class="mensaje">{{ avisoCategoria }}</p>
            <p v-if="errorCategoria" class="error">{{ errorCategoria }}</p>
          </section>


</template>
