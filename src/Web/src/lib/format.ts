export function number(value: number | string | null | undefined) {
  const n = Number(value ?? 0)
  return Number.isFinite(n) ? n.toLocaleString('zh-TW') : '0'
}

export function dateTime(value: string | null | undefined) {
  if (!value) {
    return '-'
  }

  return value.replace('T', ' ').slice(0, 16)
}
