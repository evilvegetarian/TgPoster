import {useState} from "react"
import {Link} from "react-router-dom"
import {
    CheckCircle2,
    Clock,
    Database,
    ExternalLink,
    FileText,
    Filter,
    Inbox,
    ListChecks,
    Loader2,
    MessageCircleMore,
    RefreshCw,
    Sparkles,
} from "lucide-react"
import {
    useGetApiV1ParseChannel,
    useGetApiV1ParseChannelRuns,
    useGetApiV1ParseChannelStats,
    useGetApiV1ParseChannelStatus,
} from "@/api/endpoints/parse-channel/parse-channel"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Input} from "@/components/ui/input"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import {DailyBarChart} from "@/components/discover/daily-bar-chart"
import {formatDateTime, formatRelative, percent, toIsoEnd, toIsoStart} from "@/components/discover/format"
import {NamedCountBars, type NamedCountBarItem} from "@/components/discover/named-count-bars"
import {StatTile} from "@/components/discover/stat-tile"
import {WorkerStatusCard} from "@/components/discover/worker-status-card"
import {
    ParseChannelRunStatus,
    ParseChannelSourceStatus,
    type ParseChannelRunResponse,
    type ParseChannelRunTotals,
    type ParseChannelSourceStat,
    type ParseChannelStatsResponse,
} from "@/api/endpoints/tgPosterAPI.schemas"

const HISTORY_PAGE_SIZE = 20
const ALL = "all"

const PERIOD_OPTIONS: ReadonlyArray<{value: string; label: string}> = [
    {value: "7", label: "7 дней"},
    {value: "14", label: "14 дней"},
    {value: "30", label: "30 дней"},
    {value: "90", label: "90 дней"},
]

type BadgeVariant = "default" | "secondary" | "destructive" | "outline"

const SOURCE_STATUS_META: Record<ParseChannelSourceStatus, {label: string; colorClass: string; variant: BadgeVariant}> = {
    New: {label: "Новый", colorClass: "bg-slate-400", variant: "outline"},
    InHandle: {label: "Парсится", colorClass: "bg-sky-500", variant: "secondary"},
    Waiting: {label: "Ожидает", colorClass: "bg-emerald-500", variant: "default"},
    Finished: {label: "Закончен", colorClass: "bg-teal-600", variant: "secondary"},
    Canceled: {label: "Отменён", colorClass: "bg-amber-500", variant: "outline"},
    Failed: {label: "Ошибка", colorClass: "bg-red-500", variant: "destructive"},
}

const RUN_STATUS_META: Record<ParseChannelRunStatus, {label: string; variant: BadgeVariant}> = {
    Success: {label: "Успешно", variant: "default"},
    TemporaryFailure: {label: "Временный сбой", variant: "secondary"},
    SourceUnavailable: {label: "Канал недоступен", variant: "destructive"},
    Failed: {label: "Ошибка", variant: "destructive"},
}

function skippedTotal(totals: ParseChannelRunTotals): number {
    return totals.skippedByAvoidWords + totals.skippedAsAds + totals.skippedAsDuplicates + totals.skippedEmpty
}

function channelUrl(channel: string): string {
    return channel.startsWith("http") ? channel : `https://t.me/${channel.replace(/^@/, "")}`
}

function formatDuration(startedAt: string, finishedAt: string): string {
    const seconds = Math.max(0, Math.round((new Date(finishedAt).getTime() - new Date(startedAt).getTime()) / 1000))
    if (seconds < 60) return `${seconds} с`
    const minutes = Math.floor(seconds / 60)
    return minutes < 60 ? `${minutes} мин ${seconds % 60} с` : `${Math.floor(minutes / 60)} ч ${minutes % 60} мин`
}

