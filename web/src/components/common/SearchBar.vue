<script setup lang="ts">
import { ref, computed } from 'vue';
import { NInput, NButton } from 'naive-ui';
import { Search } from 'lucide-vue-next';

const emit = defineEmits<{
  search: [keyword: string];
}>();

const keyword = ref('');
const showValidation = ref(false);

const validationMessage = computed(() => {
  if (showValidation.value && keyword.value.trim().length === 0) {
    return '请输入搜索关键词';
  }
  return '';
});

function handleSearch() {
  const trimmed = keyword.value.trim();
  if (trimmed.length === 0) {
    showValidation.value = true;
    return;
  }
  showValidation.value = false;
  emit('search', trimmed);
}

function handleInput() {
  if (showValidation.value && keyword.value.trim().length > 0) {
    showValidation.value = false;
  }
}
</script>

<template>
  <div class="relative">
    <div class="flex items-center gap-2">
      <NInput
        v-model:value="keyword"
        placeholder="搜索文件..."
        clearable
        size="small"
        :status="validationMessage ? 'error' : undefined"
        @keydown.enter="handleSearch"
        @update:value="handleInput"
      >
        <template #prefix>
          <Search :size="16" class="text-white/40" />
        </template>
      </NInput>
      <NButton size="small" type="primary" @click="handleSearch">
        搜索
      </NButton>
    </div>
    <p
      v-if="validationMessage"
      class="absolute left-0 top-full mt-1 text-xs text-red-400"
    >
      {{ validationMessage }}
    </p>
  </div>
</template>
