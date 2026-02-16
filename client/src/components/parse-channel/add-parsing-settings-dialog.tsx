import type React from "react"
import {useRef} from "react"
import {useForm, Controller} from "react-hook-form"
import {zodResolver} from "@hookform/resolvers/zod"
import {z} from "zod"
import {Button} from "@/components/ui/button"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import {Input} from "@/components/ui/input"
import {Label} from "@/components/ui/label"
import {Switch} from "@/components/ui/switch"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Badge} from "@/components/ui/badge"
import {AlertCircle, Loader2, X} from "lucide-react"
import {toast} from "sonner"
import type {CreateParseChannelRequest} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {useGetApiV1Schedule} from "@/api/endpoints/schedule/schedule.ts";
import {useGetApiV1TelegramSession} from "@/api/endpoints/telegram-session/telegram-session.ts";

const parseChannelSchema = z.object({
    channel: z.string().min(1, "Канал обязателен для заполнения"),
    alwaysCheckNewPosts: z.boolean(),
    scheduleId: z.string().min(1, "Расписание обязательно для заполнения"),
    deleteText: z.boolean(),
    deleteMedia: z.boolean(),
    avoidWords: z.array(z.string()),
    needVerifiedPosts: z.boolean(),
    dateFrom: z.string().optional(),
    dateTo: z.string().optional(),
    telegramSessionId: z.string().min(1, "Телегам аккаунт обязателен для заполнения"),
})

type ParseChannelFormData = z.infer<typeof parseChannelSchema>

const defaultFormValues: ParseChannelFormData = {
    channel: "",
    alwaysCheckNewPosts: true,
    scheduleId: "",
    deleteText: false,
    deleteMedia: false,
    avoidWords: [],
    needVerifiedPosts: false,
    dateFrom: "",
    dateTo: "",
    telegramSessionId: "",
}

interface AddParsingSettingsDialogProps {
    open: boolean
    onOpenChange: (open: boolean) => void
    onSubmit: (settings: CreateParseChannelRequest) => void
    isLoading?: boolean
}