function StatsTiles({stats}: {stats: ParseChannelStatsResponse}) {
    const {sources, posts, period} = stats
    const skipped = skippedTotal(period)
    return (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <StatTile
                label="Источников"
                value={sources.total}
                hint={`активных ${sources.active.toLocaleString("ru-RU")} · с ошибкой ${sources.failed.toLocaleString("ru-RU")}`}
                icon={<Database className="h-4 w-4"/>}
                accent={sources.failed > 0 ? "bg-red-100 text-red-700" : undefined}
            />
            <StatTile
                label="Новых постов за 24 часа"
                value={stats.last24Hours.published}
                hint={`за 7 дней ${stats.last7Days.published.toLocaleString("ru-RU")} · за 30 дней ${stats.last30Days.published.toLocaleString("ru-RU")}`}
                icon={<Sparkles className="h-4 w-4"/>}
                accent="bg-teal-100 text-teal-700"
            />
            <StatTile
                label="Отсеяно за период"
                value={skipped}
                hint={`реклама ${period.skippedAsAds.toLocaleString("ru-RU")} · стоп-слова ${period.skippedByAvoidWords.toLocaleString("ru-RU")} · дубли ${period.skippedAsDuplicates.toLocaleString("ru-RU")}`}
                icon={<Filter className="h-4 w-4"/>}
                accent="bg-orange-100 text-orange-700"
            />
            <StatTile
                label="Проходов за период"
                value={period.runs}
                hint={`неудачных ${period.failedRuns.toLocaleString("ru-RU")}`}
                icon={<ListChecks className="h-4 w-4"/>}
                accent={period.failedRuns > 0 ? "bg-red-100 text-red-700" : undefined}
            />
            <StatTile
                label="Получено сообщений"
                value={period.fetchedMessages}
                hint={`за период · собрано постов ${period.posts.toLocaleString("ru-RU")}`}
                icon={<Inbox className="h-4 w-4"/>}
            />
            <StatTile
                label="Постов из парсинга"
                value={posts.total}
                hint={`опубликовано ${posts.published.toLocaleString("ru-RU")} · в очереди ${posts.planned.toLocaleString("ru-RU")}`}
                icon={<FileText className="h-4 w-4"/>}
            />
            <StatTile
                label="Ждут подтверждения"
                value={posts.awaitingApproval}
                hint={posts.awaitingApproval > 0
                    ? <Link to="/approve-messages" className="underline underline-offset-2">открыть подтверждение</Link>
                    : "из спарсенных постов"}
                icon={<CheckCircle2 className="h-4 w-4"/>}
                accent={posts.awaitingApproval > 0 ? "bg-amber-100 text-amber-700" : undefined}
            />
            <StatTile
                label="Последний проход"
                value={stats.lastRunAt ? formatRelative(stats.lastRunAt) : "—"}
                hint={stats.lastRunAt ? formatDateTime(stats.lastRunAt) : "журнал пока пуст"}
                icon={<Clock className="h-4 w-4"/>}
            />
        </div>
    )
}

function Breakdowns({stats}: {stats: ParseChannelStatsResponse}) {
    const {posts, period} = stats

    const sourceItems: NamedCountBarItem[] = stats.sourcesByStatus.map((item) => {
        const meta = SOURCE_STATUS_META[item.status]
        return {
            key: item.status,
            label: (
                <span className="flex items-center gap-2">
                    <span className={`h-2 w-2 rounded-full shrink-0 ${meta.colorClass}`}/>
                    {meta.label}
                </span>
            ),
            count: item.count,
            colorClass: meta.colorClass,
        }
    })

    const postItems: NamedCountBarItem[] = [
        {key: "planned", label: "В очереди", count: posts.planned, colorClass: "bg-sky-500"},
        {key: "awaiting", label: "Ждут подтверждения", count: posts.awaitingApproval, colorClass: "bg-amber-500"},
        {key: "published", label: "Опубликованы", count: posts.published, colorClass: "bg-emerald-500"},
        {key: "failed", label: "Ошибка отправки", count: posts.failed, colorClass: "bg-red-500"},
        {key: "canceled", label: "Отменены", count: posts.canceled, colorClass: "bg-slate-400"},
    ]

    const skipItems: NamedCountBarItem[] = [
        {key: "duplicates", label: "Уже спарсены раньше", count: period.skippedAsDuplicates},
        {key: "ads", label: "Реклама", count: period.skippedAsAds},
        {key: "avoid", label: "Стоп-слова", count: period.skippedByAvoidWords},
        {key: "empty", label: "Без текста и медиа", count: period.skippedEmpty},
    ].filter((item) => item.count > 0)

    return (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <Card>
                <CardHeader className="pb-3">
                    <CardTitle className="text-base">Источники по статусу</CardTitle>
                </CardHeader>
                <CardContent>
                    <NamedCountBars items={sourceItems} total={stats.sources.total}/>
                </CardContent>
            </Card>
            <Card>
                <CardHeader className="pb-3">
                    <CardTitle className="text-base">Посты по состоянию</CardTitle>
                    <p className="text-xs text-muted-foreground">Неудалённые посты, созданные парсингом</p>
                </CardHeader>
                <CardContent>
                    <NamedCountBars items={postItems} total={posts.total}/>
                </CardContent>
            </Card>
            <Card>
                <CardHeader className="pb-3">
                    <CardTitle className="text-base">Почему отсеяно</CardTitle>
                    <p className="text-xs text-muted-foreground">
                        За период · {percent(skippedTotal(period), period.posts)} от собранных постов
                    </p>
                </CardHeader>
                <CardContent>
                    <NamedCountBars
                        items={skipItems}
                        total={period.posts}
                        emptyText="За период ничего не отсеяно"
                    />
                </CardContent>
            </Card>
        </div>
    )
}

