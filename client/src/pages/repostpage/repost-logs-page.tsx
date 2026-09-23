import {useState} from "react"
import {useSearchParams} from "react-router-dom"
import {AlertCircle, CheckCircle2, ExternalLink, Loader2, RefreshCw, SkipForward} from "lucide-react"
import {useGetApiV1RepostLogs, useGetApiV1RepostLogsSummary, useGetApiV1RepostSettings, useGetApiV1RepostSettingsId} from "@/api/endpoints/repost/repost"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent} from "@/components/ui/card"
import {Input} from "@/components/ui/input"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import type {RepostLogDto} from "@/api/endpoints/tgPosterAPI.schemas"
import {RepostLogReason, RepostStatus} from "@/api/endpoints/tgPosterAPI.schemas"

const PAGE_SIZE = 20
const ALL = "all"

const STATUS_LABELS: Record<RepostStatus, string> = {
    Pending: "Ожидает",
    Success: "Репост сделан",
    Failed: "Ошибка",
    Skipped: "Пропущено",
}

const REASON_LABELS: Record<RepostLogReason, string> = {
    None: "—",
    EveryNth: "Пропуск по «каждое N-е»",
    SkipProbability: "Случайный пропуск",
    DailyLimit: "Дневной лимит исчерпан",
    MessageNotPublished: "Пост не опубликован в источнике",
    DialogsUnavailable: "Сессия не отдала диалоги",
    SourceChannelNotResolved: "Канал-источник не найден",
    DestinationNotAvailable: "Аккаунт не состоит в канале",
    Banned: "Аккаунт забанен в канале",
    ForwardFailed: "Telegram отклонил пересылку",
    TopicClosed: "Тема форума закрыта",
}

function statusVariant(status: RepostStatus): "default" | "destructive" | "secondary" | "outline" {
    switch (status) {
        case RepostStatus.Success:
            return "default"
        case RepostStatus.Failed:
            return "destructive"
        case RepostStatus.Skipped:
            return "secondary"
        default:
            return "outline"
    }
}

function formatDateTime(value: string | null | undefined): string {
    if (!value) return "—"
    return new Date(value).toLocaleString("ru-RU")
}

function toIsoStart(value: string): string | undefined {
    return value ? new Date(`${value}T00:00:00`).toISOString() : undefined
}

function toIsoEnd(value: string): string | undefined {
    return value ? new Date(`${value}T23:59:59.999`).toISOString() : undefined
}

function messageLink(log: RepostLogDto): string | null {
    if (!log.telegramMessageId) return null
    if (log.destinationUsername) return `https://t.me/${log.destinationUsername}/${log.telegramMessageId}`
    return null
}

interface SummaryTileProps {
    label: string
    value: number
    icon: React.ReactNode
    accent: string
}

function SummaryTile({label, value, icon, accent}: SummaryTileProps) {
    return (
        <Card>
            <CardContent className="flex items-center gap-3 py-4">
                <div className={`flex h-9 w-9 items-center justify-center rounded-full ${accent}`}>
                    {icon}
                </div>
                <div>
                    <p className="text-2xl font-semibold leading-none">{value.toLocaleString()}</p>
                    <p className="text-xs text-muted-foreground mt-1">{label}</p>
                </div>
            </CardContent>
        </Card>
    )
}

