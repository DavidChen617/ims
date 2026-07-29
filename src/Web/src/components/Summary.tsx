export function Summary({ label, value }: { label: string; value: string }) {
  return (
    <div className="border border-slate-200 bg-white p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</div>
      <div className="mt-2 font-mono text-2xl font-semibold text-slate-950">{value}</div>
    </div>
  )
}