function TopSources({sources}: {sources: ParseChannelSourceStat[]}) {
    const max = Math.max(1, ...sources.map((s) => s.published))

    return (
        <Card>
            <CardHeader className="pb-3">
                <CardTitle className="text-base">Самые продуктивные источники</CardTitle>
                <p className="text-xs text-muted-foreground">Больше всего новых постов за период</p>
            </CardHeader>
            <CardContent>
                {sources.length === 0 ? (
                    <p className="text-sm text-muted-foreground py-2">Источников парсинга пока нет</p>
                ) : (
                    <div className="space-y-3">
                        {sources.map((source, index) => {
                            const statusMeta = SOURCE_STATUS_META[source.status]
                            const lastRunMeta = source.lastRunStatus ? RUN_STATUS_META[source.lastRunStatus] : null
                            return (
                                <div key={source.id} className="flex items-center gap-3">
                                    <span className="w-5 text-xs text-muted-foreground text-right tabular-nums">{index + 1}</span>
                                    <div className="flex-1 min-w-0">
                                        <div className="flex flex-wrap items-center gap-2 min-w-0">
                                            <a
                                                href={channelUrl(source.channel)}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="font-medium truncate hover:underline"
                                            >
                                                {source.channel}
                                            </a>
                                            <Badge variant={statusMeta.variant} className="shrink-0">{statusMeta.label}</Badge>
                                            <span className="text-xs text-muted-foreground truncate">{source.scheduleName}</span>
                                        </div>
                                        <div className="h-1.5 rounded-full bg-muted overflow-hidden mt-1.5">
                                            <div
                                                className="h-full rounded-full bg-chart-1"
                                                style={{width: `${(source.published / max) * 100}%`}}
                                            />
                                        </div>
                                        <p className="text-xs text-muted-foreground mt-1">
                                            отсеяно {source.skipped.toLocaleString("ru-RU")} · проходов {source.runs}
                                            {source.failedRuns > 0 && <span className="text-red-600"> (неудачных {source.failedRuns})</span>}
                                            {" · "}спаршено {source.parsedMessagesCount.toLocaleString("ru-RU")}
                                            {source.totalMessagesCount != null && ` / ${source.totalMessagesCount.toLocaleString("ru-RU")}`}
                                        </p>
                                    </div>
                                    <div className="text-right shrink-0">
                                        <p className="font-medium tabular-nums">{source.published.toLocaleString("ru-RU")}</p>
                                        <p className="text-xs text-muted-foreground" title={formatDateTime(source.lastRunAt)}>
                                            {source.lastRunAt ? formatRelative(source.lastRunAt) : "не парсился"}
                                        </p>
                                        {lastRunMeta && source.lastRunStatus !== ParseChannelRunStatus.Success && (
                                            <p className="text-xs text-red-600">{lastRunMeta.label}</p>
                                        )}
                                    </div>
                                </div>
                            )
                        })}
                    </div>
                )}
            </CardContent>
        </Card>
    )
}

