import { Search } from 'lucide-react'
import { useState } from 'react'

type FilterField = {
  key: string
  label: string
  type?: 'text' | 'number' | 'date' | 'select'
  placeholder?: string
  options?: { value: string; label: string }[]
}

export function FilterBar({
  fields,
  onSearch,
}: {
  fields: FilterField[]
  onSearch: (values: Record<string, string>) => void
}) {
  const [draft, setDraft] = useState<Record<string, string>>({})

  return (
    <form
      className="flex flex-wrap items-end gap-3 border border-slate-200 bg-white p-4"
      onSubmit={(event) => {
        event.preventDefault()
        onSearch(draft)
      }}
    >
      {fields.map((field) => (
        <label className="block" key={field.key}>
          <span className="mb-1 block text-xs font-semibold uppercase tracking-wide text-slate-500">
            {field.label}
          </span>
          {field.type === 'select' ? (
            <select
              className="w-40 border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              onChange={(event) => setDraft((prev) => ({ ...prev, [field.key]: event.target.value }))}
              value={draft[field.key] ?? ''}
            >
              <option value="">全部</option>
              {(field.options ?? []).map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          ) : (
            <input
              className="w-40 border border-slate-300 px-3 py-2 text-sm outline-none focus:border-sky-500"
              onChange={(event) => setDraft((prev) => ({ ...prev, [field.key]: event.target.value }))}
              placeholder={field.placeholder}
              type={field.type === 'number' ? 'number' : field.type === 'date' ? 'date' : 'text'}
              value={draft[field.key] ?? ''}
            />
          )}
        </label>
      ))}
      <button
        className="inline-flex h-10 items-center gap-2 border border-slate-300 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
        type="submit"
      >
        <Search className="h-4 w-4" />
        搜尋
      </button>
    </form>
  )
}
