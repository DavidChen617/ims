import { ChevronLeft, ChevronRight } from 'lucide-react'
import { number } from '../lib/format'

type PaginationProps = {
  page: number
  size: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, size, totalCount, onPageChange }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / size))

  return (
    <div className="flex items-center justify-between border border-t-0 border-slate-200 bg-white px-4 py-3 text-sm text-slate-600">
      <span>
        共 {number(totalCount)} 筆，第 {page} / {totalPages} 頁
      </span>
      <div className="flex items-center gap-2">
        <button
          className="inline-flex items-center gap-1 border border-slate-300 px-3 py-1.5 font-medium hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          type="button"
        >
          <ChevronLeft className="h-4 w-4" />
          上一頁
        </button>
        <button
          className="inline-flex items-center gap-1 border border-slate-300 px-3 py-1.5 font-medium hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          type="button"
        >
          下一頁
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  )
}