function RunRow({run}: {run: ParseChannelRunResponse}) {
    const statusMeta = RUN_STATUS_META[run.status]
    const skipped = run.skippedByAvoidWords + run.skippedAsAds + run.skippedAsDuplicates + run.skippedEmpty
    const skippedTitle = `реклама ${run.skippedAsAds} · стоп-слова ${run.skippedByAvoidWords} · дубли ${run.skippedAsDuplicates} · пустые ${run.skippedEmpty}`

    return (
        <tr className="border-b last:border-b-0 hover:bg-accent/40 transition-colors align-top">
            <td className="py-2.5 pr-3">
                <a
                    href={channelUrl(run.channel)}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-medium hover:underline inline-flex items-center gap-1"
                >
                    {run.channel}
                    <ExternalLink className="h-3 w-3 text-muted-foreground"/>
                </a>
                <p className="text-xs text-muted-foreground">{run.scheduleName}</p>
            </td>
            <td className="py-2.5 px-3 whitespace-nowrap">
                <p className="text-sm">{formatDateTime(run.startedAt)}</p>
                <p className="text-xs text-muted-foreground">
                    {formatRelative(run.startedAt)} · {formatDuration(run.startedAt, run.finishedAt)}
                </p>
            </td>
            <td className="py-2.5 px-3 text-right tabular-nums">{run.fetchedMessages.toLocaleString("ru-RU")}</td>
            <td className="py-2.5 px-3 text-right tabular-nums">{run.posts.toLocaleString("ru-RU")}</td>
            <td className="py-2.5 px-3 text-right tabular-nums font-medium">{run.published.toLocaleString("ru-RU")}</td>
            <td className="py-2.5 px-3 text-right tabular-nums text-muted-foreground" title={skippedTitle}>
                {skipped.toLocaleString("ru-RU")}
            </td>
            <td className="py-2.5 px-3 max-w-[16rem]">
                <Badge variant={statusMeta.variant}>{statusMeta.label}</Badge>
                {run.error && (
                    <p className="text-xs text-red-600 truncate mt-1" title={run.error}>{run.error}</p>
                )}
            </td>
            <td className="py-2.5 pl-3 text-right tabular-nums text-muted-foreground">
                {run.lastMessageId != null ? run.lastMessageId.toLocaleString("ru-RU") : "—"}
            </td>
        </tr>
    )
}

