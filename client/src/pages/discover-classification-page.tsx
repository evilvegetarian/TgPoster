import {useState} from "react"
import {Link, useSearchParams} from "react-router-dom"
import {
    BarChart3,
    CheckCircle2,
    Clock,
    Database,
    ExternalLink,
    Gauge,
    Languages,
    Loader2,
    RefreshCw,
    Search,
    Settings2,
    Sparkles,
    Tag,
    Tags,
    Telescope,
    Users,
} from "lucide-react"
import {
    useGetApiV1DiscoverClassificationHistory,
    useGetApiV1DiscoverClassificationSettings,
    useGetApiV1DiscoverClassificationStats,
    useGetApiV1DiscoverClassificationStatus,
} from "@/api/endpoints/discover/discover"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Input} from "@/components/ui/input"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/components/ui/tabs"
import {ChannelAvatar, ChannelName} from "@/components/discover/channel-identity"
import {ClassifierSettingsForm} from "@/components/discover/classifier-settings-form"
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
import {cn} from "@/lib/utils"
import {
    ClassificationConfidenceBucket,
    type ClassificationCategoryStat,
    type ClassificationHistoryItemResponse,
    type ClassificationStatsResponse,
} from "@/api/endpoints/tgPosterAPI.schemas"

const HISTORY_PAGE_SIZE = 20
const CATEGORY_LIMIT = 10
const VISIBLE_TAGS_IN_ROW = 3
const ALL = "all"

const PERIOD_OPTIONS: ReadonlyArray<{value: string; label: string}> = [
    {value: "7", label: "7 дней"},
    {value: "14", label: "14 дней"},
    {value: "30", label: "30 дней"},
    {value: "90", label: "90 дней"},
]

// Границы корзин совпадают с бэкендом: ClassificationStatsStorage
const CONFIDENCE_META: Record<ClassificationConfidenceBucket, {label: string; barClass: string; textClass: string}> = {
    Unknown: {label: "Не указана", barClass: "bg-slate-400", textClass: "text-muted-foreground"},
    UpTo50: {label: "меньше 50%", barClass: "bg-red-500", textClass: "text-red-600"},
    From50To70: {label: "50–70%", barClass: "bg-orange-500", textClass: "text-orange-600"},
    From70To80: {label: "70–80%", barClass: "bg-amber-400", textClass: "text-amber-600"},
    From80To90: {label: "80–90%", barClass: "bg-teal-500", textClass: "text-teal-600"},
    Over90: {label: "90% и выше", barClass: "bg-emerald-500", textClass: "text-emerald-600"},
}

function confidenceBucketOf(value: number | null | undefined): ClassificationConfidenceBucket {
    if (value == null) return ClassificationConfidenceBucket.Unknown
    if (value < 0.5) return ClassificationConfidenceBucket.UpTo50
    if (value < 0.7) return ClassificationConfidenceBucket.From50To70
    if (value < 0.8) return ClassificationConfidenceBucket.From70To80
    if (value < 0.9) return ClassificationConfidenceBucket.From80To90
    return ClassificationConfidenceBucket.Over90
}

function formatConfidence(value: number | null | undefined): string {
    return value == null ? "—" : `${Math.round(value * 100)}%`
}

function ConfidenceValue({value, className}: {value: number | null | undefined; className?: string}) {
    return (
        <span className={cn("tabular-nums font-medium", CONFIDENCE_META[confidenceBucketOf(value)].textClass, className)}>
            {formatConfidence(value)}
        </span>
    )
}

