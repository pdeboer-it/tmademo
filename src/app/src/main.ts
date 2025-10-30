import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { useSessionStore } from './stores/sessionStore'

const app = createApp(App)

app.use(createPinia())
app.use(router)

const session = useSessionStore()
await session.init()

app.mount('#app')
