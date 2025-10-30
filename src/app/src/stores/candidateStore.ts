// src/stores/todos.ts
import { defineStore } from 'pinia'
import type { Candidate } from '../types/candidate'
import { getCandidates } from '../api/candidatesApi'

type Status = 'idle' | 'loading' | 'loaded' | 'error'

const TTL_MS = 60_000 // 1 minute

interface State {
  items: Candidate[]
  status: Status
  error: string | null
  lastLoadedAt: number | null
  isRefreshing: boolean // background refresh flag
}

export const useCandidatesStore = defineStore('candidates', {
  state: (): State => ({
    items: [],
    status: 'idle',
    error: null,
    lastLoadedAt: null,
    isRefreshing: false,
  }),

  getters: {
    hasData: (s) => s.items.length > 0,
    isStale: (s) => s.lastLoadedAt === null || Date.now() - s.lastLoadedAt > TTL_MS,
    isLoading: (s) => s.status === 'loading',
  },

  actions: {
    /** Foreground fetch: use when we have no data yet or user forces a reload. */
    async fetch(accessToken: string) {
      if (this.status === 'loading') return
      this.status = 'loading'
      this.error = null

      try {
        const data = await getCandidates(accessToken)
        this.items = data
        this.lastLoadedAt = Date.now()
        this.status = 'loaded'
      } catch (e) {
        this.error = e instanceof Error ? e.message : String(e)
        this.status = 'error'
      }
    },

    /** Background refresh that doesn’t blank the UI. */
    async refreshInBackground(accessToken: string) {
      if (this.isRefreshing) return
      this.isRefreshing = true
      try {
        const data = await getCandidates(accessToken)
        this.items = data
        this.lastLoadedAt = Date.now()
        if (this.status !== 'loaded') this.status = 'loaded'
      } catch (e) {
        // keep current items; surface error without nuking UI
        this.error = e instanceof Error ? e.message : String(e)
      } finally {
        this.isRefreshing = false
      }
    },

    /** Show-stale-data-while-revalidating. */
    async ensureFresh(
      accessToken: string,
      { onFocus = true, intervalMs }: { onFocus?: boolean; intervalMs?: number } = {},
    ) {
      // If no data yet, do a foreground fetch so the page has something to render.
      if (!this.hasData) {
        await this.fetch(accessToken)
      } else if (this.isStale) {
        // We have data—refresh silently.
        void this.refreshInBackground(accessToken)
      }

      // Optional: auto revalidate on tab focus.
      if (onFocus) {
        const handler = () => {
          if (document.visibilityState === 'visible' && this.isStale) {
            void this.refreshInBackground(accessToken)
          }
        }
        // Avoid duplicate listeners across hot reloads
        window.removeEventListener('visibilitychange', handler as any)
        window.addEventListener('visibilitychange', handler, { once: false })
      }

      // Optional: polling (use sparingly).
      if (intervalMs && intervalMs > 0) {
        // store a timer id on the store instance if you want to clear it later
        setInterval(() => {
          if (this.isStale) void this.refreshInBackground(accessToken)
        }, intervalMs)
      }
    },
  },
})
