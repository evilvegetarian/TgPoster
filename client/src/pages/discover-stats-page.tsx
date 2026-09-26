import {useState} from "react"
import {Link} from "react-router-dom"
import {
    Ban,
    CheckCircle2,
    Clock,
    Database,
    ExternalLink,
    Loader2,
    RefreshCw,
    Search,
    Sparkles,
    Tag,
    Tags,
    Telescope,
    Users,
} from "lucide-react"
import {
    useGetApiV1DiscoverParseHistory,
    useGetApiV1DiscoverStats,
    useGetApiV1DiscoverStatus,
} from "@/api/endpoints/discover/discover"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Input} from "@/components/ui/input"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import {ChannelAvatar, ChannelName} from "@/components/discover/channel-identity"
import {DailyBarChart} from "@/components/discover/daily-bar-chart"
import {
    compactNumber,
    channelLink,
    formatDateTime,
    formatRelative,
    percent,
    toIsoEnd,
    toIsoStart,
} from "@/components/discover/format"
import {NamedCountBars, type NamedCountBarItem} from "@/components/discover/named-count-bars"
import {StatTile} from "@/components/discover/stat-tile"
import {WorkerStatusCard} from "@/components/discover/worker-status-card"
import {useDebounce} from "@/hooks/use-debounce"
import {
    DiscoverChannelStatus,
    DiscoverParticipantsBucket,
    type DiscoverParseHistoryItemResponse,
    type DiscoverSourceStat,
    type DiscoverStatsResponse,
} from "@/api/endpoints/tgPosterAPI.schemas"

const HISTORY_PAGE_SIZE = 20

const PERIOD_OPTIONS: ReadonlyArray<{value: string; label: string}> = [
    {value: "7", label: "7 дней"},
    {value: "14", label: "14 дней"},
    {value: "30", label: "30 дней"},
    {value: "90", label: "90 дней"},
]

const CHANNEL_STATUS_META: Record<DiscoverChannelStatus, {label: string; colorClass: string; variant: "default" | "secondary" | "destructive" | "outline"}> = {
    Pending: {label: "Ожидает", colorClass: "bg-slate-400", variant: "outline"},
    InProgress: {label: "В работе", colorClass: "bg-sky-500", variant: "secondary"},
    Completed: {label: "Спарсен", colorClass: "bg-emerald-500", variant: "default"},
    Error: {label: "Ошибка", colorClass: "bg-red-500", variant: "destructive"},
    Skipped: {label: "Пропущен", colorClass: "bg-amber-500", variant: "secondary"},
}

const BUCKET_LABELS: Record<DiscoverParticipantsBucket, string> = {
    Unknown: "Неизвестно",
    UpTo1K: "до 1 тыс.",
    From1KTo10K: "1–10 тыс.",
    From10KTo100K: "10–100 тыс.",
    Over100K: "100 тыс. и больше",
}

const PEER_TYPE_LABELS: Record<string, string> = {
    channel: "Каналы",
    chat: "Чаты",
    unknown: "Не определён",
}

function StatsTiles({stats}: {stats: DiscoverStatsResponse}) {
    const {totals, freshness} = stats
    return (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <StatTile
                label="Всего каналов"
                value={totals.total}
                hint={`публичных ${totals.public.toLocaleString("ru-RU")} · приватных ${totals.private.toLocaleString("ru-RU")}`}
                icon={<Database className="h-4 w-4"/>}
            />
            <StatTile
                label="Спарсено ссылок"
                value={totals.parsed}
                hint={`${percent(totals.parsed, totals.total)} от всех · в очереди ${totals.notParsed.toLocaleString("ru-RU")}`}
                icon={<CheckCircle2 className="h-4 w-4"/>}
                accent="bg-teal-100 text-teal-700"
            />
            <StatTile
                label="Спарсено за 24 часа"
                value={freshness.parsedLast24Hours}
                hint={`за 7 дней ${freshness.parsedLast7Days.toLocaleString("ru-RU")} · за 30 дней ${freshness.parsedLast30Days.toLocaleString("ru-RU")}`}
                icon={<Telescope className="h-4 w-4"/>}
                accent="bg-teal-100 text-teal-700"
            />
            <StatTile
                label="Найдено новых за 24 часа"
                value={freshness.foundLast24Hours}
                hint={`за 7 дней ${freshness.foundLast7Days.toLocaleString("ru-RU")} · за 30 дней ${freshness.foundLast30Days.toLocaleString("ru-RU")}`}
                icon={<Sparkles className="h-4 w-4"/>}
                accent="bg-orange-100 text-orange-700"
            />
            <StatTile
                label="Суммарная аудитория"
                value={compactNumber.format(totals.totalParticipants)}
                hint={`известна у ${totals.withParticipants.toLocaleString("ru-RU")} каналов`}
                icon={<Users className="h-4 w-4"/>}
            />
            <StatTile
                label="С тематикой"
                value={totals.classified}
                hint={`${percent(totals.classified, totals.total)} от всех`}
                icon={<Tag className="h-4 w-4"/>}
            />
            <StatTile
                label="Забанено"
                value={totals.banned}
                hint="скрыты из списка Discover"
                icon={<Ban className="h-4 w-4"/>}
                accent={totals.banned > 0 ? "bg-red-100 text-red-700" : undefined}
            />
            <StatTile
                label="Последний парсинг"
                value={freshness.lastParsedAt ? formatRelative(freshness.lastParsedAt) : "—"}
                hint={freshness.lastParsedAt ? formatDateTime(freshness.lastParsedAt) : "ещё не запускался"}
                icon={<Clock className="h-4 w-4"/>}
            />
        </div>
    )
}

