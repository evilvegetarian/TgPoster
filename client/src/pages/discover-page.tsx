import {useState} from "react"
import {Link} from "react-router-dom"
import {BarChart3, ExternalLink, ListPlus, Loader2, Search, Send, Users, X} from "lucide-react"
import {useGetApiV1Discover, useGetApiV1DiscoverCategories} from "@/api/endpoints/discover/discover"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent} from "@/components/ui/card"
import {Checkbox} from "@/components/ui/checkbox"
import {Input} from "@/components/ui/input"
import {Progress} from "@/components/ui/progress"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import {useRepostImportJob} from "@/hooks/use-repost-import-job"
import {AddFromDiscoverDialog} from "@/pages/repostpage/add-from-discover-dialog"
import type {
    DiscoverChannelResponse,
    DiscoverSortBy,
    RepostImportJobResponse,
    SortDirection,
} from "@/api/endpoints/tgPosterAPI.schemas"

const PAGE_SIZE = 20

const SORT_OPTIONS: ReadonlyArray<{value: DiscoverSortBy; label: string}> = [
    {value: "Participants", label: "По подписчикам"},
    {value: "DiscoveredAt", label: "По дате обнаружения"},
    {value: "Title", label: "По названию"},
]

const DIRECTION_OPTIONS: ReadonlyArray<{value: SortDirection; label: string}> = [
    {value: "Desc", label: "По убыванию"},
    {value: "Asc", label: "По возрастанию"},
]

function parseCount(value: string): number | undefined {
    const trimmed = value.trim()
    if (trimmed === "") return undefined
    const parsed = Number(trimmed)
    return Number.isNaN(parsed) ? undefined : parsed
}

interface ChannelCardProps {
    channel: DiscoverChannelResponse
    selected: boolean
    onSelectedChange: (selected: boolean) => void
}

