import type {ReactNode} from "react"
import {Link} from "react-router-dom"
import {useQueryClient} from "@tanstack/react-query"
import {format} from "date-fns"
import {ru} from "date-fns/locale"
import {toast} from "sonner"
import {AlertTriangle, CalendarClock, Image as ImageIcon, Loader2, RotateCw, Video} from "lucide-react"
import {getGetApiV1DashboardQueryKey, useGetApiV1Dashboard} from "@/api/endpoints/dashboard/dashboard"
import {usePostApiV1MessageRetry} from "@/api/endpoints/message/message"
import {
    RetryMode,
    type DashboardSchedule,
    type DashboardSession,
    type DashboardWorker,
} from "@/api/endpoints/tgPosterAPI.schemas"
import {Alert, AlertDescription, AlertTitle} from "@/components/ui/alert"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card"
import {Skeleton} from "@/components/ui/skeleton"
import {Tooltip, TooltipContent, TooltipTrigger} from "@/components/ui/tooltip"
import {formatDateTime, formatRelative} from "@/components/discover/format"

// Человекочитаемые имена фоновых задач (WorkerJobNames.cs)
const JOB_NAMES: Record<string, string> = {
    "discover-channel-links-job": "Поиск каналов (Discover)",
    "classify-channels-job": "Классификатор каналов",
    "cleanup-s3-files-job": "Очистка файлов в S3",
    "process-parse-channel-job": "Парсинг каналов",
}

const WORKER_LINKS: Record<string, string> = {
    "classify-channels-job": "/discover/classification",
    "process-parse-channel-job": "/parse-channel/stats",
}

const JOB_STATUS_LABELS: Record<string, string> = {
    Idle: "Ожидает запуска",
    Running: "Выполняется",
    CooldownWait: "Пауза",
    Failed: "Ошибка",
}

const SESSION_STATUS_LABELS: Record<string, string> = {
    AwaitingCode: "Ждёт код",
    CodeSent: "Код отправлен",
    AwaitingPassword: "Ждёт пароль",
    Authorized: "Авторизован",
    Failed: "Ошибка",
}

function workerLink(jobName: string | undefined): string {
    return WORKER_LINKS[jobName ?? ""] ?? "/discover/stats"
}

function sessionName(session: DashboardSession): string {
    return session.name?.trim() || session.phoneMasked || "Без имени"
}

function isLowQueue(schedule: DashboardSchedule): boolean {
    if (!schedule.isActive) return false
    return (schedule.plannedCount ?? 0) === 0 || (schedule.queueDays != null && schedule.queueDays < 2)
}

function postTime(value: string | undefined): string {
    return value ? format(new Date(value), "d MMM, HH:mm", {locale: ru}) : "—"
}

function Section({title, children, action}: { title: string; children: ReactNode; action?: ReactNode }) {
    return (
        <Card>
            <CardHeader className="flex flex-row items-center justify-between gap-2">
                <CardTitle className="text-base">{title}</CardTitle>
                {action}
            </CardHeader>
            <CardContent className="space-y-3">{children}</CardContent>
        </Card>
    )
}

function Empty({children}: { children: ReactNode }) {
    return <p className="text-sm text-muted-foreground">{children}</p>
}

function AttentionLink({to, children}: { to: string; children: ReactNode }) {
    return (
        <Link to={to} className="block rounded-md border p-3 text-sm hover:bg-accent">
            {children}
        </Link>
    )
}

function DashboardSkeleton() {
    return (
        <div className="space-y-4">
            <Skeleton className="h-8 w-48"/>
            <Skeleton className="h-40 w-full"/>
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                <Skeleton className="h-56 w-full"/>
                <Skeleton className="h-56 w-full"/>
                <Skeleton className="h-40 w-full"/>
                <Skeleton className="h-40 w-full"/>
            </div>
        </div>
    )
}

function ScheduleRow({schedule}: { schedule: DashboardSchedule }) {
    const planned = schedule.plannedCount ?? 0
    return (
        <div className="space-y-1 rounded-md border p-3 text-sm">
            <div className="flex flex-wrap items-center gap-2">
                <span className="font-medium">{schedule.name}</span>
                <span className="text-muted-foreground">{schedule.channelName}</span>
                {!schedule.isActive && <Badge variant="secondary">выключено</Badge>}
                {!schedule.hasSlots && <Badge variant="destructive">нет слотов</Badge>}
            </div>
            <div className="text-muted-foreground">
                Запланировано: {planned}
                {schedule.lastPlannedAt && <> · хватит до {formatDateTime(schedule.lastPlannedAt)}</>}
                {schedule.queueDays != null && <> · на {schedule.queueDays} дн.</>}
            </div>
            <div className="text-muted-foreground">
                Следующий пост: {schedule.nextPostAt ? formatDateTime(schedule.nextPostAt) : "—"}
            </div>
            {(schedule.awaitingApprovalCount ?? 0) > 0 && (
                <div className="text-amber-700">Ждут подтверждения: {schedule.awaitingApprovalCount}</div>
            )}
        </div>
    )
}

