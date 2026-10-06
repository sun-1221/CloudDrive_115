<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
const router = useRouter();
onMounted(() => void router.replace({ name: 'Login' }));
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-[#0a0a0f] text-white/70">正在打开登录指引…</div>
</template>
