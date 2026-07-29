import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { api } from '../api/client'
import type { JwtProfile } from '../api/types'
import { DataState } from '../components/DataState'
import { EmptyRow } from '../components/EmptyRow'
import { Field } from '../components/Field'
import { FilterBar } from '../components/FilterBar'
import { MutationError } from '../components/MutationError'
import { Table } from '../components/Table'
import { number } from '../lib/format'

type OutletContext = { token: string; profile: JwtProfile }

export function WarehousesPage() {
  const { token } = useOutletContext<OutletContext>()
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [filter, setFilter] = useState<{
    name?: string
    warehouseAdminName?: string
    staffCountMin?: string
    staffCountMax?: string
  }>({})
  const query = useQuery({
    queryKey: ['warehouses', filter],
    queryFn: () =>
      api.warehouses(token, {
        name: filter.name,
        warehouseAdminName: filter.warehouseAdminName,
        staffCountMin: filter.staffCountMin ? Number(filter.staffCountMin) : undefined,
        staffCountMax: filter.staffCountMax ? Number(filter.staffCountMax) : undefined,
      }),
  })
  const createWarehouse = useMutation({
    mutationFn: () => api.createWarehouse(token, { name }),
    onSuccess: async () => {
      setName('')
      await queryClient.invalidateQueries({ queryKey: ['warehouses'] })
    },
  })

  const items = query.data?.items ?? []

  return (
    <div className="space-y-4">
      <form
        className="grid gap-3 border border-slate-200 bg-white p-4 sm:grid-cols-[1fr_auto]"
        onSubmit={(event) => {
          event.preventDefault()
          createWarehouse.mutate()
        }}
      >
        <Field label="倉庫名稱">
          <input
            className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
            onChange={(event) => setName(event.target.value)}
            required
            value={name}
          />
        </Field>
        <button
          className="inline-flex h-10 items-center justify-center gap-2 self-end bg-sky-600 px-4 text-sm font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
          disabled={createWarehouse.isPending}
          type="submit"
        >
          <Plus className="h-4 w-4" />
          新增倉庫
        </button>
        <MutationError error={createWarehouse.error} />
      </form>

      <FilterBar
        fields={[
          { key: 'name', label: '倉庫名稱' },
          { key: 'warehouseAdminName', label: '管理者' },
          { key: 'staffCountMin', label: '員工數(最少)', type: 'number' },
          { key: 'staffCountMax', label: '員工數(最多)', type: 'number' },
        ]}
        onSearch={setFilter}
      />

      <DataState error={query.error} isLoading={query.isLoading}>
        <Table columns={['倉庫', '管理者', '員工數']}>
          {items.length === 0 ? (
            <EmptyRow colSpan={3} />
          ) : (
            items.map((warehouse) => (
              <tr className="hover:bg-slate-50" key={warehouse.id}>
                <td className="whitespace-nowrap px-4 py-3">
                  <div className="font-medium">{warehouse.name}</div>
                  <div className="font-mono text-xs text-slate-500">{warehouse.id}</div>
                </td>
                <td className="whitespace-nowrap px-4 py-3">{warehouse.warehouseAdminName ?? '-'}</td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(warehouse.staffCount)}</td>
              </tr>
            ))
          )}
        </Table>
      </DataState>
    </div>
  )
}
