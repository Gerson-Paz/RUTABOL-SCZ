<script setup>
import { inject } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { oscuro, pantalla, nombre, correo, password, usuario, sesion, mensaje, error, cargando, mostrarPassword, tarifas, historialTarifas, cargandoTarifas, guardandoTarifa, mensajeTarifas, errorTarifas, mostrarHistorial, nuevaTarifa, micros, cargandoMicros, guardandoMicro, errorMicros, mensajeMicros, actualizandoMicro, nuevoMicro, solicitudNueva, fotoNueva, solicitudesMias, pendientesRevision, microsPasajeId, misPasajes, resultadoPasaje, guardandoSolicitud, procesandoPasaje, errorCategoria, avisoCategoria, errorPasaje, cargarMicros, registrarMicro, cambiarOcupacion, seleccionarFoto, cargarSolicitudes, enviarSolicitudCategoria, revisarSolicitudCategoria, cargarMisPasajes, simularPasaje, cargarTarifas, registrarTarifa, cargarHistorialTarifas, alternarHistorialTarifas, cambiarTema, limpiar, cambiarPantalla, solicitar, registrar, iniciarSesion, consultarSesion, cerrarSesion , irA } = inject('rutabol')
</script>
<template>
<section class="admin-info">
<div class="section-head"><div class="heading-icon"><AppIcon name="settings"/></div><div><p class="eyebrow">Administración</p><h2>Centro de gestión</h2><p>Accesos a las herramientas que ya funcionan.</p></div></div>
<div class="access-grid">
<button type="button" class="access-card" @click="irA('micros')"><AppIcon name="bus"/><strong>Gestionar micros</strong><span>Registrar unidades y controlar ocupación</span></button>
<button type="button" class="access-card" @click="irA('tarifas')"><AppIcon name="wallet"/><strong>Gestionar tarifas</strong><span>Modificar precios y consultar versiones anteriores</span></button>
<button type="button" class="access-card" @click="irA('categorias')"><AppIcon name="graduation-cap"/><strong>Revisar categorías</strong><span>Aprobar o rechazar solicitudes con documentos</span></button>
</div>
<div class="info-banner"><AppIcon name="info"/><div><strong>Próxima función: administrar líneas e internos por separado</strong><p>La desactivación segura de unidades necesita cambios en PostgreSQL y en los controles de historial de pasajes. No incluimos eliminaciones directas en este rediseño para evitar perder datos.</p></div></div>
</section>
</template>
