// 對應後端 Domain.Users.Role enum（Admin=0, WarehouseAdmin=1, WarehouseUser=2）。
// /api/v1/users 沒有註冊 JsonStringEnumConverter，回傳的是數字，要在這裡轉成可讀名稱。
const roleLabels: Record<number, string> = {
  0: 'Admin',
  1: 'WarehouseAdmin',
  2: 'WarehouseUser',
}

const roleDisplayNames: Record<string, string> = {
  Admin: '系統管理者',
  WarehouseAdmin: '倉庫管理者',
  WarehouseUser: '倉庫人員',
}

export function roleName(role: number): string {
  return roleLabels[role] ?? `未知角色(${role})`
}

export function roleDisplayName(role: string | number): string {
  const name = typeof role === 'number' ? roleName(role) : role
  return roleDisplayNames[name] ?? name
}