function Breakdowns({stats}: {stats: DiscoverStatsResponse}) {
    const total = stats.totals.total

    const statusItems: NamedCountBarItem[] = stats.byStatus.map((item) => {
        const meta = CHANNEL_STATUS_META[item.status]
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

    const peerTypeItems: NamedCountBarItem[] = stats.byPeerType.map((item) => ({
        key: item.name,
        label: PEER_TYPE_LABELS[item.name] ?? item.name,
        count: item.count,
    }))

    const bucketItems: NamedCountBarItem[] = stats.byParticipants.map((item) => ({
        key: item.bucket,
        label: BUCKET_LABELS[item.bucket],
        count: item.count,
    }))

    const categoryItems: NamedCountBarItem[] = stats.byCategory.map((item) => ({
        key: item.name,
        label: item.name,
        count: item.count,
    }))

    const languageItems: NamedCountBarItem[] = stats.byLanguage.map((item) => ({
        key: item.name,
        label: item.name.toUpperCase(),
        count: item.count,
    }))

    const unclassified = total - stats.totals.classified
    const withoutLanguage = total - stats.byLanguage.reduce((sum, item) => sum + item.count, 0)

    return (
        <>
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">По статусу обработки</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <NamedCountBars items={statusItems} total={total}/>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">По типу</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <NamedCountBars items={peerTypeItems} total={total}/>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">По размеру аудитории</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <NamedCountBars items={bucketItems} total={total}/>
                    </CardContent>
                </Card>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">По тематике</CardTitle>
                        {unclassified > 0 && (
                            <p className="text-xs text-muted-foreground">
                                Без тематики: {unclassified.toLocaleString("ru-RU")} ({percent(unclassified, total)})
                            </p>
                        )}
                    </CardHeader>
                    <CardContent>
                        <NamedCountBars
                            items={categoryItems}
                            total={total}
                            limit={10}
                            emptyText="Ни один канал ещё не классифицирован"
                        />
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">По языку</CardTitle>
                        {withoutLanguage > 0 && (
                            <p className="text-xs text-muted-foreground">
                                Язык не определён: {withoutLanguage.toLocaleString("ru-RU")} ({percent(withoutLanguage, total)})
                            </p>
                        )}
                    </CardHeader>
                    <CardContent>
                        <NamedCountBars
                            items={languageItems}
                            total={total}
                            limit={8}
                            emptyText="Язык ещё ни у одного канала не определён"
                        />
                    </CardContent>
                </Card>
            </div>
        </>
    )
}

function TopSources({sources}: {sources: DiscoverSourceStat[]}) {
    const max = Math.max(1, ...sources.map((s) => s.foundCount))

    return (
        <Card>
            <CardHeader className="pb-3">
                <CardTitle className="text-base">Самые продуктивные источники</CardTitle>
                <p className="text-xs text-muted-foreground">Каналы, из которых найдено больше всего других каналов</p>
            </CardHeader>
            <CardContent>
                {sources.length === 0 ? (
                    <p className="text-sm text-muted-foreground py-2">Пока ни один канал не дал новых ссылок</p>
                ) : (
                    <div className="space-y-3">
                        {sources.map((source, index) => (
                            <div key={source.id} className="flex items-center gap-3">
                                <span className="w-5 text-xs text-muted-foreground text-right tabular-nums">{index + 1}</span>
                                <ChannelAvatar avatarUrl={source.avatarUrl} name={source.title ?? source.username ?? "?"}/>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2 min-w-0">
                                        <ChannelName title={source.title} username={source.username} tgUrl={source.tgUrl}/>
                                        {source.isBanned && <Badge variant="destructive" className="shrink-0">забанен</Badge>}
                                    </div>
                                    <div className="h-1.5 rounded-full bg-muted overflow-hidden mt-1.5">
                                        <div
                                            className="h-full rounded-full bg-chart-1"
                                            style={{width: `${(source.foundCount / max) * 100}%`}}
                                        />
                                    </div>
                                </div>
                                <div className="text-right shrink-0">
                                    <p className="font-medium tabular-nums">{source.foundCount.toLocaleString("ru-RU")}</p>
                                    <p className="text-xs text-muted-foreground" title={formatDateTime(source.lastParsedAt)}>
                                        {source.lastParsedAt ? formatRelative(source.lastParsedAt) : "не парсился"}
                                    </p>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </CardContent>
        </Card>
    )
}

function HistoryRow({item}: {item: DiscoverParseHistoryItemResponse}) {
    const statusMeta = CHANNEL_STATUS_META[item.status]
    const link = channelLink(item)
    const sourceName = item.sourceTitle ?? (item.sourceUsername ? `@${item.sourceUsername}` : null)

    return (
        <tr className="border-b last:border-b-0 hover:bg-accent/40 transition-colors">
            <td className="py-2.5 pr-3">
                <div className="flex items-center gap-2.5 min-w-0">
                    <ChannelAvatar avatarUrl={item.avatarUrl} name={item.title ?? item.username ?? "?"}/>
                    <div className="min-w-0">
                        <ChannelName title={item.title} username={item.username} tgUrl={item.tgUrl}/>
                        <div className="flex flex-wrap gap-1 mt-0.5">
                            {item.category && <Badge variant="secondary" className="text-[10px] px-1.5 py-0">{item.category}</Badge>}
                            {item.peerType && <Badge variant="outline" className="text-[10px] px-1.5 py-0">{item.peerType}</Badge>}
                            {item.participantsCount != null && (
                                <span className="flex items-center gap-1 text-[11px] text-muted-foreground">
                                    <Users className="h-3 w-3"/>
                                    {compactNumber.format(item.participantsCount)}
                                </span>
                            )}
                        </div>
                    </div>
                </div>
            </td>
            <td className="py-2.5 px-3 whitespace-nowrap">
                <p className="text-sm">{formatDateTime(item.parsedAt)}</p>
                <p className="text-xs text-muted-foreground">{formatRelative(item.parsedAt)}</p>
            </td>
            <td className="py-2.5 px-3 text-right tabular-nums font-medium">{item.foundCount.toLocaleString("ru-RU")}</td>
            <td className="py-2.5 px-3 text-right tabular-nums text-muted-foreground">
                {item.lastParsedMessageId != null ? item.lastParsedMessageId.toLocaleString("ru-RU") : "—"}
            </td>
            <td className="py-2.5 px-3">
                <Badge variant={statusMeta.variant}>{statusMeta.label}</Badge>
            </td>
            <td className="py-2.5 px-3 whitespace-nowrap text-xs text-muted-foreground" title={formatDateTime(item.foundAt)}>
                {item.foundAt ? formatRelative(item.foundAt) : "—"}
            </td>
            <td className="py-2.5 pl-3 text-xs text-muted-foreground max-w-[10rem] truncate" title={sourceName ?? undefined}>
                {sourceName ?? "—"}
            </td>
            <td className="py-2.5 pl-2">
                {link && (
                    <a href={link} target="_blank" rel="noopener noreferrer" aria-label="Открыть в Telegram">
                        <Button variant="ghost" size="sm" className="h-7 w-7 p-0">
                            <ExternalLink className="h-3.5 w-3.5"/>
                        </Button>
                    </a>
                )}
            </td>
        </tr>
    )
}

function ParseHistory() {
    const [search, setSearch] = useState("")
    const [from, setFrom] = useState("")
    const [to, setTo] = useState("")
    const [page, setPage] = useState(1)
    const debouncedSearch = useDebounce(search.trim(), 400)

    const {data, isLoading, isFetching} = useGetApiV1DiscoverParseHistory({
        Search: debouncedSearch || undefined,
        From: toIsoStart(from),
        To: toIsoEnd(to),
        PageNumber: page,
        PageSize: HISTORY_PAGE_SIZE,
    })

    const items = data?.data ?? []
    const totalPages = data?.totalPages ?? 1
    const totalCount = data?.totalCount ?? 0

    const resetPage = () => setPage(1)

    return (
        <Card>
            <CardHeader className="pb-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                    <div>
                        <CardTitle className="text-base">История парсинга</CardTitle>
                        <p className="text-xs text-muted-foreground">
                            Какие каналы и когда парсились, от самых свежих
                            {totalCount > 0 && ` · всего ${totalCount.toLocaleString("ru-RU")}`}
                        </p>
                    </div>
                    {isFetching && !isLoading && <Loader2 className="h-4 w-4 animate-spin text-muted-foreground"/>}
                </div>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mt-3">
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"/>
                        <Input
                            placeholder="Поиск по названию или @username"
                            value={search}
                            onChange={(e) => {
                                setSearch(e.target.value)
                                resetPage()
                            }}
                            className="pl-9"
                        />
                    </div>
                    <Input
                        type="date"
                        value={from}
                        max={to || undefined}
                        onChange={(e) => {
                            setFrom(e.target.value)
                            resetPage()
                        }}
                        aria-label="Спарсен с"
                    />
                    <Input
                        type="date"
                        value={to}
                        min={from || undefined}
                        onChange={(e) => {
                            setTo(e.target.value)
                            resetPage()
                        }}
                        aria-label="Спарсен по"
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
                        {debouncedSearch || from || to
                            ? "Под фильтр ничего не попало"
                            : "Ни один канал ещё не парсился"}
                    </p>
                ) : (
                    <div className="overflow-x-auto -mx-6 px-6">
                        <table className="w-full text-sm">
                            <thead>
                            <tr className="border-b text-xs text-muted-foreground text-left">
                                <th className="py-2 pr-3 font-medium">Канал</th>
                                <th className="py-2 px-3 font-medium">Спарсен</th>
                                <th className="py-2 px-3 font-medium text-right">Найдено из него</th>
                                <th className="py-2 px-3 font-medium text-right">Последний msg id</th>
                                <th className="py-2 px-3 font-medium">Статус</th>
                                <th className="py-2 px-3 font-medium">Найден</th>
                                <th className="py-2 pl-3 font-medium">Источник</th>
                                <th className="py-2 pl-2"/>
                            </tr>
                            </thead>
                            <tbody>
                            {items.map((item) => (
                                <HistoryRow key={item.id} item={item}/>
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

export function DiscoverStatsPage() {
    const [days, setDays] = useState("30")

    const {data: stats, isLoading, isFetching, refetch} = useGetApiV1DiscoverStats({Days: Number(days)})
    const {data: status} = useGetApiV1DiscoverStatus({query: {refetchInterval: 15_000}})

    return (
        <div className="container mx-auto p-6 max-w-6xl space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold">Статистика Discover</h1>
                    <p className="text-sm text-muted-foreground mt-1">
                        Что и когда спарсилось, откуда берутся каналы и что накопилось в базе
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
                        <Link to="/discover/classification">
                            <Tags className="h-3.5 w-3.5"/>
                            Классификация
                        </Link>
                    </Button>
                    <Button variant="outline" size="sm" className="gap-1.5" asChild>
                        <Link to="/discover">
                            <Telescope className="h-3.5 w-3.5"/>
                            К списку каналов
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
                                <CardTitle className="text-base">Спарсено каналов по дням</CardTitle>
                                <p className="text-xs text-muted-foreground">Сколько каналов воркер обработал в каждый день</p>
                            </CardHeader>
                            <CardContent>
                                <DailyBarChart data={stats.parsedByDay} colorClass="bg-chart-2" unitLabel="спарсено"/>
                            </CardContent>
                        </Card>
                        <Card>
                            <CardHeader className="pb-3">
                                <CardTitle className="text-base">Найдено новых каналов по дням</CardTitle>
                                <p className="text-xs text-muted-foreground">Сколько ранее неизвестных каналов появилось в базе</p>
                            </CardHeader>
                            <CardContent>
                                <DailyBarChart data={stats.foundByDay} colorClass="bg-chart-1" unitLabel="найдено"/>
                            </CardContent>
                        </Card>
                    </div>

                    <Breakdowns stats={stats}/>

                    <TopSources sources={stats.topSources}/>
                </>
            )}

            <ParseHistory/>
        </div>
    )
}
