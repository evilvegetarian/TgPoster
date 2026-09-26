import {useEffect, useMemo, useState} from "react"
import {useForm} from "react-hook-form"
import {zodResolver} from "@hookform/resolvers/zod"
import {z} from "zod"
import {toast} from "sonner"
import {useQueryClient} from "@tanstack/react-query"
import {AlertTriangle, Loader2, Plus, RotateCcw, Save, X} from "lucide-react"
import {
    getGetApiV1DiscoverClassificationSettingsQueryKey,
    useGetApiV1DiscoverClassificationSettings,
    usePutApiV1DiscoverClassificationSettings,
} from "@/api/endpoints/discover/discover"
import {useGetApiV1TelegramSession} from "@/api/endpoints/telegram-session/telegram-session"
import type {ClassifierSettingsResponse} from "@/api/endpoints/tgPosterAPI.schemas"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form"
import {Input} from "@/components/ui/input"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Skeleton} from "@/components/ui/skeleton"
import {Switch} from "@/components/ui/switch"
import {Textarea} from "@/components/ui/textarea"
import {formatDateTime} from "@/components/discover/format"

// Значение селекта «сессия по назначению Classification» — на бэкенд уходит как null
const PURPOSE_SESSION = "purpose"
const DEFAULT_RECLASSIFY_DAYS = 30

function buildSchema(placeholder: string) {
    return z.object({
        isEnabled: z.boolean(),
        model: z.string().trim().min(1, "Укажите модель").max(128, "Не длиннее 128 символов"),
        batchSize: z.number().int().min(1, "Не меньше 1").max(50, "Не больше 50"),
        intervalMinutes: z.number().int().min(1, "Не меньше 1").max(1440, "Не больше 1440"),
        messageSampleCount: z.number().int().min(5, "Не меньше 5").max(100, "Не больше 100"),
        photoCount: z.number().int().min(0, "Не меньше 0").max(10, "Не больше 10"),
        reclassify: z.boolean(),
        reclassifyAfterDays: z.number().int().min(1, "Не меньше 1").max(365, "Не больше 365"),
        categories: z.array(z.string().max(100, "Тематика длиннее 100 символов"))
            .min(1, "Нужна хотя бы одна тематика")
            .max(50, "Не больше 50 тематик"),
        systemPrompt: z.string()
            .min(1, "Промпт не может быть пустым")
            .max(8000, "Не длиннее 8000 символов")
            .refine((value) => value.includes(placeholder), `Промпт должен содержать ${placeholder}`),
        telegramSessionId: z.string(),
    })
}

type FormValues = z.infer<ReturnType<typeof buildSchema>>

function toFormValues(settings: ClassifierSettingsResponse): FormValues {
    return {
        isEnabled: settings.isEnabled,
        model: settings.model,
        batchSize: settings.batchSize,
        intervalMinutes: settings.intervalMinutes,
        messageSampleCount: settings.messageSampleCount,
        photoCount: settings.photoCount,
        reclassify: settings.reclassifyAfterDays != null,
        reclassifyAfterDays: settings.reclassifyAfterDays ?? DEFAULT_RECLASSIFY_DAYS,
        categories: [...settings.categories],
        systemPrompt: settings.systemPrompt,
        telegramSessionId: settings.telegramSession?.id ?? PURPOSE_SESSION,
    }
}

function NumberInput({value, onChange, min, max, disabled}: {
    value: number
    onChange: (value: number) => void
    min: number
    max: number
    disabled?: boolean
}) {
    return (
        <Input
            type="number"
            min={min}
            max={max}
            value={value}
            onChange={(event) => onChange(event.target.value === "" ? min : Number(event.target.value))}
            disabled={disabled}
            className="w-32"
        />
    )
}

function CategoriesEditor({value, onChange, defaults, disabled}: {
    value: string[]
    onChange: (value: string[]) => void
    defaults: readonly string[]
    disabled?: boolean
}) {
    const [draft, setDraft] = useState("")

    const add = () => {
        const name = draft.trim()
        if (!name) return
        if (value.some((c) => c.toLowerCase() === name.toLowerCase())) {
            toast.info(`Тематика «${name}» уже есть`)
            return
        }
        onChange([...value, name])
        setDraft("")
    }

    return (
        <div className="space-y-3">
            <div className="flex flex-wrap gap-1.5">
                {value.map((category) => (
                    <Badge key={category} variant="secondary" className="gap-1 pr-1 font-normal text-sm">
                        {category}
                        <button
                            type="button"
                            className="rounded-sm p-0.5 hover:bg-muted-foreground/20 disabled:opacity-50"
                            onClick={() => onChange(value.filter((c) => c !== category))}
                            disabled={disabled}
                            aria-label={`Убрать тематику ${category}`}
                        >
                            <X className="h-3 w-3"/>
                        </button>
                    </Badge>
                ))}
                {value.length === 0 && <span className="text-sm text-muted-foreground">Тематик нет</span>}
            </div>
            <div className="flex flex-wrap gap-2">
                <Input
                    placeholder="Новая тематика"
                    value={draft}
                    onChange={(event) => setDraft(event.target.value)}
                    onKeyDown={(event) => {
                        if (event.key === "Enter") {
                            event.preventDefault()
                            add()
                        }
                    }}
                    disabled={disabled}
                    className="w-56"
                    maxLength={100}
                />
                <Button type="button" variant="outline" size="sm" className="gap-1.5 h-9" onClick={add} disabled={disabled || !draft.trim()}>
                    <Plus className="h-3.5 w-3.5"/>
                    Добавить
                </Button>
                <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="gap-1.5 h-9"
                    onClick={() => onChange([...defaults])}
                    disabled={disabled}
                >
                    <RotateCcw className="h-3.5 w-3.5"/>
                    Стандартный список
                </Button>
            </div>
        </div>
    )
}

