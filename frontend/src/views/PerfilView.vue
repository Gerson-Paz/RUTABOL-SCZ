<script setup>
import { inject } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { oscuro, pantalla, nombre, correo, password, usuario, sesion, mensaje, error, cargando, mostrarPassword, tarifas, historialTarifas, cargandoTarifas, guardandoTarifa, mensajeTarifas, errorTarifas, mostrarHistorial, nuevaTarifa, micros, cargandoMicros, guardandoMicro, errorMicros, mensajeMicros, actualizandoMicro, nuevoMicro, solicitudNueva, fotoNueva, solicitudesMias, pendientesRevision, microsPasajeId, misPasajes, resultadoPasaje, guardandoSolicitud, procesandoPasaje, errorCategoria, avisoCategoria, errorPasaje, cargarMicros, registrarMicro, cambiarOcupacion, seleccionarFoto, cargarSolicitudes, enviarSolicitudCategoria, revisarSolicitudCategoria, cargarMisPasajes, simularPasaje, cargarTarifas, registrarTarifa, cargarHistorialTarifas, alternarHistorialTarifas, cambiarTema, limpiar, cambiarPantalla, solicitar, registrar, iniciarSesion, consultarSesion, cerrarSesion , irA } = inject('rutabol')
</script>
<template>
<div class="perfil-wrap">          <div class="panel-superior">
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
            <AppIcon name="refresh-cw" /> Actualizar sesión
          </button>

</div>
</template>