function StatsTiles({stats}: {stats: ClassificationStatsResponse}) {
    const {totals, freshness} = stats
    const lowConfidence = stats.byConfidence.find((x) => x.bucket === ClassificationConfidenceBucket.UpTo50)?.count ?? 0
    const topLanguage = stats.byLanguage[0]
    const languagesTotal = stats.byLanguage.reduce((sum, item) => sum + item.count, 0)
    const averageAccent = totals.averageConfidence == null
        ? undefined
        : totals.averageConfidence < 0.5
            ? "bg-red-100 text-red-700"
            : totals.averageConfidence < 0.8
                ? "bg-amber-100 text-amber-700"
                : "bg-emerald-100 text-emerald-700"

    return (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <StatTile
                label="Классифицировано"
                value={totals.classified}
                hint={`${percent(totals.classified, totals.eligible)} от доступных · в очереди ${totals.pending.toLocaleString("ru-RU")}`
                    + (totals.failed > 0 ? ` · не удалось ${totals.failed.toLocaleString("ru-RU")}` : "")}
                icon={<CheckCircle2 className="h-4 w-4"/>}
                accent="bg-violet-100 text-violet-700"
            />
            <StatTile
                label="Классифицировано за 24 часа"
                value={freshness.classifiedLast24Hours}
                hint={`за 7 дней ${freshness.classifiedLast7Days.toLocaleString("ru-RU")} · за 30 дней ${freshness.classifiedLast30Days.toLocaleString("ru-RU")}`}
                icon={<Sparkles className="h-4 w-4"/>}
                accent="bg-violet-100 text-violet-700"
            />
            <StatTile
                label="Средняя уверенность модели"
                value={formatConfidence(totals.averageConfidence)}
                hint={`ниже 50% у ${lowConfidence.toLocaleString("ru-RU")} каналов`}
                icon={<Gauge className="h-4 w-4"/>}
                accent={averageAccent}
            />
            <StatTile
                label="Последняя классификация"
                value={freshness.lastClassifiedAt ? formatRelative(freshness.lastClassifiedAt) : "—"}
                hint={freshness.lastClassifiedAt ? formatDateTime(freshness.lastClassifiedAt) : "ещё не запускалась"}
                icon={<Clock className="h-4 w-4"/>}
            />
            <StatTile
                label="Доступно для классификации"
                value={totals.eligible}
                hint={`из ${totals.total.toLocaleString("ru-RU")} · приватные без username пропускаются: ${totals.skipped.toLocaleString("ru-RU")}`}
                icon={<Database className="h-4 w-4"/>}
            />
            <StatTile
                label="Тематик"
                value={stats.byCategory.length}
                hint={`подкатегорий ${totals.distinctSubcategories.toLocaleString("ru-RU")} · с тематикой ${totals.withCategory.toLocaleString("ru-RU")}`}
                icon={<Tag className="h-4 w-4"/>}
            />
            <StatTile
                label="Уникальных тегов"
                value={totals.distinctTags}
                hint={`теги есть у ${totals.withTags.toLocaleString("ru-RU")} каналов`}
                icon={<Tags className="h-4 w-4"/>}
            />
            <StatTile
                label="Языков"
                value={stats.byLanguage.length}
                hint={topLanguage
                    ? `чаще всего ${topLanguage.name.toUpperCase()} · ${percent(topLanguage.count, languagesTotal)}`
                    : "язык ещё не определён"}
                icon={<Languages className="h-4 w-4"/>}
            />
        </div>
    )
}

function ConfidenceDistribution({stats}: {stats: ClassificationStatsResponse}) {
    // «Не указана» бывает только у старых записей — пустую корзину не показываем
    const items: NamedCountBarItem[] = stats.byConfidence
        .filter((item) => item.bucket !== ClassificationConfidenceBucket.Unknown || item.count > 0)
        .map((item) => {
            const meta = CONFIDENCE_META[item.bucket]
            return {
                key: item.bucket,
                label: (
                    <span className="flex items-center gap-2">
                        <span className={`h-2 w-2 rounded-full shrink-0 ${meta.barClass}`}/>
                        {meta.label}
                    </span>
                ),
                count: item.count,
                colorClass: meta.barClass,
            }
        })

    return (
        <NamedCountBars
            items={items}
            total={stats.totals.classified}
            emptyText="Ни один канал ещё не классифицирован"
        />
    )
}

