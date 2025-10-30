import type { Candidate } from '../types/candidate'
import { baseUrl } from '../utils/env'

export async function getCandidates(accessToken: string): Promise<Candidate[]> {
  const url = baseUrl()

  const res = await fetch(`${url}/api/candidates`, {
    headers: { Accept: 'application/json', Authorization: `Bearer ${accessToken}` },
  })

  if (!res.ok) throw new Error(`Failed to load candidates: ${res.status}`)

  return (await res.json()) as Candidate[]
}
