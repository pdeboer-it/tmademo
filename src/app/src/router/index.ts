import { createRouter, createWebHashHistory } from 'vue-router'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/', component: () => import('@/views/Home.vue') },
    { path: '/candidates', component: () => import('@/views/Candidates.vue') },
  ],
})

export default router
