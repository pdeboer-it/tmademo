import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { PublicClientApplication, type AccountInfo } from '@azure/msal-browser'

const msal = new PublicClientApplication({
  auth: {
    clientId: 'b1a5f44a-07f7-4780-9951-704bc4eef3c9',
    authority: 'https://login.microsoftonline.com/bf07dbcb-5446-466a-843b-35bbc7955e9d',
    redirectUri: window.location.origin,
  },
  cache: { cacheLocation: 'localStorage' },
})

// Handle redirect responses first
await msal.initialize()
await msal.handleRedirectPromise()

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.mount('#app')

// Simple login → token → call API flow
const scopes = ['api://eef58d6a-5358-4013-a7b0-c92bb0c4b6a8/api.read']
let account: AccountInfo | undefined = msal.getAllAccounts()[0]
if (!account) {
  await msal.loginRedirect({ scopes })
} else {
  const token = await msal.acquireTokenSilent({ scopes, account })
  console.log(token.accessToken)

  const response = await fetch(
    'https://tmademoapi-e9brgbcacubfgqa3.westeurope-01.azurewebsites.net/candidates',
    {
      headers: { Authorization: `Bearer ${token.accessToken}` },
    },
  )

  const data = await response.json()
  console.log(data)
}
