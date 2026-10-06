import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';

const CloudDriveLayout = () => import('@/views/CloudDriveLayout.vue');
const FileBrowserPage = () => import('@/views/FileBrowserPage.vue');
const RecycleBinPage = () => import('@/views/RecycleBinPage.vue');
const LoginPage = () => import('@/views/LoginPage.vue');
const OfflinePage = () => import('@/views/OfflinePage.vue');
const SharePage = () => import('@/views/SharePage.vue');
const UserCenterPage = () => import('@/views/UserCenterPage.vue');

const routes: RouteRecordRaw[] = [
  {
    path: '/cloud-drive',
    component: CloudDriveLayout,
    children: [
      {
        path: 'files/:directoryId?',
        name: 'FileBrowser',
        component: FileBrowserPage,
        meta: { requiresAuth: true },
      },
      {
        path: 'recycle-bin',
        name: 'RecycleBin',
        component: RecycleBinPage,
        meta: { requiresAuth: true },
      },
      { path: 'offline', name: 'Offline', component: OfflinePage, meta: { requiresAuth: true } },
      { path: 'share', name: 'Share', component: SharePage, meta: { requiresAuth: true } },
      { path: 'user', name: 'UserCenter', component: UserCenterPage, meta: { requiresAuth: true } },
      {
        path: '',
        redirect: { name: 'FileBrowser' },
      },
    ],
  },
  {
    path: '/login',
    name: 'Login',
    component: LoginPage,
    meta: { requiresAuth: false },
  },
  {
    path: '/',
    redirect: '/cloud-drive/files',
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach(async (to) => {
  if (to.meta.requiresAuth) {
    const { useAuthStore } = await import('@/stores/authStore');
    const authStore = useAuthStore();

    if (authStore.authStatus === 'unknown') {
      await authStore.bootstrapAuthState();
    }
    if (authStore.authStatus === 'unauthorized') {
      return { name: 'Login', query: { returnUrl: to.fullPath } };
    }
    if (authStore.authStatus === 'disabled') return false;
  }

  return true;
});

export default router;
