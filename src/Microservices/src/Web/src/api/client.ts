import type {
  CreateInboundCommand,
  CreateOrderDto,
  CreateOutboundCommand,
  CreateProductCommand,
  CreateProductUnitCommand,
  CreateWarehouseDto,
  CreateWarehouseRequest,
  InboundHistoryResultDto,
  InboundOrderDto,
  HttpValidationProblemDetails,
  JwtProfile,
  LoginDto,
  LoginRequest,
  OutboundHistoryDto,
  OutboundOrderDto,
  PagedResult,
  PendingOrderDto,
  ProductDto,
  ProductUnitsDto,
  RegisterFromWarehouseAdminRequest,
  RegisterUserFromAdminRequest,
  RejectOrderRequest,
  StockDto,
  UserDto,
  WarehousesDto,
} from './types'

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

const serviceBase = {
  organization: `${apiBase}/api/organization`,
  ordering: `${apiBase}/api/ordering`,
  inventory: `${apiBase}/api/inventory`,
} as const

type ServiceName = keyof typeof serviceBase
type QueryValue = string | number | boolean | null | undefined

export class ApiError extends Error {
  readonly status: number
  readonly details?: unknown

  constructor(message: string, status: number, details?: unknown) {
    super(message)
    this.status = status
    this.details = details
  }
}

// request() 有 401 就代表 access token 已經失效——這裡沒有 React context 可以直接觸發
// signOut(),所以讓 useSession 註冊一個 callback,由它決定怎麼收尾(清 token、導回登入頁)。
let onUnauthorized: (() => void) | null = null

export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler
}

function isProblemDetails(value: unknown): value is HttpValidationProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'detail' in value || 'errors' in value)
}

function problemMessage(problem: HttpValidationProblemDetails) {
  const validationErrors = Object.entries(problem.errors ?? {}).flatMap(([field, messages]) =>
    messages.map((message) => `${field}: ${message}`),
  )

  if (validationErrors.length > 0) {
    return validationErrors.join('\n')
  }

  return problem.detail ?? problem.title ?? 'API request failed'
}

function toQuery(params?: Record<string, QueryValue>) {
  const query = new URLSearchParams()
  Object.entries(params ?? {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      query.set(key, String(value))
    }
  })

  const text = query.toString()
  return text ? `?${text}` : ''
}

