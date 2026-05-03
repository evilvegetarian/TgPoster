import {useState} from "react"
import {ExternalLink, Loader2, Search, Users} from "lucide-react"
import {useGetApiV1Discover, useGetApiV1DiscoverCategories} from "@/api/endpoints/discover/discover"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent} from "@/components/ui/card"
import {Input} from "@/components/ui/input"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import type {DiscoverChannelResponse} from "@/api/endpoints/tgPosterAPI.schemas"

const PAGE_SIZE = 20

function ChannelCard({channel}: {channel: DiscoverChannelResponse}) {
    const tgLink = channel.tgUrl
        ?? (channel.username ? `https://t.me/${channel.username}` : null)

    return (
        <Card>
            <CardContent className="pt-5">
                <div className="flex gap-4">
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
                                <p className="font-semibold truncate">{channel.title ?? channel.username}</p>
                                {channel.username && (
                                    <p className="text-sm text-muted-foreground">@{channel.username}</p>
                                )}
                            </div>
                            {tgLink && (
                                <a
                                    href={tgLink}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="flex-shrink-0"
                                >
                                    <Button variant="ghost" size="icon" className="h-8 w-8">
                                        <ExternalLink className="h-4 w-4"/>
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

function ChannelCardSkeleton() {
    return (
        <Card>
            <CardContent className="pt-5">
                <div className="flex gap-4">
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
    const [page, setPage] = useState(1)

    const apiCategory = category === "all" ? undefined : category
    const apiPeerType = peerType === "all" ? undefined : peerType
    const apiSearch = search.trim() || undefined

    const {data, isLoading} = useGetApiV1Discover({
        Category: apiCategory,
        Search: apiSearch,
        PeerType: apiPeerType,
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

    return (
        <div className="container mx-auto p-6 max-w-5xl">
            <div className="flex items-center justify-between mb-6">
                <div>
                    <h1 className="text-2xl font-bold">Discover каналы</h1>
                    {totalCount > 0 && (
                        <p className="text-sm text-muted-foreground mt-1">
                            Найдено: {totalCount.toLocaleString()}
                        </p>
                    )}
                </div>
            </div>

            <Card className="mb-6">
                <CardContent className="pt-6">
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
                <div className="space-y-3">
                    {channels.map((channel) => (
                        <ChannelCard key={channel.id} channel={channel}/>
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
