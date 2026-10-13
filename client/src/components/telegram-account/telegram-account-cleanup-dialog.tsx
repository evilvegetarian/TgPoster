import { useEffect } from "react";
import { AlertTriangle, CheckCircle2, Clock, Loader2, MinusCircle, XCircle } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Progress } from "@/components/ui/progress";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
    useGetApiV1ChannelCleanupSessionsSessionIdPreview,
    usePostApiV1ChannelCleanupSessionsSessionId,
} from "@/api/endpoints/channel-cleanup/channel-cleanup";
import type {
    ChannelCleanupJobResponse,
    ChannelCleanupOutcome,
    ChannelCleanupStatus,
    ChatType,
    TelegramSessionResponse,
} from "@/api/endpoints/tgPosterAPI.schemas";
import { isCleanupJobActive, useChannelCleanupJob } from "@/hooks/use-channel-cleanup-job";

const STATUS_LABELS: Record<ChannelCleanupStatus, string> = {
    Pending: "В очереди",
    InProgress: "Выполняется",
    CooldownWait: "Пауза из-за ограничений Telegram",
    Completed: "Завершено",
    Failed: "Остановлено с ошибкой",
};

const OUTCOME_LABELS: Record<ChannelCleanupOutcome, string> = {
    Pending: "В очереди",
    Left: "Аккаунт вышел",
    NotMember: "Аккаунт уже не состоял",
    InUse: "Оставлен: начал использоваться",
    Failed: "Не удалось выйти",
    NotProcessed: "Не обработан",
};

function chatTypeLabel(chatType: ChatType) {
    return chatType === "Channel" ? "Канал" : "Группа";
}

function OutcomeIcon({ outcome }: { outcome: ChannelCleanupOutcome }) {
    if (outcome === "Left" || outcome === "NotMember") {
        return <CheckCircle2 className="h-4 w-4 text-green-600 flex-shrink-0" />;
    }

    if (outcome === "Pending") {
        return <Clock className="h-4 w-4 text-muted-foreground flex-shrink-0" />;
    }

    if (outcome === "InUse") {
        return <MinusCircle className="h-4 w-4 text-muted-foreground flex-shrink-0" />;
    }

    return <XCircle className="h-4 w-4 text-destructive flex-shrink-0" />;
}

interface PreviewStepProps {
    sessionId: string;
    onStarted: (jobId: string) => void;
    onCancel: () => void;
}

function PreviewStep({ sessionId, onStarted, onCancel }: PreviewStepProps) {
    const { data: preview, isLoading, error } = useGetApiV1ChannelCleanupSessionsSessionIdPreview(sessionId, {
        query: { retry: false, refetchOnWindowFocus: false, gcTime: 0 },
    });

    const { mutate: startCleanup, isPending } = usePostApiV1ChannelCleanupSessionsSessionId({
        mutation: {
            onSuccess: (job) => {
                onStarted(job.jobId);
                toast.success(`Очистка запущена: каналов в очереди ${job.totalCount}`, {
                    description: "Аккаунт выходит по одному каналу с паузами, окно можно свернуть",
                });
            },
            onError: (startError) => {
                toast.error("Не удалось запустить очистку", {
                    description: startError.title || "Обновите предпросмотр и попробуйте ещё раз",
                });
            },
        },
    });

    const candidates = preview?.candidates ?? [];

    function handleStart() {
        startCleanup({ sessionId, data: { chatIds: candidates.map((x) => x.chatId) } });
    }

    return (
        <>
            {isLoading ? (
                <div className="flex items-center gap-2 py-8 text-sm text-muted-foreground">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Получаем список каналов аккаунта...
                </div>
            ) : error ? (
                <p className="py-4 text-sm text-destructive">
                    {error.title || "Не удалось получить список каналов аккаунта"}
                </p>
            ) : preview ? (
                <div className="flex min-h-0 min-w-0 flex-col gap-4">
                    <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                        <span className="text-muted-foreground">Всего каналов и групп</span>
                        <span>{preview.totalCount}</span>
                        <span className="text-muted-foreground">Используются приложением</span>
                        <span>{preview.inUseCount}</span>
                        <span className="text-muted-foreground">Аккаунт создатель или админ</span>
                        <span>{preview.adminCount}</span>
                        <span className="font-medium">Аккаунт выйдет из</span>
                        <span className="font-medium">{candidates.length}</span>
                    </div>

                    {candidates.length === 0 ? (
                        <p className="text-sm text-muted-foreground">
                            Ненужных каналов нет — все используются приложением или принадлежат аккаунту
                        </p>
                    ) : (
                        <>
                            <div className="flex gap-2 text-sm text-amber-600">
                                <AlertTriangle className="h-4 w-4 flex-shrink-0 mt-0.5" />
                                <p>
                                    Выход необратим: в приватный канал без инвайт-ссылки вернуться не получится.
                                    Аккаунт выходит по одному каналу с паузами, на 100 каналов уходит около 20 минут
                                </p>
                            </div>

                            <ScrollArea className="min-h-0 min-w-0 flex-1 pr-3">
                                <div className="space-y-2">
                                    {candidates.map((item) => (
                                        <div
                                            key={item.chatId}
                                            className="flex items-center gap-2 text-sm border-b pb-2 last:border-b-0"
                                        >
                                            <div className="min-w-0 flex-1">
                                                <p className="truncate font-medium">{item.title}</p>
                                                <p className="truncate text-xs text-muted-foreground">
                                                    {item.username ? `@${item.username}` : "Приватный"}
                                                </p>
                                            </div>
                                            <Badge variant="outline">{chatTypeLabel(item.chatType)}</Badge>
                                        </div>
                                    ))}
                                </div>
                            </ScrollArea>
                        </>
                    )}
                </div>
            ) : null}

            <DialogFooter>
                <Button type="button" variant="outline" onClick={onCancel} disabled={isPending}>
                    Отмена
                </Button>
                <Button
                    type="button"
                    variant="destructive"
                    onClick={handleStart}
                    disabled={isPending || candidates.length === 0}
                >
                    {isPending ? (
                        <>
                            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                            Запуск...
                        </>
                    ) : (
                        `Выйти из ${candidates.length}`
                    )}
                </Button>
            </DialogFooter>
        </>
    );
}

