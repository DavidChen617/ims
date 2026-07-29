import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { api } from '../api/client'
import type { JwtProfile } from '../api/types'
import { DataState } from '../components/DataState'
import { EmptyRow } from '../components/EmptyRow'
import { FilterBar } from '../components/FilterBar'
import { Pagination } from '../components/Pagination'
import { Table } from '../components/Table'
import { number } from '../lib/format'

type OutletContext = { token: string; profile: JwtProfile }

const PAGE_SIZE = 20

export function StocksPage() {
  const { token, profile } = useOutletContext<OutletContext>()
  const isAdmin = profile.role === 'Admin'
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState<{
    productNo?: string
    productName?: string
    unit?: string
    warehouseId?: string
    quantityMin?: string
    quantityMax?: string
    cumulativeShippedMin?: string
    cumulativeShippedMax?: string
  }>({})

  const stockParams = {
    productNo: filter.productNo,
    productName: filter.productName,
    unit: filter.unit,
    quantityMin: filter.quantityMin ? Number(filter.quantityMin) : undefined,
    quantityMax: filter.quantityMax ? Number(filter.quantityMax) : undefined,
    cumulativeShippedMin: filter.cumulativeShippedMin ? Number(filter.cumulativeShippedMin) : undefined,
    cumulativeShippedMax: filter.cumulativeShippedMax ? Number(filter.cumulativeShippedMax) : undefined,
    page,
    size: PAGE_SIZE,
  }

  const query = useQuery({
    queryKey: ['stocks', profile.role, profile.warehouseId, filter, page],
    queryFn: () =>
      isAdmin
        ? api.stocks(token, { warehouseId: filter.warehouseId ?? null, ...stockParams })
        : api.warehouseStocks(token, stockParams),
  })

  const units = useQuery({ queryKey: ['product-units'], queryFn: () => api.productUnits(token) })

  const warehouses = useQuery({
    enabled: isAdmin,
    queryKey: ['warehouses'],
    queryFn: () => api.warehouses(token),
  })

  const items = query.data?.items ?? []

  return (
    <div className="space-y-4">
      <FilterBar
        fields={[
          { key: 'productNo', label: '商品編號' },
          { key: 'productName', label: '商品名稱' },
          {
            key: 'unit',
            label: '單位',
            type: 'select',
            options: (units.data?.items ?? []).map((item) => ({ value: item.name, label: item.name })),
          },
          ...(isAdmin
            ? [
                {
                  key: 'warehouseId',
                  label: '倉庫',
                  type: 'select' as const,
                  options: (warehouses.data?.items ?? []).map((w) => ({ value: w.id, label: w.name })),
                },
              ]
            : []),
          { key: 'quantityMin', label: '目前庫存(最少)', type: 'number' },
          { key: 'quantityMax', label: '目前庫存(最多)', type: 'number' },
          { key: 'cumulativeShippedMin', label: '累計已出庫(最少)', type: 'number' },
          { key: 'cumulativeShippedMax', label: '累計已出庫(最多)', type: 'number' },
        ]}
        onSearch={(values) => {
          setFilter(values)
          setPage(1)
        }}
      />
      <DataState error={query.error} isLoading={query.isLoading}>
        <Table columns={['商品編號', '商品名稱', '單位', '倉庫', '目前庫存', '累計已出庫']}>
          {items.length === 0 ? (
            <EmptyRow colSpan={6} />
          ) : (
            items.map((stock) => (
              <tr className="hover:bg-slate-50" key={`${stock.warehouseId}-${stock.productId}`}>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{stock.productNo}</td>
                <td className="whitespace-nowrap px-4 py-3 font-medium">{stock.productName}</td>
                <td className="whitespace-nowrap px-4 py-3">{stock.unit}</td>
                <td className="whitespace-nowrap px-4 py-3">{stock.warehouseName ?? stock.warehouseId}</td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(stock.quantity)}</td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(stock.cumulativeShipped)}</td>
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
