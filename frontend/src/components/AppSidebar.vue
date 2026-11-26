<script setup>
import AppIcon from './AppIcon.vue'
const props=defineProps({ active: String, user: Object, mobileOpen: Boolean })
const emit=defineEmits(['navigate','logout','close'])
const base=[{id:'inicio',label:'Inicio',icon:'house'},{id:'micros',label:'Micros y líneas',icon:'bus'},{id:'tarifas',label:'Tarifas',icon:'wallet'},{id:'categorias',label:'Categorías',icon:'graduation-cap'},{id:'pasajes',label:'Pasajes',icon:'ticket'}]
</script>
<template>
<aside class="sidebar" :class="{'sidebar-open':mobileOpen}" aria-label="Menú principal">
  <div class="side-brand"><div class="brand-logo"><AppIcon name="bus" :size="29" /></div><div><strong>RUTABOL<span>-SCZ</span></strong><small>Movilidad inteligente</small></div><button class="mobile-close" @click="emit('close')" aria-label="Cerrar menú"><AppIcon name="close"/></button></div>
  <div class="nav-label">NAVEGACIÓN</div>
  <nav class="side-links">
    <a v-for="link in base" :key="link.id" :href="`#/${link.id}`" :class="{'nav-active': active===link.id}" @click="emit('navigate',link.id)"><AppIcon :name="link.icon"/><span>{{link.label}}</span><AppIcon name="chevron-right" :size="14" class="nav-arrow"/></a>
    <template v-if="user?.rol==='Administrador'"><div class="nav-label nav-secondary">SISTEMA</div><a href="#/admin" :class="{'nav-active':active==='admin'}" @click="emit('navigate','admin')"><AppIcon name="settings"/><span>Administración</span><AppIcon name="chevron-right" :size="14" class="nav-arrow"/></a></template>
  </nav>
  <div class="side-bottom"><a href="#/perfil" :class="['side-account',{'nav-active':active==='perfil'}]" @click="emit('navigate','perfil')"><div class="avatar-mini">{{ user?.nombreCompleto?.charAt(0)?.toUpperCase() || 'U' }}</div><div><strong>{{user?.nombreCompleto || 'Usuario'}}</strong><small>{{user?.rol || 'Pasajero'}}</small></div></a><button class="sidebar-logout" @click="emit('logout')"><AppIcon name="log-out"/> Cerrar sesión</button></div>
</aside>
</template>