function RunsHistory() {
    const [sourceId, setSourceId] = useState(ALL)
    const [status, setStatus] = useState(ALL)
    const [from, setFrom] = useState("")
    const [to, setTo] = useState("")
    const [page, setPage] = useState(1)

    const {data: sources} = useGetApiV1ParseChannel()
    const {data, isLoading, isFetching} = useGetApiV1ParseChannelRuns({
        ParseChannelId: sourceId === ALL ? undefined : sourceId,
        Status: status === ALL ? undefined : (status as ParseChannelRunStatus),
        From: toIsoStart(from),
        To: toIsoEnd(to),
        PageNumber: page,
        PageSize: HISTORY_PAGE_SIZE,
    })

    const items = data?.data ?? []
    const totalPages = data?.totalPages ?? 1
    const totalCount = data?.totalCount ?? 0
    const hasFilters = sourceId !== ALL || status !== ALL || from !== "" || to !== ""

    const resetPage = () => setPage(1)

    return (
        <Card>
            <CardHeader className="pb-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                    <div>
                        <CardTitle className="text-base">Журнал парсинга</CardTitle>
                        <p className="text-xs text-muted-foreground">
                            Каждый проход источника, от самых свежих
                            {totalCount > 0 && ` · всего ${totalCount.toLocaleString("ru-RU")}`}
                        </p>
                    </div>
                    {isFetching && !isLoading && <Loader2 className="h-4 w-4 animate-spin text-muted-foreground"/>}
                </div>
                <div className="grid grid-cols-1 md:grid-cols-4 gap-3 mt-3">
                    <Select value={sourceId} onValueChange={(value) => {
                        setSourceId(value)
                        resetPage()
                    }}>
                        <SelectTrigger aria-label="Источник">
                            <SelectValue placeholder="Источник"/>
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value={ALL}>Все источники</SelectItem>
                            {(sources?.items ?? []).map((source) => (
                                <SelectItem key={source.id} value={source.id}>{source.channel}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                    <Select value={status} onValueChange={(value) => {
                        setStatus(value)
                        resetPage()
                    }}>
                        <SelectTrigger aria-label="Итог прохода">
                            <SelectValue placeholder="Итог"/>
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value={ALL}>Любой итог</SelectItem>
                            {Object.values(ParseChannelRunStatus).map((value) => (
                                <SelectItem key={value} value={value}>{RUN_STATUS_META[value].label}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                    <Input
                        type="date"
                        value={from}
                        max={to || undefined}
                        onChange={(e) => {
                            setFrom(e.target.value)
                            resetPage()
                        }}
                        aria-label="Проход с"
                    />
                    <Input
                        type="date"
                        value={to}
                        min={from || undefined}
                        onChange={(e) => {
                            setTo(e.target.value)
                            resetPage()
                        }}
                        aria-label="Проход по"
                    />
                </div>
            </CardHeader>
            <CardContent>
                {isLoading ? (
                    <div className="space-y-2">
                        {Array.from({length: 5}).map((_, i) => (
                            <Skeleton key={i} className="h-12 w-full"/>
                        ))}
                    </div>
                ) : items.length === 0 ? (
                    <p className="text-sm text-muted-foreground py-6 text-center">
                        {hasFilters ? "Под фильтр ничего не попало" : "Журнал пока пуст: он заполняется с каждым проходом источника"}
                    </p>
                ) : (
                    <div className="overflow-x-auto -mx-6 px-6">
                        <table className="w-full text-sm">
                            <thead>
                            <tr className="border-b text-xs text-muted-foreground text-left">
                                <th className="py-2 pr-3 font-medium">Источник</th>
                                <th className="py-2 px-3 font-medium">Проход</th>
                                <th className="py-2 px-3 font-medium text-right">Получено</th>
                                <th className="py-2 px-3 font-medium text-right">Постов</th>
                                <th className="py-2 px-3 font-medium text-right">Новых</th>
                                <th className="py-2 px-3 font-medium text-right">Отсеяно</th>
                                <th className="py-2 px-3 font-medium">Итог</th>
                                <th className="py-2 pl-3 font-medium text-right">Последний msg id</th>
                            </tr>
                            </thead>
                            <tbody>
                            {items.map((run) => (
                                <RunRow key={run.id} run={run}/>
                            ))}
                            </tbody>
                        </table>
                    </div>
                )}
                {totalPages > 1 && (
                    <div className="flex items-center justify-center gap-2 mt-4">
                        <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage((p) => p - 1)}>
                            Назад
                        </Button>
                        <span className="text-sm text-muted-foreground px-2">
                            Страница {page} из {totalPages}
                        </span>
                        <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                            Вперёд
                        </Button>
                    </div>
                )}
            </CardContent>
        </Card>
    )
}

function StatsSkeleton() {
    return (
        <div className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
                {Array.from({length: 8}).map((_, i) => (
                    <Skeleton key={i} className="h-20 w-full"/>
                ))}
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <Skeleton className="h-56 w-full"/>
                <Skeleton className="h-56 w-full"/>
            </div>
        </div>
    )
}

export function ParseChannelStatsPage() {
    const [days, setDays] = useState("30")

    const {data: stats, isLoading, isFetching, refetch} = useGetApiV1ParseChannelStats({Days: Number(days)})
    const {data: status} = useGetApiV1ParseChannelStatus({query: {refetchInterval: 15_000}})

    return (
        <div className="container mx-auto p-6 max-w-6xl space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold">Статистика парсинга</h1>
                    <p className="text-sm text-muted-foreground mt-1">
                        Сколько новых постов приносят источники, что отсеивается и где парсинг ломается
                    </p>
                </div>
                <div className="flex items-center gap-2">
                    <Select value={days} onValueChange={setDays}>
                        <SelectTrigger className="w-[130px]">
                            <SelectValue placeholder="Период"/>
                        </SelectTrigger>
                        <SelectContent>
                            {PERIOD_OPTIONS.map((opt) => (
                                <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                    <Button variant="outline" size="sm" className="gap-1.5" onClick={() => void refetch()} disabled={isFetching}>
                        <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`}/>
                        Обновить
                    </Button>
                    <Button variant="outline" size="sm" className="gap-1.5" asChild>
                        <Link to="/parse-channel">
                            <MessageCircleMore className="h-3.5 w-3.5"/>
                            К источникам
                        </Link>
                    </Button>
                </div>
            </div>

            {status && <WorkerStatusCard title="Воркер парсинга" status={status}/>}

            {isLoading || !stats ? (
                <StatsSkeleton/>
            ) : (
                <>
                    <StatsTiles stats={stats}/>

                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                        <Card>
                            <CardHeader className="pb-3">
                                <CardTitle className="text-base">Новых постов по дням</CardTitle>
                                <p className="text-xs text-muted-foreground">Сколько постов парсинг отправил в очередь</p>
                            </CardHeader>
                            <CardContent>
                                <DailyBarChart data={stats.publishedByDay} colorClass="bg-chart-1" unitLabel="новых"/>
                            </CardContent>
                        </Card>
                        <Card>
                            <CardHeader className="pb-3">
                                <CardTitle className="text-base">Отсеяно по дням</CardTitle>
                                <p className="text-xs text-muted-foreground">Реклама, стоп-слова, дубли и пустые сообщения</p>
                            </CardHeader>
                            <CardContent>
                                <DailyBarChart data={stats.skippedByDay} colorClass="bg-chart-2" unitLabel="отсеяно"/>
                            </CardContent>
                        </Card>
                    </div>

                    <Breakdowns stats={stats}/>

                    <TopSources sources={stats.topSources}/>
                </>
            )}

            <RunsHistory/>
        </div>
    )
}