function LogRow({log}: {log: RepostLogDto}) {
    const link = messageLink(log)
    const destination = log.destinationTitle ?? log.destinationUsername ?? String(log.destinationChatId)

    return (
        <div className="border rounded p-3 space-y-2 hover:bg-accent/40 transition-colors">
            <div className="flex flex-wrap items-center justify-between gap-2">
                <div className="flex items-center gap-2 min-w-0">
                    <Badge variant={statusVariant(log.status)}>{STATUS_LABELS[log.status]}</Badge>
                    <span className="text-sm font-medium truncate">{destination}</span>
                    {log.destinationUsername && (
                        <a
                            href={`https://t.me/${log.destinationUsername}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs text-muted-foreground hover:underline shrink-0"
                        >
                            @{log.destinationUsername}
                        </a>
                    )}
                </div>
                <div className="flex items-center gap-2 shrink-0">
                    <span className="text-xs text-muted-foreground">{formatDateTime(log.createdAt)}</span>
                    {link && (
                        <a href={link} target="_blank" rel="noopener noreferrer">
                            <Button variant="outline" size="sm" className="h-7 gap-1.5">
                                <ExternalLink className="h-3.5 w-3.5"/>
                                <span className="text-xs">Пост</span>
                            </Button>
                        </a>
                    )}
                </div>
            </div>

            <div className="text-xs text-muted-foreground">
                <span className="font-medium text-foreground">{log.scheduleName}</span>
                {" · источник "}
                <span>{log.sourceChannelName}</span>
                {" · пост от "}
                <span>{formatDateTime(log.messageTimePosting)}</span>
            </div>

            {log.messagePreview && (
                <p className="text-sm line-clamp-2">{log.messagePreview}</p>
            )}

            {log.reason !== RepostLogReason.None && (
                <div className="flex flex-wrap items-center gap-2">
                    <Badge variant="outline" className="text-xs">{REASON_LABELS[log.reason]}</Badge>
                    {log.error && (
                        <span className="text-xs text-muted-foreground break-all">{log.error}</span>
                    )}
                </div>
            )}
        </div>
    )
}

export function RepostLogsPage() {
    const [searchParams] = useSearchParams()
    const [settingsId, setSettingsId] = useState<string>(searchParams.get("settingsId") ?? ALL)
    const [destinationId, setDestinationId] = useState<string>(ALL)
    const [status, setStatus] = useState<string>(ALL)
    const [from, setFrom] = useState("")
    const [to, setTo] = useState("")
    const [page, setPage] = useState(1)

    const filters = {
        RepostSettingsId: settingsId === ALL ? undefined : settingsId,
        DestinationId: destinationId === ALL ? undefined : destinationId,
        Status: status === ALL ? undefined : (status as RepostStatus),
        From: toIsoStart(from),
        To: toIsoEnd(to),
    }

    const {data, isLoading, isFetching, refetch} = useGetApiV1RepostLogs({
        ...filters,
        PageNumber: page,
        PageSize: PAGE_SIZE,
    })

    const {data: summary, refetch: refetchSummary} = useGetApiV1RepostLogsSummary({
        RepostSettingsId: filters.RepostSettingsId,
        DestinationId: filters.DestinationId,
        From: filters.From,
        To: filters.To,
    })

    const {data: settingsList} = useGetApiV1RepostSettings()
    const {data: settingsDetails} = useGetApiV1RepostSettingsId(
        settingsId === ALL ? "" : settingsId,
        {query: {enabled: settingsId !== ALL}},
    )

    const logs = data?.data ?? []
    const totalPages = data?.totalPages ?? 1
    const destinations = settingsDetails?.destinations ?? []

    function handleSettingsChange(value: string) {
        setSettingsId(value)
        setDestinationId(ALL)
        setPage(1)
    }

    function handleDestinationChange(value: string) {
        setDestinationId(value)
        setPage(1)
    }

    function handleStatusChange(value: string) {
        setStatus(value)
        setPage(1)
    }

    function handleFromChange(e: React.ChangeEvent<HTMLInputElement>) {
        setFrom(e.target.value)
        setPage(1)
    }

    function handleToChange(e: React.ChangeEvent<HTMLInputElement>) {
        setTo(e.target.value)
        setPage(1)
    }

    function handleRefresh() {
        void refetch()
        void refetchSummary()
    }

    return (
        <div className="container mx-auto p-6 max-w-5xl">
            <div className="flex items-center justify-between gap-4 mb-6">
                <div>
                    <h1 className="text-2xl font-bold">Логи репостов</h1>
                    <p className="text-sm text-muted-foreground mt-1">
                        По каждому посту видно, в какой канал он ушёл, дошёл ли и что помешало
                    </p>
                </div>
                <Button variant="outline" size="sm" className="gap-1.5" onClick={handleRefresh}>
                    <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`}/>
                    Обновить
                </Button>
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
                <SummaryTile
                    label="Всего записей"
                    value={summary?.total ?? 0}
                    icon={<RefreshCw className="h-4 w-4"/>}
                    accent="bg-muted text-muted-foreground"
                />
                <SummaryTile
                    label="Репост сделан"
                    value={summary?.success ?? 0}
                    icon={<CheckCircle2 className="h-4 w-4"/>}
                    accent="bg-emerald-500/15 text-emerald-600"
                />
                <SummaryTile
                    label="Пропущено"
                    value={summary?.skipped ?? 0}
                    icon={<SkipForward className="h-4 w-4"/>}
                    accent="bg-amber-500/15 text-amber-600"
                />
                <SummaryTile
                    label="Ошибки"
                    value={summary?.failed ?? 0}
                    icon={<AlertCircle className="h-4 w-4"/>}
                    accent="bg-destructive/15 text-destructive"
                />
            </div>

            <Card className="mb-6">
                <CardContent className="pt-6 space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <Select value={settingsId} onValueChange={handleSettingsChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Все настройки репоста"/>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value={ALL}>Все настройки репоста</SelectItem>
                                {(settingsList?.items ?? []).map((item) => (
                                    <SelectItem key={item.id} value={item.id}>{item.scheduleName}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        <Select
                            value={destinationId}
                            onValueChange={handleDestinationChange}
                            disabled={settingsId === ALL}
                        >
                            <SelectTrigger>
                                <SelectValue placeholder="Все целевые каналы"/>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value={ALL}>Все целевые каналы</SelectItem>
                                {destinations.map((dest) => (
                                    <SelectItem key={dest.id} value={dest.id}>
                                        {dest.title ?? dest.username ?? String(dest.chatId)}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        <Select value={status} onValueChange={handleStatusChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Любой статус"/>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value={ALL}>Любой статус</SelectItem>
                                <SelectItem value={RepostStatus.Success}>{STATUS_LABELS.Success}</SelectItem>
                                <SelectItem value={RepostStatus.Failed}>{STATUS_LABELS.Failed}</SelectItem>
                                <SelectItem value={RepostStatus.Skipped}>{STATUS_LABELS.Skipped}</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-1">
                            <label className="text-xs text-muted-foreground">Период с</label>
                            <Input type="date" value={from} onChange={handleFromChange}/>
                        </div>
                        <div className="space-y-1">
                            <label className="text-xs text-muted-foreground">Период по</label>
                            <Input type="date" value={to} onChange={handleToChange}/>
                        </div>
                    </div>
                    {(summary?.reasons?.length ?? 0) > 0 && (
                        <div className="flex flex-wrap gap-2 pt-1">
                            {summary!.reasons.map((reason) => (
                                <Badge key={reason.reason} variant="outline" className="text-xs">
                                    {REASON_LABELS[reason.reason]}: {reason.count}
                                </Badge>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            {isLoading ? (
                <div className="space-y-3">
                    {Array.from({length: 5}).map((_, i) => (
                        <Skeleton key={i} className="h-24 w-full rounded"/>
                    ))}
                </div>
            ) : logs.length === 0 ? (
                <div className="text-center py-16 text-muted-foreground">
                    <Loader2 className="h-8 w-8 mx-auto mb-3 opacity-30"/>
                    <p>Записей нет. Логи появятся после ближайшего репоста.</p>
                </div>
            ) : (
                <div className="space-y-3">
                    {logs.map((log) => (
                        <LogRow key={log.id} log={log}/>
                    ))}
                </div>
            )}

            {totalPages > 1 && (
                <div className="flex items-center justify-center gap-2 mt-6">
                    <Button
                        variant="outline"
                        size="sm"
                        disabled={page === 1}
                        onClick={() => setPage((p) => p - 1)}
                    >
                        Назад
                    </Button>
                    <span className="text-sm text-muted-foreground px-2">
                        Страница {page} из {totalPages}
                    </span>
                    <Button
                        variant="outline"
                        size="sm"
                        disabled={page >= totalPages}
                        onClick={() => setPage((p) => p + 1)}
                    >
                        Вперёд
                    </Button>
                </div>
            )}
        </div>
    )
}
