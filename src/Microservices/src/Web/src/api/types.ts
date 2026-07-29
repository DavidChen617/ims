export type Role = 'Admin' | 'WarehouseAdmin' | 'WarehouseUser' | string

export type LoginRequest = {
  username: string
  password: string
}

export type LoginDto = {
  userId: string
  accessToken: string
  refreshToken: string
  refreshTokenExpiredAt: string
}

export type ProblemDetails = {
  type?: string | null
  title?: string | null
  status?: number | string | null
  detail?: string | null
  instance?: string | null
}

export type HttpValidationProblemDetails = ProblemDetails & {
  errors?: Record<string, string[]>
}

// 後端 Domain.Users.Role 沒有註冊 JsonStringEnumConverter，/api/v1/users 回傳的是數字。
export type UserDto = {
  id: string
  warehouseId: string | null
  warehouseName: string | null
  name: string
  username: string
  role: number
  createdAt: string
}

export type WarehouseListItemDto = {
  id: string
  name: string
  warehouseAdminName: string | null
  staffCount: number
}

export type WarehousesDto = {
  items: WarehouseListItemDto[]
}

export type CreateWarehouseDto = {
  id: string
  name: string
}

export type ProductDto = {
  id: string
  productNo: string
  name: string
  unit: string
  price: number
}

export type ProductUnitDto = {
  name: string
}

export type ProductUnitsDto = {
  items: ProductUnitDto[]
}

export type CreateProductCommand = {
  productNo: string
  name: string
  unit: string
  price: number
}

export type CreateProductUnitCommand = {
  name: string
}

export type CreateProductDto = {
  productId: string
}

export type CreateWarehouseRequest = {
  name: string
}

export type RegisterUserFromAdminRequest = {
  warehouseId: string
  name: string
  username: string
  password: string
  role: 1 | 2
}

export type RegisterFromWarehouseAdminRequest = {
  name: string
  username: string
  password: string
}

export type PagedResult<T> = {
  items: T[]
  totalCount: number
  page: number
  size: number
}

export type InboundHistoryLineDto = {
  orderNo: string
  productId: string
  productNo: string
  productName: string
  quantity: number
  unitPrice: number
  totalAmount: number
}

export type InboundHistoryResultDto = {
  page: PagedResult<InboundHistoryLineDto>
  totalQuantity: number
  totalAmount: number
}

export type OutboundHistoryDto = {
  id: string
  orderNo: string
  warehouseId: string
  status: string
  requestedAt: string
  confirmedAt: string | null
  requestedBy: string
  requestedByName: string
  confirmedBy: string | null
  confirmedByName: string | null
}

export type StockDto = {
  productId: string
  productNo: string | null
  productName: string | null
  unit: string | null
  warehouseId: string
  warehouseName: string | null
  quantity: number
  cumulativeShipped: number
}

export type JwtProfile = {
  userId: string
  name: string
  username: string
  role: Role
  warehouseId: string | null
  warehouseName: string | null
}

// --- 進貨/出貨申請與審核流程 ---

export type CreateInboundItem = {
  productId: string
  productNo: string
  quantity: number
  unitPrice?: number | null
}

export type CreateInboundCommand = {
  orderNo: string
  items: CreateInboundItem[]
}

export type CreateOutboundItem = {
  productId: string
  productNo: string
  quantity: number
}

export type CreateOutboundCommand = {
  orderNo: string
  items: CreateOutboundItem[]
}

export type CreateOrderDto = {
  id: string
  status: string
}

export type PendingOrderDto = {
  id: string
  orderNo: string
}

export type InboundOrderItemDto = {
  productId: string
  productNo: string
  productName: string
  unit: string
  quantity: number
  unitPrice: number
  totalAmount: number
}

export type InboundOrderDto = {
  id: string
  orderNo: string
  warehouseId: string
  status: string
  rejectReason: string | null
  requestedBy: string
  requestedByName: string
  requestedAt: string
  confirmedBy: string | null
  confirmedByName: string | null
  confirmedAt: string | null
  items: InboundOrderItemDto[]
}

export type OutboundOrderItemDto = {
  productId: string
  productNo: string
  productName: string
  unit: string
  quantity: number
}

export type OutboundOrderDto = {
  id: string
  orderNo: string
  warehouseId: string
  status: string
  rejectReason: string | null
  requestedBy: string
  requestedByName: string
  requestedAt: string
  confirmedBy: string | null
  confirmedByName: string | null
  confirmedAt: string | null
  items: OutboundOrderItemDto[]
}

export type RejectOrderRequest = {
  reason: string
}
