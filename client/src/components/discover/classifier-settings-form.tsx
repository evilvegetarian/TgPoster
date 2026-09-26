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
import type {ClassifierSessionOption, ClassifierSettingsResponse} from "@/api/endpoints/tgPosterAPI.schemas"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Checkbox} from "@/components/ui/checkbox"
import {Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form"
import {Input} from "@/components/ui/input"
import {Skeleton} from "@/components/ui/skeleton"
import {Switch} from "@/components/ui/switch"
import {Textarea} from "@/components/ui/textarea"
import {formatDateTime} from "@/components/discover/format"

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
        telegramSessionIds: z.array(z.string()).max(50, "Не больше 50 сессий"),
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
        telegramSessionIds: settings.sessions.filter((s) => s.isOwn && s.isSelected).map((s) => s.id),
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

function SessionStateBadges({session}: {session: ClassifierSessionOption}) {
    return (
        <>
            {!session.isActive && (
                <Badge variant="outline" className="shrink-0 border-amber-300 text-amber-700 font-normal">неактивна</Badge>
            )}
            {!session.isAuthorized && (
                <Badge variant="outline" className="shrink-0 border-amber-300 text-amber-700 font-normal">не авторизована</Badge>
            )}
        </>
    )
}

// Свои сессии отмечаются галочками; чужие, уже отданные классификатору, видны, но менять их нельзя
function SessionsPicker({sessions, value, onChange, disabled}: {
    sessions: ClassifierSessionOption[]
    value: string[]
    onChange: (value: string[]) => void
    disabled?: boolean
}) {
    const own = sessions.filter((s) => s.isOwn)
    const foreign = sessions.filter((s) => !s.isOwn)

    const toggle = (id: string, checked: boolean) =>
        onChange(checked ? [...value, id] : value.filter((x) => x !== id))

    return (
        <div className="space-y-1.5">
            {own.length === 0 && (
                <p className="text-sm text-muted-foreground">
                    У вас нет Telegram-сессий — добавьте их на странице «Telegram Аккаунты»
                </p>
            )}
            {own.map((session) => (
                <label
                    key={session.id}
                    className="flex items-center gap-3 rounded-md border px-3 py-2 cursor-pointer hover:bg-accent/40"
                >
                    <Checkbox
                        checked={value.includes(session.id)}
                        onCheckedChange={(checked) => toggle(session.id, checked === true)}
                        disabled={disabled}
                    />
                    <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium truncate">{session.name || session.phoneNumber || "Без названия"}</p>
                        {session.name && session.phoneNumber && (
                            <p className="text-xs text-muted-foreground truncate">{session.phoneNumber}</p>
                        )}
                    </div>
                    <SessionStateBadges session={session}/>
                </label>
            ))}
            {foreign.map((session) => (
                <div
                    key={session.id}
                    className="flex items-center gap-3 rounded-md border border-dashed px-3 py-2 text-muted-foreground"
                    title="Сессия другого пользователя: снять назначение может только владелец"
                >
                    <Checkbox checked disabled/>
                    <p className="min-w-0 flex-1 text-sm truncate">{session.name || "Без названия"}</p>
                    <Badge variant="secondary" className="shrink-0 font-normal">другого пользователя</Badge>
                    <SessionStateBadges session={session}/>
                </div>
            ))}
        </div>
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
                telegramSessionIds: values.telegramSessionIds,
            },
        })
    }

    const watched = form.watch()
    const perDay = Math.floor(1440 / Math.max(1, watched.intervalMinutes || 1)) * (watched.batchSize || 0)
    // Рабочие сессии: отмеченные свои плюс чужие, уже отданные классификатору
    const workingSessions = settings.sessions.filter((s) =>
        s.isActive && s.isAuthorized && (s.isOwn ? watched.telegramSessionIds.includes(s.id) : s.isSelected)).length

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
                                    name="telegramSessionIds"
                                    render={({field}) => (
                                        <FormItem>
                                            <FormLabel>Telegram-сессии для чтения постов</FormLabel>
                                            <SessionsPicker
                                                sessions={settings.sessions}
                                                value={field.value}
                                                onChange={field.onChange}
                                                disabled={isPending}
                                            />
                                            {workingSessions === 0 ? (
                                                <p className="flex items-center gap-1.5 text-xs text-amber-600">
                                                    <AlertTriangle className="h-3.5 w-3.5 shrink-0"/>
                                                    Нет ни одной активной авторизованной сессии — классификатор не сможет читать каналы
                                                </p>
                                            ) : (
                                                <FormDescription>
                                                    Каналы пачки раздаются выбранным сессиям по очереди. Если Telegram ограничит
                                                    сессию (FloodWait), её каналы заберут остальные
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
