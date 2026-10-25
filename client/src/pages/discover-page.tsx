import {useState} from "react"
import {Link, useSearchParams} from "react-router-dom"
import {BarChart3, ExternalLink, List, ListPlus, Loader2, Search, Send, Settings2, Tags, Users, X} from "lucide-react"
import {
    useGetApiV1Discover,
    useGetApiV1DiscoverCategories,
    useGetApiV1DiscoverSettings,
    useGetApiV1DiscoverStatus,
} from "@/api/endpoints/discover/discover"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Checkbox} from "@/components/ui/checkbox"
import {Input} from "@/components/ui/input"
import {Progress} from "@/components/ui/progress"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/components/ui/tabs"
import {ChannelAvatar, ChannelName} from "@/components/discover/channel-identity"
import {DiscoverSettingsForm} from "@/components/discover/discover-settings-form"
import {DiscoverStatsTab} from "@/components/discover/discover-stats-tab"
import {channelLink, formatDateTime, formatRelative} from "@/components/discover/format"
import {WorkerStatusCard} from "@/components/discover/worker-status-card"
import {useDebounce} from "@/hooks/use-debounce"
import {useRepostImportJob} from "@/hooks/use-repost-import-job"
import {cn} from "@/lib/utils"
import {AddFromDiscoverDialog} from "@/pages/repostpage/add-from-discover-dialog"
import type {
    DiscoverChannelResponse,
    DiscoverSortBy,
    RepostImportJobResponse,
    SortDirection,
} from "@/api/endpoints/tgPosterAPI.schemas"

const PAGE_SIZE = 20

const CHANNELS_TAB = "channels"
const STATS_TAB = "stats"
const SETTINGS_TAB = "settings"

const SORT_OPTIONS: ReadonlyArray<{value: DiscoverSortBy; label: string}> = [
    {value: "Participants", label: "По подписчикам"},
    {value: "DiscoveredAt", label: "По дате обнаружения"},
    {value: "Title", label: "По названию"},
]

const DIRECTION_OPTIONS: ReadonlyArray<{value: SortDirection; label: string}> = [
    {value: "Desc", label: "По убыванию"},
    {value: "Asc", label: "По возрастанию"},
]

const PEER_TYPE_LABELS: Record<string, string> = {
    channel: "Канал",
    chat: "Чат",
}

function parseCount(value: string): number | undefined {
    const trimmed = value.trim()
    if (trimmed === "") return undefined
    const parsed = Number(trimmed)
    return Number.isNaN(parsed) ? undefined : parsed
}

interface ChannelRowProps {
    channel: DiscoverChannelResponse
    selected: boolean
    onSelectedChange: (selected: boolean) => void
}

