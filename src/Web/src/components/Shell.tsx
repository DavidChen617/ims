import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import {
  Boxes,
  Building2,
  ClipboardCheck,
  History,
  LogOut,
  Package,
  PackageCheck,
  PackageMinus,
  Users,
} from 'lucide-react'
import type { JwtProfile } from '../api/types'
import { roleDisplayName } from '../lib/role'

type NavItem = {
  path: string
  label: string
  icon: React.ComponentType<{ className?: string }>
  adminOnly?: boolean
  managerOnly?: boolean
  staffOnly?: boolean
}

const navItems: NavItem[] = [
  { path: '/products', label: '商品管理', icon: Package },
  { path: '/inbound/operations', label: '進貨作業', icon: PackageCheck, staffOnly: true },
  { path: '/outbound/operations', label: '出貨作業', icon: PackageMinus, staffOnly: true },
  { path: '/inbound/history', label: '入庫歷程', icon: History },
  { path: '/outbound/history', label: '出庫歷程', icon: ClipboardCheck },
  { path: '/stocks', label: '庫存狀況', icon: Boxes },
  { path: '/warehouses', label: '倉庫管理', icon: Building2, adminOnly: true },
  { path: '/users', label: '人員管理', icon: Users, managerOnly: true },
]

export function Shell({
  token,
  profile,
  signOut,
}: {
  token: string
  profile: JwtProfile
  signOut: () => void
}) {
  const location = useLocation()
  const navigate = useNavigate()

  const visibleNav = navItems.filter((item) => {
    if (item.adminOnly) {
      return profile.role === 'Admin'
    }

    if (item.managerOnly) {
      return profile.role === 'Admin' || profile.role === 'WarehouseAdmin'
    }

    if (item.staffOnly) {
      return profile.role === 'WarehouseAdmin' || profile.role === 'WarehouseUser'
    }

    return true
  })

  const currentLabel = navItems.find((item) => location.pathname.startsWith(item.path))?.label

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
            const active = location.pathname.startsWith(item.path)
            return (
              <button
                className={`flex w-full items-center gap-3 px-3 py-2 text-left text-sm font-medium ${
                  active ? 'bg-sky-50 text-sky-700' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-950'
                }`}
                key={item.path}
                onClick={() => navigate(item.path)}
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
            <div className="text-lg font-semibold">{currentLabel}</div>
            <div className="text-sm text-slate-500">
              {profile.name || profile.username} · {roleDisplayName(profile.role)}
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

        <main className="p-6">
          <Outlet context={{ token, profile }} />
        </main>
      </div>
    </div>
  )
}
