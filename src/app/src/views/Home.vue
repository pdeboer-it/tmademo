<script setup lang="ts">
import { useSessionStore } from '../stores/sessionStore'
import { onMounted } from 'vue'

const session = useSessionStore()

onMounted(async () => {
  if (!session.isLoggedIn) {
    await session.login()
    return
  }

  const user = await session.getUser()
  console.log(user)
})
</script>

<template>
  <h1>Home</h1>
  <router-link to="/candidates">Candidates</router-link>
</template>

<style scoped></style>
