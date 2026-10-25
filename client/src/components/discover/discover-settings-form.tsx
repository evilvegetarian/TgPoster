import {useEffect, useMemo} from "react"
import {useForm} from "react-hook-form"
import {zodResolver} from "@hookform/resolvers/zod"
import {z} from "zod"
import {toast} from "sonner"
import {useQueryClient} from "@tanstack/react-query"
import {AlertTriangle, Loader2, Save} from "lucide-react"
import {
    getGetApiV1DiscoverSettingsQueryKey,
    useGetApiV1DiscoverSettings,
    usePutApiV1DiscoverSettings,
} from "@/api/endpoints/discover/discover"
import type {DiscoverSettingsResponse} from "@/api/endpoints/tgPosterAPI.schemas"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form"
import {Skeleton} from "@/components/ui/skeleton"
import {Switch} from "@/components/ui/switch"
import {formatDateTime} from "@/components/discover/format"
import {NumberInput, SessionsPicker} from "@/components/discover/settings-fields"

const schema = z.object({
    isEnabled: z.boolean(),
    intervalMinutes: z.number().int().min(1, "Не меньше 1").max(1440, "Не больше 1440"),
    channelsPerSession: z.number().int().min(1, "Не меньше 1").max(20, "Не больше 20"),
    telegramSessionIds: z.array(z.string()).max(50, "Не больше 50 сессий"),
})

type FormValues = z.infer<typeof schema>

function toFormValues(settings: DiscoverSettingsResponse): FormValues {
    return {
        isEnabled: settings.isEnabled,
        intervalMinutes: settings.intervalMinutes,
        channelsPerSession: settings.channelsPerSession,
        telegramSessionIds: settings.sessions.filter((s) => s.isOwn && s.isSelected).map((s) => s.id),
    }
}

function SettingsForm({settings}: {settings: DiscoverSettingsResponse}) {
    const queryClient = useQueryClient()
    const defaultValues = useMemo(() => toFormValues(settings), [settings])

    const form = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues,
    })

    useEffect(() => {
        form.reset(defaultValues)
    }, [defaultValues, form])

    const {mutate: save, isPending} = usePutApiV1DiscoverSettings({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки сохранены", {description: "Воркер подхватит их в течение минуты"})
                void queryClient.invalidateQueries({queryKey: getGetApiV1DiscoverSettingsQueryKey()})
            },
            onError: (error) => {
                toast.error("Ошибка сохранения", {description: error.title || "Не удалось сохранить настройки"})
            },
        },
    })

    const onSubmit = (values: FormValues) => {
        save({data: values})
    }

    const watched = form.watch()
    // Рабочие сессии: отмеченные свои плюс чужие, уже отданные поиску каналов
    const workingSessions = settings.sessions.filter((s) =>
        s.isActive && s.isAuthorized && (s.isOwn ? watched.telegramSessionIds.includes(s.id) : s.isSelected)).length
    const perRun = (watched.channelsPerSession || 0) * workingSessions
    const perDay = Math.floor(1440 / Math.max(1, watched.intervalMinutes || 1)) * perRun

    return (
        <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                    <Card>
                        <CardHeader className="pb-3">
                            <CardTitle className="text-base">Работа</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <FormField
                                control={form.control}
                                name="isEnabled"
                                render={({field}) => (
                                    <FormItem className="flex items-center justify-between gap-4 rounded-md border p-3">
                                        <div>
                                            <FormLabel>Поиск каналов включён</FormLabel>
                                            <FormDescription>
                                                Выключенный не парсит каналы, список и статистика остаются
                                            </FormDescription>
                                        </div>
                                        <FormControl>
                                            <Switch checked={field.value} onCheckedChange={field.onChange} disabled={isPending}/>
                                        </FormControl>
                                    </FormItem>
                                )}
                            />
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                <FormField
                                    control={form.control}
                                    name="intervalMinutes"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Интервал запуска, мин</FormLabel>
                                            <FormControl>
                                                <NumberInput value={field.value} onChange={field.onChange} min={1} max={1440} disabled={isPending}/>
                                            </FormControl>
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                                <FormField
                                    control={form.control}
                                    name="channelsPerSession"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Каналов на сессию за запуск</FormLabel>
                                            <FormControl>
                                                <NumberInput value={field.value} onChange={field.onChange} min={1} max={20} disabled={isPending}/>
                                            </FormControl>
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                            </div>
                            <p className="text-xs text-muted-foreground">
                                Рабочих сессий: {workingSessions} · за запуск — до {perRun.toLocaleString("ru-RU")} каналов
                                · в сутки — до {perDay.toLocaleString("ru-RU")}. Канал с длинной историей парсится долго:
                                если запуск не уложится в интервал, следующий начнётся сразу после него
                            </p>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader className="pb-3">
                            <CardTitle className="text-base">Telegram-сессии</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <FormField
                                control={form.control}
                                name="telegramSessionIds"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Сессии для чтения каналов</FormLabel>
                                        <SessionsPicker
                                            sessions={settings.sessions}
                                            value={field.value}
                                            onChange={field.onChange}
                                            disabled={isPending}
                                        />
                                        {workingSessions === 0 ? (
                                            <p className="flex items-center gap-1.5 text-xs text-amber-600">
                                                <AlertTriangle className="h-3.5 w-3.5 shrink-0"/>
                                                Нет ни одной активной авторизованной сессии — поиск каналов не сможет читать каналы
                                            </p>
                                        ) : (
                                            <FormDescription>
                                                Каналы запуска раскладываются по выбранным сессиям по кругу, сессии работают
                                                параллельно. Если в канале забанена одна сессия, его возьмёт другая
                                            </FormDescription>
                                        )}
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                        </CardContent>
                    </Card>
                </div>

                <div className="sticky bottom-0 z-10 -mx-1 flex flex-wrap items-center justify-between gap-3 rounded-lg border bg-background/95 px-4 py-3 backdrop-blur">
                    <p className="text-xs text-muted-foreground">
                        {settings.updatedAt ? `Изменено: ${formatDateTime(settings.updatedAt)}` : "Используются стандартные настройки"}
                        {form.formState.isDirty && " · есть несохранённые изменения"}
                    </p>
                    <div className="flex gap-2">
                        <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => form.reset(defaultValues)}
                            disabled={isPending || !form.formState.isDirty}
                        >
                            Отменить изменения
                        </Button>
                        <Button type="submit" size="sm" className="gap-1.5" disabled={isPending || !form.formState.isDirty}>
                            {isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin"/> : <Save className="h-3.5 w-3.5"/>}
                            Сохранить
                        </Button>
                    </div>
                </div>
            </form>
        </Form>
    )
}

// Настройки поиска каналов: грузим текущие значения и отдаём в форму
export function DiscoverSettingsForm() {
    const {data: settings, isLoading} = useGetApiV1DiscoverSettings()

    if (isLoading || !settings) {
        return (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                <Skeleton className="h-72 w-full"/>
                <Skeleton className="h-72 w-full"/>
            </div>
        )
    }

    return <SettingsForm settings={settings}/>
}
