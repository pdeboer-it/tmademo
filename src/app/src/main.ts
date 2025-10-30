import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { useSessionStore } from './stores/sessionStore'

const app = createApp(App)

app.use(createPinia())
app.use(router)

const session = useSessionStore()
const isAuthRedirect =
  window.location.hash.includes('code=') ||
  window.location.hash.includes('id_token=') ||
  window.location.search.includes('code=') ||
  window.location.search.includes('id_token=')
await session.init()
if (isAuthRedirect) {
  await router.replace('/')
}

app.mount('#app')
