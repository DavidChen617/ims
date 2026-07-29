import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { ShieldCheck, Warehouse } from 'lucide-react'
import { api } from '../api/client'

export function LoginPage({ onLogin }: { onLogin: (token: string, refreshToken: string) => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')

  const login = useMutation({
    mutationFn: () => api.login({ username, password }),
    onSuccess: (result) => onLogin(result.accessToken, result.refreshToken),
  })

  return (
    <main className="min-h-screen bg-slate-100 p-6 text-slate-950">
      <section className="mx-auto grid min-h-[calc(100vh-3rem)] max-w-5xl content-center gap-8 lg:grid-cols-[1fr_380px]">
        <div className="flex flex-col justify-center">
          <div className="mb-5 inline-flex w-fit items-center gap-2 border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-600">
            <Warehouse className="h-4 w-4 text-sky-600" />
            IMS Warehouse Console
          </div>
          <h1 className="max-w-xl text-4xl font-semibold tracking-tight text-slate-950">倉儲後台工作台</h1>
        </div>

        <form
          className="border border-slate-200 bg-white p-6 shadow-sm"
          onSubmit={(event) => {
            event.preventDefault()
            login.mutate()
          }}
        >
          <div className="mb-5">
            <h2 className="text-lg font-semibold text-slate-950">登入</h2>
            <p className="mt-1 text-sm text-slate-500">使用 Organization API 取得 JWT。</p>
          </div>
          <label className="mb-4 block">
            <span className="mb-1 block text-sm font-medium text-slate-700">帳號</span>
            <input
              className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              onChange={(event) => setUsername(event.target.value)}
              value={username}
            />
          </label>
          <label className="mb-5 block">
            <span className="mb-1 block text-sm font-medium text-slate-700">密碼</span>
            <input
              className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              value={password}
            />
          </label>
          {login.error ? (
            <div className="mb-4 border border-red-200 bg-red-50 p-3 text-sm text-red-700">{login.error.message}</div>
          ) : null}
          <button
            className="inline-flex w-full items-center justify-center gap-2 bg-sky-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
            disabled={login.isPending}
            type="submit"
          >
            <ShieldCheck className="h-4 w-4" />
            {login.isPending ? '登入中' : '登入系統'}
          </button>
        </form>
      </section>
    </main>
  )
}
