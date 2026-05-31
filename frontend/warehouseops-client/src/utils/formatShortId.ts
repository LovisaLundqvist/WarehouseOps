export function formatShortId(id: string | null | undefined, prefix: string) {
  if (!id) {
    return `${prefix} #UNKNOWN`;
  }

  return `${prefix} #${id.slice(0, 8).toUpperCase()}`;
}
