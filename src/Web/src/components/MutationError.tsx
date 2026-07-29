export function MutationError({ error }: { error: Error | null }) {
  if (!error) {
    return null
  }

  return (
    <div className="lg:col-span-full border border-red-200 bg-red-50 p-3 text-sm text-red-700 whitespace-pre-line">
      {error.message}
    </div>
  )
}
