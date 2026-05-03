<script setup lang="ts">
import { ref } from 'vue';
import { NCheckbox } from 'naive-ui';
import FileTypeIcon from '@/components/common/FileTypeIcon.vue';
import { formatFileSize, formatRelativeDate } from '@/utils/formatters';
import type { CloudDriveFileDto } from '@/types';

defineProps<{
  file: CloudDriveFileDto;
  mode: 'list' | 'grid';
  selected: boolean;
  focused: boolean;
}>();

const emit = defineEmits<{
  click: [event: MouseEvent];
  dblclick: [];
  contextmenu: [event: MouseEvent];
  select: [event: MouseEvent];
}>();

const thumbnailError = ref(false);

function handleThumbnailError() {
  thumbnailError.value = true;
}
</script>

<template>
  <!-- 列表模式 -->
  <div
    v-if="mode === 'list'"
    class="flex items-center gap-3 px-4 py-2.5 cursor-pointer transition-colors duration-150"
    :class="[
      selected ? 'bg-indigo-500/15' : 'hover:bg-white/5',
      focused ? 'ring-1 ring-indigo-500/30' : '',
    ]"
    @click="emit('click', $event)"
    @dblclick="emit('dblclick')"
    @contextmenu.prevent="emit('contextmenu', $event)"
  >
    <NCheckbox
      :checked="selected"
      class="shrink-0"
      @click.stop
      @update:checked="emit('select', $event as unknown as MouseEvent)"
    />

    <!-- 缩略图/图标 -->
    <div class="w-8 h-8 flex items-center justify-center shrink-0">
      <img
        v-if="file.thumbnailUrl && !thumbnailError"
        :src="file.thumbnailUrl"
        :alt="file.name"
        class="w-8 h-8 rounded object-cover"
        loading="lazy"
        @error="handleThumbnailError"
      />
      <FileTypeIcon v-else :file-name="file.name" :is-directory="file.isDirectory" />
    </div>

    <!-- 文件名 -->
    <span class="flex-1 text-sm text-white/90 truncate min-w-0">{{ file.name }}</span>

    <!-- 大小 -->
    <span class="text-xs text-white/40 w-20 text-right shrink-0">
      {{ file.isDirectory ? '-' : formatFileSize(file.size) }}
    </span>

    <!-- 类型 -->
    <span class="text-xs text-white/40 w-16 text-right shrink-0">
      {{ file.isDirectory ? '文件夹' : file.fileType }}
    </span>

    <!-- 修改时间 -->
    <span class="text-xs text-white/40 w-28 text-right shrink-0">
      {{ formatRelativeDate(file.modifiedTime) }}
    </span>
  </div>

  <!-- 网格模式 -->
  <div
    v-else
    class="glass-card p-3 cursor-pointer transition-all duration-200 hover:scale-[1.02]"
    :class="[
      selected ? 'ring-2 ring-indigo-500/40' : '',
      focused ? 'ring-1 ring-indigo-500/30' : '',
    ]"
    @click="emit('click', $event)"
    @dblclick="emit('dblclick')"
    @contextmenu.prevent="emit('contextmenu', $event)"
  >
    <div class="flex flex-col items-center gap-2">
      <!-- 缩略图/图标 -->
      <div class="w-16 h-16 flex items-center justify-center rounded-lg bg-white/5">
        <img
          v-if="file.thumbnailUrl && !thumbnailError"
          :src="file.thumbnailUrl"
          :alt="file.name"
          class="w-16 h-16 rounded-lg object-cover"
          loading="lazy"
          @error="handleThumbnailError"
        />
        <FileTypeIcon v-else :file-name="file.name" :is-directory="file.isDirectory" :size="32" />
      </div>

      <!-- 文件名 -->
      <p class="text-xs text-white/80 text-center truncate w-full">{{ file.name }}</p>

      <!-- 大小 -->
      <p class="text-[10px] text-white/40">
        {{ file.isDirectory ? '文件夹' : formatFileSize(file.size) }}
      </p>
    </div>
  </div>
</template>
