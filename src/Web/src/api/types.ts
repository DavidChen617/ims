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

export type UserDto = {
  id: string
  warehouseId: string | null
  warehouseName: string | null
  name: string
  username: string
  role: Role | number
  createdAt: string
}

export type UsersDto = {
  items: UserDto[]
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

export type ProductDto = {
  id: string
  productNo: string
  name: string
  unit: string
  price: number
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

export type OutboundHistoryResultDto = {
  items: OutboundHistoryDto[]
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
