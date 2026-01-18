import {useEffect, useMemo, useState} from "react"
import {Button} from "@/components/ui/button.tsx"
import {Card, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx"
import {Input} from "@/components/ui/input.tsx"
import {Label} from "@/components/ui/label.tsx"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select.tsx"
import {Badge} from "@/components/ui/badge.tsx"
import {ArrowLeft, Calendar, Clock, Loader2, Plus, Power, PowerOff, Save, Settings, Trash2, X,} from "lucide-react"
import {Separator} from "@/components/ui/separator.tsx"
import {toast} from "sonner"
import {
    useDeleteApiV1ScheduleId,
    useGetApiV1Schedule,
    usePatchApiV1ScheduleIdStatus, usePutApiV1ScheduleId,
} from "@/api/endpoints/schedule/schedule.ts"
import {useGetApiV1Day, usePatchApiV1DayTime} from "@/api/endpoints/day/day.ts"
import type {DayOfWeek, ScheduleResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts"
import {Switch} from "@/components/ui/switch.tsx"
import {Popover, PopoverContent, PopoverTrigger} from "@/components/ui/popover.tsx"
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/components/ui/tabs.tsx"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog.tsx"
import {EditIiComponent} from "@/pages/edit-ii-component.tsx";
import {usePutApiV1MessageScheduleIdTimes} from "@/api/endpoints/message/message.ts";
import {convertLocalToIsoTime, convertUtcTimeToLocalTime} from "@/utils/convertLocalToIsoTime.tsx"
import {CreateScheduleComponent} from "@/pages/schedulepage/create-schedule-component.tsx";
import {useGetApiV1Youtube} from "@/api/endpoints/you-tube-account/you-tube-account.ts";


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

interface ScheduleDay {
    dayOfWeek: DayOfWeek
    timePostings?: string[] | null
}

const HOURS = Array.from({length: 24}, (_, i) => i.toString().padStart(2, "0"))
const MINUTES = Array.from({length: 60}, (_, i) => i.toString().padStart(2, "0"))
const DAYS_ORDER: DayOfWeek[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
export const DAY_NAMES: Record<string, string> = {
    'Monday': "Понедельник",
    'Tuesday': "Вторник",
    'Wednesday': "Среда",
    'Thursday': "Четверг",
    'Friday': "Пятница",
    'Saturday': "Суббота",
    'Sunday': "Воскресенье",
};

const DEFAULT_NEW_TIME_SLOT: NewTimeSlot = {
    hour: "09",
    minute: "00",
}

const DEFAULT_INTERVAL_TIME_SLOT: IntervalTimeSlot = {
    startHour: "09",
    startMinute: "00",
    endHour: "22",
    endMinute: "00",
    intervalMinutes: 120,
}

export function SchedulePage() {
    const {data: schedules = [], isLoading: schedulesLoading, refetch: refetchSchedules} = useGetApiV1Schedule();


    const {mutate: deleteScheduleMutate, isPending: deleteSchedulePending} = useDeleteApiV1ScheduleId({
        mutation: {
            onSuccess: () => {
                refetchSchedules();
                toast.success("Расписание удалено");
            },
            onError: () => {
                toast.error("Не удалось удалить расписание");
            },
        },
    });

    const {mutate: toggleActiveMutate, isPending: toggleActivePending} = usePatchApiV1ScheduleIdStatus({
        mutation: {
            onSuccess: () => {
                refetchSchedules();
            },
            onError: () => {
                toast.error("Не удалось изменить статус расписания");
            },
        },
    });

    const {mutate: updateTimeMutate, isPending: updateTimePending} = usePatchApiV1DayTime({
        mutation: {
            onSuccess: () => {
                refetchDays();
            },
            onError: () => {
                toast.error("Не удалось обновить время");
            },
        },
    });

    const {data: youtubeAccounts = [], isLoading: youtubeLoading} = useGetApiV1Youtube();

    const {mutate: updateScheduleMutate, isPending: updateSchedulePending} = usePutApiV1ScheduleId({
        mutation: {
            onSuccess: () => {
                toast.success("Расписание обновлено");
            },
            onError: () => {
                toast.error("Не удалось обновить расписание");
            },
        },
    });

    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)

    const [editingSchedule, setEditingSchedule] = useState<ScheduleResponse | null>(null)

    const [timeMode, setTimeMode] = useState<"single" | "interval">("single")
    const [newTimeSlot, setNewTimeSlot] = useState<NewTimeSlot>({...DEFAULT_NEW_TIME_SLOT})
    const [intervalTimeSlot, setIntervalTimeSlot] = useState<IntervalTimeSlot>({...DEFAULT_INTERVAL_TIME_SLOT})
    const [popoverOpenForDay, setPopoverOpenForDay] = useState<DayOfWeek | null>(null)

    const {
        data: scheduleDaysData,
        refetch: refetchDays,
        isFetching: daysFetching,
    } = useGetApiV1Day(
        {scheduleId: editingSchedule?.id || ""},
        {query: {enabled: !!editingSchedule?.id}},
    )

    const scheduleDays = useMemo<ScheduleDay[]>(() => {
        if (!Array.isArray(scheduleDaysData)) {
            return [];
        }
        return scheduleDaysData.map(day => {
            const localTimePostings = day.timePostings?.sort().map(utcTime => convertUtcTimeToLocalTime(utcTime));
            return {
                ...day,
                timePostings: localTimePostings
            };
        });
    }, [scheduleDaysData]);

    useEffect(() => {
        if (!isEditDialogOpen) {
            setEditingSchedule(null)
            resetTimeInputs()
        }
    }, [isEditDialogOpen])


    const resetTimeInputs = () => {
        setPopoverOpenForDay(null)
        setTimeMode("single")
        setNewTimeSlot({...DEFAULT_NEW_TIME_SLOT})
        setIntervalTimeSlot({...DEFAULT_INTERVAL_TIME_SLOT})
    }

    const getTimesForDay = (dayOfWeek: DayOfWeek) => {
        return scheduleDays.find((day) => day.dayOfWeek === dayOfWeek)?.timePostings ?? []
    }


    const {mutate: updateTimeMessagesMutate, isPending: updateTimeMessagesPending} = usePutApiV1MessageScheduleIdTimes({
        mutation: {
            onSuccess: () => {
                toast.success("Успех", {description: `Обновлено время`})
            },
            onError: (error) => {
                toast("Ошибка", {description: error.title || "Не удалось обновить время",})
            },
        },
    })

    const handleUpdateMessages = (scheduleId: string) => {
        updateTimeMessagesMutate({
            scheduleId: scheduleId,
        })
    }

    const handleDeleteSchedule = (scheduleId: string) => {
        deleteScheduleMutate({id: scheduleId})
    }

    const handleToggleActive = (scheduleId: string, currentStatus: boolean) => {
        toggleActiveMutate(
            {id: scheduleId},
            {
                onSuccess: () => {
                    toast.success(`Расписание ${currentStatus ? "деактивировано" : "активировано"}`)
                }
            }
        )
    }

    const openEditDialog = (schedule: ScheduleResponse) => {
        setEditingSchedule(schedule)
        resetTimeInputs()
        setIsEditDialogOpen(true)
    }

    const addTimeToDay = (dayOfWeek: DayOfWeek) => {
        if (!editingSchedule) return

        const existingDay = scheduleDays.find((day) => day.dayOfWeek === dayOfWeek)
        const currentTimes = existingDay?.timePostings || []

        if (timeMode === "single") {
            const time = `${newTimeSlot.hour}:${newTimeSlot.minute}`
            const allTimes = [...currentTimes, time]?.map(time => convertLocalToIsoTime(time))
            updateTimeMutate(
                {
                    data: {
                        scheduleId: editingSchedule.id,
                        dayOfWeek,
                        times: allTimes,
                    },
                },
                {
                    onSuccess: () => {
                        resetTimeInputs()
                        toast.success("Время добавлено")
                    }
                }
            )
        } else {
            const startMinutes = Number.parseInt(intervalTimeSlot.startHour) * 60 + Number.parseInt(intervalTimeSlot.startMinute)
            const endMinutes = Number.parseInt(intervalTimeSlot.endHour) * 60 + Number.parseInt(intervalTimeSlot.endMinute)

            if (intervalTimeSlot.intervalMinutes <= 0) {
                toast.error("Интервал должен быть больше нуля")
                return
            }

            if (endMinutes < startMinutes) {
                toast.error("Окончание интервала должно быть позже начала")
                return
            }

            const generatedTimes: string[] = []
            for (let minutes = startMinutes; minutes <= endMinutes; minutes += intervalTimeSlot.intervalMinutes) {
                const hours = Math.floor(minutes / 60)
                    .toString()
                    .padStart(2, "0")
                const mins = (minutes % 60).toString().padStart(2, "0")
                generatedTimes.push(`${hours}:${mins}`)
            }
            const allTimes = [...currentTimes, ...generatedTimes]?.map(time => convertLocalToIsoTime(time))

            updateTimeMutate(
                {
                    data: {
                        scheduleId: editingSchedule.id,
                        dayOfWeek,
                        times: allTimes,
                    },
                },
                {
                    onSuccess: () => {
                        resetTimeInputs()
                        toast.success("Время добавлено")
                    }
                }
            )
        }
    }
    const removeAllTimeFromDay = (dayOfWeek: DayOfWeek) => {
        if (!editingSchedule) return

        const existingDay = scheduleDays.find((day) => day.dayOfWeek === dayOfWeek)
        if (!existingDay) return

        updateTimeMutate(
            {
                data: {
                    scheduleId: editingSchedule.id,
                    dayOfWeek,
                    times: [],
                },
            },
            {
                onSuccess: () => {
                    toast.success("Время удалено")
                }
            }
        )
    }
    const removeTimeFromDay = (dayOfWeek: DayOfWeek, timeToRemove: string) => {
        if (!editingSchedule) return

        const existingDay = scheduleDays.find((day) => day.dayOfWeek === dayOfWeek)
        if (!existingDay) return

        const updatedTimes = (existingDay.timePostings || []).filter((time) => time !== timeToRemove)
            .map(time => convertLocalToIsoTime(time))

        updateTimeMutate(
            {
                data: {
                    scheduleId: editingSchedule.id,
                    dayOfWeek,
                    times: updatedTimes,
                },
            },
            {
                onSuccess: () => {
                    toast.success("Время удалено")
                }
            }
        )
    }

    function updateYouTube(id: string, newValue: string|null) {
        updateScheduleMutate({
            id: id,
            data: { youTubeAccountId: newValue }
        });
    }

    return (
        <div className="max-w-6xl mx-auto p-6 space-y-8">
            <header className="flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold">Расписания</h1>
                    <p className="text-muted-foreground">Управление расписаниями постинга в Telegram</p>
                </div>
                <CreateScheduleComponent/>
            </header>

            {schedulesLoading ? (
                <div className="flex items-center justify-center py-12">
                    <Loader2 className="h-8 w-8 animate-spin"/>
                </div>
            ) : schedules.length === 0 ? (
                <div className="text-center py-12 border border-dashed rounded-lg">
                    <Calendar className="h-12 w-12 mx-auto text-muted-foreground mb-4"/>
                    <h3 className="text-lg font-medium mb-2">Нет расписаний</h3>
                    <p className="text-muted-foreground mb-4">Создайте первое расписание для начала работы</p>
                    <CreateScheduleComponent/>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {schedules.map((schedule) => (
                        <Card key={schedule.id} className="hover:shadow-md transition-shadow">
                            <CardHeader className="pb-3">
                                <div className="flex items-start justify-between">
                                    <div className="flex-1">
                                        <div className="flex items-center gap-2 mb-1">
                                            <CardTitle className="text-lg">{schedule.name}</CardTitle>
                                            <Badge variant={schedule.isActive ? "default" : "secondary"}
                                                   className="text-xs">
                                                {schedule.isActive ? (
                                                    <>
                                                        <Power className="h-3 w-3 mr-1"/>
                                                        Активно
                                                    </>
                                                ) : (
                                                    <>
                                                        <PowerOff className="h-3 w-3 mr-1"/>
                                                        Неактивно
                                                    </>
                                                )}
                                            </Badge>
                                        </div>
                                        <CardDescription className="mt-1">Канал: {schedule.botName}</CardDescription>
                                        <CardDescription className="mt-1">Бот: {schedule.channelName}</CardDescription>
                                    </div>
                                    <Switch
                                        checked={schedule.isActive}
                                        onCheckedChange={() => handleToggleActive(schedule.id, schedule.isActive)}
                                        disabled={toggleActivePending}
                                    />
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
                                        {DAYS_ORDER.map((dayOfWeek) => (
                                            <Badge
                                                key={`${schedule.id}-${dayOfWeek}`}
                                                variant="outline"
                                                className="text-xs"
                                            >
                                                {(DAY_NAMES[dayOfWeek] ?? "??").slice(0, 2)}
                                            </Badge>
                                        ))}
                                    </div>
                                </div>

                                <Separator/>

                                <div className="flex items-center justify-between">
                                    <div className="flex gap-2">
                                        <EditIiComponent scheduleId={schedule.id} openRouterId={schedule.openRouterId}
                                                         promptId={schedule.promptId}/>
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => openEditDialog(schedule)}
                                        >
                                            <Settings className="h-4 w-4"/>
                                        </Button>
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => handleDeleteSchedule(schedule.id)}
                                            disabled={deleteSchedulePending}
                                        >
                                            {deleteSchedulePending ? (
                                                <Loader2 className="h-4 w-4 animate-spin"/>
                                            ) : (
                                                <Trash2 className="h-4 w-4"/>
                                            )}
                                        </Button>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}

            <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
                <DialogContent className="max-w-4xl">
                    <DialogHeader className="space-y-1.5">
                        <div className="flex items-center gap-3">
                            <Button variant="outline" size="icon" onClick={() => setIsEditDialogOpen(false)}>
                                <ArrowLeft className="h-4 w-4"/>
                            </Button>
                            <div className="text-left">
                                <DialogTitle className="text-2xl font-bold leading-tight">
                                    {editingSchedule?.name ?? "Расписание"}
                                </DialogTitle>
                                <DialogDescription>
                                    {editingSchedule?.channelName || "Канал не указан"}
                                </DialogDescription>
                            </div>
                        </div>
                    </DialogHeader>

                    {editingSchedule ? (
                        <div className="space-y-6 max-h-[70vh] overflow-y-auto pr-1">
                            <div className="flex items-center justify-between rounded-lg border p-4">
                                <div className="flex items-center gap-3">
                                    <Badge variant={editingSchedule.isActive ? "default" : "secondary"}>
                                        {editingSchedule.isActive ? (
                                            <>
                                                <Power className="h-3 w-3 mr-1"/>
                                                Активно
                                            </>
                                        ) : (
                                            <>
                                                <PowerOff className="h-3 w-3 mr-1"/>
                                                Неактивно
                                            </>
                                        )}
                                    </Badge>
                                </div>
                                <div className="flex items-center gap-2">
                                    <Label htmlFor="edit-schedule-active" className="text-sm">Активность</Label>
                                    <Switch
                                        id="edit-schedule-active"
                                        checked={editingSchedule.isActive}
                                        onCheckedChange={() => handleToggleActive(editingSchedule.id, editingSchedule.isActive)}
                                        disabled={toggleActivePending}
                                    />
                                </div>
                            </div>

                            <div className="flex items-center justify-between rounded-lg border p-4">
                                <div className="flex items-center gap-2 w-full">
                                    <Label htmlFor="edit-schedule-youtube" className="text-sm min-w-fit">YouTube
                                        Аккаунт</Label>
                                    <Select
                                        value={editingSchedule.youTubeAccountId || "none"}
                                        onValueChange={(value) => {
                                            const newValue = value === "none" ? undefined : value;
                                        updateYouTube(editingSchedule?.id, newValue??null)
                                            setEditingSchedule(prev => prev ? {
                                                ...prev,
                                                youTubeAccountId: newValue || null
                                            } : null);
                                        }}
                                        disabled={youtubeLoading || updateSchedulePending}
                                    >
                                        <SelectTrigger className="w-full">
                                            <SelectValue placeholder="Выберите аккаунт"/>
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="none">Не выбрано</SelectItem>
                                            {youtubeAccounts.map((account) => (
                                                <SelectItem key={account.id} value={account.id}>
                                                    {account.name}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>

                            <Card>
                                <CardHeader>
                                    <CardTitle className="flex items-center gap-2">
                                        <Clock className="h-5 w-5"/>
                                        Настройка времени отправки
                                    </CardTitle>
                                    <CardDescription>
                                        Настройте время отправки сообщений для каждого дня недели
                                    </CardDescription>
                                </CardHeader>

                                <CardContent className="space-y-6">
                                    {daysFetching && !scheduleDays.length ? (
                                        <div className="flex items-center gap-2 text-muted-foreground">
                                            <Loader2 className="h-4 w-4 animate-spin"/>
                                            Загрузка расписания по дням...
                                        </div>
                                    ) : (
                                        DAYS_ORDER.map((dayOfWeek, index) => (
                                            <div key={dayOfWeek} className="space-y-3">
                                                <div className="flex items-center justify-between">
                                                    <Label className="text-base font-medium">
                                                        {DAY_NAMES[dayOfWeek] ?? "Неизвестный день"}
                                                    </Label>
                                                    <Button
                                                        onClick={() => removeAllTimeFromDay(dayOfWeek)}>Стереть</Button>
                                                    <Popover
                                                        open={popoverOpenForDay === dayOfWeek}
                                                        onOpenChange={(isOpen) => setPopoverOpenForDay(isOpen ? dayOfWeek : null)}
                                                    >
                                                        <PopoverTrigger asChild>
                                                            <Button variant="outline" size="sm"
                                                                    className="flex items-center gap-2">
                                                                <Plus className="h-4 w-4"/>
                                                                Добавить время
                                                            </Button>
                                                        </PopoverTrigger>
                                                        <PopoverContent className="w-[26rem]">
                                                            <div className="grid gap-4">
                                                                <div className="space-y-1.5">
                                                                    <h4 className="font-medium leading-none">Добавить
                                                                        время</h4>
                                                                    <p className="text-sm text-muted-foreground">
                                                                        Выберите способ добавления для
                                                                        «{DAY_NAMES[dayOfWeek] ?? "Неизвестный день"}».
                                                                    </p>
                                                                </div>

                                                                <Tabs value={timeMode}
                                                                      onValueChange={(value) => setTimeMode(value as "single" | "interval")}>
                                                                    <TabsList className="grid w-full grid-cols-2">
                                                                        <TabsTrigger value="single">Точное
                                                                            время</TabsTrigger>
                                                                        <TabsTrigger
                                                                            value="interval">Интервал</TabsTrigger>
                                                                    </TabsList>

                                                                    <TabsContent value="single"
                                                                                 className="pt-4 space-y-4">
                                                                        <div className="flex items-center gap-2">
                                                                            <Label
                                                                                className="text-sm font-medium">Время:</Label>
                                                                            <Select
                                                                                value={newTimeSlot.hour}
                                                                                onValueChange={(value) => setNewTimeSlot((prev) => ({
                                                                                    ...prev,
                                                                                    hour: value
                                                                                }))}
                                                                            >
                                                                                <SelectTrigger className="w-24">
                                                                                    <SelectValue/>
                                                                                </SelectTrigger>
                                                                                <SelectContent>
                                                                                    {HOURS.map((hour) => (
                                                                                        <SelectItem key={hour}
                                                                                                    value={hour}>
                                                                                            {hour}
                                                                                        </SelectItem>
                                                                                    ))}
                                                                                </SelectContent>
                                                                            </Select>
                                                                            <span>:</span>
                                                                            <Select
                                                                                value={newTimeSlot.minute}
                                                                                onValueChange={(value) => setNewTimeSlot((prev) => ({
                                                                                    ...prev,
                                                                                    minute: value
                                                                                }))}
                                                                            >
                                                                                <SelectTrigger className="w-24">
                                                                                    <SelectValue/>
                                                                                </SelectTrigger>
                                                                                <SelectContent>
                                                                                    {MINUTES.map((minute) => (
                                                                                        <SelectItem key={minute}
                                                                                                    value={minute}>
                                                                                            {minute}
                                                                                        </SelectItem>
                                                                                    ))}
                                                                                </SelectContent>
                                                                            </Select>
                                                                        </div>
                                                                    </TabsContent>

                                                                    <TabsContent value="interval"
                                                                                 className="pt-4 space-y-4">
                                                                        <div className="grid grid-cols-2 gap-4">
                                                                            <div className="space-y-2">
                                                                                <Label
                                                                                    className="text-sm font-medium">Начало</Label>
                                                                                <div
                                                                                    className="flex items-center gap-2">
                                                                                    <Select
                                                                                        value={intervalTimeSlot.startHour}
                                                                                        onValueChange={(value) => setIntervalTimeSlot((prev) => ({
                                                                                            ...prev,
                                                                                            startHour: value
                                                                                        }))}
                                                                                    >
                                                                                        <SelectTrigger
                                                                                            className="flex-1">
                                                                                            <SelectValue/>
                                                                                        </SelectTrigger>
                                                                                        <SelectContent>
                                                                                            {HOURS.map((hour) => (
                                                                                                <SelectItem key={hour}
                                                                                                            value={hour}>
                                                                                                    {hour}
                                                                                                </SelectItem>
                                                                                            ))}
                                                                                        </SelectContent>
                                                                                    </Select>
                                                                                    :
                                                                                    <Select
                                                                                        value={intervalTimeSlot.startMinute}
                                                                                        onValueChange={(value) => setIntervalTimeSlot((prev) => ({
                                                                                            ...prev,
                                                                                            startMinute: value
                                                                                        }))}
                                                                                    >
                                                                                        <SelectTrigger
                                                                                            className="flex-1">
                                                                                            <SelectValue/>
                                                                                        </SelectTrigger>
                                                                                        <SelectContent>
                                                                                            {MINUTES.map((minute) => (
                                                                                                <SelectItem key={minute}
                                                                                                            value={minute}>
                                                                                                    {minute}
                                                                                                </SelectItem>
                                                                                            ))}
                                                                                        </SelectContent>
                                                                                    </Select>
                                                                                </div>
                                                                            </div>

                                                                            <div className="space-y-2">
                                                                                <Label
                                                                                    className="text-sm font-medium">Окончание</Label>
                                                                                <div
                                                                                    className="flex items-center gap-2">
                                                                                    <Select
                                                                                        value={intervalTimeSlot.endHour}
                                                                                        onValueChange={(value) => setIntervalTimeSlot((prev) => ({
                                                                                            ...prev,
                                                                                            endHour: value
                                                                                        }))}
                                                                                    >
                                                                                        <SelectTrigger
                                                                                            className="flex-1">
                                                                                            <SelectValue/>
                                                                                        </SelectTrigger>
                                                                                        <SelectContent>
                                                                                            {HOURS.map((hour) => (
                                                                                                <SelectItem key={hour}
                                                                                                            value={hour}>
                                                                                                    {hour}
                                                                                                </SelectItem>
                                                                                            ))}
                                                                                        </SelectContent>
                                                                                    </Select>
                                                                                    :
                                                                                    <Select
                                                                                        value={intervalTimeSlot.endMinute}
                                                                                        onValueChange={(value) => setIntervalTimeSlot((prev) => ({
                                                                                            ...prev,
                                                                                            endMinute: value
                                                                                        }))}
                                                                                    >
                                                                                        <SelectTrigger
                                                                                            className="flex-1">
                                                                                            <SelectValue/>
                                                                                        </SelectTrigger>
                                                                                        <SelectContent>
                                                                                            {MINUTES.map((minute) => (
                                                                                                <SelectItem key={minute}
                                                                                                            value={minute}>
                                                                                                    {minute}
                                                                                                </SelectItem>
                                                                                            ))}
                                                                                        </SelectContent>
                                                                                    </Select>
                                                                                </div>
                                                                            </div>
                                                                        </div>

                                                                        <div className="space-y-2">
                                                                            <Label className="text-sm font-medium">Интервал
                                                                                (минуты)</Label>
                                                                            <Input
                                                                                type="number"
                                                                                value={intervalTimeSlot.intervalMinutes}
                                                                                onChange={(e) => setIntervalTimeSlot((prev) => ({
                                                                                    ...prev,
                                                                                    intervalMinutes: Number.parseInt(e.target.value) || 0,
                                                                                }))}
                                                                            />
                                                                        </div>
                                                                    </TabsContent>
                                                                </Tabs>

                                                                <div className="flex gap-2">
                                                                    <Button
                                                                        size="sm"
                                                                        onClick={() => addTimeToDay(dayOfWeek)}
                                                                        disabled={updateTimePending}
                                                                    >
                                                                        {updateTimePending ? (
                                                                            <>
                                                                                <Loader2
                                                                                    className="h-4 w-4 mr-2 animate-spin"/>
                                                                                Добавление...
                                                                            </>
                                                                        ) : (
                                                                            <>
                                                                                <Save className="h-4 w-4 mr-2"/>
                                                                                Добавить
                                                                            </>
                                                                        )}
                                                                    </Button>
                                                                    <Button
                                                                        variant="outline"
                                                                        size="sm"
                                                                        onClick={() => setPopoverOpenForDay(null)}
                                                                    >
                                                                        <X className="h-4 w-4 mr-2"/>
                                                                        Отмена
                                                                    </Button>
                                                                </div>
                                                            </div>
                                                        </PopoverContent>
                                                    </Popover>
                                                </div>

                                                <div
                                                    className="flex flex-wrap items-center gap-2 p-3 border rounded-lg min-h-[52px]">
                                                    {getTimesForDay(dayOfWeek).length ? (
                                                        getTimesForDay(dayOfWeek).map((time) => (
                                                            <Badge
                                                                key={time}
                                                                variant="secondary"
                                                                className="inline-flex items-center gap-x-1 pl-2.5 pr-1 py-0.5 rounded-full"
                                                            >
                                                                <span
                                                                    className="font-mono text-sm leading-none">{time}</span>
                                                                <button
                                                                    onClick={() => removeTimeFromDay(dayOfWeek, time)}
                                                                    disabled={updateTimePending}
                                                                    className="flex-shrink-0 group rounded-full p-0.5 text-muted-foreground/50 hover:bg-destructive/20 hover:text-destructive transition-colors disabled:cursor-not-allowed"
                                                                    aria-label={`Удалить время ${time}`}
                                                                >
                                                                    {updateTimePending ? (
                                                                        <Loader2 className="h-3 w-3 animate-spin"/>
                                                                    ) : (
                                                                        <X className="h-3 w-3 group-hover:text-destructive-foreground"/>
                                                                    )}
                                                                </button>
                                                            </Badge>
                                                        ))
                                                    ) : (
                                                        <p className="text-sm text-muted-foreground">Время не задано</p>
                                                    )}
                                                </div>

                                                {index < DAYS_ORDER.length - 1 && <Separator className="pt-2"/>}
                                            </div>
                                        ))
                                    )}
                                </CardContent>
                            </Card>
                        </div>
                    ) : (
                        <div className="flex items-center justify-center py-12 text-muted-foreground">
                            <Loader2 className="h-5 w-5 mr-2 animate-spin"/>
                            Загрузка данных расписания...
                        </div>
                    )}

                    <DialogFooter className="flex justify-end">
                        <Button className="float-left" variant="outline"
                                onClick={() => handleUpdateMessages(editingSchedule!.id)} disabled={updateTimeMessagesPending}>
                            Обновить время сообщений
                        </Button>
                        <Button variant="outline" onClick={() => setIsEditDialogOpen(false)}>
                            Закрыть
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    )
}
