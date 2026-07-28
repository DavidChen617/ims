import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Boxes,
  Building2,
  ClipboardCheck,
  History,
  LogOut,
  Package,
  ShieldCheck,
  Users,
  Warehouse,
} from 'lucide-react'
import { api, decodeProfile } from './api/client'
import type { JwtProfile } from './api/types'
import { DataState } from './components/DataState'
import { StatusBadge } from './components/StatusBadge'
import { Table } from './components/Table'
import { dateTime, number } from './lib/format'

type ViewKey = 'products' | 'inboundHistory' | 'outboundHistory' | 'stocks' | 'warehouses' | 'users'

const navItems = [
  { key: 'products', label: '商品管理', icon: Package },
  { key: 'inboundHistory', label: '入庫歷程', icon: History },
  { key: 'outboundHistory', label: '出庫歷程', icon: ClipboardCheck },
  { key: 'stocks', label: '庫存狀況', icon: Boxes },
  { key: 'warehouses', label: '倉庫管理', icon: Building2, adminOnly: true },
  { key: 'users', label: '人員管理', icon: Users, managerOnly: true },
] satisfies Array<{
  key: ViewKey
  label: string
  icon: React.ComponentType<{ className?: string }>
  adminOnly?: boolean
  managerOnly?: boolean
}>

function useSession() {
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

  return { token, profile, signIn, signOut }
}

function LoginScreen({ onLogin }: { onLogin: (token: string, refreshToken: string) => void }) {
  const [username, setUsername] = useState('admin')
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
          <p className="mt-4 max-w-2xl text-base leading-7 text-slate-600">
            前端透過 nginx prefix 呼叫 Organization、Ordering、Inventory 三組 API；本機 Vite dev server 已先配置對應 proxy。
          </p>
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

function Shell({
  profile,
  signOut,
  view,
  setView,
  children,
}: {
  profile: JwtProfile
  signOut: () => void
  view: ViewKey
  setView: (view: ViewKey) => void
  children: React.ReactNode
}) {
  const visibleNav = navItems.filter((item) => {
    if (item.adminOnly) {
      return profile.role === 'Admin'
    }

    if (item.managerOnly) {
      return profile.role === 'Admin' || profile.role === 'WarehouseAdmin'
    }

    return true
  })

  return (
    <div className="grid min-h-screen bg-slate-100 text-slate-950 lg:grid-cols-[248px_1fr]">
      <aside className="border-r border-slate-200 bg-white">
        <div className="border-b border-slate-200 p-5">
          <div className="flex items-center gap-3">
            <div className="grid h-9 w-9 place-items-center bg-sky-600 text-sm font-bold text-white">IMS</div>
            <div>
              <div className="text-sm font-semibold">倉儲控制台</div>
              <div className="text-xs text-slate-500">Microservices UI</div>
            </div>
          </div>
        </div>

        <nav className="space-y-1 p-3">
          {visibleNav.map((item) => {
            const Icon = item.icon
            return (
              <button
                className={`flex w-full items-center gap-3 px-3 py-2 text-left text-sm font-medium ${
                  view === item.key ? 'bg-sky-50 text-sky-700' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-950'
                }`}
                key={item.key}
                onClick={() => setView(item.key)}
                type="button"
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </button>
            )
          })}
        </nav>
      </aside>

      <div className="min-w-0">
        <header className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-200 bg-white px-6 py-4">
          <div>
            <div className="text-lg font-semibold">{navItems.find((item) => item.key === view)?.label}</div>
            <div className="text-sm text-slate-500">
              {profile.name || profile.username} · {profile.role}
              {profile.warehouseName ? ` · ${profile.warehouseName}` : ' · 全倉'}
            </div>
          </div>
          <button
            className="inline-flex items-center gap-2 border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
            onClick={signOut}
            type="button"
          >
            <LogOut className="h-4 w-4" />
            登出
          </button>
        </header>

        <main className="p-6">{children}</main>
      </div>
    </div>
  )
}

function ProductsView({ token }: { token: string }) {
  const query = useQuery({ queryKey: ['products'], queryFn: () => api.products(token, { page: 1, size: 50 }) })

  return (
    <DataState error={query.error} isLoading={query.isLoading}>
      <Table columns={['商品編號', '名稱', '單位', '價格']}>
        {(query.data?.items ?? []).map((product) => (
          <tr className="hover:bg-slate-50" key={product.id}>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{product.productNo}</td>
            <td className="whitespace-nowrap px-4 py-3 font-medium">{product.name}</td>
            <td className="whitespace-nowrap px-4 py-3">{product.unit}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(product.price)}</td>
          </tr>
        ))}
      </Table>
    </DataState>
  )
}

function InboundHistoryView({ token, profile }: { token: string; profile: JwtProfile }) {
  const query = useQuery({
    queryKey: ['inbound-history', profile.role, profile.warehouseId],
    queryFn: () =>
      profile.role === 'Admin'
        ? api.inboundHistory(token, { warehouseId: null, page: 1, size: 50 })
        : api.inboundWarehouseHistory(token, { page: 1, size: 50 }),
  })

  return (
    <DataState error={query.error} isLoading={query.isLoading}>
      <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Summary label="總數量" value={number(query.data?.totalQuantity)} />
        <Summary label="總金額" value={number(query.data?.totalAmount)} />
        <Summary label="筆數" value={number(query.data?.page.totalCount)} />
      </div>
      <Table columns={['單號', '商品編號', '商品名稱', '數量', '單價', '總額']}>
        {(query.data?.page.items ?? []).map((line) => (
          <tr className="hover:bg-slate-50" key={`${line.orderNo}-${line.productId}`}>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{line.orderNo}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{line.productNo}</td>
            <td className="whitespace-nowrap px-4 py-3 font-medium">{line.productName}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(line.quantity)}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(line.unitPrice)}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(line.totalAmount)}</td>
          </tr>
        ))}
      </Table>
    </DataState>
  )
}

