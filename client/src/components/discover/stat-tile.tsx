import type {ReactNode} from "react"
import {Card, CardContent} from "@/components/ui/card"
import {cn} from "@/lib/utils"

interface StatTileProps {
    label: string
    value: number | string
    /** Вторая строка под значением: доля, пояснение, дата */
    hint?: ReactNode
    icon: ReactNode
    /** Tailwind-классы фона и цвета иконки, например "bg-emerald-100 text-emerald-700" */
    accent?: string
}

export function StatTile({label, value, hint, icon, accent = "bg-muted text-muted-foreground"}: StatTileProps) {
    return (
        <Card>
            <CardContent className="flex items-center gap-3 py-4">
                <div className={cn("flex h-9 w-9 shrink-0 items-center justify-center rounded-full", accent)}>
                    {icon}
                </div>
                <div className="min-w-0">
                    <p className="text-2xl font-semibold leading-none truncate">
                        {typeof value === "number" ? value.toLocaleString("ru-RU") : value}
                    </p>
                    <p className="text-xs text-muted-foreground mt-1 truncate">{label}</p>
                    {hint && <p className="text-xs text-muted-foreground truncate">{hint}</p>}
                </div>
            </CardContent>
        </Card>
    )
}
