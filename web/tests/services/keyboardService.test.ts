import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

// Mock uiStore 返回值
const mockUIStore = {
  activeModal: null as string | null,
  keyboardSuspended: false,
  fileBrowserFocused: true,
};

vi.mock('@/stores/uiStore', () => ({
  useUIStore: () => mockUIStore,
}));

import * as keyboardService from '@/services/keyboardService';

beforeEach(() => {
  setActivePinia(createPinia());
  keyboardService.unregisterAll();
  mockUIStore.activeModal = null;
  mockUIStore.keyboardSuspended = false;
  mockUIStore.fileBrowserFocused = true;
});

afterEach(() => {
  keyboardService.unregisterAll();
});

describe('Property 24: Backspace 不触发浏览器历史回退', () => {
  it('fileBrowser 作用域的 Backspace 必须调用 preventDefault', () => {
    const handler = vi.fn();
    keyboardService.register({
      key: 'Backspace',
      scope: 'fileBrowser',
      handler,
    });

    const event = new KeyboardEvent('keydown', {
      key: 'Backspace',
      bubbles: true,
      cancelable: true,
    });

    const preventDefaultSpy = vi.spyOn(event, 'preventDefault');
    document.dispatchEvent(event);

    expect(handler).toHaveBeenCalledOnce();
    expect(preventDefaultSpy).toHaveBeenCalledOnce();
  });

  it('fileBrowserFocused=false 时 Backspace 不触发 handler', () => {
    mockUIStore.fileBrowserFocused = false;

    const handler = vi.fn();
    keyboardService.register({
      key: 'Backspace',
      scope: 'fileBrowser',
      handler,
    });

    const event = new KeyboardEvent('keydown', {
      key: 'Backspace',
      bubbles: true,
      cancelable: true,
    });

    document.dispatchEvent(event);

    expect(handler).not.toHaveBeenCalled();
  });

  it('模态窗口打开时 fileBrowser Backspace 不响应', () => {
    mockUIStore.activeModal = 'imageViewer';

    const handler = vi.fn();
    keyboardService.register({
      key: 'Backspace',
      scope: 'fileBrowser',
      handler,
    });

    const event = new KeyboardEvent('keydown', {
      key: 'Backspace',
      bubbles: true,
      cancelable: true,
    });

    document.dispatchEvent(event);

    expect(handler).not.toHaveBeenCalled();
  });

  it('keyboardSuspended=true 时 Backspace 不响应', () => {
    mockUIStore.keyboardSuspended = true;

    const handler = vi.fn();
    keyboardService.register({
      key: 'Backspace',
      scope: 'fileBrowser',
      handler,
    });

    const event = new KeyboardEvent('keydown', {
      key: 'Backspace',
      bubbles: true,
      cancelable: true,
    });

    document.dispatchEvent(event);

    expect(handler).not.toHaveBeenCalled();
  });
});

describe('keyboardService 多快捷键支持', () => {
  it('Ctrl+A 触发 fileBrowser 作用域 handler', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'a', ctrl: true, scope: 'fileBrowser', handler });

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'a', ctrlKey: true, bubbles: true, cancelable: true,
    }));

    expect(handler).toHaveBeenCalledOnce();
  });

  it('Delete 键触发 fileBrowser 作用域 handler', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'Delete', scope: 'fileBrowser', handler });

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'Delete', bubbles: true, cancelable: true,
    }));

    expect(handler).toHaveBeenCalledOnce();
  });

  it('Enter 键触发 fileBrowser 作用域 handler', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'Enter', scope: 'fileBrowser', handler });

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'Enter', bubbles: true, cancelable: true,
    }));

    expect(handler).toHaveBeenCalledOnce();
  });

  it('ArrowDown 键触发 fileBrowser 作用域 handler', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'ArrowDown', scope: 'fileBrowser', handler });

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'ArrowDown', bubbles: true, cancelable: true,
    }));

    expect(handler).toHaveBeenCalledOnce();
  });

  it('ArrowUp 键触发 fileBrowser 作用域 handler', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'ArrowUp', scope: 'fileBrowser', handler });

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'ArrowUp', bubbles: true, cancelable: true,
    }));

    expect(handler).toHaveBeenCalledOnce();
  });

  it('模态打开时 Escape 仅触发对应模态作用域 handler', () => {
    mockUIStore.activeModal = 'imageViewer';

    const fileBrowserHandler = vi.fn();
    const imageViewerHandler = vi.fn();

    keyboardService.register({ key: 'Escape', scope: 'fileBrowser', handler: fileBrowserHandler });
    keyboardService.register({ key: 'Escape', scope: 'imageViewer' as any, handler: imageViewerHandler });

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'Escape', bubbles: true, cancelable: true,
    }));

    expect(fileBrowserHandler).not.toHaveBeenCalled();
    expect(imageViewerHandler).toHaveBeenCalledOnce();
  });

  it('Ctrl 未按下时 Ctrl+A 绑定不触发', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'a', ctrl: true, scope: 'fileBrowser', handler });

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'a', ctrlKey: false, bubbles: true, cancelable: true,
    }));

    expect(handler).not.toHaveBeenCalled();
  });

  it('交互控件（input）获焦时 fileBrowser 快捷键不触发', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'Delete', scope: 'fileBrowser', handler });

    const input = document.createElement('input');
    document.body.appendChild(input);
    input.focus();

    const event = new KeyboardEvent('keydown', {
      key: 'Delete', bubbles: true, cancelable: true,
    });
    Object.defineProperty(event, 'target', { value: input });
    document.dispatchEvent(event);

    expect(handler).not.toHaveBeenCalled();
    document.body.removeChild(input);
  });

  it('unregister 后快捷键不再触发', () => {
    const handler = vi.fn();
    keyboardService.register({ key: 'F2', scope: 'fileBrowser', handler });

    keyboardService.unregister('fileBrowser', 'F2');

    document.dispatchEvent(new KeyboardEvent('keydown', {
      key: 'F2', bubbles: true, cancelable: true,
    }));

    expect(handler).not.toHaveBeenCalled();
  });
});
