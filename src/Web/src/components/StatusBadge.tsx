const statusClasses: Record<string, string> = {
  Pending: 'border-amber-200 bg-amber-50 text-amber-700',
  Confirmed: 'border-emerald-200 bg-emerald-50 text-emerald-700',
  Rejected: 'border-red-200 bg-red-50 text-red-700',
  待審核: 'border-amber-200 bg-amber-50 text-amber-700',
  已確認: 'border-emerald-200 bg-emerald-50 text-emerald-700',
  已拒絕: 'border-red-200 bg-red-50 text-red-700',
}

export function StatusBadge({ value }: { value: string }) {
  return (
    <span
      className={`inline-flex items-center border px-2 py-0.5 text-xs font-medium ${
        statusClasses[value] ?? 'border-slate-200 bg-slate-50 text-slate-600'
      }`}
    >
      {value}
    </span>
  )
}
