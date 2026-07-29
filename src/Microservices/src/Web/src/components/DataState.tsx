type DataStateProps = {
  isLoading: boolean
  error: Error | null
  children: React.ReactNode
}

export function DataState({ isLoading, error, children }: DataStateProps) {
  if (isLoading) {
    return <div className="border border-dashed border-slate-300 bg-white p-8 text-sm text-slate-500">載入中</div>
  }

  if (error) {
    return (
      <div className="border border-red-200 bg-red-50 p-4 text-sm text-red-700">
        {error.message || '資料讀取失敗'}
      </div>
    )
  }

  return children
}