export function AddParsingSettingsDialog({
                                             open,
                                             onOpenChange,
                                             onSubmit,
                                             isLoading = false,
                                         }: AddParsingSettingsDialogProps) {
    const {
        control,
        register,
        handleSubmit,
        reset,
        watch,
        setValue,
        formState: {errors},
    } = useForm<ParseChannelFormData>({
        resolver: zodResolver(parseChannelSchema),
        defaultValues: defaultFormValues,
    })

    const newAvoidWordRef = useRef<HTMLInputElement>(null)
    const avoidWords = watch("avoidWords")

    const {data: schedulesData, isLoading: schedulesLoading, error: schedulesError} = useGetApiV1Schedule()
    const {data: sessionsData, isLoading: sessionsLoading, error: sessionsError} = useGetApiV1TelegramSession()
    const schedules = schedulesData?.items ?? []
    const telegramSessions = sessionsData?.items ?? []
    const getUtcString = (dateValue: string | Date | null | undefined): string | null => {
        if (!dateValue) {
            return null;
        }
        return new Date(dateValue).toISOString();
    };

    const onFormSubmit = (data: ParseChannelFormData) => {
        const settings: CreateParseChannelRequest = {
            ...data,
            avoidWords: data.avoidWords && data.avoidWords.length > 0 ? data.avoidWords : [],
            dateFrom: getUtcString(data.dateFrom),
            dateTo: getUtcString(data.dateTo),
        }

        onSubmit(settings)
    }

    const resetForm = () => {
        reset()
        if (newAvoidWordRef.current) {
            newAvoidWordRef.current.value = ""
        }
    }

    const handleOpenChange = (newOpen: boolean) => {
        if (!newOpen && !isLoading) {
            resetForm()
        }
        onOpenChange(newOpen)
    }

    const addAvoidWord = () => {
        const word = newAvoidWordRef.current?.value.trim()
        if (!word) return

        if (!avoidWords?.includes(word)) {
            setValue("avoidWords", [...avoidWords ?? [], word])
            toast.success("Слово добавлено", {
                description: `"${word}" добавлено в список исключений`,
            })
            if (newAvoidWordRef.current) {
                newAvoidWordRef.current.value = ""
            }
        } else {
            toast.error("Слово уже существует", {
                description: "Это слово уже есть в списке исключений",
            })
        }
    }

    const removeAvoidWord = (word: string) => {
        setValue("avoidWords", avoidWords?.filter((w) => w !== word) ?? [])
        toast.info("Слово удалено", {
            description: `"${word}" удалено из списка исключений`,
        })
    }

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === "Enter") {
            e.preventDefault()
            addAvoidWord()
        }
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Добавить настройку парсинга</DialogTitle>
                    <DialogDescription>Создайте новую настройку для парсинга канала</DialogDescription>
                </DialogHeader>

                <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="channel">Канал для парсинга *</Label>
                            <Input
                                id="channel"
                                placeholder="@channel_name"
                                disabled={isLoading}
                                {...register("channel")}
                            />
                            {errors.channel && (
                                <p className="text-sm text-red-600">{errors.channel.message}</p>
                            )}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="schedule">Расписание *</Label>
                            {schedulesError ? (
                                <div className="flex items-center gap-2 p-2 text-sm text-red-600 bg-red-50 rounded-md">
                                    <AlertCircle className="h-4 w-4"/>
                                    <span>Ошибка загрузки расписаний</span>
                                </div>
                            ) : (
                                <Controller
                                    name="scheduleId"
                                    control={control}
                                    render={({field}) => (
                                        <>
                                            <Select
                                                value={field.value}
                                                onValueChange={field.onChange}
                                                disabled={isLoading || schedulesLoading}
                                            >
                                                <SelectTrigger>
                                                    <SelectValue
                                                        placeholder={schedulesLoading ? "Загрузка..." : "Выберите расписание"}/>
                                                </SelectTrigger>
                                                <SelectContent>
                                                    {schedulesLoading ? (
                                                        <SelectItem value="loading" disabled>
                                                            <div className="flex items-center gap-2">
                                                                <Loader2 className="h-4 w-4 animate-spin"/>
                                                                Загрузка расписаний...
                                                            </div>
                                                        </SelectItem>
                                                    ) : schedules.length === 0 ? (
                                                        <SelectItem value="empty" disabled>
                                                            Нет доступных расписаний
                                                        </SelectItem>
                                                    ) : (
                                                        schedules
                                                            .filter((schedule) => schedule.isActive)
                                                            .map((schedule) => (
                                                                <SelectItem key={schedule.id} value={schedule.id}>
                                                                    <div className="flex flex-col">
                                                                        <span>{schedule.name}</span>
                                                                        {schedule.name && (
                                                                            <span
                                                                                className="text-xs text-muted-foreground">{schedule.name}</span>
                                                                        )}
                                                                    </div>
                                                                </SelectItem>
                                                            ))
                                                    )}
                                                </SelectContent>
                                            </Select>
                                            {errors.scheduleId && (
                                                <p className="text-sm text-red-600">{errors.scheduleId.message}</p>
                                            )}
                                        </>
                                    )}
                                />
                            )}
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="telegramSession">Telegram сессия</Label>
                            {sessionsError ? (
                                <div className="flex items-center gap-2 p-2 text-sm text-red-600 bg-red-50 rounded-md">
                                    <AlertCircle className="h-4 w-4"/>
                                    <span>Ошибка загрузки сессий</span>
                                </div>
                            ) : (
                                <Controller
                                    name="telegramSessionId"
                                    control={control}
                                    render={({field}) => (
                                        <Select
                                            value={field.value || "none"}
                                            onValueChange={(value) => field.onChange(value === "none" ? "" : value)}
                                            disabled={isLoading || sessionsLoading}
                                        >
                                            <SelectTrigger>
                                                <SelectValue
                                                    placeholder={sessionsLoading ? "Загрузка..." : "Выберите сессию (необязательно)"}/>
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value="none">
                                                    Не использовать
                                                </SelectItem>
                                                {sessionsLoading ? (
                                                    <SelectItem value="loading" disabled>
                                                        <div className="flex items-center gap-2">
                                                            <Loader2 className="h-4 w-4 animate-spin"/>
                                                            Загрузка сессий...
                                                        </div>
                                                    </SelectItem>
                                                ) : telegramSessions.length === 0 ? (
                                                    <SelectItem value="empty" disabled>
                                                        Нет доступных сессий
                                                    </SelectItem>
                                                ) : (
                                                    telegramSessions
                                                        .filter((session) => session.isActive)
                                                        .map((session) => (
                                                            <SelectItem key={session.id} value={session.id!}>
                                                                <div className="flex flex-col">
                                                                    <span>{session.name || session.phoneNumber}</span>
                                                                    {session.name && (
                                                                        <span
                                                                            className="text-xs text-muted-foreground">{session.phoneNumber}</span>
                                                                    )}
                                                                </div>
                                                            </SelectItem>
                                                        ))
                                                )}
                                            </SelectContent>
                                        </Select>
                                    )}
                                />
                            )}
                        </div>
                    </div>

                    <div className="space-y-4">
                        <Controller
                            name="alwaysCheckNewPosts"
                            control={control}
                            render={({field}) => (
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Всегда проверять новые посты</Label>
                                        <p className="text-sm text-muted-foreground">Раз в день будет проверять новые
                                            посты</p>
                                    </div>
                                    <Switch
                                        checked={field.value}
                                        onCheckedChange={field.onChange}
                                        disabled={isLoading}
                                    />
                                </div>
                            )}
                        />

                        <Controller
                            name="deleteText"
                            control={control}
                            render={({field}) => (
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Удалять текст</Label>
                                        <p className="text-sm text-muted-foreground">Оставлять только картинки</p>
                                    </div>
                                    <Switch
                                        checked={field.value}
                                        onCheckedChange={field.onChange}
                                        disabled={isLoading}
                                    />
                                </div>
                            )}
                        />

                        <Controller
                            name="deleteMedia"
                            control={control}
                            render={({field}) => (
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Удалять медиа</Label>
                                        <p className="text-sm text-muted-foreground">Оставлять только текст</p>
                                    </div>
                                    <Switch
                                        checked={field.value}
                                        onCheckedChange={field.onChange}
                                        disabled={isLoading}
                                    />
                                </div>
                            )}
                        />

                        <Controller
                            name="needVerifiedPosts"
                            control={control}
                            render={({field}) => (
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Требовать подтверждение</Label>
                                        <p className="text-sm text-muted-foreground">Подтверждать посты перед
                                            публикацией</p>
                                    </div>
                                    <Switch
                                        checked={field.value}
                                        onCheckedChange={field.onChange}
                                        disabled={isLoading}
                                    />
                                </div>
                            )}
                        />
                    </div>

                    <div className="space-y-2">
                        <Label>Исключаемые слова</Label>
                        <div className="flex gap-2">
                            <Input
                                ref={newAvoidWordRef}
                                placeholder="Добавить слово для исключения"
                                onKeyDown={handleKeyDown}
                                disabled={isLoading}
                            />
                            <Button type="button" onClick={addAvoidWord} variant="outline" disabled={isLoading}>
                                Добавить
                            </Button>
                        </div>
                        {(avoidWords?.length ?? 0) > 0 && (
                            <div className="flex flex-wrap gap-2 mt-2">
                                {avoidWords?.map((word, index) => (
                                    <Badge key={index} variant="secondary" className="gap-1">
                                        {word}
                                        <X className="h-3 w-3 cursor-pointer"
                                           onClick={() => !isLoading && removeAvoidWord(word)}/>
                                    </Badge>
                                ))}
                            </div>
                        )}
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="dateFrom">Дата начала парсинга</Label>
                            <Input
                                id="dateFrom"
                                type="date"
                                disabled={isLoading}
                                {...register("dateFrom")}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="dateTo">Дата окончания парсинга</Label>
                            <Input
                                id="dateTo"
                                type="date"
                                disabled={isLoading}
                                {...register("dateTo")}
                            />
                        </div>
                    </div>

                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}
                                disabled={isLoading}>
                            Отмена
                        </Button>
                        <Button type="submit" disabled={isLoading || schedulesLoading}>
                            {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin"/>}
                            Создать настройку
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