async function request<T>(
  service: ServiceName,
  path: string,
  token: string | null,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${serviceBase[service]}${path}`, {
    ...init,
    headers,
  })

  if (!response.ok) {
    const contentType = response.headers.get('content-type') ?? ''
    const details = contentType.includes('json')
      ? await response.json().catch(() => undefined)
      : await response.text().catch(() => undefined)

    const message = isProblemDetails(details)
      ? problemMessage(details)
      : typeof details === 'string' && details
        ? details
        : response.statusText

    if (response.status === 401 && token) {
      onUnauthorized?.()
    }

    throw new ApiError(message, response.status, details)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  if (!text) {
    return undefined as T
  }

  return JSON.parse(text) as T
}

type InboundHistoryParams = {
  orderNo?: string
  productNo?: string
  productName?: string
  status?: string
  requestedFrom?: string
  requestedTo?: string
  quantityMin?: number
  quantityMax?: number
  unitPriceMin?: number
  unitPriceMax?: number
  amountMin?: number
  amountMax?: number
  page?: number
  size?: number
}

function inboundHistoryQuery(params: InboundHistoryParams) {
  return {
    orderNo: params.orderNo,
    productNo: params.productNo,
    productName: params.productName,
    status: params.status,
    requestedFrom: params.requestedFrom,
    requestedTo: params.requestedTo,
    quantityMin: params.quantityMin,
    quantityMax: params.quantityMax,
    unitPriceMin: params.unitPriceMin,
    unitPriceMax: params.unitPriceMax,
    amountMin: params.amountMin,
    amountMax: params.amountMax,
    page: params.page ?? 1,
    size: params.size ?? 20,
  }
}

type OutboundHistoryParams = {
  orderNo?: string
  status?: string
  requestedFrom?: string
  requestedTo?: string
  completedFrom?: string
  completedTo?: string
  productNo?: string
  productName?: string
  unit?: string
  requestedByName?: string
  confirmedByName?: string
  page?: number
  size?: number
}

function outboundHistoryQuery(params: OutboundHistoryParams) {
  return {
    orderNo: params.orderNo,
    status: params.status,
    requestedFrom: params.requestedFrom,
    requestedTo: params.requestedTo,
    completedFrom: params.completedFrom,
    completedTo: params.completedTo,
    productNo: params.productNo,
    productName: params.productName,
    unit: params.unit,
    requestedByName: params.requestedByName,
    confirmedByName: params.confirmedByName,
    page: params.page ?? 1,
    size: params.size ?? 20,
  }
}

type StockParams = {
  productNo?: string
  productName?: string
  unit?: string
  quantityMin?: number
  quantityMax?: number
  cumulativeShippedMin?: number
  cumulativeShippedMax?: number
  page?: number
  size?: number
}

function stockQuery(params: StockParams) {
  return {
    productNo: params.productNo,
    productName: params.productName,
    unit: params.unit,
    quantityMin: params.quantityMin,
    quantityMax: params.quantityMax,
    cumulativeShippedMin: params.cumulativeShippedMin,
    cumulativeShippedMax: params.cumulativeShippedMax,
    page: params.page ?? 1,
    size: params.size ?? 20,
  }
}

export const api = {
  login: (body: LoginRequest) =>
    request<LoginDto>('organization', '/api/v1/auth/login', null, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  warehouses: (
    token: string,
    params: {
      name?: string
      warehouseAdminName?: string
      staffCountMin?: number
      staffCountMax?: number
    } = {},
  ) =>
    request<WarehousesDto>(
      'organization',
      `/api/v1/warehouse${toQuery({
        name: params.name,
        warehouseAdminName: params.warehouseAdminName,
        staffCountMin: params.staffCountMin,
        staffCountMax: params.staffCountMax,
      })}`,
      token,
    ),

  createWarehouse: (token: string, body: CreateWarehouseRequest) =>
    request<CreateWarehouseDto>('organization', '/api/v1/warehouse', token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  users: (
    token: string,
    params: {
      name?: string
      username?: string
      role?: string
      warehouseName?: string
      createdFrom?: string
      createdTo?: string
      page?: number
      size?: number
    } = {},
  ) =>
    request<PagedResult<UserDto>>(
      'organization',
      `/api/v1/users${toQuery({
        name: params.name,
        username: params.username,
        role: params.role,
        warehouseName: params.warehouseName,
        createdFrom: params.createdFrom,
        createdTo: params.createdTo,
        page: params.page ?? 1,
        size: params.size ?? 20,
      })}`,
      token,
    ),

  registerUserByAdmin: (token: string, body: RegisterUserFromAdminRequest) =>
    request<LoginDto>('organization', '/api/v1/auth/admin/register/user', token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  registerWarehouseUser: (token: string, body: RegisterFromWarehouseAdminRequest) =>
    request<LoginDto>('organization', '/api/v1/auth/warehouseAdmin/register/warehouseUser', token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  products: (
    token: string,
    params: {
      productNo?: string
      name?: string
      unit?: string
      priceMin?: number
      priceMax?: number
      page?: number
      size?: number
    } = {},
  ) =>
    request<PagedResult<ProductDto>>(
      'ordering',
      `/api/v1/products${toQuery({
        productNo: params.productNo,
        name: params.name,
        unit: params.unit,
        priceMin: params.priceMin,
        priceMax: params.priceMax,
        page: params.page ?? 1,
        size: params.size ?? 20,
      })}`,
      token,
    ),

  // 後端用 ToCreatedAtRoute() 回應,只有 201 + Location header,body 是空的——
  // CreateProductDto 只存在於 OpenAPI 文件的宣告,實際 response 不會有這個 JSON。
  createProduct: (token: string, body: CreateProductCommand) =>
    request<void>('ordering', '/api/v1/products', token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  productUnits: (token: string) => request<ProductUnitsDto>('ordering', '/api/v1/products/units', token),

  createProductUnit: (token: string, body: CreateProductUnitCommand) =>
    request<void>('ordering', '/api/v1/products/units', token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  deleteProductUnit: (token: string, name: string) =>
    request<void>('ordering', `/api/v1/products/units/${encodeURIComponent(name)}`, token, {
      method: 'DELETE',
    }),

  inboundHistory: (
    token: string,
    params: InboundHistoryParams & { warehouseId?: string | null } = {},
  ) =>
    request<InboundHistoryResultDto>(
      'ordering',
      `/api/v1/orders/inbound/history${toQuery({ ...inboundHistoryQuery(params), warehouseId: params.warehouseId })}`,
      token,
    ),

  inboundWarehouseHistory: (token: string, params: InboundHistoryParams = {}) =>
    request<InboundHistoryResultDto>(
      'ordering',
      `/api/v1/orders/inbound/history/warehouse${toQuery(inboundHistoryQuery(params))}`,
      token,
    ),

  outboundHistory: (
    token: string,
    params: OutboundHistoryParams & { warehouseId?: string | null } = {},
  ) =>
    request<PagedResult<OutboundHistoryDto>>(
      'ordering',
      `/api/v1/orders/outbound/history${toQuery({ ...outboundHistoryQuery(params), warehouseId: params.warehouseId })}`,
      token,
    ),

  outboundWarehouseHistory: (token: string, params: OutboundHistoryParams = {}) =>
    request<PagedResult<OutboundHistoryDto>>(
      'ordering',
      `/api/v1/orders/outbound/history/warehouse${toQuery(outboundHistoryQuery(params))}`,
      token,
    ),

  stocks: (token: string, params: StockParams & { warehouseId?: string | null } = {}) =>
    request<PagedResult<StockDto>>(
      'inventory',
      `/api/v1/stocks${toQuery({ ...stockQuery(params), warehouseId: params.warehouseId })}`,
      token,
    ),

  warehouseStocks: (token: string, params: StockParams = {}) =>
    request<PagedResult<StockDto>>('inventory', `/api/v1/stocks/warehouse${toQuery(stockQuery(params))}`, token),

  // --- 進貨作業 ---

  // 跟 createProduct 同一種情況：ToCreatedAtRoute() 回應沒有 body。
  createInbound: (token: string, body: CreateInboundCommand) =>
    request<void>('ordering', '/api/v1/orders/inbound', token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  listPendingInboundOrders: (token: string, params: { page?: number; size?: number } = {}) =>
    request<PagedResult<PendingOrderDto>>(
      'ordering',
      `/api/v1/orders/inbound/pending${toQuery({ page: params.page ?? 1, size: params.size ?? 20 })}`,
      token,
    ),

  getInboundOrder: (token: string, id: string) =>
    request<InboundOrderDto>('ordering', `/api/v1/orders/inbound/${id}`, token),

  confirmInbound: (token: string, id: string) =>
    request<CreateOrderDto>('ordering', `/api/v1/orders/inbound/${id}/confirm`, token, { method: 'POST' }),

  rejectInbound: (token: string, id: string, body: RejectOrderRequest) =>
    request<CreateOrderDto>('ordering', `/api/v1/orders/inbound/${id}/reject`, token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  // --- 出貨作業 ---

  createOutbound: (token: string, body: CreateOutboundCommand) =>
    request<void>('ordering', '/api/v1/orders/outbound', token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  listPendingOutboundOrders: (token: string, params: { page?: number; size?: number } = {}) =>
    request<PagedResult<PendingOrderDto>>(
      'ordering',
      `/api/v1/orders/outbound/pending${toQuery({ page: params.page ?? 1, size: params.size ?? 20 })}`,
      token,
    ),

  getOutboundOrder: (token: string, id: string) =>
    request<OutboundOrderDto>('ordering', `/api/v1/orders/outbound/${id}`, token),

  confirmOutbound: (token: string, id: string) =>
    request<CreateOrderDto>('ordering', `/api/v1/orders/outbound/${id}/confirm`, token, { method: 'POST' }),

  rejectOutbound: (token: string, id: string, body: RejectOrderRequest) =>
    request<CreateOrderDto>('ordering', `/api/v1/orders/outbound/${id}/reject`, token, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
}

export function decodeProfile(token: string): JwtProfile | null {
  const [, payload] = token.split('.')
  if (!payload) {
    return null
  }

  try {
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
    const json = JSON.parse(atob(normalized)) as Record<string, unknown>
    const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    const idClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
    const usernameClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'

    return {
      userId: String(json[idClaim] ?? ''),
      name: String(json.name ?? json[usernameClaim] ?? ''),
      username: String(json[usernameClaim] ?? ''),
      role: String(json[roleClaim] ?? ''),
      warehouseId: String(json.warehouseId ?? '') || null,
      warehouseName: String(json.warehouseName ?? '') || null,
    }
  } catch {
    return null
  }
}
