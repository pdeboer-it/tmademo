<script setup lang="ts">
import { useSessionStore } from '../stores/sessionStore'
import { useCandidatesStore } from '../stores/candidateStore'
import { onMounted } from 'vue'

const session = useSessionStore()
const candidates = useCandidatesStore()

onMounted(async () => {
  if (!session.isLoggedIn) {
    await session.login()
    return
  }

  const token = await session.getAccessToken()
  if (!token) return
  await candidates.ensureFresh(token)
})
</script>

<template>
  <h1>Candidates</h1>
  <router-link to="/">Home</router-link>
  <div v-if="candidates.isLoading">Loading...</div>
  <div v-else-if="candidates.error">Error: {{ candidates.error }}</div>
  <div v-else>
    <div v-for="candidate in candidates.items" :key="candidate.id">
      {{ candidate.name }}
    </div>
  </div>
</template>

<style scoped></style>
