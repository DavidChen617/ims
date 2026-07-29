import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { Plus, Ruler, Trash2 } from 'lucide-react'
import { api } from '../api/client'
import type { JwtProfile } from '../api/types'
import { DataState } from '../components/DataState'
import { EmptyRow } from '../components/EmptyRow'
import { Field } from '../components/Field'
import { MutationError } from '../components/MutationError'
import { FilterBar } from '../components/FilterBar'
import { Pagination } from '../components/Pagination'
import { Table } from '../components/Table'
import { number } from '../lib/format'

type OutletContext = { token: string; profile: JwtProfile }

const PAGE_SIZE = 20

export function ProductsPage() {
  const { token, profile } = useOutletContext<OutletContext>()
  const canManage = profile.role === 'Admin' || profile.role === 'WarehouseAdmin'

  const queryClient = useQueryClient()
  const [productTab, setProductTab] = useState<'list' | 'units'>('list')
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState<{
    productNo?: string
    name?: string
    unit?: string
    priceMin?: string
    priceMax?: string
  }>({})
  const [productNo, setProductNo] = useState('')
  const [name, setName] = useState('')
  const [unit, setUnit] = useState('')
  const [price, setPrice] = useState('')
  const [newUnit, setNewUnit] = useState('')
  const query = useQuery({
    queryKey: ['products', filter, page],
    queryFn: () =>
      api.products(token, {
        productNo: filter.productNo,
        name: filter.name,
        unit: filter.unit,
        priceMin: filter.priceMin ? Number(filter.priceMin) : undefined,
        priceMax: filter.priceMax ? Number(filter.priceMax) : undefined,
        page,
        size: PAGE_SIZE,
      }),
  })
  const units = useQuery({ queryKey: ['product-units'], queryFn: () => api.productUnits(token) })
  const createProduct = useMutation({
    mutationFn: () =>
      api.createProduct(token, {
        productNo,
        name,
        unit,
        price: Number(price),
      }),
    onSuccess: async () => {
      setProductNo('')
      setName('')
      setUnit('')
      setPrice('')
      await queryClient.invalidateQueries({ queryKey: ['products'] })
    },
  })
  const createUnit = useMutation({
    mutationFn: () => api.createProductUnit(token, { name: newUnit }),
    onSuccess: async () => {
      setUnit(newUnit)
      setNewUnit('')
      await queryClient.invalidateQueries({ queryKey: ['product-units'] })
    },
  })
  const deleteUnit = useMutation({
    mutationFn: (unitName: string) => api.deleteProductUnit(token, unitName),
    onSuccess: async () => {
      setUnit('')
      await queryClient.invalidateQueries({ queryKey: ['product-units'] })
      await queryClient.invalidateQueries({ queryKey: ['products'] })
    },
  })
  // 「使用中商品數」要算全部商品，不能只看目前這一頁——分頁上限 20 筆，
  // 用分頁後的 productItems 算會嚴重低估。
  const allProducts = useQuery({
    enabled: productTab === 'units',
    queryKey: ['products', 'all-for-unit-usage'],
    queryFn: () => api.products(token, { page: 1, size: 1000 }),
  })

  const productItems = query.data?.items ?? []
  const unitItems = units.data?.items ?? []

  return (
    <div className="space-y-4">
      {canManage ? (
        <div className="inline-flex border border-slate-200 bg-white p-1">
          <button
            className={`px-4 py-2 text-sm font-semibold ${
              productTab === 'list' ? 'bg-sky-600 text-white' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-950'
            }`}
            onClick={() => setProductTab('list')}
            type="button"
          >
            商品清單
          </button>
          <button
            className={`px-4 py-2 text-sm font-semibold ${
              productTab === 'units' ? 'bg-sky-600 text-white' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-950'
            }`}
            onClick={() => setProductTab('units')}
            type="button"
          >
            單位管理
          </button>
        </div>
      ) : null}

      {productTab === 'list' || !canManage ? (
        <>
          {canManage ? (
            <form
              className="grid gap-3 border border-slate-200 bg-white p-4 lg:grid-cols-[160px_1fr_160px_140px_auto]"
              onSubmit={(event) => {
                event.preventDefault()
                createProduct.mutate()
              }}
            >
              <Field label="商品編號">
                <input
                  className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
                  onChange={(event) => setProductNo(event.target.value)}
                  required
                  value={productNo}
                />
              </Field>
              <Field label="商品名稱">
                <input
                  className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
                  onChange={(event) => setName(event.target.value)}
                  required
                  value={name}
                />
              </Field>
              <Field label="單位">
                <select
                  className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
                  disabled={units.isLoading}
                  onChange={(event) => setUnit(event.target.value)}
                  required
                  value={unit}
                >
                  <option value="">選擇單位</option>
                  {(units.data?.items ?? []).map((item) => (
                    <option key={item.name} value={item.name}>
                      {item.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="價格">
                <input
                  className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
                  min="0"
                  onChange={(event) => setPrice(event.target.value)}
                  required
                  step="0.01"
                  type="number"
                  value={price}
                />
              </Field>
              <button
                className="inline-flex h-10 items-center justify-center gap-2 self-end bg-sky-600 px-4 text-sm font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
                disabled={createProduct.isPending || units.isLoading}
                type="submit"
              >
                <Plus className="h-4 w-4" />
                新增商品
              </button>
              <MutationError error={createProduct.error} />
            </form>
          ) : null}

          <FilterBar
            fields={[
              { key: 'productNo', label: '商品編號' },
              { key: 'name', label: '名稱' },
              {
                key: 'unit',
                label: '單位',
                type: 'select',
                options: (units.data?.items ?? []).map((item) => ({ value: item.name, label: item.name })),
              },
              { key: 'priceMin', label: '價格(最低)', type: 'number' },
              { key: 'priceMax', label: '價格(最高)', type: 'number' },
            ]}
            onSearch={(values) => {
              setFilter(values)
              setPage(1)
            }}
          />

          <DataState error={query.error} isLoading={query.isLoading}>
            <Table columns={['商品編號', '名稱', '單位', '價格']}>
              {productItems.length === 0 ? (
                <EmptyRow colSpan={4} />
              ) : (
                productItems.map((product) => (
                  <tr className="hover:bg-slate-50" key={product.id}>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-500">{product.productNo}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-medium">{product.name}</td>
                    <td className="whitespace-nowrap px-4 py-3">{product.unit}</td>
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(product.price)}</td>
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
        </>
      ) : (
        <section className="border border-slate-200 bg-white">
          <div className="border-b border-slate-200 px-4 py-3">
            <div className="flex items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <div className="grid h-9 w-9 place-items-center border border-slate-200 bg-slate-50 text-slate-600">
                  <Ruler className="h-4 w-4" />
                </div>
                <div>
                  <h2 className="text-sm font-semibold text-slate-950">單位管理</h2>
                  <p className="text-xs text-slate-500">維護商品可選單位，刪除前可先檢查目前使用中的商品數。</p>
                </div>
              </div>
              <span className="whitespace-nowrap border border-slate-200 bg-slate-50 px-2 py-1 font-mono text-xs text-slate-500">
                {number(unitItems.length)} 筆
              </span>
            </div>
          </div>
          <form
            className="grid gap-3 border-b border-slate-200 p-4 sm:grid-cols-[1fr_auto]"
            onSubmit={(event) => {
              event.preventDefault()
              createUnit.mutate()
            }}
          >
            <Field label="新增單位">
              <input
                className="w-full border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
                onChange={(event) => setNewUnit(event.target.value)}
                required
                value={newUnit}
              />
            </Field>
            <button
              className="inline-flex h-10 items-center justify-center gap-2 self-end bg-sky-600 px-4 text-sm font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
              disabled={createUnit.isPending}
              type="submit"
            >
              <Plus className="h-4 w-4" />
              新增單位
            </button>
            <MutationError error={createUnit.error ?? deleteUnit.error} />
          </form>
          <DataState error={units.error} isLoading={units.isLoading}>
            <div className="p-4">
              <Table columns={['單位名稱', '使用中商品數', '操作']}>
                {unitItems.length === 0 ? (
                  <EmptyRow colSpan={3} />
                ) : (
                  unitItems.map((item) => {
                    const usage = (allProducts.data?.items ?? []).filter((product) => product.unit === item.name).length
                    return (
                      <tr className="hover:bg-slate-50" key={item.name}>
                        <td className="whitespace-nowrap px-4 py-3 font-medium">{item.name}</td>
                        <td className="whitespace-nowrap px-4 py-3 font-mono text-sm">{number(usage)}</td>
                        <td className="whitespace-nowrap px-4 py-3">
                          <button
                            className="inline-flex items-center gap-2 border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50"
                            disabled={deleteUnit.isPending}
                            onClick={() => deleteUnit.mutate(item.name)}
                            type="button"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                            刪除
                          </button>
                        </td>
                      </tr>
                    )
                  })
                )}
              </Table>
            </div>
          </DataState>
        </section>
      )}
    </div>
  )
}