interface ProgressStepProps {
    job: ChannelCleanupJobResponse;
    onMinimize: () => void;
    onFinish: () => void;
}

function ProgressStep({ job, onMinimize, onFinish }: ProgressStepProps) {
    const processed = job.totalCount - job.pendingCount;
    const percent = job.totalCount > 0 ? Math.round((processed / job.totalCount) * 100) : 100;

    return (
        <>
            <div className="flex min-h-0 min-w-0 flex-col gap-4">
                <div className="space-y-2">
                    <div className="flex items-center justify-between gap-2">
                        <Badge variant={job.status === "Failed" ? "destructive" : "secondary"}>
                            {STATUS_LABELS[job.status]}
                        </Badge>
                        <span className="text-sm text-muted-foreground">
                            {processed} из {job.totalCount}
                        </span>
                    </div>
                    <Progress value={percent} />
                    <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
                        <span>Вышли: {job.leftCount}</span>
                        <span>Оставлены: {job.skippedCount}</span>
                        <span>Ошибки: {job.failedCount}</span>
                        <span>В очереди: {job.pendingCount}</span>
                    </div>
                </div>

                {job.status === "CooldownWait" && (
                    <p className="text-sm text-amber-600">
                        Telegram ограничил сессию
                        {job.retryAfterSeconds
                            ? `, очистка продолжится примерно через ${Math.ceil(job.retryAfterSeconds / 60)} мин`
                            : ", очистка продолжится позже"}
                    </p>
                )}

                {job.status === "Failed" && job.lastError && (
                    <p className="text-sm text-destructive">Остановлено: {job.lastError}</p>
                )}

                <ScrollArea className="min-h-0 min-w-0 flex-1 pr-3">
                    <div className="space-y-2">
                        {job.results.map((item) => (
                            <div
                                key={item.chatId}
                                className="flex items-start gap-2 text-sm border-b pb-2 last:border-b-0"
                            >
                                <OutcomeIcon outcome={item.outcome} />
                                <div className="min-w-0 flex-1">
                                    <p className="truncate font-medium">{item.title}</p>
                                    <p className="text-xs text-muted-foreground break-words">
                                        {OUTCOME_LABELS[item.outcome]}
                                        {item.error ? ` — ${item.error}` : ""}
                                    </p>
                                </div>
                            </div>
                        ))}
                    </div>
                </ScrollArea>
            </div>

            <DialogFooter>
                {isCleanupJobActive(job) ? (
                    <Button type="button" onClick={onMinimize}>
                        Свернуть
                    </Button>
                ) : (
                    <Button type="button" onClick={onFinish}>
                        Готово
                    </Button>
                )}
            </DialogFooter>
        </>
    );
}

interface CleanupDialogProps {
    sessionId: string;
    accountName: string;
    onClose: () => void;
}

function CleanupDialog({ sessionId, accountName, onClose }: CleanupDialogProps) {
    const { jobId, job, isMissing, startJob, clearJob } = useChannelCleanupJob(sessionId);

    useEffect(() => {
        if (isMissing) {
            clearJob();
        }
    }, [isMissing, clearJob]);

    function handleOpenChange(isOpen: boolean) {
        if (isOpen) {
            return;
        }

        // Завершённую задачу забываем при закрытии, иначе в следующий раз откроется старый отчёт вместо предпросмотра
        if (job != null && !isCleanupJobActive(job)) {
            clearJob();
        }

        onClose();
    }

    function handleFinish() {
        clearJob();
        onClose();
    }

    return (
        <Dialog open onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[560px] max-h-[85dvh] grid-rows-[auto_minmax(0,1fr)_auto]">
                <DialogHeader>
                    <DialogTitle>Очистка каналов</DialogTitle>
                    <DialogDescription>
                        {accountName}: выход из каналов и групп, которые приложение
                        не использует. Направления и источники репоста, каналы парсинга и репоста комментариев,
                        а также каналы, где аккаунт создатель или админ, остаются
                    </DialogDescription>
                </DialogHeader>

                {jobId === null ? (
                    <PreviewStep sessionId={sessionId} onStarted={startJob} onCancel={onClose} />
                ) : job ? (
                    <ProgressStep job={job} onMinimize={onClose} onFinish={handleFinish} />
                ) : (
                    <>
                        <div className="flex items-center gap-2 py-8 text-sm text-muted-foreground">
                            <Loader2 className="h-4 w-4 animate-spin" />
                            Загружаем состояние очистки...
                        </div>
                        <DialogFooter />
                    </>
                )}
            </DialogContent>
        </Dialog>
    );
}

interface TelegramAccountCleanupDialogProps {
    account: TelegramSessionResponse | null;
    onClose: () => void;
}

export function TelegramAccountCleanupDialog({ account, onClose }: TelegramAccountCleanupDialogProps) {
    const sessionId = account?.id;
    if (account == null || sessionId == null) {
        return null;
    }

    // key: хук задачи читает localStorage по id сессии только при монтировании
    return (
        <CleanupDialog
            key={sessionId}
            sessionId={sessionId}
            accountName={account.name || account.phoneNumber || "Аккаунт"}
            onClose={onClose}
        />
    );
}