function OutboundHistoryView({ token, profile }: { token: string; profile: JwtProfile }) {
  const query = useQuery({
    queryKey: ['outbound-history', profile.role, profile.warehouseId],
    queryFn: () =>
      profile.role === 'Admin' ? api.outboundHistory(token, { warehouseId: null }) : api.outboundWarehouseHistory(token),
  })

  return (
    <DataState error={query.error} isLoading={query.isLoading}>
      <Table columns={['單號', '倉庫', '申請人', '審核人', '申請時間', '完成時間', '狀態']}>
        {(query.data?.items ?? []).map((order) => (
          <tr className="hover:bg-slate-50" key={order.id}>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{order.orderNo}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{order.warehouseId}</td>
            <td className="whitespace-nowrap px-4 py-3">{order.requestedByName}</td>
            <td className="whitespace-nowrap px-4 py-3">{order.confirmedByName ?? '-'}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{dateTime(order.requestedAt)}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{dateTime(order.confirmedAt)}</td>
            <td className="whitespace-nowrap px-4 py-3">
              <StatusBadge value={order.status} />
            </td>
          </tr>
        ))}
      </Table>
    </DataState>
  )
}

function StocksView({ token, profile }: { token: string; profile: JwtProfile }) {
  const query = useQuery({
    queryKey: ['stocks', profile.role, profile.warehouseId],
    queryFn: () =>
      profile.role === 'Admin'
        ? api.stocks(token, { warehouseId: null, page: 1, size: 50 })
        : api.warehouseStocks(token, { page: 1, size: 50 }),
  })

  return (
    <DataState error={query.error} isLoading={query.isLoading}>
      <Table columns={['商品編號', '商品名稱', '單位', '倉庫', '目前庫存', '累計已出庫']}>
        {(query.data?.items ?? []).map((stock) => (
          <tr className="hover:bg-slate-50" key={`${stock.warehouseId}-${stock.productId}`}>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{stock.productNo}</td>
            <td className="whitespace-nowrap px-4 py-3 font-medium">{stock.productName}</td>
            <td className="whitespace-nowrap px-4 py-3">{stock.unit}</td>
            <td className="whitespace-nowrap px-4 py-3">{stock.warehouseName ?? stock.warehouseId}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(stock.quantity)}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(stock.cumulativeShipped)}</td>
          </tr>
        ))}
      </Table>
    </DataState>
  )
}

function WarehousesView({ token }: { token: string }) {
  const query = useQuery({ queryKey: ['warehouses'], queryFn: () => api.warehouses(token) })

  return (
    <DataState error={query.error} isLoading={query.isLoading}>
      <Table columns={['倉庫', '管理者', '員工數']}>
        {(query.data?.items ?? []).map((warehouse) => (
          <tr className="hover:bg-slate-50" key={warehouse.id}>
            <td className="whitespace-nowrap px-4 py-3">
              <div className="font-medium">{warehouse.name}</div>
              <div className="font-mono text-xs text-slate-500">{warehouse.id}</div>
            </td>
            <td className="whitespace-nowrap px-4 py-3">{warehouse.warehouseAdminName ?? '-'}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(warehouse.staffCount)}</td>
          </tr>
        ))}
      </Table>
    </DataState>
  )
}

function UsersView({ token }: { token: string }) {
  const query = useQuery({ queryKey: ['users'], queryFn: () => api.users(token) })

  return (
    <DataState error={query.error} isLoading={query.isLoading}>
      <Table columns={['姓名', '帳號', '角色', '倉庫', '建立時間']}>
        {(query.data?.items ?? []).map((user) => (
          <tr className="hover:bg-slate-50" key={user.id}>
            <td className="whitespace-nowrap px-4 py-3 font-medium">{user.name}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{user.username}</td>
            <td className="whitespace-nowrap px-4 py-3">{String(user.role)}</td>
            <td className="whitespace-nowrap px-4 py-3">{user.warehouseName ?? '-'}</td>
            <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{dateTime(user.createdAt)}</td>
          </tr>
        ))}
      </Table>
    </DataState>
  )
}

function Summary({ label, value }: { label: string; value: string }) {
  return (
    <div className="border border-slate-200 bg-white p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</div>
      <div className="mt-2 font-mono text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}

export default function App() {
  const session = useSession()
  const [view, setView] = useState<ViewKey>('products')
  const queryClient = useQueryClient()

  if (!session.token || !session.profile) {
    return (
      <LoginScreen
        onLogin={(token, refreshToken) => {
          session.signIn(token, refreshToken)
          queryClient.clear()
        }}
      />
    )
  }

  return (
    <Shell
      profile={session.profile}
      setView={setView}
      signOut={() => {
        session.signOut()
        queryClient.clear()
      }}
      view={view}
    >
      {view === 'products' ? <ProductsView token={session.token} /> : null}
      {view === 'inboundHistory' ? <InboundHistoryView profile={session.profile} token={session.token} /> : null}
      {view === 'outboundHistory' ? <OutboundHistoryView profile={session.profile} token={session.token} /> : null}
      {view === 'stocks' ? <StocksView profile={session.profile} token={session.token} /> : null}
      {view === 'warehouses' ? <WarehousesView token={session.token} /> : null}
      {view === 'users' ? <UsersView token={session.token} /> : null}
    </Shell>
  )
}
