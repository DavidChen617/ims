import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { Check, Plus, Trash2, X } from 'lucide-react'
import { api } from '../api/client'
import type { CreateOutboundItem, JwtProfile } from '../api/types'
import { DataState } from '../components/DataState'
import { EmptyRow } from '../components/EmptyRow'
import { Field } from '../components/Field'
import { MutationError } from '../components/MutationError'
import { Pagination } from '../components/Pagination'
import { StatusBadge } from '../components/StatusBadge'
import { Table } from '../components/Table'
import { dateTime, number } from '../lib/format'

type OutletContext = { token: string; profile: JwtProfile }

export function OutboundOperationsPage() {
  const { token, profile } = useOutletContext<OutletContext>()
  const canCreate = profile.role === 'WarehouseUser'
  const canReview = profile.role === 'WarehouseAdmin'

  return (
    <div className="space-y-6">
      {canCreate ? <CreateOutboundForm token={token} /> : null}
      <PendingOutboundList canReview={canReview} token={token} />
    </div>
  )
}

function CreateOutboundForm({ token }: { token: string }) {
  const queryClient = useQueryClient()
  const products = useQuery({ queryKey: ['products'], queryFn: () => api.products(token, { page: 1, size: 100 }) })
  const [orderNo, setOrderNo] = useState('')
  const [items, setItems] = useState<CreateOutboundItem[]>([])
  const [productId, setProductId] = useState('')
  const [quantity, setQuantity] = useState('')

  const productItems = products.data?.items ?? []
  const productMap = new Map(productItems.map((product) => [product.id, product]))

  function addItem() {
    const product = productMap.get(productId)
    if (!product || !quantity) {
      return
    }

    setItems((prev) => [...prev, { productId: product.id, productNo: product.productNo, quantity: Number(quantity) }])
    setProductId('')
    setQuantity('')
  }

  const createOutbound = useMutation({
    mutationFn: () => api.createOutbound(token, { orderNo, items }),
    onSuccess: async () => {
      setOrderNo('')
      setItems([])
      await queryClient.invalidateQueries({ queryKey: ['outbound-pending'] })
    },
  })

  return (
    <section className="border border-slate-200 bg-white">
      <div className="border-b border-slate-200 px-4 py-3">
        <h2 className="text-sm font-semibold text-slate-950">新增出貨申請</h2>
        <p className="text-xs text-slate-500">加入商品品項後送出，Inventory 確認庫存足夠後才會進入待審核。</p>
      </div>
      <div className="grid gap-4 p-4">
        <Field label="單號">
          <input
            className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
            onChange={(event) => setOrderNo(event.target.value)}
            value={orderNo}
          />
        </Field>

        <div className="grid gap-3 border border-dashed border-slate-300 p-3 sm:grid-cols-[1fr_120px_auto]">
          <Field label="商品">
            <select
              className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              disabled={products.isLoading}
              onChange={(event) => setProductId(event.target.value)}
              value={productId}
            >
              <option value="">選擇商品</option>
              {productItems.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.productNo} - {product.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="數量">
            <input
              className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              min="1"
              onChange={(event) => setQuantity(event.target.value)}
              type="number"
              value={quantity}
            />
          </Field>
          <button
            className="inline-flex h-10 items-center justify-center gap-2 self-end bg-slate-800 px-4 text-sm font-semibold text-white hover:bg-slate-900 disabled:opacity-50"
            disabled={!productId || !quantity}
            onClick={addItem}
            type="button"
          >
            <Plus className="h-4 w-4" />
            加入品項
          </button>
        </div>

        <Table columns={['商品編號', '數量', '操作']}>
          {items.length === 0 ? (
            <EmptyRow colSpan={3} message="尚未加入品項" />
          ) : (
            items.map((item, index) => (
              <tr className="hover:bg-slate-50" key={`${item.productId}-${index}`}>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{item.productNo}</td>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(item.quantity)}</td>
                <td className="whitespace-nowrap px-4 py-3">
                  <button
                    className="inline-flex items-center gap-2 border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50"
                    onClick={() => setItems((prev) => prev.filter((_, i) => i !== index))}
                    type="button"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                    移除
                  </button>
                </td>
              </tr>
            ))
          )}
        </Table>

        <div>
          <button
            className="inline-flex h-10 items-center justify-center gap-2 bg-sky-600 px-4 text-sm font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
            disabled={createOutbound.isPending || !orderNo || items.length === 0}
            onClick={() => createOutbound.mutate()}
            type="button"
          >
            <Plus className="h-4 w-4" />
            送出申請
          </button>
        </div>
        <MutationError error={createOutbound.error} />
      </div>
    </section>
  )
}

