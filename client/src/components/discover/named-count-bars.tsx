import {useState, type ReactNode} from "react"
import {Button} from "@/components/ui/button"
import {cn} from "@/lib/utils"

export interface NamedCountBarItem {
    key: string
    label: ReactNode
    count: number
    /** Tailwind-класс цвета заливки конкретной строки (для статусов) */
    colorClass?: string
}

interface NamedCountBarsProps {
    items: NamedCountBarItem[]
    /** База для процентов; по умолчанию — сумма всех элементов */
    total?: number
    /** Сколько строк показывать до нажатия «показать все» */
    limit?: number
    /** Цвет заливки по умолчанию */
    colorClass?: string
    emptyText?: string
}

// Горизонтальные полосы «название — доля — число»: одна шкала, один оттенок,
// длинный хвост прячется за кнопкой «показать все»
export function NamedCountBars({
    items,
    total,
    limit,
    colorClass = "bg-chart-2",
    emptyText = "Нет данных",
}: NamedCountBarsProps) {
    const [expanded, setExpanded] = useState(false)

    if (items.length === 0) {
        return <p className="text-sm text-muted-foreground py-2">{emptyText}</p>
    }

    const base = total ?? items.reduce((sum, item) => sum + item.count, 0)
    const max = Math.max(1, ...items.map((item) => item.count))
    const visible = limit != null && !expanded ? items.slice(0, limit) : items
    const hiddenCount = items.length - visible.length

    return (
        <div className="space-y-2">
            {visible.map((item) => {
                const percent = base > 0 ? (item.count / base) * 100 : 0
                return (
                    <div key={item.key} className="grid grid-cols-[minmax(0,9rem)_1fr_auto] items-center gap-3 text-sm">
                        <div className="truncate" title={typeof item.label === "string" ? item.label : undefined}>
                            {item.label}
                        </div>
                        <div className="h-2 rounded-full bg-muted overflow-hidden">
                            <div
                                className={cn("h-full rounded-full", item.colorClass ?? colorClass)}
                                style={{width: `${(item.count / max) * 100}%`}}
                            />
                        </div>
                        <div className="text-right tabular-nums whitespace-nowrap">
                            <span className="font-medium">{item.count.toLocaleString("ru-RU")}</span>
                            <span className="text-xs text-muted-foreground ml-1.5">{percent.toFixed(percent >= 10 ? 0 : 1)}%</span>
                        </div>
                    </div>
                )
            })}
            {(hiddenCount > 0 || expanded) && (
                <Button variant="ghost" size="sm" className="h-7 px-2 text-xs" onClick={() => setExpanded((v) => !v)}>
                    {expanded ? "Свернуть" : `Показать все (ещё ${hiddenCount})`}
                </Button>
            )}
        </div>
    )
}