function SettingsForm({settings}: {settings: ClassifierSettingsResponse}) {
    const queryClient = useQueryClient()
    const schema = useMemo(() => buildSchema(settings.categoriesPlaceholder), [settings.categoriesPlaceholder])
    const defaultValues = useMemo(() => toFormValues(settings), [settings])

    const form = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues,
    })

    useEffect(() => {
        form.reset(defaultValues)
    }, [defaultValues, form])

    // В контракте сессий все поля опциональны (позиционный record на бэке) — без id сессию не выбрать
    const {data: sessionsData} = useGetApiV1TelegramSession()
    const sessions = (sessionsData?.items ?? []).flatMap((s) => (s.id ? [{...s, id: s.id}] : []))

    const {mutate: save, isPending} = usePutApiV1DiscoverClassificationSettings({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки сохранены", {description: "Воркер подхватит их в течение минуты"})
                void queryClient.invalidateQueries({queryKey: getGetApiV1DiscoverClassificationSettingsQueryKey()})
            },
            onError: (error) => {
                toast.error("Ошибка сохранения", {description: error.title || "Не удалось сохранить настройки"})
            },
        },
    })

    const onSubmit = (values: FormValues) => {
        save({
            data: {
                isEnabled: values.isEnabled,
                model: values.model.trim(),
                batchSize: values.batchSize,
                intervalMinutes: values.intervalMinutes,
                messageSampleCount: values.messageSampleCount,
                photoCount: values.photoCount,
                reclassifyAfterDays: values.reclassify ? values.reclassifyAfterDays : null,
                categories: values.categories,
                systemPrompt: values.systemPrompt,
                telegramSessionId: values.telegramSessionId === PURPOSE_SESSION ? null : values.telegramSessionId,
            },
        })
    }

    const watched = form.watch()
    const perDay = Math.floor(1440 / Math.max(1, watched.intervalMinutes || 1)) * (watched.batchSize || 0)
    const savedSession = settings.telegramSession
    const savedSessionIsForeign = savedSession != null && !sessions.some((s) => s.id === savedSession.id)
    const selectedSession = sessions.find((s) => s.id === watched.telegramSessionId)
        ?? (savedSession?.id === watched.telegramSessionId ? savedSession : undefined)
    const selectedSessionInactive = selectedSession?.isActive === false

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
                                            <FormLabel>Классификатор включён</FormLabel>
                                            <FormDescription>Выключенный не берёт новые каналы, статистика остаётся</FormDescription>
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
                                    name="batchSize"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Каналов за запуск</FormLabel>
                                            <FormControl>
                                                <NumberInput value={field.value} onChange={field.onChange} min={1} max={50} disabled={isPending}/>
                                            </FormControl>
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                            </div>
                            <p className="text-xs text-muted-foreground">
                                Пропускная способность — до {perDay.toLocaleString("ru-RU")} каналов в сутки
                            </p>
                            <FormField
                                control={form.control}
                                name="reclassify"
                                render={({field}) => (
                                    <FormItem className="flex items-center justify-between gap-4 rounded-md border p-3">
                                        <div>
                                            <FormLabel>Переклассифицировать старые</FormLabel>
                                            <FormDescription>
                                                Когда очередь пуста, заново проходить каналы, классифицированные давно
                                            </FormDescription>
                                        </div>
                                        <FormControl>
                                            <Switch checked={field.value} onCheckedChange={field.onChange} disabled={isPending}/>
                                        </FormControl>
                                    </FormItem>
                                )}
                            />
                            {watched.reclassify && (
                                <FormField
                                    control={form.control}
                                    name="reclassifyAfterDays"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Через сколько дней</FormLabel>
                                            <FormControl>
                                                <NumberInput value={field.value} onChange={field.onChange} min={1} max={365} disabled={isPending}/>
                                            </FormControl>
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                            )}
                        </CardContent>
                    </Card>

                    <div className="space-y-4">
                        <Card>
                            <CardHeader className="pb-3">
                                <CardTitle className="text-base">Модель и сессия</CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <FormField
                                    control={form.control}
                                    name="model"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Модель OpenRouter</FormLabel>
                                            <FormControl>
                                                <Input {...field} placeholder="qwen/qwen3-vl-8b-instruct" disabled={isPending}/>
                                            </FormControl>
                                            <FormDescription>
                                                Ключ OpenRouter задаётся в конфиге воркера (OpenRouterOptions:SecretKey)
                                            </FormDescription>
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                                <FormField
                                    control={form.control}
                                    name="telegramSessionId"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Telegram-сессия для чтения постов</FormLabel>
                                            <Select value={field.value} onValueChange={field.onChange} disabled={isPending}>
                                                <FormControl>
                                                    <SelectTrigger>
                                                        <SelectValue placeholder="Выберите сессию"/>
                                                    </SelectTrigger>
                                                </FormControl>
                                                <SelectContent>
                                                    <SelectItem value={PURPOSE_SESSION}>Сессия с назначением «Классификация»</SelectItem>
                                                    {sessions.map((session) => (
                                                        <SelectItem key={session.id} value={session.id}>
                                                            {session.name || session.phoneNumber}
                                                            {session.isActive === false && " — неактивна"}
                                                            {session.status != null && session.status !== "Authorized" && " — не авторизована"}
                                                        </SelectItem>
                                                    ))}
                                                    {savedSessionIsForeign && savedSession && (
                                                        <SelectItem value={savedSession.id}>
                                                            {savedSession.name || "Без названия"} — другого пользователя
                                                        </SelectItem>
                                                    )}
                                                </SelectContent>
                                            </Select>
                                            {selectedSessionInactive ? (
                                                <p className="flex items-center gap-1.5 text-xs text-amber-600">
                                                    <AlertTriangle className="h-3.5 w-3.5"/>
                                                    Сессия неактивна — воркер будет искать сессию по назначению
                                                </p>
                                            ) : (
                                                <FormDescription>
                                                    Через неё классификатор открывает каналы и читает последние посты
                                                </FormDescription>
                                            )}
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="pb-3">
                                <CardTitle className="text-base">Выборка из канала</CardTitle>
                            </CardHeader>
                            <CardContent className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                <FormField
                                    control={form.control}
                                    name="messageSampleCount"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Последних постов</FormLabel>
                                            <FormControl>
                                                <NumberInput value={field.value} onChange={field.onChange} min={5} max={100} disabled={isPending}/>
                                            </FormControl>
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                                <FormField
                                    control={form.control}
                                    name="photoCount"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Фото в модель</FormLabel>
                                            <FormControl>
                                                <NumberInput value={field.value} onChange={field.onChange} min={0} max={10} disabled={isPending}/>
                                            </FormControl>
                                            <FormDescription>0 — без фото; модель должна понимать изображения</FormDescription>
                                            <FormMessage/>
                                        </FormItem>
                                    )}
                                />
                            </CardContent>
                        </Card>
                    </div>
                </div>

                <Card>
                    <CardHeader className="pb-3">
                        <CardTitle className="text-base">Тематики</CardTitle>
                        <p className="text-xs text-muted-foreground">
                            Модель выбирает ровно одну. Уже классифицированные каналы сохраняют старую тематику до переклассификации
                        </p>
                    </CardHeader>
                    <CardContent>
                        <FormField
                            control={form.control}
                            name="categories"
                            render={({field}) => (
                                <FormItem>
                                    <FormControl>
                                        <CategoriesEditor
                                            value={field.value}
                                            onChange={field.onChange}
                                            defaults={settings.defaultCategories}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="pb-3">
                        <div className="flex flex-wrap items-start justify-between gap-2">
                            <div>
                                <CardTitle className="text-base">Системный промпт</CardTitle>
                                <p className="text-xs text-muted-foreground">
                                    {settings.categoriesPlaceholder} заменится списком тематик через запятую. Формат ответа
                                    (JSON с category, subcategory, tags, language, confidence) лучше не менять — иначе ответ не разберётся
                                </p>
                            </div>
                            <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="gap-1.5"
                                onClick={() => form.setValue("systemPrompt", settings.defaultSystemPrompt, {shouldDirty: true, shouldValidate: true})}
                                disabled={isPending}
                            >
                                <RotateCcw className="h-3.5 w-3.5"/>
                                Стандартный промпт
                            </Button>
                        </div>
                    </CardHeader>
                    <CardContent>
                        <FormField
                            control={form.control}
                            name="systemPrompt"
                            render={({field}) => (
                                <FormItem>
                                    <FormControl>
                                        <Textarea {...field} rows={16} className="font-mono text-xs" disabled={isPending}/>
                                    </FormControl>
                                    <div className="flex justify-between gap-2">
                                        <FormMessage/>
                                        <span className="text-xs text-muted-foreground ml-auto tabular-nums">
                                            {field.value.length.toLocaleString("ru-RU")} / 8 000
                                        </span>
                                    </div>
                                </FormItem>
                            )}
                        />
                    </CardContent>
                </Card>

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

// Настройки классификатора: грузим текущие значения и отдаём в форму
export function ClassifierSettingsForm() {
    const {data: settings, isLoading} = useGetApiV1DiscoverClassificationSettings()

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
