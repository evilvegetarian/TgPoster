"use client"

import { CalendarIcon, Search } from "lucide-react"
import { format } from "date-fns"
import { ru } from "date-fns/locale"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Card, CardContent } from "@/components/ui/card"
import { cn } from "@/lib/utils"
import type {MessageSortBy, MessageStatus, SortDirection} from "@/api/endpoints/tgPosterAPI.schemas"
import { useGetApiV1Schedule } from "@/api/endpoints/schedule/schedule"
import {Popover, PopoverContent, PopoverTrigger} from "@/components/ui/popover.tsx";
import {Calendar} from "@/components/ui/calendar.tsx";
import {
    useGetApiOptionsMessageSortFields,
    useGetApiOptionsMessageStatuses, useGetApiOptionsSortDirections,
} from "@/api/endpoints/options/options.ts";
import type {DateRange} from "react-day-picker";

interface FiltersPanelProps {
    scheduleId: string
    status: MessageStatus
    searchText: string
    sortBy: MessageSortBy
    sortDirection: SortDirection
    dateRange: DateRange | undefined
    onScheduleChange: (value: string) => void
    onStatusChange: (value: MessageStatus) => void
    onSearchChange: (value: string) => void
    onSortByChange: (value: MessageSortBy) => void
    onSortDirectionChange: (value: SortDirection) => void
    onDateRangeChange: (range: DateRange | undefined) => void
}

export function FiltersPanel({
                                 scheduleId,
                                 status,
                                 searchText,
                                 sortBy,
                                 sortDirection,
                                 dateRange,
                                 onScheduleChange,
                                 onStatusChange,
                                 onSearchChange,
                                 onSortByChange,
                                 onSortDirectionChange,
                                 onDateRangeChange,
                             }: FiltersPanelProps) {
    const { data: schedules, isLoading: schedulesLoading } = useGetApiV1Schedule()
    const { data: sortFields, isLoading: sortFieldsLoading } = useGetApiOptionsMessageSortFields()
    const { data: statusMessage, isLoading: statusMessageLoading } = useGetApiOptionsMessageStatuses()
    const { data: sortDirections, isLoading: sortDirectionsLoading } = useGetApiOptionsSortDirections()

    return (
        <Card>
            <CardContent className="p-6">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                    {/* Выбор расписания */}
                    <div className="space-y-2">
                        <label className="text-sm font-medium">Расписание *</label>
                        <Select value={scheduleId} onValueChange={onScheduleChange} disabled={schedulesLoading}>
                            <SelectTrigger>
                                <SelectValue placeholder="Выберите расписание" />
                            </SelectTrigger>
                            <SelectContent>
                                {schedules?.map((schedule) => (
                                    <SelectItem key={schedule.id} value={schedule.id}>
                                        {schedule.name}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                    {/* Фильтр по статусу */}
                    <div className="space-y-2">
                        <label className="text-sm font-medium">Статус</label>
                        <Select
                            value={status.toString()}
                            onValueChange={(value) => onStatusChange(Number(value) as MessageStatus)}
                            disabled={!scheduleId && statusMessageLoading}
                        >
                            <SelectTrigger>
                                <SelectValue placeholder="Все статусы" />
                            </SelectTrigger>
                            <SelectContent>
                                {statusMessage?.map((stat) => (
                                    <SelectItem key={stat.value} value={stat.value.toString()}>
                                        {stat.name}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm font-medium">Сортировка</label>
                        <Select
                            value={sortBy.toString()}
                            onValueChange={(value) => onSortByChange(Number(value) as MessageSortBy)}
                            disabled={!scheduleId && sortFieldsLoading}
                        >
                            <SelectTrigger>
                                <SelectValue placeholder="Поле сортировки" />
                            </SelectTrigger>
                            <SelectContent>
                                {sortFields?.map((sortf) => (
                                    <SelectItem key={sortf.value} value={sortf.value.toString()}>
                                        {sortf.name}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                    {/* Направление сортировки */}
                    <div className="space-y-2">
                        <label className="text-sm font-medium">Направление</label>
                        <Select
                            value={sortDirection.toString()}
                            onValueChange={(value) => onSortDirectionChange(Number(value) as SortDirection)}
                            disabled={!scheduleId && sortDirectionsLoading}
                        >
                            <SelectTrigger>
                                <SelectValue placeholder="Направление" />
                            </SelectTrigger>
                            <SelectContent>
                                {sortDirections?.map((sortD) => (
                                    <SelectItem key={sortD.value} value={sortD.value.toString()}>
                                        {sortD.name}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                    {/* Поиск */}
                    <div className="space-y-2">
                        <label className="text-sm font-medium">Поиск</label>
                        <div className="relative">
                            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                            <Input
                                placeholder="Поиск по тексту..."
                                value={searchText}
                                onChange={(e) => onSearchChange(e.target.value)}
                                className="pl-10"
                                disabled={!scheduleId}
                            />
                        </div>
                    </div>

                    {/* Фильтр по дате */}
                    <div className="space-y-2 md:col-span-2 lg:col-span-3">
                        <label className="text-sm font-medium">Период создания</label>
                        <Popover>
                            <PopoverTrigger asChild>
                                <Button
                                    variant="outline"
                                    className={cn("w-full justify-start text-left font-normal", !dateRange && "text-muted-foreground")}
                                    disabled={!scheduleId}
                                >
                                    <CalendarIcon className="mr-2 h-4 w-4" />
                                    {dateRange?.from ? (
                                        dateRange.to ? (
                                            <>
                                                {format(dateRange.from, "dd.MM.yyyy", { locale: ru })} -{" "}
                                                {format(dateRange.to, "dd.MM.yyyy", { locale: ru })}
                                            </>
                                        ) : (
                                            format(dateRange.from, "dd.MM.yyyy", { locale: ru })
                                        )
                                    ) : (
                                        "Выберите период"
                                    )}
                                </Button>
                            </PopoverTrigger>
                            <PopoverContent className="w-auto p-0" align="start">
                                <Calendar
                                    mode="range"
                                    defaultMonth={dateRange?.from}
                                    selected={dateRange}
                                    onSelect={onDateRangeChange}
                                    numberOfMonths={2}
                                    locale={ru}
                                />
                            </PopoverContent>
                        </Popover>
                    </div>
                </div>
            </CardContent>
        </Card>
    )
}
