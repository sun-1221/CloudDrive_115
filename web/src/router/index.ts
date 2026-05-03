import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';

const CloudDriveLayout = () => import('@/views/CloudDriveLayout.vue');
const FileBrowserPage = () => import('@/views/FileBrowserPage.vue');
const SearchResultPage = () => import('@/views/SearchResultPage.vue');
const RecycleBinPage = () => import('@/views/RecycleBinPage.vue');
const AuthCallbackPage = () => import('@/views/AuthCallbackPage.vue');

const routes: RouteRecordRaw[] = [
  {
    path: '/cloud-drive',
    component: CloudDriveLayout,
    children: [
      {
        path: 'auth',
        name: 'AuthCallback',
        component: AuthCallbackPage,
        meta: { requiresAuth: false },
      },
      {
        path: 'files/:directoryId?',
        name: 'FileBrowser',
        component: FileBrowserPage,
        meta: { requiresAuth: true },
      },
      {
        path: 'search',
        name: 'SearchResult',
        component: SearchResultPage,
        meta: { requiresAuth: true },
      },
      {
        path: 'recycle-bin',
        name: 'RecycleBin',
        component: RecycleBinPage,
        meta: { requiresAuth: true },
      },
      {
        path: '',
        redirect: { name: 'FileBrowser' },
      },
    ],
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
  }

  return true;
});

export default router;
