<script setup lang="ts">
defineProps<{
  mode: 'list' | 'grid';
  count?: number;
}>();
</script>

<template>
  <div v-if="mode === 'list'" class="space-y-2">
    <div
      v-for="i in (count ?? 8)"
      :key="i"
      class="flex items-center gap-3 px-4 py-3 rounded-lg animate-pulse"
    >
      <div class="w-5 h-5 rounded bg-white/10 shrink-0" />
      <div class="flex-1 space-y-2">
        <div class="h-4 rounded bg-white/10 w-3/5" />
        <div class="h-3 rounded bg-white/10 w-2/5" />
      </div>
      <div class="h-3 rounded bg-white/10 w-16 shrink-0" />
      <div class="h-3 rounded bg-white/10 w-24 shrink-0" />
    </div>
  </div>

  <div v-else class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4 p-4">
    <div
      v-for="i in (count ?? 12)"
      :key="i"
      class="flex flex-col items-center gap-2 p-4 rounded-lg animate-pulse"
    >
      <div class="w-12 h-12 rounded-lg bg-white/10" />
      <div class="h-3 rounded bg-white/10 w-4/5" />
      <div class="h-2 rounded bg-white/10 w-3/5" />
    </div>
  </div>
</template>
