import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { api } from '../api/client'
import type { JwtProfile } from '../api/types'
import { DataState } from '../components/DataState'
import { EmptyRow } from '../components/EmptyRow'
import { FilterBar } from '../components/FilterBar'
import { Pagination } from '../components/Pagination'
import { StatusBadge } from '../components/StatusBadge'
import { Table } from '../components/Table'
import { dateTime } from '../lib/format'

type OutletContext = { token: string; profile: JwtProfile }

const PAGE_SIZE = 20

export function OutboundHistoryPage() {
  const { token, profile } = useOutletContext<OutletContext>()
  const isAdmin = profile.role === 'Admin'
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState<{
    orderNo?: string
    warehouseId?: string
    status?: string
    requestedFrom?: string
    requestedTo?: string
    completedFrom?: string
    completedTo?: string
    productNo?: string
    productName?: string
    requestedByName?: string
    confirmedByName?: string
  }>({})

  const historyParams = {
    orderNo: filter.orderNo,
    status: filter.status,
    requestedFrom: filter.requestedFrom,
    requestedTo: filter.requestedTo,
    completedFrom: filter.completedFrom,
    completedTo: filter.completedTo,
    productNo: filter.productNo,
    productName: filter.productName,
    requestedByName: filter.requestedByName,
    confirmedByName: filter.confirmedByName,
    page,
    size: PAGE_SIZE,
  }

  const query = useQuery({
    queryKey: ['outbound-history', profile.role, profile.warehouseId, filter, page],
    queryFn: () =>
      isAdmin
        ? api.outboundHistory(token, { warehouseId: filter.warehouseId ?? null, ...historyParams })
        : api.outboundWarehouseHistory(token, historyParams),
  })

  // 只有 Admin 的合併歷程會混到多個倉庫,才需要另外查倉庫清單把 id 轉成名稱、當篩選下拉選單；
  // 一般倉庫人員本來就只看得到自己那個倉庫,直接用 JWT 裡的 warehouseName 即可。
  const warehouses = useQuery({
    enabled: isAdmin,
    queryKey: ['warehouses'],
    queryFn: () => api.warehouses(token),
  })

  const warehouseNames = useMemo(
    () => new Map((warehouses.data?.items ?? []).map((warehouse) => [warehouse.id, warehouse.name])),
    [warehouses.data],
  )

  const items = query.data?.items ?? []

  return (
    <div className="space-y-4">
      <FilterBar
        fields={[
          { key: 'orderNo', label: '單號' },
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
          { key: 'requestedByName', label: '申請人' },
          { key: 'confirmedByName', label: '審核人' },
          { key: 'requestedFrom', label: '申請時間(起)', type: 'date' as const },
          { key: 'requestedTo', label: '申請時間(迄)', type: 'date' as const },
          { key: 'completedFrom', label: '完成時間(起)', type: 'date' as const },
          { key: 'completedTo', label: '完成時間(迄)', type: 'date' as const },
          {
            key: 'status',
            label: '狀態',
            type: 'select' as const,
            options: [
              { value: 'Confirmed', label: '已確認' },
              { value: 'Rejected', label: '已拒絕' },
            ],
          },
          { key: 'productNo', label: '商品編號' },
          { key: 'productName', label: '商品名稱' },
        ]}
        onSearch={(values) => {
          setFilter(values)
          setPage(1)
        }}
      />
      <DataState error={query.error} isLoading={query.isLoading}>
      <Table columns={['單號', '倉庫', '申請人', '審核人', '申請時間', '完成時間', '狀態']}>
        {items.length === 0 ? (
          <EmptyRow colSpan={7} />
        ) : (
          items.map((order) => (
            <tr className="hover:bg-slate-50" key={order.id}>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{order.orderNo}</td>
              <td className="whitespace-nowrap px-4 py-3">
                {isAdmin ? (warehouseNames.get(order.warehouseId) ?? order.warehouseId) : profile.warehouseName}
              </td>
              <td className="whitespace-nowrap px-4 py-3">{order.requestedByName}</td>
              <td className="whitespace-nowrap px-4 py-3">{order.confirmedByName ?? '-'}</td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{dateTime(order.requestedAt)}</td>
              <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{dateTime(order.confirmedAt)}</td>
              <td className="whitespace-nowrap px-4 py-3">
                <StatusBadge value={order.status} />
              </td>
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
