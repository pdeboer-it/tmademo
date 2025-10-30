import { defineStore } from 'pinia'
import { getAccessToken as apiGetAccessToken, getUser, login, logout } from '../api/sessionApi'
import type { User } from '../types/user'

interface State {
  user: User | null
}

export const useSessionStore = defineStore('session', {
  state: (): State => ({
    user: null,
  }),

  getters: {
    isLoggedIn: (s) => s.user !== null,
  },

  actions: {
    async init() {
      const account = await getUser()
      this.user = account ? { name: account.name ?? '' } : null
    },
    async login() {
      await login()
    },
    async logout() {
      await logout()
      this.user = null
    },
    async getUser() {
      const user = await getUser()
      if (!user) return null
      return {
        name: user.name ?? '',
      }
    },
    async getAccessToken() {
      return await apiGetAccessToken()
    },
  },
})
