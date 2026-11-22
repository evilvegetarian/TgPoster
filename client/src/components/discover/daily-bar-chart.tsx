import {type ReactNode, useRef, useState} from "react"
import {format, parseISO} from "date-fns"
import {ru} from "date-fns/locale"
import {Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle} from "@/components/ui/dialog"
import {cn} from "@/lib/utils"
import type {DiscoverDailyCount} from "@/api/endpoints/tgPosterAPI.schemas"

interface DailyBarChartProps {
    data: DiscoverDailyCount[]
    /** Tailwind-класс цвета столбцов, например "bg-chart-2" */
    colorClass: string
    /** Подпись единицы в подсказке, например "спарсено" */
    unitLabel: string
    height?: number
    dayDetails?: {title: string; render: (date: string) => ReactNode}
}

function formatDay(date: string): string {
    return format(parseISO(date), "d MMM", {locale: ru})
}

function formatFullDay(date: string): string {
    return format(parseISO(date), "d MMMM yyyy", {locale: ru})
}

// Столбчатый график «по дням»: один ряд, один оттенок, hover подсвечивает столбец
// и выводит дату со значением над графиком
export function DailyBarChart({data, colorClass, unitLabel, height = 160, dayDetails}: DailyBarChartProps) {
    const [hovered, setHovered] = useState<number | null>(null)
    const [selectedDay, setSelectedDay] = useState<string | null>(null)
    const trigger = useRef<HTMLButtonElement>(null)

    if (data.length === 0) {
        return (
            <div className="flex items-center justify-center text-sm text-muted-foreground" style={{height}}>
                Нет данных за период
            </div>
        )
    }

    const max = Math.max(1, ...data.map((d) => d.count))
    const total = data.reduce((sum, d) => sum + d.count, 0)
    const active = hovered != null ? data[hovered] : null

    return (
        <div>
            <div className="flex items-baseline justify-between gap-2 mb-2 text-xs text-muted-foreground h-4">
                <span className="truncate">
                    {active
                        ? `${formatFullDay(active.date)} — ${active.count.toLocaleString("ru-RU")} ${unitLabel}`
                        : `Всего за период: ${total.toLocaleString("ru-RU")}`}
                </span>
                <span className="shrink-0">макс. {max.toLocaleString("ru-RU")}</span>
            </div>
            <div
                className="flex items-end gap-0.5 border-b"
                style={{height}}
                onMouseLeave={() => setHovered(null)}
            >
                {data.map((d, i) => {
                    const label = `${formatFullDay(d.date)} — ${d.count.toLocaleString("ru-RU")} ${unitLabel}`
                    const columnClass = "flex-1 h-full flex items-end justify-center min-w-0"
                    const bar = (
                        <span className={cn("w-full max-w-6 rounded-t-[4px] transition-opacity", colorClass,
                            hovered != null && hovered !== i && "opacity-40")}
                              style={{height: `${(d.count / max) * 100}%`, minHeight: d.count > 0 ? 2 : 0}}/>
                    )
                    if (dayDetails && d.count > 0) {
                        return (
                            <button key={d.date} type="button" title={label} aria-label={label} aria-haspopup="dialog"
                                    className={cn(columnClass, "cursor-pointer focus-visible:outline-2 focus-visible:outline-ring")}
                                    onMouseEnter={() => setHovered(i)} onFocus={() => setHovered(i)} onBlur={() => setHovered(null)}
                                    onClick={event => {
                                        trigger.current = event.currentTarget
                                        setSelectedDay(d.date)
                                    }}>
                                {bar}
                            </button>
                        )
                    }
                    return (
                        <div key={d.date} className={columnClass} title={label} onMouseEnter={() => setHovered(i)}>
                            {bar}
                        </div>
                    )
                })}
            </div>
            <div className="flex justify-between mt-1 text-[10px] text-muted-foreground">
                <span>{formatDay(data[0].date)}</span>
                {data.length > 2 && <span>{formatDay(data[Math.floor(data.length / 2)].date)}</span>}
                <span>{formatDay(data[data.length - 1].date)}</span>
            </div>
            {dayDetails && (
                <Dialog open={selectedDay !== null} onOpenChange={open => { if (!open) setSelectedDay(null) }}>
                    <DialogContent className="max-h-[90dvh] overflow-y-auto sm:max-w-5xl" onCloseAutoFocus={event => {
                        event.preventDefault()
                        trigger.current?.focus()
                    }}>
                        {selectedDay && <>
                            <DialogHeader>
                                <DialogTitle>{dayDetails.title}</DialogTitle>
                                <DialogDescription>{formatFullDay(selectedDay)} (UTC)</DialogDescription>
                            </DialogHeader>
                            {dayDetails.render(selectedDay)}
                        </>}
                    </DialogContent>
                </Dialog>
            )}
        </div>
    )
}