function CategoryTable({categories, total}: {categories: ClassificationCategoryStat[]; total: number}) {
    const [expanded, setExpanded] = useState(false)

    if (categories.length === 0) {
        return <p className="text-sm text-muted-foreground py-2">Ни один канал ещё не классифицирован</p>
    }

    const max = Math.max(1, ...categories.map((c) => c.count))
    const visible = expanded ? categories : categories.slice(0, CATEGORY_LIMIT)
    const hiddenCount = categories.length - visible.length

    return (
        <div className="space-y-2">
            <div className="grid grid-cols-[1fr_auto_auto] gap-x-4 text-xs text-muted-foreground">
                <span>Тематика</span>
                <span className="text-right">Каналов</span>
                <span className="text-right w-20">Уверенность</span>
            </div>
            {visible.map((category) => (
                <div key={category.name} className="grid grid-cols-[1fr_auto_auto] items-center gap-x-4 text-sm">
                    <div className="min-w-0">
                        <p className="truncate" title={category.name}>{category.name}</p>
                        <div className="h-1.5 rounded-full bg-muted overflow-hidden mt-1">
                            <div
                                className="h-full rounded-full bg-violet-500"
                                style={{width: `${(category.count / max) * 100}%`}}
                            />
                        </div>
                    </div>
                    <div className="text-right tabular-nums whitespace-nowrap">
                        <span className="font-medium">{category.count.toLocaleString("ru-RU")}</span>
                        <span className="text-xs text-muted-foreground ml-1.5">{percent(category.count, total)}</span>
                    </div>
                    <div className="text-right w-20" title="Средняя уверенность модели в этой тематике">
                        <ConfidenceValue value={category.averageConfidence}/>
                    </div>
                </div>
            ))}
            {(hiddenCount > 0 || expanded) && (
                <Button variant="ghost" size="sm" className="h-7 px-2 text-xs" onClick={() => setExpanded((v) => !v)}>
                    {expanded ? "Свернуть" : `Показать все (ещё ${hiddenCount})`}
                </Button>
            )}
        </div>
    )
}

function Breakdowns({stats}: {stats: ClassificationStatsResponse}) {
    const classified = stats.totals.classified

    const languageItems: NamedCountBarItem[] = stats.byLanguage.map((item) => ({
        key: item.name,
        label: item.name.toUpperCase(),
        count: item.count,
    }))

    const subcategoryItems: NamedCountBarItem[] = stats.topSubcategories.map((item) => ({
        key: `${item.category ?? ""}/${item.subcategory}`,
        label: (
            <div className="min-w-0" title={`${item.category ?? "Без тематики"} → ${item.subcategory}`}>
                <p className="truncate">{item.subcategory}</p>
                <p className="truncate text-xs text-muted-foreground">{item.category ?? "Без тематики"}</p>
            </div>
        ),
        count: item.count,
    }))

    const withoutCategory = Math.max(0, classified - stats.totals.withCategory)
    const withoutLanguage = Math.max(0, classified - stats.byLanguage.reduce((sum, item) => sum + item.count, 0))
    const maxTag = Math.max(1, ...stats.topTags.map((t) => t.count))

    return (
        <>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">По тематике</CardTitle>
                        <p className="text-xs text-muted-foreground">
                            Доля — от классифицированных каналов; уверенность — средняя по тематике
                            {withoutCategory > 0 && ` · без тематики ${withoutCategory.toLocaleString("ru-RU")}`}
                        </p>
                    </CardHeader>
                    <CardContent>
                        <CategoryTable categories={stats.byCategory} total={classified}/>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">Популярные подкатегории</CardTitle>
                        <p className="text-xs text-muted-foreground">
                            Топ-{stats.topSubcategories.length} из {stats.totals.distinctSubcategories.toLocaleString("ru-RU")}
                        </p>
                    </CardHeader>
                    <CardContent>
                        <NamedCountBars
                            items={subcategoryItems}
                            total={classified}
                            limit={8}
                            colorClass="bg-violet-400"
                            emptyText="Подкатегорий пока нет"
                        />
                    </CardContent>
                </Card>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">Популярные теги</CardTitle>
                        <p className="text-xs text-muted-foreground">
                            Топ-{stats.topTags.length} из {stats.totals.distinctTags.toLocaleString("ru-RU")}, без учёта регистра
                        </p>
                    </CardHeader>
                    <CardContent>
                        {stats.topTags.length === 0 ? (
                            <p className="text-sm text-muted-foreground py-2">Тегов пока нет</p>
                        ) : (
                            <div className="flex flex-wrap gap-1.5">
                                {stats.topTags.map((tag) => (
                                    <Badge
                                        key={tag.name}
                                        variant="secondary"
                                        className={cn("gap-1.5 font-normal", tag.count / maxTag >= 0.5 && "text-sm")}
                                    >
                                        {tag.name}
                                        <span className="text-muted-foreground tabular-nums">{tag.count.toLocaleString("ru-RU")}</span>
                                    </Badge>
                                ))}
                            </div>
                        )}
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">По языку</CardTitle>
                        {withoutLanguage > 0 && (
                            <p className="text-xs text-muted-foreground">
                                Язык не определён: {withoutLanguage.toLocaleString("ru-RU")} ({percent(withoutLanguage, classified)})
                            </p>
                        )}
                    </CardHeader>
                    <CardContent>
                        <NamedCountBars
                            items={languageItems}
                            total={classified}
                            limit={8}
                            emptyText="Язык ещё ни у одного канала не определён"
                        />
                    </CardContent>
                </Card>
            </div>
        </>
    )
}