function ChannelCard({channel, selected, onSelectedChange}: ChannelCardProps) {
    const tgLink = channel.tgUrl
        ?? (channel.username ? `https://t.me/${channel.username}` : null)

    return (
        <Card>
            <CardContent className="pt-5">
                <div className="flex gap-4">
                    <div className="flex-shrink-0 flex items-center">
                        <Checkbox
                            checked={selected}
                            onCheckedChange={(value) => onSelectedChange(value === true)}
                            aria-label={`Выбрать ${channel.title ?? channel.username ?? "канал"}`}
                        />
                    </div>
                    <div className="flex-shrink-0">
                        {channel.avatarUrl ? (
                            <img
                                src={channel.avatarUrl}
                                alt={channel.title ?? ""}
                                className="w-12 h-12 rounded-full object-cover"
                            />
                        ) : (
                            <div className="w-12 h-12 rounded-full bg-muted flex items-center justify-center text-muted-foreground font-semibold text-lg">
                                {(channel.title ?? channel.username ?? "?")[0].toUpperCase()}
                            </div>
                        )}
                    </div>
                    <div className="flex-1 min-w-0">
                        <div className="flex items-start justify-between gap-2">
                            <div className="min-w-0">
                                {tgLink ? (
                                    <a
                                        href={tgLink}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="font-semibold truncate hover:underline block"
                                    >
                                        {channel.title ?? channel.username}
                                    </a>
                                ) : (
                                    <p className="font-semibold truncate">{channel.title ?? channel.username}</p>
                                )}
                                {channel.username && (
                                    tgLink ? (
                                        <a
                                            href={tgLink}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="text-sm text-muted-foreground hover:text-primary hover:underline"
                                        >
                                            @{channel.username}
                                        </a>
                                    ) : (
                                        <p className="text-sm text-muted-foreground">@{channel.username}</p>
                                    )
                                )}
                            </div>
                            {tgLink && (
                                <a
                                    href={tgLink}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="flex-shrink-0"
                                >
                                    <Button variant="outline" size="sm" className="h-8 gap-1.5">
                                        <ExternalLink className="h-3.5 w-3.5"/>
                                        <span className="text-xs">Открыть в Telegram</span>
                                    </Button>
                                </a>
                            )}
                        </div>
                        {channel.description && (
                            <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                                {channel.description}
                            </p>
                        )}
                        <div className="flex flex-wrap items-center gap-2 mt-2">
                            {channel.category && (
                                <Badge variant="secondary">{channel.category}</Badge>
                            )}
                            {channel.subcategory && (
                                <Badge variant="outline">{channel.subcategory}</Badge>
                            )}
                            {channel.language && (
                                <Badge variant="outline">{channel.language.toUpperCase()}</Badge>
                            )}
                            {channel.peerType && (
                                <Badge variant="outline">{channel.peerType}</Badge>
                            )}
                            {channel.participantsCount != null && (
                                <span className="flex items-center gap-1 text-sm text-muted-foreground">
                                    <Users className="h-3.5 w-3.5"/>
                                    {channel.participantsCount.toLocaleString()}
                                </span>
                            )}
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
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
        <Card className="mb-6">
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

function ChannelCardSkeleton() {
    return (
        <Card>
            <CardContent className="pt-5">
                <div className="flex gap-4">
                    <Skeleton className="w-4 h-4 rounded flex-shrink-0 mt-4"/>
                    <Skeleton className="w-12 h-12 rounded-full flex-shrink-0"/>
                    <div className="flex-1 space-y-2">
                        <Skeleton className="h-4 w-48"/>
                        <Skeleton className="h-3 w-32"/>
                        <Skeleton className="h-3 w-full"/>
                        <div className="flex gap-2">
                            <Skeleton className="h-5 w-16 rounded-full"/>
                            <Skeleton className="h-5 w-12 rounded-full"/>
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
    )
}

export function DiscoverPage() {
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

    const apiCategory = category === "all" ? undefined : category
    const apiPeerType = peerType === "all" ? undefined : peerType
    const apiSearch = search.trim() || undefined
    const apiMinParticipants = parseCount(minParticipants)
    const apiMaxParticipants = parseCount(maxParticipants)

    const {job, startJob, clearJob} = useRepostImportJob()

    const {data, isLoading} = useGetApiV1Discover({
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

    const handleCategoryChange = (value: string) => {
        setCategory(value)
        setPage(1)
    }

    const handlePeerTypeChange = (value: string) => {
        setPeerType(value)
        setPage(1)
    }

    const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setSearch(e.target.value)
        setPage(1)
    }

    const handleMinParticipantsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setMinParticipants(e.target.value)
        setPage(1)
    }

    const handleMaxParticipantsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setMaxParticipants(e.target.value)
        setPage(1)
    }

    const handleSortByChange = (value: string) => {
        setSortBy(value as DiscoverSortBy)
        setPage(1)
    }

    const handleSortDirectionChange = (value: string) => {
        setSortDirection(value as SortDirection)
        setPage(1)
    }

    const pageChannelIds = channels.map((channel) => channel.id)
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
        <div className="container mx-auto p-6 max-w-5xl">
            <div className="flex items-center justify-between gap-4 mb-6">
                <div>
                    <h1 className="text-2xl font-bold">Discover каналы</h1>
                    {totalCount > 0 && (
                        <p className="text-sm text-muted-foreground mt-1">
                            Найдено: {totalCount.toLocaleString()}
                        </p>
                    )}
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                    <Button variant="outline" className="gap-1.5" asChild>
                        <Link to="/discover/stats">
                            <BarChart3 className="h-4 w-4"/>
                            Статистика
                        </Link>
                    </Button>
                    <Button
                        variant="outline"
                        className="gap-1.5"
                        disabled={totalCount === 0}
                        onClick={() => openAddDialog("filter")}
                    >
                        <ListPlus className="h-4 w-4"/>
                        Добавить все по фильтру
                    </Button>
                </div>
            </div>

            {job != null && (
                <ImportJobBanner
                    job={job}
                    onOpen={() => setIsAddToRepostOpen(true)}
                    onDismiss={clearJob}
                />

            )}

            <Card className="mb-6">
                <CardContent className="pt-6 space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <Select value={category} onValueChange={handleCategoryChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Все тематики"/>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Все тематики</SelectItem>
                                {categories.map((cat) => (
                                    <SelectItem key={cat} value={cat}>{cat}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        <Select value={peerType} onValueChange={handlePeerTypeChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Тип"/>
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Все типы</SelectItem>
                                <SelectItem value="channel">Каналы</SelectItem>
                                <SelectItem value="chat">Чаты</SelectItem>
                            </SelectContent>
                        </Select>
                        <div className="relative">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"/>
                            <Input
                                placeholder="Поиск по названию или @username"
                                value={search}
                                onChange={handleSearchChange}
                                className="pl-9"
                            />
                        </div>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                        <div className="relative">
                            <Users className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"/>
                            <Input
                                type="number"
                                min={0}
                                placeholder="Подписчиков от"
                                value={minParticipants}
                                onChange={handleMinParticipantsChange}
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
                                onChange={handleMaxParticipantsChange}
                                className="pl-9"
                            />
                        </div>
                        <Select value={sortBy} onValueChange={handleSortByChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Сортировка"/>
                            </SelectTrigger>
                            <SelectContent>
                                {SORT_OPTIONS.map((opt) => (
                                    <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        <Select value={sortDirection} onValueChange={handleSortDirectionChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Направление"/>
                            </SelectTrigger>
                            <SelectContent>
                                {DIRECTION_OPTIONS.map((opt) => (
                                    <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                </CardContent>
            </Card>

            {isLoading ? (
                <div className="space-y-3">
                    {Array.from({length: 5}).map((_, i) => (
                        <ChannelCardSkeleton key={i}/>
                    ))}
                </div>
            ) : channels.length === 0 ? (
                <div className="text-center py-16 text-muted-foreground">
                    <Loader2 className="h-8 w-8 mx-auto mb-3 opacity-30"/>
                    <p>Каналы не найдены. Попробуйте изменить фильтры.</p>
                </div>
            ) : (
                <>
                    <div className="flex items-center justify-between gap-4 mb-3">
                        <label className="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer">
                            <Checkbox
                                checked={allOnPageSelected}
                                onCheckedChange={(value) => handleSelectAllOnPage(value === true)}
                            />
                            Выбрать все на странице
                        </label>
                        {selectedIds.length > 0 && (
                            <div className="flex items-center gap-2">
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
                    </div>
                    <div className="space-y-3">
                        {channels.map((channel) => (
                            <ChannelCard
                                key={channel.id}
                                channel={channel}
                                selected={selectedIds.includes(channel.id)}
                                onSelectedChange={(selected) => handleSelectedChange(channel.id, selected)}
                            />
                        ))}
                    </div>
                </>
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
