export function EmptyRow({ colSpan, message = '尚無資料' }: { colSpan: number; message?: string }) {
  return (
    <tr>
      <td className="px-4 py-8 text-center text-sm text-slate-400" colSpan={colSpan}>
        {message}
      </td>
    </tr>
  )
}