function HistoryRow({item}: {item: ClassificationHistoryItemResponse}) {
    const link = channelLink(item)
    const visibleTags = item.tags.slice(0, VISIBLE_TAGS_IN_ROW)
    const hiddenTags = item.tags.length - visibleTags.length

    return (
        <tr className="border-b last:border-b-0 hover:bg-accent/40 transition-colors">
            <td className="py-2.5 pr-3">
                <div className="flex items-center gap-2.5 min-w-0">
                    <ChannelAvatar avatarUrl={item.avatarUrl} name={item.title ?? item.username ?? "?"}/>
                    <div className="min-w-0">
                        <ChannelName title={item.title} username={item.username} tgUrl={item.tgUrl}/>
                        {item.participantsCount != null && (
                            <span className="flex items-center gap-1 text-[11px] text-muted-foreground mt-0.5">
                                <Users className="h-3 w-3"/>
                                {compactNumber.format(item.participantsCount)}
                            </span>
                        )}
                    </div>
                </div>
            </td>
            <td className="py-2.5 px-3">
                {item.category ? (
                    <div className="min-w-0 max-w-[12rem]">
                        <Badge variant="secondary" className="text-[11px] px-1.5 py-0">{item.category}</Badge>
                        {item.subcategory && (
                            <p className="text-xs text-muted-foreground truncate mt-0.5" title={item.subcategory}>
                                {item.subcategory}
                            </p>
                        )}
                    </div>
                ) : (
                    <span className="text-xs text-muted-foreground">—</span>
                )}
            </td>
            <td className="py-2.5 px-3">
                {item.tags.length === 0 ? (
                    <span className="text-xs text-muted-foreground">—</span>
                ) : (
                    <div className="flex flex-wrap gap-1 max-w-[16rem]" title={item.tags.join(", ")}>
                        {visibleTags.map((tag) => (
                            <Badge key={tag} variant="outline" className="text-[10px] px-1.5 py-0 font-normal">{tag}</Badge>
                        ))}
                        {hiddenTags > 0 && <span className="text-[11px] text-muted-foreground">+{hiddenTags}</span>}
                    </div>
                )}
            </td>
            <td className="py-2.5 px-3 text-xs uppercase text-muted-foreground">{item.language ?? "—"}</td>
            <td className="py-2.5 px-3 text-right">
                <ConfidenceValue value={item.confidence}/>
            </td>
            <td className="py-2.5 px-3 whitespace-nowrap">
                <p className="text-sm">{formatDateTime(item.classifiedAt)}</p>
                <p className="text-xs text-muted-foreground">{formatRelative(item.classifiedAt)}</p>
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

function ClassificationHistory({categories}: {categories: string[]}) {
    const [search, setSearch] = useState("")
    const [category, setCategory] = useState(ALL)
    const [confidence, setConfidence] = useState(ALL)
    const [from, setFrom] = useState("")
    const [to, setTo] = useState("")
    const [page, setPage] = useState(1)
    const debouncedSearch = useDebounce(search.trim(), 400)

    const {data, isLoading, isFetching} = useGetApiV1DiscoverClassificationHistory({
        Search: debouncedSearch || undefined,
        Category: category === ALL ? undefined : category,
        Confidence: confidence === ALL ? undefined : (confidence as ClassificationConfidenceBucket),
        From: toIsoStart(from),
        To: toIsoEnd(to),
        PageNumber: page,
        PageSize: HISTORY_PAGE_SIZE,
    })

    const items = data?.data ?? []
    const totalPages = data?.totalPages ?? 1
    const totalCount = data?.totalCount ?? 0
    const hasFilters = Boolean(debouncedSearch || from || to) || category !== ALL || confidence !== ALL

    const resetPage = () => setPage(1)

    return (
        <Card>
            <CardHeader className="pb-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                    <div>
                        <CardTitle className="text-base">История классификации</CardTitle>
                        <p className="text-xs text-muted-foreground">
                            Что модель решила про каждый канал, от самых свежих
                            {totalCount > 0 && ` · всего ${totalCount.toLocaleString("ru-RU")}`}
                        </p>
                    </div>
                    {isFetching && !isLoading && <Loader2 className="h-4 w-4 animate-spin text-muted-foreground"/>}
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-3 mt-3">
                    <div className="relative md:col-span-2 lg:col-span-1">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"/>
                        <Input
                            placeholder="Название или @username"
                            value={search}
                            onChange={(e) => {
                                setSearch(e.target.value)
                                resetPage()
                            }}
                            className="pl-9"
                        />
                    </div>
                    <Select
                        value={category}
                        onValueChange={(value) => {
                            setCategory(value)
                            resetPage()
                        }}
                    >
                        <SelectTrigger aria-label="Тематика">
                            <SelectValue placeholder="Тематика"/>
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value={ALL}>Все тематики</SelectItem>
                            {categories.map((name) => (
                                <SelectItem key={name} value={name}>{name}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                    <Select
                        value={confidence}
                        onValueChange={(value) => {
                            setConfidence(value)
                            resetPage()
                        }}
                    >
                        <SelectTrigger aria-label="Уверенность">
                            <SelectValue placeholder="Уверенность"/>
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value={ALL}>Любая уверенность</SelectItem>
                            {Object.values(ClassificationConfidenceBucket).map((bucket) => (
                                <SelectItem key={bucket} value={bucket}>
                                    Уверенность: {CONFIDENCE_META[bucket].label}
                                </SelectItem>
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
                        aria-label="Классифицирован с"
                    />
                    <Input
                        type="date"
                        value={to}
                        min={from || undefined}
                        onChange={(e) => {
                            setTo(e.target.value)
                            resetPage()
                        }}
                        aria-label="Классифицирован по"
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
                        {hasFilters ? "Под фильтр ничего не попало" : "Ни один канал ещё не классифицирован"}
                    </p>
                ) : (
                    <div className="overflow-x-auto -mx-6 px-6">
                        <table className="w-full text-sm">
                            <thead>
                            <tr className="border-b text-xs text-muted-foreground text-left">
                                <th className="py-2 pr-3 font-medium">Канал</th>
                                <th className="py-2 px-3 font-medium">Тематика</th>
                                <th className="py-2 px-3 font-medium">Теги</th>
                                <th className="py-2 px-3 font-medium">Язык</th>
                                <th className="py-2 px-3 font-medium text-right">Уверенность</th>
                                <th className="py-2 px-3 font-medium">Классифицирован</th>
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

function StatsTab() {
    const [days, setDays] = useState("30")

    const {data: stats, isLoading, isFetching, refetch} = useGetApiV1DiscoverClassificationStats({Days: Number(days)})

    return (
        <div className="space-y-4">
            <div className="flex flex-wrap items-center justify-end gap-2">
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
            </div>

            {isLoading || !stats ? (
                <StatsSkeleton/>
            ) : (
                <>
                    <StatsTiles stats={stats}/>

                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                        <Card>
                            <CardHeader className="pb-3">
                                <CardTitle className="text-base">Классифицировано по дням</CardTitle>
                                <p className="text-xs text-muted-foreground">
                                    По дате последней классификации: повторная классификация переносит канал на новый день
                                </p>
                            </CardHeader>
                            <CardContent>
                                <DailyBarChart data={stats.classifiedByDay} colorClass="bg-violet-500" unitLabel="классифицировано"/>
                            </CardContent>
                        </Card>
                        <Card>
                            <CardHeader className="pb-3">
                                <CardTitle className="text-base">Уверенность модели</CardTitle>
                                <p className="text-xs text-muted-foreground">
                                    Ниже 50% модель ставит, когда данных мало, — такие каналы стоит проверить руками
                                </p>
                            </CardHeader>
                            <CardContent>
                                <ConfidenceDistribution stats={stats}/>
                            </CardContent>
                        </Card>
                    </div>

                    <Breakdowns stats={stats}/>
                </>
            )}

            <ClassificationHistory categories={stats?.byCategory.map((c) => c.name) ?? []}/>
        </div>
    )
}

const SETTINGS_TAB = "settings"

export function DiscoverClassificationPage() {
    const [searchParams, setSearchParams] = useSearchParams()
    const tab = searchParams.get("tab") === SETTINGS_TAB ? SETTINGS_TAB : "stats"

    const {data: status} = useGetApiV1DiscoverClassificationStatus({query: {refetchInterval: 15_000}})
    const {data: settings} = useGetApiV1DiscoverClassificationSettings()

    const openTab = (value: string) =>
        setSearchParams(value === SETTINGS_TAB ? {tab: SETTINGS_TAB} : {}, {replace: true})

    return (
        <div className="container mx-auto p-6 max-w-6xl space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <div className="flex flex-wrap items-center gap-2">
                        <h1 className="text-2xl font-bold">Классификация каналов</h1>
                        {settings && !settings.isEnabled && (
                            <Badge variant="outline" className="border-amber-300 bg-amber-50 text-amber-700">
                                Выключена
                            </Badge>
                        )}
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                        Как LLM раскладывает каналы по тематикам — статистика, история и настройки классификатора
                    </p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                    <Button variant="outline" size="sm" className="gap-1.5" asChild>
                        <Link to="/discover/stats">
                            <BarChart3 className="h-3.5 w-3.5"/>
                            Статистика Discover
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

            {status && <WorkerStatusCard title="Воркер классификации" status={status}/>}

            <Tabs value={tab} onValueChange={openTab}>
                <TabsList>
                    <TabsTrigger value="stats" className="gap-1.5">
                        <BarChart3 className="h-3.5 w-3.5"/>
                        Статистика
                    </TabsTrigger>
                    <TabsTrigger value={SETTINGS_TAB} className="gap-1.5">
                        <Settings2 className="h-3.5 w-3.5"/>
                        Настройки
                    </TabsTrigger>
                </TabsList>
                <TabsContent value="stats" className="mt-4">
                    <StatsTab/>
                </TabsContent>
                <TabsContent value={SETTINGS_TAB} className="mt-4">
                    <ClassifierSettingsForm/>
                </TabsContent>
            </Tabs>
        </div>
    )
}
