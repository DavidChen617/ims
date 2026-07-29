import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { api } from '../api/client'
import type { JwtProfile } from '../api/types'
import { DataState } from '../components/DataState'
import { EmptyRow } from '../components/EmptyRow'
import { Field } from '../components/Field'
import { MutationError } from '../components/MutationError'
import { FilterBar } from '../components/FilterBar'
import { Pagination } from '../components/Pagination'
import { Table } from '../components/Table'
import { dateTime } from '../lib/format'
import { roleDisplayName } from '../lib/role'

type OutletContext = { token: string; profile: JwtProfile }

const PAGE_SIZE = 20

export function UsersPage() {
  const { token, profile } = useOutletContext<OutletContext>()
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState<{
    name?: string
    username?: string
    role?: string
    warehouseName?: string
    createdFrom?: string
    createdTo?: string
  }>({})
  const [warehouseId, setWarehouseId] = useState('')
  const [name, setName] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'1' | '2'>('2')
  const query = useQuery({
    queryKey: ['users', filter, page],
    queryFn: () =>
      api.users(token, {
        name: filter.name,
        username: filter.username,
        role: filter.role,
        warehouseName: filter.warehouseName,
        createdFrom: filter.createdFrom,
        createdTo: filter.createdTo,
        page,
        size: PAGE_SIZE,
      }),
  })
  const warehouses = useQuery({
    enabled: profile.role === 'Admin',
    queryKey: ['warehouses'],
    queryFn: () => api.warehouses(token),
  })
  const registerUser = useMutation({
    mutationFn: () =>
      profile.role === 'Admin'
        ? api.registerUserByAdmin(token, {
            warehouseId,
            name,
            username,
            password,
            role: Number(role) as 1 | 2,
          })
        : api.registerWarehouseUser(token, {
            name,
            username,
            password,
          }),
    onSuccess: async () => {
      setWarehouseId('')
      setName('')
      setUsername('')
      setPassword('')
      setRole('2')
      await queryClient.invalidateQueries({ queryKey: ['users'] })
      await queryClient.invalidateQueries({ queryKey: ['warehouses'] })
    },
  })

  const items = query.data?.items ?? []

  return (
    <div className="space-y-4">
      <form
        className="grid gap-3 border border-slate-200 bg-white p-4 lg:grid-cols-5"
        onSubmit={(event) => {
          event.preventDefault()
          registerUser.mutate()
        }}
      >
        {profile.role === 'Admin' ? (
          <Field label="倉庫">
            <select
              className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              onChange={(event) => setWarehouseId(event.target.value)}
              required
              value={warehouseId}
            >
              <option value="">選擇倉庫</option>
              {(warehouses.data?.items ?? []).map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.name}
                </option>
              ))}
            </select>
          </Field>
        ) : null}
        <Field label="姓名">
          <input
            className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
            onChange={(event) => setName(event.target.value)}
            required
            value={name}
          />
        </Field>
        <Field label="帳號">
          <input
            className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
            onChange={(event) => setUsername(event.target.value)}
            required
            value={username}
          />
        </Field>
        <Field label="密碼">
          <input
            className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
            minLength={8}
            onChange={(event) => setPassword(event.target.value)}
            required
            type="password"
            value={password}
          />
        </Field>
        {profile.role === 'Admin' ? (
          <Field label="角色">
            <select
              className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              onChange={(event) => setRole(event.target.value as '1' | '2')}
              value={role}
            >
              <option value="2">一般員工</option>
              <option value="1">倉庫管理者</option>
            </select>
          </Field>
        ) : null}
        <button
          className="inline-flex h-10 items-center justify-center gap-2 self-end bg-sky-600 px-4 text-sm font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
          disabled={registerUser.isPending}
          type="submit"
        >
          <Plus className="h-4 w-4" />
          新增人員
        </button>
        <MutationError error={registerUser.error} />
      </form>

      <FilterBar
        fields={[
          { key: 'name', label: '姓名' },
          { key: 'username', label: '帳號' },
          {
            key: 'role',
            label: '角色',
            type: 'select',
            options: [
              { value: 'Admin', label: roleDisplayName('Admin') },
              { value: 'WarehouseAdmin', label: roleDisplayName('WarehouseAdmin') },
              { value: 'WarehouseUser', label: roleDisplayName('WarehouseUser') },
            ],
          },
          ...(profile.role === 'Admin' ? [{ key: 'warehouseName', label: '倉庫' }] : []),
          { key: 'createdFrom', label: '建立時間(起)', type: 'date' as const },
          { key: 'createdTo', label: '建立時間(迄)', type: 'date' as const },
        ]}
        onSearch={(values) => {
          setFilter(values)
          setPage(1)
        }}
      />

      <DataState error={query.error} isLoading={query.isLoading}>
        <Table columns={['姓名', '帳號', '角色', '倉庫', '建立時間']}>
          {items.length === 0 ? (
            <EmptyRow colSpan={5} />
          ) : (
            items.map((user) => (
              <tr className="hover:bg-slate-50" key={user.id}>
                <td className="whitespace-nowrap px-4 py-3 font-medium">{user.name}</td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{user.username}</td>
                <td className="whitespace-nowrap px-4 py-3">{roleDisplayName(user.role)}</td>
                <td className="whitespace-nowrap px-4 py-3">{user.warehouseName ?? '-'}</td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{dateTime(user.createdAt)}</td>
              </tr>
            ))
          )}
        </Table>
        <Pagination
          onPageChange={setPage}
          page={query.data?.page ?? page}
          size={query.data?.size ?? PAGE_SIZE}
          totalCount={query.data?.totalCount ?? 0}
        />
      </DataState>
    </div>
  )
}
