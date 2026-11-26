<script setup>
import { inject } from 'vue'
import AppIcon from '../components/AppIcon.vue'
const { oscuro, pantalla, nombre, correo, password, usuario, sesion, mensaje, error, cargando, mostrarPassword, tarifas, historialTarifas, cargandoTarifas, guardandoTarifa, mensajeTarifas, errorTarifas, mostrarHistorial, nuevaTarifa, micros, cargandoMicros, guardandoMicro, errorMicros, mensajeMicros, actualizandoMicro, nuevoMicro, solicitudNueva, fotoNueva, solicitudesMias, pendientesRevision, microsPasajeId, misPasajes, resultadoPasaje, guardandoSolicitud, procesandoPasaje, errorCategoria, avisoCategoria, errorPasaje, cargarMicros, registrarMicro, cambiarOcupacion, seleccionarFoto, cargarSolicitudes, enviarSolicitudCategoria, revisarSolicitudCategoria, cargarMisPasajes, simularPasaje, cargarTarifas, registrarTarifa, cargarHistorialTarifas, alternarHistorialTarifas, cambiarTema, limpiar, cambiarPantalla, solicitar, registrar, iniciarSesion, consultarSesion, cerrarSesion , irA } = inject('rutabol')
</script>
<template>
<div class="home-view">
<section class="welcome-banner"><div><p class="eyebrow">MOVILIDAD INTELIGENTE · SANTA CRUZ</p><h2>Bienvenido a tu centro de movilidad.</h2><p>Consulta unidades, administra tarifas y revisa tus operaciones desde un solo lugar.</p><button type="button" @click="irA('micros')">Explorar micros <AppIcon name="arrow-right" /></button></div><div class="home-bus"><AppIcon name="bus" /></div></section>
<div class="stats-grid">
<div class="stat-card"><AppIcon name="bus"/><div><span>Micros registrados</span><strong>{{ micros.length }}</strong></div></div>
<div class="stat-card"><AppIcon name="users"/><div><span>Pasajeros a bordo</span><strong>{{ micros.reduce((total,m)=>total+m.pasajerosActuales,0) }}</strong></div></div>
<div class="stat-card"><AppIcon name="wallet"/><div><span>Tarifas vigentes</span><strong>{{ tarifas.length }}</strong></div></div>
<div class="stat-card"><AppIcon name="ticket"/><div><span>Mis simulaciones</span><strong>{{ misPasajes.length }}</strong></div></div>
</div>
<div class="home-grid"><section class="summary-card"><div class="summary-title"><h3>Estado de la flota</h3><button class="text-link" @click="irA('micros')">Ver unidades <AppIcon name="arrow-right" /></button></div><div v-if="!micros.length" class="empty-state">No hay unidades registradas.</div><div v-for="m in micros.slice(0,5)" :key="m.id" class="summary-row"><div class="line-pill"><AppIcon name="bus" /></div><div><strong>Línea {{ m.linea }}</strong><p>Interno {{ m.numeroInterno }} · {{ m.pasajerosActuales }}/{{ m.capacidadMaxima }} pasajeros</p></div><span class="state-pill" :class="{'state-full':m.pasajerosActuales>=m.capacidadMaxima}">{{ m.estado }}</span></div></section>
<section class="summary-card"><div class="summary-title"><h3>Tarifas vigentes</h3><button class="text-link" @click="irA('tarifas')">Ver tarifas <AppIcon name="arrow-right" /></button></div><div v-if="!tarifas.length" class="empty-state">Todavía no hay tarifas vigentes.</div><div v-for="t in tarifas.slice(0,6)" :key="t.id" class="summary-row"><div class="line-pill"><AppIcon name="wallet"/></div><div><strong>{{ t.nombre }}</strong><p>Tarifa registrada</p></div><strong class="money">Bs {{ Number(t.monto).toFixed(2) }}</strong></div></section></div>
</div>
</template>
