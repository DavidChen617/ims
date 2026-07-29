import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { api } from '../api/client'
import type { JwtProfile } from '../api/types'
import { DataState } from '../components/DataState'
import { EmptyRow } from '../components/EmptyRow'
import { FilterBar } from '../components/FilterBar'
import { Pagination } from '../components/Pagination'
import { Summary } from '../components/Summary'
import { Table } from '../components/Table'
import { number } from '../lib/format'

type OutletContext = { token: string; profile: JwtProfile }

const PAGE_SIZE = 20

export function InboundHistoryPage() {
  const { token, profile } = useOutletContext<OutletContext>()
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState<{
    orderNo?: string
    productNo?: string
    productName?: string
    quantityMin?: string
    quantityMax?: string
    unitPriceMin?: string
    unitPriceMax?: string
    amountMin?: string
    amountMax?: string
  }>({})

  const historyParams = {
    orderNo: filter.orderNo,
    productNo: filter.productNo,
    productName: filter.productName,
    quantityMin: filter.quantityMin ? Number(filter.quantityMin) : undefined,
    quantityMax: filter.quantityMax ? Number(filter.quantityMax) : undefined,
    unitPriceMin: filter.unitPriceMin ? Number(filter.unitPriceMin) : undefined,
    unitPriceMax: filter.unitPriceMax ? Number(filter.unitPriceMax) : undefined,
    amountMin: filter.amountMin ? Number(filter.amountMin) : undefined,
    amountMax: filter.amountMax ? Number(filter.amountMax) : undefined,
    page,
    size: PAGE_SIZE,
  }

  const query = useQuery({
    queryKey: ['inbound-history', profile.role, profile.warehouseId, filter, page],
    queryFn: () =>
      profile.role === 'Admin'
        ? api.inboundHistory(token, { warehouseId: null, ...historyParams })
        : api.inboundWarehouseHistory(token, historyParams),
  })

  const lines = query.data?.page.items ?? []

  return (
    <div className="space-y-4">
      <FilterBar
        fields={[
          { key: 'orderNo', label: '單號' },
          { key: 'productNo', label: '商品編號' },
          { key: 'productName', label: '商品名稱' },
          { key: 'quantityMin', label: '數量(最少)', type: 'number' },
          { key: 'quantityMax', label: '數量(最多)', type: 'number' },
          { key: 'unitPriceMin', label: '單價(最低)', type: 'number' },
          { key: 'unitPriceMax', label: '單價(最高)', type: 'number' },
          { key: 'amountMin', label: '總額(最低)', type: 'number' },
          { key: 'amountMax', label: '總額(最高)', type: 'number' },
        ]}
        onSearch={(values) => {
          setFilter(values)
          setPage(1)
        }}
      />
      <DataState error={query.error} isLoading={query.isLoading}>
      <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Summary label="總數量" value={number(query.data?.totalQuantity)} />
        <Summary label="總金額" value={number(query.data?.totalAmount)} />
        <Summary label="筆數" value={number(query.data?.page.totalCount)} />
      </div>
      <Table columns={['單號', '商品編號', '商品名稱', '數量', '單價', '總額']}>
        {lines.length === 0 ? (
          <EmptyRow colSpan={6} />
        ) : (
          lines.map((line) => (
            <tr className="hover:bg-slate-50" key={`${line.orderNo}-${line.productId}`}>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{line.orderNo}</td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{line.productNo}</td>
              <td className="whitespace-nowrap px-4 py-3 font-medium">{line.productName}</td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(line.quantity)}</td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(line.unitPrice)}</td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(line.totalAmount)}</td>
            </tr>
          ))
        )}
      </Table>
      <Pagination
        onPageChange={setPage}
        page={query.data?.page.page ?? page}
        size={query.data?.page.size ?? PAGE_SIZE}
        totalCount={query.data?.page.totalCount ?? 0}
      />
      </DataState>
    </div>
  )
}