const PENDING_PAGE_SIZE = 20

function PendingOutboundList({ token, canReview }: { token: string; canReview: boolean }) {
  const queryClient = useQueryClient()
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [rejectReason, setRejectReason] = useState('')
  const [page, setPage] = useState(1)

  const pending = useQuery({
    queryKey: ['outbound-pending', page],
    queryFn: () => api.listPendingOutboundOrders(token, { page, size: PENDING_PAGE_SIZE }),
  })

  const detail = useQuery({
    enabled: !!selectedId,
    queryKey: ['outbound-order', selectedId],
    queryFn: () => api.getOutboundOrder(token, selectedId!),
  })

  async function refresh() {
    setSelectedId(null)
    setRejectReason('')
    await queryClient.invalidateQueries({ queryKey: ['outbound-pending'] })
    await queryClient.invalidateQueries({ queryKey: ['outbound-history'] })
  }

  const confirm = useMutation({
    mutationFn: (id: string) => api.confirmOutbound(token, id),
    onSuccess: refresh,
  })

  const reject = useMutation({
    mutationFn: (id: string) => api.rejectOutbound(token, id, { reason: rejectReason }),
    onSuccess: refresh,
  })

  const items = pending.data?.items ?? []

  return (
    <section className="border border-slate-200 bg-white">
      <div className="border-b border-slate-200 px-4 py-3">
        <h2 className="text-sm font-semibold text-slate-950">待審核出貨單</h2>
      </div>

      <DataState error={pending.error} isLoading={pending.isLoading}>
        <Table columns={['單號', '操作']}>
          {items.length === 0 ? (
            <EmptyRow colSpan={2} />
          ) : (
            items.map((item) => (
              <tr className="hover:bg-slate-50" key={item.id}>
                <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{item.orderNo}</td>
                <td className="whitespace-nowrap px-4 py-3">
                  <button
                    className="border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                    onClick={() => setSelectedId(item.id)}
                    type="button"
                  >
                    查看明細
                  </button>
                </td>
              </tr>
            ))
          )}
        </Table>
        <Pagination
          onPageChange={setPage}
          page={pending.data?.page ?? page}
          size={pending.data?.size ?? PENDING_PAGE_SIZE}
          totalCount={pending.data?.totalCount ?? 0}
        />
      </DataState>

      {selectedId ? (
        <div className="border-t border-slate-200 p-4">
          <DataState error={detail.error} isLoading={detail.isLoading}>
            {detail.data ? (
              <div className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <div className="font-mono text-sm font-semibold">{detail.data.orderNo}</div>
                    <div className="text-xs text-slate-500">
                      申請人 {detail.data.requestedByName} · {dateTime(detail.data.requestedAt)}
                    </div>
                  </div>
                  <StatusBadge value={detail.data.status} />
                </div>

                <Table columns={['商品編號', '商品名稱', '單位', '數量']}>
                  {detail.data.items.map((item) => (
                    <tr key={item.productId}>
                      <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{item.productNo}</td>
                      <td className="whitespace-nowrap px-4 py-3">{item.productName}</td>
                      <td className="whitespace-nowrap px-4 py-3">{item.unit}</td>
                      <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(item.quantity)}</td>
                    </tr>
                  ))}
                </Table>

                {canReview ? (
                  <div className="flex flex-wrap items-end gap-3">
                    <button
                      className="inline-flex items-center gap-2 bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                      disabled={confirm.isPending}
                      onClick={() => confirm.mutate(selectedId)}
                      type="button"
                    >
                      <Check className="h-4 w-4" />
                      確認出貨
                    </button>
                    <Field label="拒絕原因">
                      <input
                        className="w-64 border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
                        onChange={(event) => setRejectReason(event.target.value)}
                        value={rejectReason}
                      />
                    </Field>
                    <button
                      className="inline-flex items-center gap-2 border border-red-200 px-4 py-2 text-sm font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50"
                      disabled={reject.isPending || !rejectReason}
                      onClick={() => reject.mutate(selectedId)}
                      type="button"
                    >
                      <X className="h-4 w-4" />
                      拒絕
                    </button>
                  </div>
                ) : null}
                <MutationError error={confirm.error ?? reject.error} />
              </div>
            ) : null}
          </DataState>
        </div>
      ) : null}
    </section>
  )
}
