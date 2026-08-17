import { PublicClientApplication, type AccountInfo } from '@azure/msal-browser'
import {
  apiFetch,
  setAuthSource,
  setStoredToken,
  type AuthUser,
} from '../api/client'

const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID ?? ''
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID ?? ''
const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE ?? ''

let msalInstance: PublicClientApplication | null = null

export function isEntraConfigured(): boolean {
  return Boolean(clientId && tenantId && apiScope)
}

function getMsal(): PublicClientApplication | null {
  if (!isEntraConfigured()) {
    return null
  }

  if (!msalInstance) {
    msalInstance = new PublicClientApplication({
      auth: {
        clientId,
        authority: `https://login.microsoftonline.com/${tenantId}`,
        redirectUri: window.location.origin,
      },
      cache: {
        cacheLocation: 'localStorage',
      },
    })
  }

  return msalInstance
}

async function acquireAccessToken(
  msal: PublicClientApplication,
  account: AccountInfo,
): Promise<string> {
  try {
    const silent = await msal.acquireTokenSilent({
      account,
      scopes: [apiScope],
    })
    return silent.accessToken
  } catch {
    const popup = await msal.acquireTokenPopup({
      account,
      scopes: [apiScope],
    })
    return popup.accessToken
  }
}

type MeResponse = {
  userId: number
  email: string
  role: string
  firstName: string
  lastName: string
}

export async function signInWithMicrosoft(): Promise<AuthUser> {
  const msal = getMsal()
  if (!msal) {
    throw new Error('Microsoft sign-in is not configured.')
  }

  await msal.initialize()
  const login = await msal.loginPopup({ scopes: [apiScope] })
  const account = login.account
  if (!account) {
    throw new Error('Microsoft sign-in did not return an account.')
  }

  const accessToken = await acquireAccessToken(msal, account)
  setStoredToken(accessToken)
  setAuthSource('entra')

  const profile = await apiFetch<MeResponse>('/api/auth/me')
  return {
    token: accessToken,
    userId: profile.userId,
    email: profile.email,
    role: profile.role,
    firstName: profile.firstName,
    lastName: profile.lastName,
  }
}

export async function signOutMicrosoft(): Promise<void> {
  const msal = getMsal()
  if (!msal) {
    return
  }

  await msal.initialize()
  const account = msal.getAllAccounts()[0]
  if (account) {
    await msal.logoutPopup({
      account,
      postLogoutRedirectUri: window.location.origin,
    })
  }
}
