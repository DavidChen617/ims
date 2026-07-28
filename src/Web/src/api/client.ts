import type {
  InboundHistoryResultDto,
  JwtProfile,
  LoginDto,
  LoginRequest,
  OutboundHistoryResultDto,
  PagedResult,
  ProductDto,
  StockDto,
  UsersDto,
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
    const details = await response.text().catch(() => undefined)
    throw new ApiError(details || response.statusText, response.status, details)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const api = {
  login: (body: LoginRequest) =>
    request<LoginDto>('organization', '/api/v1/auth/login', null, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  warehouses: (token: string) => request<WarehousesDto>('organization', '/api/v1/warehouse', token),

  users: (token: string) => request<UsersDto>('organization', '/api/v1/users', token),

  products: (token: string, params: { page?: number; size?: number } = {}) =>
    request<PagedResult<ProductDto>>(
      'ordering',
      `/api/v1/products${toQuery({ page: params.page ?? 1, size: params.size ?? 20 })}`,
      token,
    ),

  inboundHistory: (
    token: string,
    params: { warehouseId?: string | null; page?: number; size?: number } = {},
  ) =>
    request<InboundHistoryResultDto>(
      'ordering',
      `/api/v1/orders/inbound/history${toQuery({
        warehouseId: params.warehouseId,
        page: params.page ?? 1,
        size: params.size ?? 20,
      })}`,
      token,
    ),

  inboundWarehouseHistory: (token: string, params: { page?: number; size?: number } = {}) =>
    request<InboundHistoryResultDto>(
      'ordering',
      `/api/v1/orders/inbound/history/warehouse${toQuery({
        page: params.page ?? 1,
        size: params.size ?? 20,
      })}`,
      token,
    ),

  outboundHistory: (token: string, params: { warehouseId?: string | null } = {}) =>
    request<OutboundHistoryResultDto>(
      'ordering',
      `/api/v1/orders/outbound/history${toQuery({ warehouseId: params.warehouseId })}`,
      token,
    ),

  outboundWarehouseHistory: (token: string) =>
    request<OutboundHistoryResultDto>('ordering', '/api/v1/orders/outbound/history/warehouse', token),

  stocks: (
    token: string,
    params: { warehouseId?: string | null; page?: number; size?: number } = {},
  ) =>
    request<PagedResult<StockDto>>(
      'inventory',
      `/api/v1/stocks${toQuery({
        warehouseId: params.warehouseId,
        page: params.page ?? 1,
        size: params.size ?? 20,
      })}`,
      token,
    ),

  warehouseStocks: (token: string, params: { page?: number; size?: number } = {}) =>
    request<PagedResult<StockDto>>(
      'inventory',
      `/api/v1/stocks/warehouse${toQuery({ page: params.page ?? 1, size: params.size ?? 20 })}`,
      token,
    ),
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
