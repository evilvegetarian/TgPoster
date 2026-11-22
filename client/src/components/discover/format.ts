import {format, formatDistanceToNow} from "date-fns"
import {ru} from "date-fns/locale"

export const compactNumber = new Intl.NumberFormat("ru-RU", {notation: "compact", maximumFractionDigits: 1})
export const utcTime = new Intl.DateTimeFormat("ru-RU", {hour: "2-digit", minute: "2-digit", timeZone: "UTC"})

export function formatDateTime(value: string | null | undefined): string {
    if (!value) return "—"
    return format(new Date(value), "d MMM yyyy, HH:mm", {locale: ru})
}

export function formatRelative(value: string | null | undefined): string {
    if (!value) return "—"
    return formatDistanceToNow(new Date(value), {locale: ru, addSuffix: true})
}

export function percent(part: number, total: number): string {
    if (total <= 0) return "0%"
    return `${((part / total) * 100).toFixed(part / total >= 0.1 ? 0 : 1)}%`
}

export function channelLink(channel: {tgUrl?: string | null; username?: string | null}): string | null {
    return channel.tgUrl ?? (channel.username ? `https://t.me/${channel.username}` : null)
}

// Графики группируют по суткам UTC, верхняя граница фильтра истории включительна
export function utcDayRange(day: string) {
    return {From: `${day}T00:00:00Z`, To: `${day}T23:59:59.999999Z`}
}

// Значение <input type="date"> → начало/конец этого дня в локальной зоне, в ISO для API
export function toIsoStart(value: string): string | undefined {
    return value ? new Date(`${value}T00:00:00`).toISOString() : undefined
}

export function toIsoEnd(value: string): string | undefined {
    return value ? new Date(`${value}T23:59:59.999`).toISOString() : undefined
}
