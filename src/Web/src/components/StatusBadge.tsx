// 值來自後端 enum.ToString()：Inbound 是 Pending/Confirmed/Rejected，
// Outbound 多一個 Processing（還在等 Inventory 回覆庫存是否足夠）。
const statusClasses: Record<string, string> = {
  Processing: 'border-sky-200 bg-sky-50 text-sky-700',
  Pending: 'border-amber-200 bg-amber-50 text-amber-700',
  Confirmed: 'border-emerald-200 bg-emerald-50 text-emerald-700',
  Rejected: 'border-red-200 bg-red-50 text-red-700',
}

const statusLabels: Record<string, string> = {
  Processing: '處理中',
  Pending: '待審核',
  Confirmed: '已確認',
  Rejected: '已拒絕',
}

export function StatusBadge({ value }: { value: string }) {
  return (
    <span
      className={`inline-flex items-center border px-2 py-0.5 text-xs font-medium ${
        statusClasses[value] ?? 'border-slate-200 bg-slate-50 text-slate-600'
      }`}
    >
      {statusLabels[value] ?? value}
    </span>
  )
}
