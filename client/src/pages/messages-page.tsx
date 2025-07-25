
import { useState, useEffect } from "react"
import { Check, AlertCircle } from "lucide-react"
import type { DateRange } from "react-day-picker"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Card, CardContent } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Alert, AlertDescription } from "@/components/ui/alert"
import  {MessageSortBy, MessageStatus, SortDirection} from "@/api/endpoints/tgPosterAPI.schemas"
import { useGetApiV1Message, usePatchApiV1Message } from "@/api/endpoints/message/message"
import {
    Pagination,
    PaginationContent,
    PaginationItem,
    PaginationLink,
    PaginationNext,
    PaginationPrevious
} from "@/components/ui/pagination"
import {FiltersPanel} from "@/components/message/filters-panel.tsx";
import {MessageCard} from "@/components/message/message-card.tsx";
import {CreateMessageDialog} from "@/components/message/create-message-dialog.tsx";
import {toast} from "sonner";

export function MessagesPage() {
    // Состояние фильтров
    const [scheduleId, setScheduleId] = useState("")
    const [status, setStatus] = useState<MessageStatus>(MessageStatus.NUMBER_0)
    const [searchText, setSearchText] = useState("")
    const [sortBy, setSortBy] = useState<MessageSortBy>(MessageSortBy.NUMBER_0)
    const [sortDirection, setSortDirection] = useState<SortDirection>(SortDirection.NUMBER_1)
    const [dateRange, setDateRange] = useState<DateRange | undefined>()
    const [currentPage, setCurrentPage] = useState(1)
    const pageSize = 10

    // Состояние выбора
    const [selectedMessageIds, setSelectedMessageIds] = useState<string[]>([])

    // API запросы
    const {
        data: messagesData,
        isLoading,
        error,
    } = useGetApiV1Message(
        {
            ScheduleId: scheduleId,
            Status: status !== 0 ? status : 0,
            SearchText: searchText || undefined,
            CreatedFrom: dateRange?.from?.toISOString(),
            CreatedTo: dateRange?.to?.toISOString(),
            SortBy: sortBy,
            SortDirection: sortDirection,
            PageNumber: currentPage,
            PageSize: pageSize,
        },
        {
            query: { enabled: !!scheduleId },
        },
    )

    const confirmMessages = usePatchApiV1Message({
        mutation: {
            onSuccess: () => {
                toast.success("Успех",{description: `Подтверждено сообщений: ${selectedMessageIds.length}`})
                setSelectedMessageIds([])
            },
            onError: (error) => {
                toast( "Ошибка",{description: error.title || "Не удалось подтвердить сообщения",})
            },
        },
    })

    // Сброс выбора при изменении фильтров
    useEffect(() => {
        setSelectedMessageIds([])
    }, [scheduleId, status, searchText, dateRange, sortBy, sortDirection, currentPage])

    // Сброс страницы при изменении фильтров
    useEffect(() => {
        setCurrentPage(1)
    }, [scheduleId, status, searchText, dateRange, sortBy, sortDirection])

    const handleSelectAll = (checked: boolean) => {
        if (checked && messagesData?.data) {
            setSelectedMessageIds(messagesData.data.map((m) => m.id))
        } else {
            setSelectedMessageIds([])
        }
    }

    const handleSelectMessage = (messageId: string, selected: boolean) => {
        if (selected) {
            setSelectedMessageIds((prev) => [...prev, messageId])
        } else {
            setSelectedMessageIds((prev) => prev.filter((id) => id !== messageId))
        }
    }

    const handleConfirmSelected = () => {
        confirmMessages.mutate({
            data: {
                messagesIds: selectedMessageIds,
            },
        })
    }

    const renderPagination = () => {
        if (!messagesData || !messagesData.totalPages || messagesData.totalPages <= 1) return null

        const pages = []
        const maxVisiblePages = 5
        const startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2))
        const endPage = Math.min(messagesData.totalPages, startPage + maxVisiblePages - 1)

        for (let i = startPage; i <= endPage; i++) {
            pages.push(i)
        }

        return (
            <Pagination>
                <PaginationContent>
                    <PaginationItem>
                        <PaginationPrevious
                            onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                            className={currentPage === 1 ? "pointer-events-none opacity-50" : "cursor-pointer"}
                        />
                    </PaginationItem>

                    {pages.map((page) => (
                        <PaginationItem key={page}>
                            <PaginationLink
                                onClick={() => setCurrentPage(page)}
                                isActive={currentPage === page}
                                className="cursor-pointer"
                            >
                                {page}
                            </PaginationLink>
                        </PaginationItem>
                    ))}

                    <PaginationItem>
                        <PaginationNext
                            onClick={() => setCurrentPage(Math.min(messagesData.totalPages!, currentPage + 1))}
                            className={currentPage === messagesData.totalPages ? "pointer-events-none opacity-50" : "cursor-pointer"}
                        />
                    </PaginationItem>
                </PaginationContent>
            </Pagination>
        )
    }

    return (
        <div className="container mx-auto p-6 space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="text-3xl font-bold">Управление сообщениями</h1>
                {scheduleId && <CreateMessageDialog scheduleId={scheduleId} />}
            </div>

            {/* Панель фильтров */}
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
            />

            {/* Контекстная панель для массовых операций */}
            {selectedMessageIds.length > 0 && (
                <Card className="border-primary">
                    <CardContent className="p-4">
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-4">
                                <span className="font-medium">Выбрано: {selectedMessageIds.length}</span>
                                <Button variant="outline" onClick={() => setSelectedMessageIds([])}>
                                    Отменить выбор
                                </Button>
                            </div>
                            <div className="flex items-center gap-2">
                                <Button onClick={handleConfirmSelected} disabled={confirmMessages.isPending}>
                                    <Check className="h-4 w-4 mr-2" />
                                    Подтвердить выбранные ({selectedMessageIds.length})
                                </Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* Основной контент */}
            {!scheduleId ? (
                <Alert>
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>Выберите расписание для отображения сообщений</AlertDescription>
                </Alert>
            ) : error ? (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>Произошла ошибка при загрузке сообщений. Попробуйте обновить страницу.</AlertDescription>
                </Alert>
            ) : isLoading ? (
                <div className="space-y-4">
                    <div className="flex items-center gap-3">
                        <Checkbox disabled />
                        <span className="text-sm font-medium">Выбрать все</span>
                    </div>
                    {Array.from({ length: 3 }).map((_, i) => (
                        <Card key={i}>
                            <CardContent className="p-4">
                                <div className="flex items-start gap-3">
                                    <Skeleton className="h-4 w-4 mt-1" />
                                    <div className="flex-1 space-y-3">
                                        <div className="flex items-center justify-between">
                                            <div className="flex items-center gap-2">
                                                <Skeleton className="h-5 w-20" />
                                                <Skeleton className="h-4 w-24" />
                                            </div>
                                            <Skeleton className="h-8 w-24" />
                                        </div>
                                        <Skeleton className="h-16 w-full" />
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            ) : !messagesData?.data?.length ? (
                <Alert>
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>
                        Сообщений не найдено. Попробуйте изменить фильтры или создать новое сообщение.
                    </AlertDescription>
                </Alert>
            ) : (
                <div className="space-y-4">
                    {/* Чекбокс "Выбрать все" */}
                    <div className="flex items-center gap-3">
                        <Checkbox
                            checked={messagesData.data.length > 0 && selectedMessageIds.length === messagesData.data.length}
                            onCheckedChange={handleSelectAll}
                        />
                        <span className="text-sm font-medium">Выбрать все</span>
                    </div>

                    {/* Список сообщений */}
                    {messagesData.data.map((message) => (
                        <MessageCard
                            key={message.id}
                            message={message}
                            isSelected={selectedMessageIds.includes(message.id)}
                            onSelectionChange={(selected) => handleSelectMessage(message.id, selected)}
                        />
                    ))}

                    {/* Пагинация */}
                    <div className="flex justify-center">{renderPagination()}</div>
                </div>
            )}
        </div>
    )
}
