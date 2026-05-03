import { useUIStore } from '@/stores/uiStore';
import type { KeyboardScope } from '@/types';

type KeyHandler = (event: KeyboardEvent) => void;

interface KeyBinding {
  key: string;
  ctrl?: boolean;
  shift?: boolean;
  alt?: boolean;
  handler: KeyHandler;
  scope: KeyboardScope;
}

const bindings: KeyBinding[] = [];
let initialized = false;

const INTERACTIVE_ELEMENTS = new Set(['INPUT', 'TEXTAREA', 'SELECT', 'BUTTON']);
const INTERACTIVE_ROLES = new Set(['button', 'combobox', 'menuitem', 'textbox', 'listbox', 'option']);

function isInteractiveElement(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  if (INTERACTIVE_ELEMENTS.has(target.tagName)) return true;
  if (target.isContentEditable) return true;
  const role = target.getAttribute('role');
  if (role && INTERACTIVE_ROLES.has(role)) return true;
  return false;
}

function shouldHandleEvent(event: KeyboardEvent, binding: KeyBinding): boolean {
  const uiStore = useUIStore();

  // 模态窗口打开时，仅处理对应模态作用域快捷键
  if (uiStore.activeModal !== null) {
    const modalScope = uiStore.activeModal as string;
    return binding.scope === modalScope;
  }

  // 键盘暂停时跳过所有快捷键
  if (uiStore.keyboardSuspended) return false;

  // 交互控件获焦时跳过文件浏览器快捷键
  if (binding.scope === 'fileBrowser' && isInteractiveElement(event.target)) {
    return false;
  }

  // 文件浏览器未聚焦时跳过文件浏览器快捷键
  if (binding.scope === 'fileBrowser' && !uiStore.fileBrowserFocused) {
    return false;
  }

  return true;
}

function matchesBinding(event: KeyboardEvent, binding: KeyBinding): boolean {
  if (event.key.toLowerCase() !== binding.key.toLowerCase()) return false;
  if (!!binding.ctrl !== (event.ctrlKey || event.metaKey)) return false;
  if (!!binding.shift !== event.shiftKey) return false;
  if (!!binding.alt !== event.altKey) return false;
  return true;
}

function handleKeyDown(event: KeyboardEvent): void {
  for (const binding of bindings) {
    if (!matchesBinding(event, binding)) continue;
    if (!shouldHandleEvent(event, binding)) continue;

    // Backspace 在 fileBrowser 作用域必须阻止浏览器历史回退
    if (binding.key === 'Backspace' && binding.scope === 'fileBrowser') {
      event.preventDefault();
    }

    binding.handler(event);
    return;
  }
}

export function register(binding: Omit<KeyBinding, 'handler'> & { handler: KeyHandler }): void {
  bindings.push(binding);
  ensureInitialized();
}

export function unregister(scope: KeyboardScope, key?: string): void {
  for (let i = bindings.length - 1; i >= 0; i--) {
    const b = bindings[i];
    if (b && b.scope === scope && (!key || b.key === key)) {
      bindings.splice(i, 1);
    }
  }

  if (bindings.length === 0) {
    teardown();
  }
}

export function unregisterAll(): void {
  bindings.length = 0;
  teardown();
}

function ensureInitialized(): void {
  if (initialized) return;
  document.addEventListener('keydown', handleKeyDown);
  initialized = true;
}

function teardown(): void {
  if (!initialized) return;
  document.removeEventListener('keydown', handleKeyDown);
  initialized = false;
}
