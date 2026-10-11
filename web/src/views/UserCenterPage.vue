<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { NButton } from 'naive-ui';
import { useUserCenterStore } from '@/stores/userCenterStore';
import { useAuthStore } from '@/stores/authStore';
import { useRouter } from 'vue-router';

const store = useUserCenterStore();
const authStore = useAuthStore();
const router = useRouter();
const accountUid = ref('');
const cookieUid = ref('');
const cookieCid = ref('');
const cookieUssid = ref('');
const accountError = ref('');

const canLoadUpstreamProfile = computed(() => Boolean(
  authStore.currentUser?.currentCloudAccountUid
  && authStore.loginCheckResult?.isLoggedIn === true,
));

function formatAccountDate(value: string | undefined): string {
  if (!value) return '时间未知';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '时间未知' : date.toLocaleString();
}

async function loadUserCenterData(): Promise<void> {
  try {
    await authStore.loadAccounts();
    // 账号上下文未选择或上游会话不可用时，只展示账号管理，避免
    // VIP/余额/空间接口触发 ambiguous 或失效会话请求。
    if (canLoadUpstreamProfile.value) await store.load();
  } catch {
    accountError.value = '账号列表加载失败，请稍后重试。';
  }
}

onMounted(() => { void loadUserCenterData(); });

async function addAccount(): Promise<void> {
  accountError.value = '';
  try {
    if (![accountUid.value, cookieUid.value, cookieCid.value, cookieUssid.value].every((value) => value.trim().length > 0)) {
      accountError.value = '请填写完整的账号信息。';
      return;
    }
    await authStore.bindAccount({ cloudAccountUid: accountUid.value.trim(), uid: cookieUid.value, cid: cookieCid.value, ussid: cookieUssid.value });
    if (canLoadUpstreamProfile.value) await store.load();
  } catch { accountError.value = '绑定账号失败，请检查输入。'; }
  finally {
    // 凭据只在本次提交期间存在于组件内存，成功或失败后都立即清理。
    accountUid.value = cookieUid.value = cookieCid.value = cookieUssid.value = '';
  }
}

async function removeAccount(uid: string): Promise<void> {
  try {
    await authStore.unbindAccount(uid);
    if (canLoadUpstreamProfile.value) await store.load();
  } catch { accountError.value = '解绑账号失败，请稍后重试。'; }
}

async function chooseAccount(uid: string): Promise<void> {
  try {
    await authStore.selectAccount(uid);
    if (canLoadUpstreamProfile.value) await store.load();
  } catch {
    accountError.value = '选择账号失败，请稍后重试。';
  }
}

async function reload(): Promise<void> {
  accountError.value = '';
  await loadUserCenterData();
}

async function logout(): Promise<void> {
  await authStore.signOut();
  await router.replace({ name: 'Login' });
}
</script>
<template><div class="p-4 md:p-6 space-y-5 text-white/85"><div class="flex items-center justify-between"><h1 class="text-lg font-semibold">用户中心</h1><div class="flex gap-2"><NButton size="small" :loading="authStore.accountLoading" @click="reload">刷新</NButton><NButton size="small" @click="logout">退出登录</NButton></div></div>
  <section class="glass-card p-4 space-y-3"><h2 class="font-medium">115 账号</h2><p class="text-xs text-white/50">Cookie 仅传给后端，后端使用 Data Protection 加密存储。</p><div v-if="authStore.accountLoading" class="text-sm text-white/50">账号信息加载中...</div><div v-else-if="!(authStore.currentUser?.accounts.length)" class="text-sm text-white/50">尚未绑定 115 账号。</div><div v-for="account in authStore.currentUser?.accounts || []" :key="account.cloudAccountUid" class="flex items-center justify-between rounded bg-white/5 px-3 py-2"><span><span>{{ account.cloudAccountUid }}</span><span class="ml-2 text-xs text-white/50">状态：{{ account.status }}</span><span class="ml-2 text-xs text-white/40">创建：{{ formatAccountDate(account.creationTime) }}</span><em v-if="account.cloudAccountUid === authStore.currentUser?.currentCloudAccountUid" class="ml-2 text-indigo-300">（当前）</em></span><span class="flex gap-2"><NButton size="tiny" :disabled="account.cloudAccountUid === authStore.currentUser?.currentCloudAccountUid || authStore.accountLoading" @click="chooseAccount(account.cloudAccountUid)">设为当前</NButton><NButton size="tiny" type="error" :loading="authStore.accountLoading" @click="removeAccount(account.cloudAccountUid)">解绑</NButton></span></div><div class="grid md:grid-cols-4 gap-2"><input v-model="accountUid" autocomplete="off" class="rounded bg-white/10 px-2 py-1" placeholder="115 账号 UID" /><input v-model="cookieUid" type="password" autocomplete="off" class="rounded bg-white/10 px-2 py-1" placeholder="UID" /><input v-model="cookieCid" type="password" autocomplete="off" class="rounded bg-white/10 px-2 py-1" placeholder="CID" /><input v-model="cookieUssid" type="password" autocomplete="off" class="rounded bg-white/10 px-2 py-1" placeholder="USSID" /></div><NButton type="primary" :loading="authStore.accountLoading" @click="addAccount">绑定账号</NButton><p v-if="accountError" class="text-sm text-amber-300">{{ accountError }}</p></section>
  <div class="grid md:grid-cols-3 gap-4"><section class="glass-card p-4"><h2 class="font-medium">VIP</h2><p v-if="store.errors.vip" class="text-red-300 mt-2">{{ store.errors.vip }}</p><p v-else-if="store.loading.vip" class="text-white/50 mt-2">加载中...</p><div v-else class="text-sm text-white/60 mt-2">等级：{{ store.vip?.levelName || '未提供' }}<br>到期：{{ store.vip?.expire || '未提供' }}</div></section><section class="glass-card p-4"><h2 class="font-medium">余额</h2><p v-if="store.errors.balance" class="text-red-300 mt-2">{{ store.errors.balance }}</p><p v-else-if="store.loading.balance" class="text-white/50 mt-2">加载中...</p><p v-else class="text-sm text-white/60 mt-2">{{ store.balance?.displayText || store.balance?.money || '未提供' }}</p></section><section class="glass-card p-4"><h2 class="font-medium">空间</h2><p v-if="store.errors.storage" class="text-red-300 mt-2">{{ store.errors.storage }}</p><p v-else-if="store.loading.storage" class="text-white/50 mt-2">加载中...</p><dl v-else-if="store.storage?.schemaStatus === 'available'" class="text-sm text-white/60 mt-2 space-y-1"><div>总空间：{{ store.storage.allTotal?.sizeFormat }}</div><div>已使用：{{ store.storage.allUse?.sizeFormat }}</div><div>剩余：{{ store.storage.allRemain?.sizeFormat }}</div></dl><p v-else class="text-white/50 mt-2">空间明细暂不可用</p></section></div><p class="text-xs text-white/40">当前用户：{{ authStore.currentUser?.userName }}</p></div></template>