function WorkerRow({worker}: { worker: DashboardWorker }) {
    const name = JOB_NAMES[worker.jobName ?? ""] ?? worker.jobName ?? "—"
    const status = worker.status ?? "Idle"
    const badge = (
        <Badge variant={status === "Failed" ? "destructive" : "secondary"}>
            {JOB_STATUS_LABELS[status] ?? status}
        </Badge>
    )
    return (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border p-3 text-sm">
            <div className="min-w-0">
                <p className="font-medium">{name}</p>
                <p className="text-muted-foreground">
                    Следующий запуск: {worker.nextRunAt ? formatRelative(worker.nextRunAt) : "—"}
                </p>
            </div>
            {worker.lastError ? (
                <Tooltip>
                    <TooltipTrigger asChild><span>{badge}</span></TooltipTrigger>
                    <TooltipContent className="max-w-sm">{worker.lastError}</TooltipContent>
                </Tooltip>
            ) : badge}
        </div>
    )
}

export function HomePage() {
    const queryClient = useQueryClient()
    const {data, isLoading, isError, error} = useGetApiV1Dashboard({query: {refetchInterval: 60_000}})

    const {mutate: retryAll, isPending: isRetrying} = usePostApiV1MessageRetry({
        mutation: {
            onSuccess: (response) => {
                toast.success("Повтор запланирован", {description: `Постов: ${response.items.length}`})
                queryClient.invalidateQueries({queryKey: getGetApiV1DashboardQueryKey()})
            },
            onError: (err) => {
                toast.error("Ошибка", {description: err.title ?? "Не удалось запланировать повтор"})
            },
        },
    })

    if (isLoading) return <DashboardSkeleton/>

    if (isError || !data) {
        return (
            <Alert variant="destructive">
                <AlertTriangle className="h-4 w-4"/>
                <AlertTitle>Не удалось загрузить главную страницу</AlertTitle>
                <AlertDescription>{error?.title ?? "Попробуйте обновить страницу позже"}</AlertDescription>
            </Alert>
        )
    }

    const lowQueueSchedules = data.schedules.filter(isLowQueue)
    const problemSessions = data.telegramSessions.filter(s => s.hasProblem)
    const failedWorkers = data.workers.filter(w => w.status === "Failed")
    const hasAttention = data.failedPosts.length > 0 || lowQueueSchedules.length > 0
        || problemSessions.length > 0 || data.parsingProblems.length > 0 || failedWorkers.length > 0
    const failedIds = data.failedPosts.map(p => p.id).filter((id): id is string => !!id)
    const reposts = data.reposts

    return (
        <div className="space-y-4">
            <h1 className="text-2xl font-semibold">Главная</h1>

            {hasAttention && (
                <Section title="Требует внимания">
                    {data.failedPosts.length > 0 && (
                        <div className="space-y-2">
                            <div className="flex flex-wrap items-center justify-between gap-2">
                                <p className="text-sm font-medium text-destructive">
                                    Не отправлено постов: {data.failedPostsTotal}
                                </p>
                                <Button size="sm" variant="outline" disabled={isRetrying || failedIds.length === 0}
                                        onClick={() => retryAll({data: {ids: failedIds, mode: RetryMode.NextFreeSlot}})}>
                                    {isRetrying
                                        ? <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        : <RotateCw className="mr-2 h-4 w-4"/>}
                                    Повторить все в свободные слоты
                                </Button>
                            </div>
                            {data.failedPosts.map((post, i) => (
                                <AttentionLink key={post.id ?? i} to="/messages">
                                    <div className="font-medium">
                                        {post.scheduleName} · {postTime(post.timePosting)}
                                    </div>
                                    <div className="text-muted-foreground">{post.sendError ?? "Причина не сохранена"}</div>
                                </AttentionLink>
                            ))}
                            {data.failedPostsTotal > data.failedPosts.length && (
                                <p className="text-xs text-muted-foreground">
                                    Показаны первые {data.failedPosts.length} из {data.failedPostsTotal}
                                </p>
                            )}
                        </div>
                    )}
                    {lowQueueSchedules.map((s, i) => (
                        <AttentionLink key={s.id ?? i} to="/messages">
                            <span className="font-medium">{s.name}</span>:{" "}
                            {(s.plannedCount ?? 0) === 0
                                ? "очередь пуста, нет запланированных постов"
                                : `очереди хватит меньше чем на 2 дня (${s.queueDays} дн.)`}
                        </AttentionLink>
                    ))}
                    {problemSessions.map((s, i) => (
                        <AttentionLink key={s.id ?? i} to="/telegram-accounts">
                            <span className="font-medium">{sessionName(s)}</span>: проблема с Telegram-аккаунтом
                            ({SESSION_STATUS_LABELS[s.status ?? ""] ?? s.status})
                        </AttentionLink>
                    ))}
                    {data.parsingProblems.map((p, i) => (
                        <AttentionLink key={p.id ?? i} to="/parse-channel">
                            <span className="font-medium">{p.channel}</span> ({p.scheduleName}): парсинг не работает
                            {p.lastParseDate && <>, последний раз {formatDateTime(p.lastParseDate)}</>}
                        </AttentionLink>
                    ))}
                    {failedWorkers.map((w, i) => (
                        <AttentionLink key={w.jobName ?? i} to={workerLink(w.jobName)}>
                            <span className="font-medium">{JOB_NAMES[w.jobName ?? ""] ?? w.jobName}</span>:
                            последний запуск завершился ошибкой
                        </AttentionLink>
                    ))}
                </Section>
            )}

            {data.schedules.length === 0 ? (
                <Section title="Расписания">
                    <Empty>Расписаний пока нет.</Empty>
                    <Button asChild size="sm">
                        <Link to="/schedule"><CalendarClock className="mr-2 h-4 w-4"/>Создать расписание</Link>
                    </Button>
                </Section>
            ) : (
                <Section title="Расписания">
                    <div className="grid grid-cols-1 gap-3 lg:grid-cols-2 xl:grid-cols-3">
                        {data.schedules.map((s, i) => <ScheduleRow key={s.id ?? i} schedule={s}/>)}
                    </div>
                </Section>
            )}

            <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                <Section title="Ближайшие посты">
                    {data.upcomingPosts.length === 0 && <Empty>Запланированных постов нет.</Empty>}
                    {data.upcomingPosts.slice(0, 10).map((post, i) => (
                        <div key={post.id ?? i} className="rounded-md border p-3 text-sm">
                            <div className="flex flex-wrap items-center gap-2">
                                <span className="font-medium">{postTime(post.timePosting)}</span>
                                <span className="text-muted-foreground">{post.scheduleName}</span>
                                {post.needApprove && <Badge variant="secondary">ждёт подтверждения</Badge>}
                                {(post.photoCount ?? 0) > 0 && (
                                    <span className="inline-flex items-center gap-1 text-muted-foreground">
                                        <ImageIcon className="h-4 w-4"/>{post.photoCount}
                                    </span>
                                )}
                                {(post.videoCount ?? 0) > 0 && (
                                    <span className="inline-flex items-center gap-1 text-muted-foreground">
                                        <Video className="h-4 w-4"/>{post.videoCount}
                                    </span>
                                )}
                            </div>
                            {post.textPreview && (
                                <p className="mt-1 truncate text-muted-foreground">{post.textPreview}</p>
                            )}
                        </div>
                    ))}
                </Section>

                <Section title="Репосты за сутки"
                         action={<Button asChild variant="link" size="sm"><Link to="/repost-logs">Журнал</Link></Button>}>
                    <div className="flex flex-wrap gap-2 text-sm">
                        <Badge variant="secondary">Успешно: {reposts.successCount ?? 0}</Badge>
                        <Badge variant={(reposts.failedCount ?? 0) > 0 ? "destructive" : "secondary"}>
                            Ошибки: {reposts.failedCount ?? 0}
                        </Badge>
                        <Badge variant="secondary">Пропущено: {reposts.skippedCount ?? 0}</Badge>
                    </div>
                    {(reposts.topErrors ?? []).length > 0 && (
                        <div className="space-y-1 text-sm">
                            <p className="text-muted-foreground">Частые ошибки</p>
                            {(reposts.topErrors ?? []).map((e, i) => (
                                <div key={i} className="flex justify-between gap-2">
                                    <span className="truncate">{e.error}</span>
                                    <span className="shrink-0 text-muted-foreground">{e.count}</span>
                                </div>
                            ))}
                        </div>
                    )}
                </Section>

                <Section title="Telegram-аккаунты"
                         action={<Button asChild variant="link" size="sm"><Link to="/telegram-accounts">Все</Link></Button>}>
                    {data.telegramSessions.length === 0 && <Empty>Аккаунтов нет.</Empty>}
                    {data.telegramSessions.map((s, i) => (
                        <div key={s.id ?? i}
                             className="flex flex-wrap items-center justify-between gap-2 rounded-md border p-3 text-sm">
                            <div className="min-w-0">
                                <p className="font-medium">{sessionName(s)}</p>
                                <p className="text-muted-foreground">{s.phoneMasked}</p>
                                {s.floodWaitUntil && new Date(s.floodWaitUntil) > new Date() && (
                                    <p className="text-amber-700">FloodWait до {formatDateTime(s.floodWaitUntil)}</p>
                                )}
                            </div>
                            <Badge variant={s.hasProblem ? "destructive" : "secondary"}>
                                {SESSION_STATUS_LABELS[s.status ?? ""] ?? s.status}
                                {!s.isActive && " · выключен"}
                            </Badge>
                        </div>
                    ))}
                </Section>

                <Section title="Фоновые задачи">
                    {data.workers.length === 0 && <Empty>Данных о задачах нет.</Empty>}
                    {data.workers.map((w, i) => <WorkerRow key={w.jobName ?? i} worker={w}/>)}
                </Section>
            </div>
        </div>
    )
}
