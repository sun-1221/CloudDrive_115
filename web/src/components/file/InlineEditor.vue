<script setup lang="ts">
import { ref, onMounted, nextTick, watch } from 'vue';
import { NInput } from 'naive-ui';
import { validateFileName } from '@/utils/fileNameValidator';

const props = defineProps<{
  initialValue: string;
}>();

const emit = defineEmits<{
  confirm: [value: string];
  cancel: [];
}>();

const value = ref(props.initialValue);
const errorMessage = ref('');
const inputRef = ref<InstanceType<typeof NInput> | null>(null);

onMounted(async () => {
  await nextTick();
  inputRef.value?.focus();
  // 选中不含扩展名的部分
  const dotIndex = props.initialValue.lastIndexOf('.');
  if (dotIndex > 0) {
    const el = (inputRef.value?.$el as HTMLElement)?.querySelector('input');
    el?.setSelectionRange(0, dotIndex);
  }
});

watch(value, (v) => {
  const result = validateFileName(v);
  errorMessage.value = result.valid ? '' : (result.reason ?? '');
});

function handleConfirm() {
  const trimmed = value.value.trim();
  if (!trimmed) return;
  const result = validateFileName(trimmed);
  if (!result.valid) {
    errorMessage.value = result.reason ?? '文件名无效';
    return;
  }
  emit('confirm', trimmed);
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter') {
    event.preventDefault();
    handleConfirm();
  } else if (event.key === 'Escape') {
    event.preventDefault();
    emit('cancel');
  }
}
</script>

<template>
  <div class="inline-flex flex-col gap-1">
    <NInput
      ref="inputRef"
      v-model:value="value"
      size="small"
      :status="errorMessage ? 'error' : undefined"
      @keydown="handleKeydown"
      @blur="handleConfirm"
    />
    <span v-if="errorMessage" class="text-xs text-red-400">{{ errorMessage }}</span>
  </div>
</template>
