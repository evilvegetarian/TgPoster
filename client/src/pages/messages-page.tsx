import {useCallback, useEffect, useMemo, useState} from "react"
import {AlertCircle, Check, Loader2, MessageSquarePlus, Trash2} from "lucide-react"
import type {DateRange} from "react-day-picker"
import {toast} from "sonner"
import {useQueryClient} from "@tanstack/react-query"
import {Button} from "@/components/ui/button"
import {Checkbox} from "@/components/ui/checkbox"
import {Card, CardContent} from "@/components/ui/card"
import {Skeleton} from "@/components/ui/skeleton"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Alert, AlertDescription} from "@/components/ui/alert"
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger
} from "@/components/ui/alert-dialog"
import {MessageSortBy, MessageStatus, SortDirection} from "@/api/endpoints/tgPosterAPI.schemas"
import {
    useDeleteApiV1Message,
    useGetApiV1MessageScheduleIdTime,
    usePatchApiV1Message
} from "@/api/endpoints/message/message"

import {FiltersPanel} from "@/components/message/filters-panel.tsx"
import {MessageCard} from "@/components/message/message-card.tsx"
import {CreateMessageDialog} from "@/components/message/create-message-dialog.tsx"
import usePersistentState from "./use-persistent-state"
import {useDebounce} from "@/hooks/use-debounce.ts"
import {useInfiniteMessages} from "@/hooks/use-infinite-messages"
import {useIntersectionObserver} from "@/hooks/use-intersection-observer"

const STORAGE_KEYS = {
    SCHEDULE_ID: "tg_poster_scheduleId",
    STATUS: "tg_poster_status",
    SORT_BY: "tg_poster_sortBy",
    SORT_DIR: "tg_poster_sortDirection",
    PAGE_SIZE: "tg_poster_pageSize"
};

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];

