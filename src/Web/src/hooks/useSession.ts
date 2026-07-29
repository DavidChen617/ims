import { useEffect, useMemo, useState } from 'react'
import { decodeProfile, setUnauthorizedHandler } from '../api/client'

export function useSession() {
  const [token, setToken] = useState(() => localStorage.getItem('ims.accessToken') ?? '')
  const profile = useMemo(() => (token ? decodeProfile(token) : null), [token])

  function signIn(nextToken: string, nextRefreshToken: string) {
    localStorage.setItem('ims.accessToken', nextToken)
    localStorage.setItem('ims.refreshToken', nextRefreshToken)
    setToken(nextToken)
  }

  function signOut() {
    localStorage.removeItem('ims.accessToken')
    localStorage.removeItem('ims.refreshToken')
    setToken('')
  }

  // access token 過期後,任何一支 API 都會回 401——這裡讓 client.ts 在遇到 401 時
  // 直接呼叫 signOut(),而不是讓每個畫面各自顯示一個永遠不會消失的錯誤訊息。
  useEffect(() => {
    setUnauthorizedHandler(signOut)
    return () => setUnauthorizedHandler(null)
  }, [])

  return { token, profile, signIn, signOut }
}
