import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { Calendar, Clock, Plus, Save, X, ArrowLeft, Settings, Trash2, Loader2, Power, PowerOff } from "lucide-react"
import { Separator } from "@/components/ui/separator"
import { toast } from "sonner"
import {useGetApiV1TelegramBot} from "@/api/endpoints/telegram-bot/telegram-bot.ts";
import {useDeleteApiV1ScheduleId, useGetApiV1Schedule, usePatchApiV1ScheduleIdStatus, usePostApiV1Schedule} from "@/api/endpoints/schedule/schedule"
import {useGetApiV1Day, usePatchApiV1DayTime} from "@/api/endpoints/day/day"
import type {CreateScheduleRequest, DayOfWeek, ScheduleResponse} from "@/api/endpoints/tgPosterAPI.schemas"
import {Switch} from "@/components/ui/switch.tsx";

interface NewTimeSlot {
    hour: string
    minute: string
}

interface IntervalTimeSlot {
    startHour: string
    startMinute: string
    endHour: string
    endMinute: string
    intervalMinutes: number
}

export default function ScheduleManager() {

    const { data: schedules = [], isLoading: schedulesLoading, refetch: refetchSchedules } = useGetApiV1Schedule()
    const { data: telegramBots = [], isLoading: botsLoading } = useGetApiV1TelegramBot()
    const createScheduleMutation = usePostApiV1Schedule()
    const deleteScheduleMutation = useDeleteApiV1ScheduleId()
    const toggleActiveMutation = usePatchApiV1ScheduleIdStatus()
    const updateTimeMutation = usePatchApiV1DayTime()

    const [currentView, setCurrentView] = useState<"list" | "create" | "edit">("list")
    const [editingSchedule, setEditingSchedule] = useState<ScheduleResponse | null>(null)
    const [newSchedule, setNewSchedule] = useState<CreateScheduleRequest>({
        name: "",
        channel: "",
        telegramBotId: "",
    })

    const [editingDay, setEditingDay] = useState<DayOfWeek | null>(null)
    const [newTimeSlot, setNewTimeSlot] = useState<NewTimeSlot>({
        hour: "09",
        minute: "00",
    })

    const [timeMode, setTimeMode] = useState<"single" | "interval">("single")
    const [intervalTimeSlot, setIntervalTimeSlot] = useState<IntervalTimeSlot>({
        startHour: "09",
        startMinute: "00",
        endHour: "17",
        endMinute: "00",
        intervalMinutes: 120,
    })

    // Get days for editing schedule
    const { data: scheduleDays = [], refetch: refetchDays } = useGetApiV1Day(
        { scheduleId: editingSchedule?.id || "" },
        { query: { enabled: !!editingSchedule?.id } },
    )

    const handleCreateSchedule = async () => {
        if (!newSchedule.name || !newSchedule.channel || !newSchedule.telegramBotId) {
            toast.error("Заполните все обязательные поля")
            return
        }

        try {
            await createScheduleMutation.mutateAsync({ data: newSchedule })
            setNewSchedule({ name: "", channel: "", telegramBotId: "" })
            setCurrentView("list")
            await refetchSchedules()
            toast.success("Расписание создано")
        } catch {
            toast.error("Не удалось создать расписание")
        }
    }

    const handleEditSchedule = (schedule: ScheduleResponse) => {
        setEditingSchedule(schedule)
        setCurrentView("edit")
    }

    const handleDeleteSchedule = async (scheduleId: string) => {
        try {
            await deleteScheduleMutation.mutateAsync({ id: scheduleId })
            await refetchSchedules()
            toast.success("Расписание удалено")
        } catch {
            toast.error("Не удалось удалить расписание")
        }
    }

    const handleToggleActive = async (scheduleId: string, currentStatus: boolean) => {
        try {
            await toggleActiveMutation.mutateAsync({ id: scheduleId })
            await refetchSchedules()
            toast.success(`Расписание ${currentStatus ? "деактивировано" : "активировано"}`)
        } catch {
            toast.error("Не удалось изменить статус расписания")
        }
    }

    const addTimeToDay = async (dayOfWeek: DayOfWeek) => {
        if (!editingSchedule) return

        try {
            if (timeMode === "single") {
                // Добавить одно время
                const time = `${newTimeSlot.hour}:${newTimeSlot.minute}`
                const existingDay = scheduleDays.find((day) => day.dayOfWeek === dayOfWeek)
                const currentTimes = existingDay?.timePostings || []

                await updateTimeMutation.mutateAsync({
                    data: {
                        scheduleId: editingSchedule.id,
                        dayOfWeek,
                        times: [...currentTimes, time],
                    },
                })
            } else {
                // Добавить через интервал
                const startTimeMinutes =
                    Number.parseInt(intervalTimeSlot.startHour) * 60 + Number.parseInt(intervalTimeSlot.startMinute)
                const endTimeMinutes =
                    Number.parseInt(intervalTimeSlot.endHour) * 60 + Number.parseInt(intervalTimeSlot.endMinute)

                const newTimes: string[] = []
                for (let time = startTimeMinutes; time <= endTimeMinutes; time += intervalTimeSlot.intervalMinutes) {
                    const hours = Math.floor(time / 60)
                        .toString()
                        .padStart(2, "0")
                    const minutes = (time % 60).toString().padStart(2, "0")
                    newTimes.push(`${hours}:${minutes}`)
                }

                const existingDay = scheduleDays.find((day) => day.dayOfWeek === dayOfWeek)
                const currentTimes = existingDay?.timePostings || []

                await updateTimeMutation.mutateAsync({
                    data: {
                        scheduleId: editingSchedule.id,
                        dayOfWeek,
                        times: [...currentTimes, ...newTimes],
                    },
                })
            }

            await refetchDays()
            setEditingDay(null)
            setNewTimeSlot({ hour: "09", minute: "00" })
            setIntervalTimeSlot({
                startHour: "09",
                startMinute: "00",
                endHour: "17",
                endMinute: "00",
                intervalMinutes: 120,
            })

            toast.success("Время добавлено")
        } catch {
            toast.error("Не удалось добавить время")
        }
    }

    const removeTimeFromDay = async (dayOfWeek: DayOfWeek, timeToRemove: string) => {
        if (!editingSchedule) return

        try {
            const day = scheduleDays.find((d) => d.dayOfWeek === dayOfWeek)
            if (!day) return

            const updatedTimes = (day.timePostings || []).filter((time) => time !== timeToRemove)

            await updateTimeMutation.mutateAsync({
                data: {
                    scheduleId: editingSchedule.id,
                    dayOfWeek,
                    times: updatedTimes,
                },
            })

            await refetchDays()
            toast.success("Время удалено")
        } catch {
            toast.error("Не удалось удалить время")
        }
    }

    const generateHourOptions = () => {
        return Array.from({ length: 24 }, (_, i) => {
            const hour = i.toString().padStart(2, "0")
            return (
                <SelectItem key={hour} value={hour}>
                    {hour}
                </SelectItem>
            )
        })
    }

    const generateMinuteOptions = () => {
        return Array.from({ length: 60 }, (_, i) => {
            const minute = i.toString().padStart(2, "0")
            return (
                <SelectItem key={minute} value={minute}>
                    {minute}
                </SelectItem>
            )
        })
    }

    const getDayName = (dayOfWeek: DayOfWeek) => {
        const dayNames = ["Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота"]
        return dayNames[dayOfWeek] || "Неизвестный день"
    }

    const getTimesForDay = (dayOfWeek: DayOfWeek) => {
        const day = scheduleDays.find((d) => d.dayOfWeek === dayOfWeek)
        return day?.timePostings || []
    }

    const DAYS_ORDER: DayOfWeek[] = [1, 2, 3, 4, 5, 6, 0] // Понедельник - Воскресенье

    // Список расписаний
    if (currentView === "list") {
        return (
            <div className="max-w-6xl mx-auto p-6">
                <div className="flex items-center justify-between mb-6">
                    <div>
                        <h1 className="text-3xl font-bold">Расписания</h1>
                        <p className="text-muted-foreground">Управление расписаниями постинга в Telegram</p>
                    </div>
                    <Button onClick={() => setCurrentView("create")} className="flex items-center gap-2">
                        <Plus className="h-4 w-4" />
                        Создать расписание
                    </Button>
                </div>

                {schedulesLoading ? (
                    <div className="flex items-center justify-center py-12">
                        <Loader2 className="h-8 w-8 animate-spin" />
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                        {schedules.map((schedule) => (
                            <Card key={schedule.id} className="cursor-pointer hover:shadow-md transition-shadow">
                                <CardHeader className="pb-3">
                                    <div className="flex items-start justify-between">
                                        <div className="flex-1">
                                            <div className="flex items-center gap-2 mb-1">
                                                <CardTitle className="text-lg">{schedule.name}</CardTitle>
                                                <Badge variant={schedule.isActive ? "default" : "secondary"} className="text-xs">
                                                    {schedule.isActive ? (
                                                        <>
                                                            <Power className="h-3 w-3 mr-1" />
                                                            Активно
                                                        </>
                                                    ) : (
                                                        <>
                                                            <PowerOff className="h-3 w-3 mr-1" />
                                                            Неактивно
                                                        </>
                                                    )}
                                                </Badge>
                                            </div>
                                            <CardDescription className="mt-1">{schedule.channelName || "Канал не указан"}</CardDescription>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <Switch
                                                checked={schedule.isActive}
                                                onCheckedChange={() => handleToggleActive(schedule.id, schedule.isActive)}
                                                onClick={(e) => e.stopPropagation()}
                                                disabled={toggleActiveMutation.isPending}
                                            />
                                        </div>
                                    </div>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    <div className="flex items-center justify-between text-sm">
                                        <span className="text-muted-foreground">Статус:</span>
                                        <span className={schedule.isActive ? "text-green-600" : "text-gray-500"}>
                      {schedule.isActive ? "Работает" : "Остановлено"}
                    </span>
                                    </div>

                                    <div className="space-y-2">
                                        <span className="text-sm text-muted-foreground">Активные дни:</span>
                                        <div className="flex flex-wrap gap-1">
                                            {DAYS_ORDER.map((dayOfWeek) => {
                                                // Здесь можно добавить логику для определения активных дней
                                                // Пока используем заглушку
                                                const hasTime = Math.random() > 0.5 // Заглушка
                                                return (
                                                    <Badge key={dayOfWeek} variant={hasTime ? "default" : "outline"} className="text-xs">
                                                        {getDayName(dayOfWeek).slice(0, 2)}
                                                    </Badge>
                                                )
                                            })}
                                        </div>
                                    </div>

                                    <Separator />

                                    <div className="flex items-center justify-between">
                                        <span className="text-xs text-muted-foreground">ID: {schedule.id}</span>
                                        <div className="flex gap-2">
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                onClick={(e) => {
                                                    e.stopPropagation()
                                                    handleEditSchedule(schedule)
                                                }}
                                            >
                                                <Settings className="h-4 w-4" />
                                            </Button>
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                onClick={(e) => {
                                                    e.stopPropagation()
                                                    handleDeleteSchedule(schedule.id)
                                                }}
                                                disabled={deleteScheduleMutation.isPending}
                                            >
                                                {deleteScheduleMutation.isPending ? (
                                                    <Loader2 className="h-4 w-4 animate-spin" />
                                                ) : (
                                                    <Trash2 className="h-4 w-4" />
                                                )}
                                            </Button>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                    </div>
                )}

                {schedules.length === 0 && !schedulesLoading && (
                    <div className="text-center py-12">
                        <Calendar className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                        <h3 className="text-lg font-medium mb-2">Нет расписаний</h3>
                        <p className="text-muted-foreground mb-4">Создайте первое расписание для начала работы</p>
                        <Button onClick={() => setCurrentView("create")}>
                            <Plus className="h-4 w-4 mr-2" />
                            Создать расписание
                        </Button>
                    </div>
                )}
            </div>
        )
    }

    // Форма создания расписания
    if (currentView === "create") {
        return (
            <div className="max-w-2xl mx-auto p-6">
                <div className="flex items-center gap-4 mb-6">
                    <Button variant="outline" size="sm" onClick={() => setCurrentView("list")}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <div>
                        <h1 className="text-2xl font-bold">Создание расписания</h1>
                        <p className="text-muted-foreground">Заполните основную информацию</p>
                    </div>
                </div>

                <Card>
                    <CardContent className="space-y-6 pt-6">
                        <div className="space-y-2">
                            <Label htmlFor="name">Название расписания</Label>
                            <Input
                                id="name"
                                placeholder="Введите название расписания"
                                value={newSchedule.name}
                                onChange={(e) => setNewSchedule((prev) => ({ ...prev, name: e.target.value }))}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="channel">Канал Telegram</Label>
                            <Input
                                id="channel"
                                placeholder="@channel_name или ID канала"
                                value={newSchedule.channel}
                                onChange={(e) => setNewSchedule((prev) => ({ ...prev, channel: e.target.value }))}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="bot">Telegram бот</Label>
                            <Select
                                value={newSchedule.telegramBotId}
                                onValueChange={(value) => setNewSchedule((prev) => ({ ...prev, telegramBotId: value }))}
                            >
                                <SelectTrigger>
                                    <SelectValue placeholder="Выберите бота" />
                                </SelectTrigger>
                                <SelectContent>
                                    {botsLoading ? (
                                        <SelectItem value="" disabled>
                                            Загрузка...
                                        </SelectItem>
                                    ) : (
                                        telegramBots.map((bot) => (
                                            <SelectItem key={bot.id} value={bot.id}>
                                                {bot.name || `Бот ${bot.id}`}
                                            </SelectItem>
                                        ))
                                    )}
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="flex gap-2 pt-4">
                            <Button
                                onClick={handleCreateSchedule}
                                className="flex-1"
                                disabled={
                                    !newSchedule.name ||
                                    !newSchedule.channel ||
                                    !newSchedule.telegramBotId ||
                                    createScheduleMutation.isPending
                                }
                            >
                                {createScheduleMutation.isPending ? (
                                    <>
                                        <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                        Создание...
                                    </>
                                ) : (
                                    "Создать расписание"
                                )}
                            </Button>
                            <Button variant="outline" onClick={() => setCurrentView("list")}>
                                Отмена
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            </div>
        )
    }

    // Редактирование расписания
    if (currentView === "edit" && editingSchedule) {
        return (
            <div className="max-w-4xl mx-auto p-6">
                <div className="flex items-center gap-4 mb-6">
                    <Button variant="outline" size="sm" onClick={() => setCurrentView("list")}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <div className="flex-1">
                        <div className="flex items-center gap-3">
                            <h1 className="text-2xl font-bold">{editingSchedule.name}</h1>
                            <Badge variant={editingSchedule.isActive ? "default" : "secondary"}>
                                {editingSchedule.isActive ? (
                                    <>
                                        <Power className="h-3 w-3 mr-1" />
                                        Активно
                                    </>
                                ) : (
                                    <>
                                        <PowerOff className="h-3 w-3 mr-1" />
                                        Неактивно
                                    </>
                                )}
                            </Badge>
                        </div>
                        <p className="text-muted-foreground">{editingSchedule.channelName || "Канал не указан"}</p>
                    </div>
                    <div className="flex items-center gap-2">
                        <Label htmlFor="schedule-active" className="text-sm">
                            Активность:
                        </Label>
                        <Switch
                            id="schedule-active"
                            checked={editingSchedule.isActive}
                            onCheckedChange={() => handleToggleActive(editingSchedule.id, editingSchedule.isActive)}
                            disabled={toggleActiveMutation.isPending}
                        />
                    </div>
                </div>

                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Clock className="h-5 w-5" />
                            Настройка времени отправки
                        </CardTitle>
                        <CardDescription>Настройте время отправки сообщений для каждого дня недели</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-6">
                        {DAYS_ORDER.map((dayOfWeek) => (
                            <div key={dayOfWeek} className="space-y-3">
                                <div className="flex items-center justify-between">
                                    <Label className="text-base font-medium">{getDayName(dayOfWeek)}</Label>
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => setEditingDay(dayOfWeek)}
                                        className="flex items-center gap-2"
                                    >
                                        <Plus className="h-4 w-4" />
                                        Добавить время
                                    </Button>
                                </div>

                                <div className="space-y-2">
                                    {getTimesForDay(dayOfWeek).map((time, index) => (
                                        <div key={index} className="flex items-center justify-between p-3 border rounded-lg">
                                            <Badge variant="secondary">{time}</Badge>
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                onClick={() => removeTimeFromDay(dayOfWeek, time)}
                                                className="text-destructive hover:text-destructive"
                                                disabled={updateTimeMutation.isPending}
                                            >
                                                {updateTimeMutation.isPending ? (
                                                    <Loader2 className="h-4 w-4 animate-spin" />
                                                ) : (
                                                    <Trash2 className="h-4 w-4" />
                                                )}
                                            </Button>
                                        </div>
                                    ))}

                                    {getTimesForDay(dayOfWeek).length === 0 && (
                                        <p className="text-sm text-muted-foreground p-3 border rounded-lg border-dashed">Время не задано</p>
                                    )}
                                </div>

                                {editingDay === dayOfWeek && (
                                    <div className="space-y-4 p-4 border rounded-lg bg-muted/50">
                                        <div className="space-y-3">
                                            <div className="flex items-center space-x-4">
                                                <div className="flex items-center space-x-2">
                                                    <input
                                                        type="radio"
                                                        id="single-mode"
                                                        name="time-mode"
                                                        checked={timeMode === "single"}
                                                        onChange={() => setTimeMode("single")}
                                                    />
                                                    <Label htmlFor="single-mode">Добавить одно время</Label>
                                                </div>
                                                <div className="flex items-center space-x-2">
                                                    <input
                                                        type="radio"
                                                        id="interval-mode"
                                                        name="time-mode"
                                                        checked={timeMode === "interval"}
                                                        onChange={() => setTimeMode("interval")}
                                                    />
                                                    <Label htmlFor="interval-mode">Добавить через интервал</Label>
                                                </div>
                                            </div>

                                            {timeMode === "single" ? (
                                                <div className="flex items-center gap-2">
                                                    <Label className="text-sm font-medium">Время:</Label>
                                                    <Select
                                                        value={newTimeSlot.hour}
                                                        onValueChange={(value) => setNewTimeSlot((prev) => ({ ...prev, hour: value }))}
                                                    >
                                                        <SelectTrigger className="w-20">
                                                            <SelectValue />
                                                        </SelectTrigger>
                                                        <SelectContent>{generateHourOptions()}</SelectContent>
                                                    </Select>

                                                    <span>:</span>
                                                    <Select
                                                        value={newTimeSlot.minute}
                                                        onValueChange={(value) => setNewTimeSlot((prev) => ({ ...prev, minute: value }))}
                                                    >
                                                        <SelectTrigger className="w-20">
                                                            <SelectValue />
                                                        </SelectTrigger>
                                                        <SelectContent>{generateMinuteOptions()}</SelectContent>
                                                    </Select>
                                                </div>
                                            ) : (
                                                <div className="space-y-3">
                                                    <div className="grid grid-cols-2 gap-4">
                                                        <div className="space-y-2">
                                                            <Label className="text-sm font-medium">Время начала:</Label>
                                                            <div className="flex items-center gap-2">
                                                                <Select
                                                                    value={intervalTimeSlot.startHour}
                                                                    onValueChange={(value) =>
                                                                        setIntervalTimeSlot((prev) => ({ ...prev, startHour: value }))
                                                                    }
                                                                >
                                                                    <SelectTrigger className="w-20">
                                                                        <SelectValue />
                                                                    </SelectTrigger>
                                                                    <SelectContent>{generateHourOptions()}</SelectContent>
                                                                </Select>
                                                                <span>:</span>
                                                                <Select
                                                                    value={intervalTimeSlot.startMinute}
                                                                    onValueChange={(value) =>
                                                                        setIntervalTimeSlot((prev) => ({ ...prev, startMinute: value }))
                                                                    }
                                                                >
                                                                    <SelectTrigger className="w-20">
                                                                        <SelectValue />
                                                                    </SelectTrigger>
                                                                    <SelectContent>{generateMinuteOptions()}</SelectContent>
                                                                </Select>
                                                            </div>
                                                        </div>
                                                        <div className="space-y-2">
                                                            <Label className="text-sm font-medium">Время окончания:</Label>
                                                            <div className="flex items-center gap-2">
                                                                <Select
                                                                    value={intervalTimeSlot.endHour}
                                                                    onValueChange={(value) =>
                                                                        setIntervalTimeSlot((prev) => ({ ...prev, endHour: value }))
                                                                    }
                                                                >
                                                                    <SelectTrigger className="w-20">
                                                                        <SelectValue />
                                                                    </SelectTrigger>
                                                                    <SelectContent>{generateHourOptions()}</SelectContent>
                                                                </Select>
                                                                <span>:</span>
                                                                <Select
                                                                    value={intervalTimeSlot.endMinute}
                                                                    onValueChange={(value) =>
                                                                        setIntervalTimeSlot((prev) => ({ ...prev, endMinute: value }))
                                                                    }
                                                                >
                                                                    <SelectTrigger className="w-20">
                                                                        <SelectValue />
                                                                    </SelectTrigger>
                                                                    <SelectContent>{generateMinuteOptions()}</SelectContent>
                                                                </Select>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div className="space-y-2">
                                                        <Label className="text-sm font-medium">Интервал (минуты):</Label>
                                                        <Input
                                                            value={intervalTimeSlot.intervalMinutes}
                                                            onChange={(value) =>
                                                                setIntervalTimeSlot((prev) => ({ ...prev, intervalMinutes: Number.parseInt(value.target.value) }))
                                                            }
                                                        >
                                                        </Input>
                                                    </div>
                                                </div>
                                            )}
                                        </div>

                                        <div className="flex gap-2">
                                            <Button size="sm" onClick={() => addTimeToDay(dayOfWeek)} disabled={updateTimeMutation.isPending}>
                                                {updateTimeMutation.isPending ? (
                                                    <>
                                                        <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                                        Добавление...
                                                    </>
                                                ) : (
                                                    <>
                                                        <Save className="h-4 w-4 mr-2" />
                                                        Добавить
                                                    </>
                                                )}
                                            </Button>
                                            <Button variant="outline" size="sm" onClick={() => setEditingDay(null)}>
                                                <X className="h-4 w-4 mr-2" />
                                                Отмена
                                            </Button>
                                        </div>
                                    </div>
                                )}

                                <Separator />
                            </div>
                        ))}
                    </CardContent>
                </Card>
            </div>
        )
    }

    return null
}