export function MessagesPage() {
    const queryClient = useQueryClient()

    // 1. Использование хука для автоматического кэширования состояний
    const [scheduleId, setScheduleId] = usePersistentState<string>(STORAGE_KEYS.SCHEDULE_ID, "")
    const [status, setStatus] = usePersistentState<MessageStatus>(STORAGE_KEYS.STATUS, MessageStatus.Planed)
    const [sortBy, setSortBy] = usePersistentState<MessageSortBy>(STORAGE_KEYS.SORT_BY, MessageStatus.All as unknown as MessageSortBy)
    const [sortDirection, setSortDirection] = usePersistentState<SortDirection>(STORAGE_KEYS.SORT_DIR, SortDirection.Asc)
    const [pageSize, setPageSize] = usePersistentState<number>(STORAGE_KEYS.PAGE_SIZE, 20)

    // Локальные состояния (не кэшируем)
    const [searchText, setSearchText] = useState("")
    const debouncedSearchText = useDebounce(searchText, 400)
    const [dateRange, setDateRange] = useState<DateRange | undefined>()

    // Вспомогательные состояния
    const [availableTimes, setAvailableTimes] = useState<string[] | null | undefined>([]);
    const [selectedMessageIds, setSelectedMessageIds] = useState<string[]>([])

    // API Query: Получение времени
    const {
        data: timesData,
        refetch: refetchTimes
    } = useGetApiV1MessageScheduleIdTime(scheduleId, {query: {enabled: !!scheduleId}});

    // Синхронизация времени
    useEffect(() => {
        setAvailableTimes(timesData?.postingTimes);
    }, [timesData]);

    const handleTimeSelect = useCallback((time: string) => {
        setAvailableTimes(prev => prev?.filter(t => t !== time));
    }, []);

    // API Query: Бесконечная загрузка сообщений
    const {
        data,
        isLoading,
        error,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage,
    } = useInfiniteMessages({
        ScheduleId: scheduleId,
        Status: status !== MessageStatus.All ? status : MessageStatus.All,
        SearchText: debouncedSearchText || undefined,
        CreatedFrom: dateRange?.from?.toISOString(),
        CreatedTo: dateRange?.to?.toISOString(),
        SortBy: sortBy,
        SortDirection: sortDirection,
        PageSize: pageSize,
    })

    const allMessages = useMemo(
        () => data?.pages.flatMap(p => p.data ?? []) ?? [],
        [data]
    )
    const totalCount = data?.pages[0]?.totalCount

    const sentinelRef = useIntersectionObserver(
        () => fetchNextPage(),
        {enabled: hasNextPage === true && !isFetchingNextPage}
    )

    // Мутации
    const deleteMessages = useDeleteApiV1Message({
        mutation: {
            onSuccess: () => {
                toast.success("Успех", {description: `Удалено сообщений: ${selectedMessageIds.length}`})
                setSelectedMessageIds([])
                queryClient.invalidateQueries({queryKey: ["/api/v1/message"]})
            },
            onError: (error) => toast("Ошибка", {description: error.title || "Не удалось удалить сообщения"}),
        },
    })

    const confirmMessages = usePatchApiV1Message({
        mutation: {
            onSuccess: () => {
                toast.success("Успех", {description: `Подтверждено сообщений: ${selectedMessageIds.length}`})
                setSelectedMessageIds([])
                queryClient.invalidateQueries({queryKey: ["/api/v1/message"]})
            },
            onError: (error) => toast("Ошибка", {description: error.title || "Не удалось подтвердить сообщения"}),
        },
    })

    // Сброс выделения и обновление времени при смене фильтров
    useEffect(() => {
        setSelectedMessageIds([])
        if (scheduleId) refetchTimes();
    }, [scheduleId, status, debouncedSearchText, dateRange, sortBy, sortDirection, pageSize, refetchTimes])

    // Обработчики выделения
    const handleSelectAll = (checked: boolean) => {
        if (checked && allMessages.length > 0) {
            setSelectedMessageIds(allMessages.map((m) => m.id))
        } else {
            setSelectedMessageIds([])
        }
    }

    const handleSelectMessage = (messageId: string, selected: boolean) => {
        setSelectedMessageIds((prev) =>
            selected ? [...prev, messageId] : prev.filter((id) => id !== messageId)
        )
    }

    // Обработчики действий
    const handleConfirmSelected = () => confirmMessages.mutate({data: {messagesIds: selectedMessageIds}})
    const handleDeleteSelected = () => deleteMessages.mutate({data: selectedMessageIds})

    // Сброс фильтров
    const handleResetFilters = useCallback(() => {
        setStatus(MessageStatus.Planed)
        setSortBy(MessageStatus.All as unknown as MessageSortBy)
        setSortDirection(SortDirection.Asc)
        setSearchText("")
        setDateRange(undefined)
        setPageSize(20)
    }, [setStatus, setSortBy, setSortDirection, setPageSize])

    const hasActiveFilters = useMemo(() => {
        return status !== MessageStatus.Planed ||
            debouncedSearchText !== "" ||
            dateRange !== undefined ||
            sortDirection !== SortDirection.Asc
    }, [status, debouncedSearchText, dateRange, sortDirection])

    // -- RENDER --
    return (
        <div className="container mx-auto p-6 space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="text-3xl font-bold">Управление сообщениями</h1>
                {scheduleId &&
                    <CreateMessageDialog
                        scheduleId={scheduleId}
                        availableTimes={availableTimes}
                        onTimeSelect={handleTimeSelect}
                    />}
            </div>

            <FiltersPanel
                scheduleId={scheduleId}
                status={status}
                searchText={searchText}
                sortBy={sortBy}
                sortDirection={sortDirection}
                dateRange={dateRange}
                onScheduleChange={setScheduleId}
                onStatusChange={setStatus}
                onSearchChange={setSearchText}
                onSortByChange={setSortBy}
                onSortDirectionChange={setSortDirection}
                onDateRangeChange={setDateRange}
                hasActiveFilters={hasActiveFilters}
                onResetFilters={handleResetFilters}
            />

            {/* Панель массовых действий */}
            {selectedMessageIds.length > 0 && (
                <Card className="border-primary sticky top-4 z-10 shadow-md">
                    <CardContent className="p-4">
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-4">
                                <span className="font-medium">Выбрано: {selectedMessageIds.length}</span>
                                <Button variant="outline" size="sm" onClick={() => setSelectedMessageIds([])}>
                                    Отменить
                                </Button>
                            </div>
                            <div className="flex items-center gap-2">
                                <Button onClick={handleConfirmSelected} disabled={confirmMessages.isPending} size="sm">
                                    <Check className="h-4 w-4 mr-2" />
                                    Подтвердить
                                </Button>
                                <AlertDialog>
                                    <AlertDialogTrigger asChild>
                                        <Button disabled={deleteMessages.isPending} variant="destructive" size="sm">
                                            <Trash2 className="h-4 w-4 mr-2" />
                                            Удалить
                                        </Button>
                                    </AlertDialogTrigger>
                                    <AlertDialogContent>
                                        <AlertDialogHeader>
                                            <AlertDialogTitle>Удалить сообщения?</AlertDialogTitle>
                                            <AlertDialogDescription>
                                                Вы собираетесь удалить {selectedMessageIds.length} {selectedMessageIds.length === 1 ? "сообщение" : "сообщений"}. Это действие нельзя отменить.
                                            </AlertDialogDescription>
                                        </AlertDialogHeader>
                                        <AlertDialogFooter>
                                            <AlertDialogCancel>Отмена</AlertDialogCancel>
                                            <AlertDialogAction onClick={handleDeleteSelected} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                                                Удалить
                                            </AlertDialogAction>
                                        </AlertDialogFooter>
                                    </AlertDialogContent>
                                </AlertDialog>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            )}

            {!scheduleId ? (
                <Alert>
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>Выберите расписание для отображения сообщений</AlertDescription>
                </Alert>
            ) : error ? (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>Произошла ошибка при загрузке сообщений.</AlertDescription>
                </Alert>
            ) : isLoading ? (
                <div className="space-y-4">
                    {/* Заглушка загрузки */}
                    <div className="flex items-center gap-3">
                        <Checkbox disabled />
                        <span className="text-sm font-medium">Выбрать все</span>
                    </div>
                    {Array.from({ length: 3 }).map((_, i) => (
                        <Card key={i}>
                            <CardContent className="p-4 flex gap-4">
                                <Skeleton className="h-4 w-4 mt-1" />
                                <div className="flex-1 space-y-2">
                                    <Skeleton className="h-4 w-1/4" />
                                    <Skeleton className="h-16 w-full" />
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            ) : allMessages.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-16 text-center">
                    <div className="rounded-full bg-muted p-6 mb-4">
                        <MessageSquarePlus className="h-10 w-10 text-muted-foreground"/>
                    </div>
                    <h3 className="text-lg font-semibold mb-1">Сообщений не найдено</h3>
                    <p className="text-sm text-muted-foreground mb-4 max-w-sm">
                        {debouncedSearchText || dateRange || status !== MessageStatus.Planed
                            ? "Попробуйте изменить фильтры или параметры поиска"
                            : "Создайте первое сообщение, чтобы начать"}
                    </p>
                    {!debouncedSearchText && !dateRange && status === MessageStatus.Planed && (
                        <CreateMessageDialog
                            scheduleId={scheduleId}
                            availableTimes={availableTimes}
                            onTimeSelect={handleTimeSelect}
                        />
                    )}
                </div>
            ) : (
                <div className="space-y-4">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <Checkbox
                                checked={allMessages.length > 0 && selectedMessageIds.length === allMessages.length}
                                onCheckedChange={handleSelectAll}
                            />
                            <span className="text-sm font-medium">Выбрать все</span>
                        </div>
                        <div className="flex items-center gap-4">
                            {totalCount != null && (
                                <span className="text-sm text-muted-foreground">
                                    Всего: {totalCount}
                                </span>
                            )}
                            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                <span>Размер загрузки:</span>
                                <Select
                                    value={pageSize.toString()}
                                    onValueChange={(value) => setPageSize(Number(value))}
                                >
                                    <SelectTrigger className="w-[70px] h-8">
                                        <SelectValue placeholder="20"/>
                                    </SelectTrigger>
                                    <SelectContent>
                                        {PAGE_SIZE_OPTIONS.map((size) => (
                                            <SelectItem key={size} value={size.toString()}>
                                                {size}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                        </div>
                    </div>

                    {allMessages.map((message) => (
                        <MessageCard
                            key={message.id}
                            message={message}
                            isSelected={selectedMessageIds.includes(message.id)}
                            onSelectionChange={(selected) => handleSelectMessage(message.id, selected)}
                            availableTimes={availableTimes}
                            onTimeSelect={handleTimeSelect}
                        />
                    ))}

                    {/* Sentinel для бесконечной прокрутки */}
                    <div ref={sentinelRef} className="h-1"/>

                    {isFetchingNextPage && (
                        <div className="flex justify-center py-6">
                            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground"/>
                        </div>
                    )}

                    {!hasNextPage && allMessages.length > 0 && (
                        <p className="text-center text-sm text-muted-foreground py-4">
                            Все сообщения загружены
                        </p>
                    )}
                </div>
            )}
        </div>
    )
}
