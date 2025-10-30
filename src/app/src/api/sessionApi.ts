import { PublicClientApplication } from '@azure/msal-browser'

let msalReady: Promise<void> | null = null
const scopes = ['api://eef58d6a-5358-4013-a7b0-c92bb0c4b6a8/api.read']
const redirectUri = new URL('/', window.location.origin).toString()

const msal = new PublicClientApplication({
  auth: {
    clientId: 'b1a5f44a-07f7-4780-9951-704bc4eef3c9',
    authority: 'https://login.microsoftonline.com/bf07dbcb-5446-466a-843b-35bbc7955e9d',
    redirectUri,
    postLogoutRedirectUri: redirectUri,
  },
  cache: { cacheLocation: 'localStorage' },
})

function ensureInitialized() {
  if (!msalReady) {
    msalReady = (async () => {
      await msal.initialize()
      await msal.handleRedirectPromise()
    })()
  }
  return msalReady
}

export async function login() {
  await ensureInitialized()
  let account = msal.getAllAccounts()[0]
  if (!account) {
    await msal.loginRedirect({ scopes, redirectStartPage: redirectUri })
  } else {
    const token = await msal.acquireTokenSilent({
      scopes,
      account,
    })

    return token.accessToken
  }
}

export async function initSession() {
  await ensureInitialized()
  const account = msal.getAllAccounts()[0]
  if (!account) return { accessToken: null as string | null, account: null as any }
  const token = await msal.acquireTokenSilent({ scopes, account })
  return { accessToken: token.accessToken, account }
}

export async function logout() {
  await ensureInitialized()
  await msal.logout()
}

export async function getUser() {
  await ensureInitialized()
  const account = msal.getAllAccounts()[0]
  return account
}

export async function getAccessToken() {
  await ensureInitialized()
  const account = msal.getAllAccounts()[0]
  if (!account) return null
  const { accessToken } = await msal.acquireTokenSilent({ scopes, account })
  return accessToken
}
