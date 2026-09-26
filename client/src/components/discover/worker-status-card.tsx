import type {ReactNode} from "react"
import {AlertCircle, Clock, Loader2, PauseCircle} from "lucide-react"
import {Card, CardContent} from "@/components/ui/card"
import {Progress} from "@/components/ui/progress"
import {formatDateTime, formatRelative} from "@/components/discover/format"
import type {DiscoverJobStatus, DiscoverStatusResponse} from "@/api/endpoints/tgPosterAPI.schemas"

const JOB_STATUS_META: Record<DiscoverJobStatus, {label: string; accent: string; icon: ReactNode}> = {
    Idle: {label: "Ожидает следующего запуска", accent: "bg-muted text-muted-foreground", icon: <Clock className="h-4 w-4"/>},
    Running: {label: "Выполняется", accent: "bg-sky-100 text-sky-700", icon: <Loader2 className="h-4 w-4 animate-spin"/>},
    CooldownWait: {label: "Пауза: Telegram ограничил сессии", accent: "bg-amber-100 text-amber-700", icon: <PauseCircle className="h-4 w-4"/>},
    Failed: {label: "Последний запуск завершился ошибкой", accent: "bg-red-100 text-red-700", icon: <AlertCircle className="h-4 w-4"/>},
    Unknown: {label: "Нет сигнала от воркера", accent: "bg-amber-100 text-amber-700", icon: <AlertCircle className="h-4 w-4"/>},
}

interface WorkerStatusCardProps {
    /** Название воркера перед статусом, например «Воркер парсинга» */
    title: string
    status: DiscoverStatusResponse
}

// Состояние фоновой задачи: статус, прогресс текущего запуска и расписание
export function WorkerStatusCard({title, status}: WorkerStatusCardProps) {
    const meta = JOB_STATUS_META[status.status]
    const hasProgress = status.status === "Running" && status.progressTotal != null && status.progressTotal > 0
    const progressPercent = hasProgress
        ? Math.round(((status.progressCurrent ?? 0) / status.progressTotal!) * 100)
        : 0

    return (
        <Card>
            <CardContent className="pt-6 space-y-3">
                <div className="flex flex-wrap items-center justify-between gap-3">
                    <div className="flex items-center gap-3 min-w-0">
                        <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full ${meta.accent}`}>
                            {meta.icon}
                        </div>
                        <div className="min-w-0">
                            <p className="text-sm font-medium">{title}: {meta.label}</p>
                            {status.status === "Running" && status.progressMessage && (
                                <p className="text-xs text-muted-foreground truncate">{status.progressMessage}</p>
                            )}
                            {status.status === "CooldownWait" && status.cooldownUntil && (
                                <p className="text-xs text-muted-foreground">
                                    До {formatDateTime(status.cooldownUntil)} ({formatRelative(status.cooldownUntil)})
                                </p>
                            )}
                            {status.status === "Failed" && status.lastError && (
                                <p className="text-xs text-red-600 truncate" title={status.lastError}>{status.lastError}</p>
                            )}
                        </div>
                    </div>
                    <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
                        <span>Начат: {formatDateTime(status.lastStartedAt)}</span>
                        <span>Завершён: {formatDateTime(status.lastFinishedAt)}</span>
                        <span>Следующий: {formatDateTime(status.nextRunAt)}</span>
                    </div>
                </div>
                {hasProgress && (
                    <div className="space-y-1">
                        <Progress value={progressPercent}/>
                        <p className="text-xs text-muted-foreground">
                            {status.progressCurrent ?? 0} из {status.progressTotal} каналов
                        </p>
                    </div>
                )}
            </CardContent>
        </Card>
    )
}