function ChannelRow({channel, selected, onSelectedChange}: ChannelRowProps) {
    const link = channelLink(channel)

    return (
        <tr className={cn("border-b last:border-b-0 hover:bg-accent/40 transition-colors", selected && "bg-accent/30")}>
            <td className="py-2.5 pr-3">
                <Checkbox
                    checked={selected}
                    onCheckedChange={(value) => onSelectedChange(value === true)}
                    aria-label={`Выбрать ${channel.title ?? channel.username ?? "канал"}`}
                />
            </td>
            <td className="py-2.5 pr-3">
                <div className="flex items-center gap-2.5 min-w-0">
                    <ChannelAvatar avatarUrl={channel.avatarUrl} name={channel.title ?? channel.username ?? "?"}/>
                    <div className="min-w-0 max-w-[22rem]">
                        <ChannelName title={channel.title} username={channel.username} tgUrl={channel.tgUrl}/>
                        {channel.description && (
                            <p className="text-xs text-muted-foreground truncate mt-0.5" title={channel.description}>
                                {channel.description}
                            </p>
                        )}
                    </div>
                </div>
            </td>
            <td className="py-2.5 px-3">
                {channel.category ? (
                    <div className="min-w-0 max-w-[12rem]">
                        <Badge variant="secondary" className="text-[11px] px-1.5 py-0">{channel.category}</Badge>
                        {channel.subcategory && (
                            <p className="text-xs text-muted-foreground truncate mt-0.5" title={channel.subcategory}>
                                {channel.subcategory}
                            </p>
                        )}
                    </div>
                ) : (
                    <span className="text-xs text-muted-foreground">—</span>
                )}
            </td>
            <td className="py-2.5 px-3 text-xs uppercase text-muted-foreground">{channel.language ?? "—"}</td>
            <td className="py-2.5 px-3 text-xs text-muted-foreground">
                {channel.peerType ? PEER_TYPE_LABELS[channel.peerType] ?? channel.peerType : "—"}
            </td>
            <td className="py-2.5 px-3 text-right tabular-nums">
                {channel.participantsCount != null ? channel.participantsCount.toLocaleString("ru-RU") : "—"}
            </td>
            <td className="py-2.5 px-3 whitespace-nowrap text-xs text-muted-foreground" title={formatDateTime(channel.lastDiscoveredAt)}>
                {channel.lastDiscoveredAt ? formatRelative(channel.lastDiscoveredAt) : "не парсился"}
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

interface ImportJobBannerProps {
    job: RepostImportJobResponse
    onOpen: () => void
    onDismiss: () => void
}

function ImportJobBanner({job, onOpen, onDismiss}: ImportJobBannerProps) {
    const processed = job.totalCount - job.pendingCount
    const percent = job.totalCount > 0 ? Math.round((processed / job.totalCount) * 100) : 100
    const isRunning = job.status !== "Completed" && job.status !== "Failed"

    return (
        <Card>
            <CardContent className="pt-6 space-y-3">
                <div className="flex items-center justify-between gap-4">
                    <div className="flex items-center gap-2 min-w-0">
                        {isRunning && <Loader2 className="h-4 w-4 animate-spin flex-shrink-0"/>}
                        <p className="text-sm font-medium truncate">
                            {isRunning
                                ? `Добавление каналов в репост: ${processed} из ${job.totalCount}`
                                : `Добавление завершено: ${job.addedCount} из ${job.totalCount}`}
                        </p>
                    </div>
                    <div className="flex items-center gap-2 flex-shrink-0">
                        <Button variant="outline" size="sm" onClick={onOpen}>
                            Подробнее
                        </Button>
                        {!isRunning && (
                            <Button variant="ghost" size="icon" onClick={onDismiss} title="Скрыть">
                                <X className="h-4 w-4"/>
                                <span className="sr-only">Скрыть</span>
                            </Button>
                        )}
                    </div>
                </div>
                <Progress value={percent}/>
                {job.status === "CooldownWait" && (
                    <p className="text-sm text-amber-600">
                        Telegram ограничил сессию, обработка продолжится позже
                    </p>
                )}
            </CardContent>
        </Card>
    )
}

function ChannelsTab() {
    const [category, setCategory] = useState<string>("all")
    const [peerType, setPeerType] = useState<string>("all")
    const [search, setSearch] = useState("")
    const [minParticipants, setMinParticipants] = useState("")
    const [maxParticipants, setMaxParticipants] = useState("")
    const [sortBy, setSortBy] = useState<DiscoverSortBy>("Participants")
    const [sortDirection, setSortDirection] = useState<SortDirection>("Desc")
    const [page, setPage] = useState(1)
    const [selectedIds, setSelectedIds] = useState<string[]>([])
    const [isAddToRepostOpen, setIsAddToRepostOpen] = useState(false)
    const [addMode, setAddMode] = useState<"selected" | "filter">("selected")
    const debouncedSearch = useDebounce(search.trim(), 400)

    const apiCategory = category === "all" ? undefined : category
    const apiPeerType = peerType === "all" ? undefined : peerType
    const apiSearch = debouncedSearch || undefined
    const apiMinParticipants = parseCount(minParticipants)
    const apiMaxParticipants = parseCount(maxParticipants)

    const {job, startJob, clearJob} = useRepostImportJob()

    const {data, isLoading, isFetching} = useGetApiV1Discover({
        Category: apiCategory,
        Search: apiSearch,
        PeerType: apiPeerType,
        MinParticipants: apiMinParticipants,
        MaxParticipants: apiMaxParticipants,
        SortBy: sortBy,
        SortDirection: sortDirection,
        PageNumber: page,
        PageSize: PAGE_SIZE,
    })

    const {data: categoriesData} = useGetApiV1DiscoverCategories()
    const categories = categoriesData ?? []

    const channels = data?.data ?? []
    const totalPages = data?.totalPages ?? 1
    const totalCount = data?.totalCount ?? 0

    const resetPage = () => setPage(1)

    const pageChannelIds = channels
        .map((channel) => channel.id)
        .filter((id): id is string => !!id)
    const allOnPageSelected = pageChannelIds.length > 0
        && pageChannelIds.every((id) => selectedIds.includes(id))

    const handleSelectedChange = (channelId: string, selected: boolean) => {
        setSelectedIds((prev) => selected
            ? [...prev, channelId]
            : prev.filter((id) => id !== channelId))
    }

    const handleSelectAllOnPage = (selected: boolean) => {
        setSelectedIds((prev) => selected
            ? [...new Set([...prev, ...pageChannelIds])]
            : prev.filter((id) => !pageChannelIds.includes(id)))
    }

    const openAddDialog = (mode: "selected" | "filter") => {
        setAddMode(mode)
        setIsAddToRepostOpen(true)
    }

    return (
        <div className="space-y-4">
            {job != null && (
                <ImportJobBanner
                    job={job}
                    onOpen={() => setIsAddToRepostOpen(true)}
                    onDismiss={clearJob}
                />
            )}

            <Card>
                <CardHeader className="pb-3">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                            <div>
                                <CardTitle className="text-base">Каналы</CardTitle>
                                <p className="text-xs text-muted-foreground">
                                    Найденные по ссылкам в других каналах
                                    {totalCount > 0 && ` · по фильтру ${totalCount.toLocaleString("ru-RU")}`}
                                </p>
                            </div>
                            {isFetching && !isLoading && <Loader2 className="h-4 w-4 animate-spin text-muted-foreground"/>}
                        </div>
                        <Button
                            variant="outline"
                            size="sm"
                            className="gap-1.5"
                            disabled={totalCount === 0}
                            onClick={() => openAddDialog("filter")}
                        >
                            <ListPlus className="h-3.5 w-3.5"/>
                            Добавить все по фильтру
                        </Button>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mt-3">
                        <div className="relative">
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
                                <SelectValue placeholder="Все тематики"/>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Все тематики</SelectItem>
                                {categories.map((cat) => (
                                    <SelectItem key={cat} value={cat}>{cat}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        <Select
                            value={peerType}
                            onValueChange={(value) => {
                                setPeerType(value)
                                resetPage()
                            }}
                        >
                            <SelectTrigger aria-label="Тип">
                                <SelectValue placeholder="Тип"/>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Все типы</SelectItem>
                                <SelectItem value="channel">Каналы</SelectItem>
                                <SelectItem value="chat">Чаты</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 mt-3">
                        <div className="relative">
                            <Users className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"/>
                            <Input
                                type="number"
                                min={0}
                                placeholder="Подписчиков от"
                                value={minParticipants}
                                onChange={(e) => {
                                    setMinParticipants(e.target.value)
                                    resetPage()
                                }}
                                className="pl-9"
                            />
                        </div>
                        <div className="relative">
                            <Users className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"/>
                            <Input
                                type="number"
                                min={0}
                                placeholder="Подписчиков до"
                                value={maxParticipants}
                                onChange={(e) => {
                                    setMaxParticipants(e.target.value)
                                    resetPage()
                                }}
                                className="pl-9"
                            />
                        </div>
                        <Select
                            value={sortBy}
                            onValueChange={(value) => {
                                setSortBy(value as DiscoverSortBy)
                                resetPage()
                            }}
                        >
                            <SelectTrigger aria-label="Сортировка">
                                <SelectValue placeholder="Сортировка"/>
                            </SelectTrigger>
                            <SelectContent>
                                {SORT_OPTIONS.map((opt) => (
                                    <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        <Select
                            value={sortDirection}
                            onValueChange={(value) => {
                                setSortDirection(value as SortDirection)
                                resetPage()
                            }}
                        >
                            <SelectTrigger aria-label="Направление">
                                <SelectValue placeholder="Направление"/>
                            </SelectTrigger>
                            <SelectContent>
                                {DIRECTION_OPTIONS.map((opt) => (
                                    <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                </CardHeader>
                <CardContent>
                    {selectedIds.length > 0 && (
                        <div className="flex flex-wrap items-center justify-end gap-2 mb-3">
                            <span className="text-sm text-muted-foreground">
                                Выбрано: {selectedIds.length}
                            </span>
                            <Button variant="ghost" size="sm" onClick={() => setSelectedIds([])}>
                                Сбросить
                            </Button>
                            <Button size="sm" className="gap-1.5" onClick={() => openAddDialog("selected")}>
                                <Send className="h-3.5 w-3.5"/>
                                Добавить в репост
                            </Button>
                        </div>
                    )}
                    {isLoading ? (
                        <div className="space-y-2">
                            {Array.from({length: 5}).map((_, i) => (
                                <Skeleton key={i} className="h-12 w-full"/>
                            ))}
                        </div>
                    ) : channels.length === 0 ? (
                        <p className="text-sm text-muted-foreground py-6 text-center">
                            Каналы не найдены. Попробуйте изменить фильтры.
                        </p>
                    ) : (
                        <div className="overflow-x-auto -mx-6 px-6">
                            <table className="w-full text-sm">
                                <thead>
                                <tr className="border-b text-xs text-muted-foreground text-left">
                                    <th className="py-2 pr-3 w-8">
                                        <Checkbox
                                            checked={allOnPageSelected}
                                            onCheckedChange={(value) => handleSelectAllOnPage(value === true)}
                                            aria-label="Выбрать все на странице"
                                        />
                                    </th>
                                    <th className="py-2 pr-3 font-medium">Канал</th>
                                    <th className="py-2 px-3 font-medium">Тематика</th>
                                    <th className="py-2 px-3 font-medium">Язык</th>
                                    <th className="py-2 px-3 font-medium">Тип</th>
                                    <th className="py-2 px-3 font-medium text-right">Подписчики</th>
                                    <th className="py-2 px-3 font-medium" title="Когда воркер последний раз искал ссылки в канале">
                                        Парсинг
                                    </th>
                                    <th className="py-2 pl-2"/>
                                </tr>
                                </thead>
                                <tbody>
                                {channels.map((channel) => (
                                    <ChannelRow
                                        key={channel.id}
                                        channel={channel}
                                        selected={!!channel.id && selectedIds.includes(channel.id)}
                                        onSelectedChange={(selected) => {
                                            if (channel.id) handleSelectedChange(channel.id, selected)
                                        }}
                                    />
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

            <AddFromDiscoverDialog
                open={isAddToRepostOpen}
                onOpenChange={setIsAddToRepostOpen}
                mode={addMode}
                selectedChannelIds={selectedIds}
                filter={{
                    category: apiCategory,
                    search: apiSearch,
                    peerType: apiPeerType,
                    minParticipants: apiMinParticipants,
                    maxParticipants: apiMaxParticipants,
                    sortBy,
                    sortDirection,
                }}
                matchedCount={totalCount}
                job={job}
                onJobStarted={(jobId) => {
                    startJob(jobId)
                    setSelectedIds([])
                }}
                onJobCleared={clearJob}
            />
        </div>
    )
}

export function DiscoverPage() {
    const [searchParams, setSearchParams] = useSearchParams()
    const tabParam = searchParams.get("tab")
    const tab = tabParam === STATS_TAB || tabParam === SETTINGS_TAB ? tabParam : CHANNELS_TAB

    const {data: status} = useGetApiV1DiscoverStatus({query: {refetchInterval: 15_000}})
    const {data: settings} = useGetApiV1DiscoverSettings()

    const openTab = (value: string) =>
        setSearchParams(value === CHANNELS_TAB ? {} : {tab: value}, {replace: true})

    return (
        <div className="container mx-auto p-6 max-w-6xl space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <div className="flex flex-wrap items-center gap-2">
                        <h1 className="text-2xl font-bold">Discover каналы</h1>
                        {settings && !settings.isEnabled && (
                            <Badge variant="outline" className="border-amber-300 bg-amber-50 text-amber-700">
                                Поиск выключен
                            </Badge>
                        )}
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                        Каналы, найденные по ссылкам в других каналах, — список, статистика парсинга и настройки воркера
                    </p>
                </div>
                <Button variant="outline" size="sm" className="gap-1.5" asChild>
                    <Link to="/discover/classification">
                        <Tags className="h-3.5 w-3.5"/>
                        Классификация
                    </Link>
                </Button>
            </div>

            {status && <WorkerStatusCard title="Воркер парсинга" status={status}/>}

            <Tabs value={tab} onValueChange={openTab}>
                <TabsList>
                    <TabsTrigger value={CHANNELS_TAB} className="gap-1.5">
                        <List className="h-3.5 w-3.5"/>
                        Каналы
                    </TabsTrigger>
                    <TabsTrigger value={STATS_TAB} className="gap-1.5">
                        <BarChart3 className="h-3.5 w-3.5"/>
                        Статистика
                    </TabsTrigger>
                    <TabsTrigger value={SETTINGS_TAB} className="gap-1.5">
                        <Settings2 className="h-3.5 w-3.5"/>
                        Настройки
                    </TabsTrigger>
                </TabsList>
                {/* Список не размонтируется при переключении вкладок: фильтры, страница и выбор каналов сохраняются */}
                <TabsContent value={CHANNELS_TAB} forceMount className="mt-4 data-[state=inactive]:hidden">
                    <ChannelsTab/>
                </TabsContent>
                <TabsContent value={STATS_TAB} className="mt-4">
                    <DiscoverStatsTab/>
                </TabsContent>
                <TabsContent value={SETTINGS_TAB} className="mt-4">
                    <DiscoverSettingsForm/>
                </TabsContent>
            </Tabs>
        </div>
    )
}
