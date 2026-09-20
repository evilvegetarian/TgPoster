import {useState} from "react"
import {format, parseISO} from "date-fns"
import {ru} from "date-fns/locale"
import {cn} from "@/lib/utils"
import type {DiscoverDailyCount} from "@/api/endpoints/tgPosterAPI.schemas"

interface DailyBarChartProps {
    data: DiscoverDailyCount[]
    /** Tailwind-класс цвета столбцов, например "bg-chart-2" */
    colorClass: string
    /** Подпись единицы в подсказке, например "спарсено" */
    unitLabel: string
    height?: number
}

function formatDay(date: string): string {
    return format(parseISO(date), "d MMM", {locale: ru})
}

function formatFullDay(date: string): string {
    return format(parseISO(date), "d MMMM yyyy", {locale: ru})
}

// Столбчатый график «по дням»: один ряд, один оттенок, hover подсвечивает столбец
// и выводит дату со значением над графиком
export function DailyBarChart({data, colorClass, unitLabel, height = 160}: DailyBarChartProps) {
    const [hovered, setHovered] = useState<number | null>(null)

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
                {data.map((d, i) => (
                    <div
                        key={d.date}
                        className="flex-1 h-full flex items-end justify-center min-w-0 cursor-default"
                        onMouseEnter={() => setHovered(i)}
                        title={`${formatFullDay(d.date)}: ${d.count.toLocaleString("ru-RU")} ${unitLabel}`}
                    >
                        <div
                            className={cn(
                                "w-full max-w-6 rounded-t-[4px] transition-opacity",
                                colorClass,
                                hovered != null && hovered !== i && "opacity-40",
                            )}
                            style={{
                                height: `${(d.count / max) * 100}%`,
                                minHeight: d.count > 0 ? 2 : 0,
                            }}
                        />
                    </div>
                ))}
            </div>
            <div className="flex justify-between mt-1 text-[10px] text-muted-foreground">
                <span>{formatDay(data[0].date)}</span>
                {data.length > 2 && <span>{formatDay(data[Math.floor(data.length / 2)].date)}</span>}
                <span>{formatDay(data[data.length - 1].date)}</span>
            </div>
        </div>
    )
}
