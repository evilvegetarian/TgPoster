import {useState} from "react";
import {AlertTriangle, CheckCircle2, Clock, Loader2, XCircle} from "lucide-react";
import {toast} from "sonner";
import {Badge} from "@/components/ui/badge";
import {Button} from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import {Label} from "@/components/ui/label";
import {Progress} from "@/components/ui/progress";
import {ScrollArea} from "@/components/ui/scroll-area";
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select";
import {Switch} from "@/components/ui/switch";
import {
    useGetApiV1RepostSettings,
    usePostApiV1RepostSettingsSettingsIdDestinationsFromDiscover,
    usePostApiV1RepostSettingsSettingsIdDestinationsFromDiscoverFilter,
} from "@/api/endpoints/repost/repost.ts";
import {RepostImportStatus} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import type {
    AddDestinationOutcome,
    AddDestinationsFromDiscoverFilterRequest,
    ProblemDetails,
    RepostImportJobResponse,
} from "@/api/endpoints/tgPosterAPI.schemas.ts";

const MAX_SELECTED_CHANNELS = 20;
const MAX_FILTER_CHANNELS = 200;

export type DiscoverFilterValues = Omit<AddDestinationsFromDiscoverFilterRequest, "autoJoin">;

const OUTCOME_LABELS: Record<AddDestinationOutcome, string> = {
    Added: "Добавлен",
    AlreadyAdded: "Уже добавлен",
    SourceChannel: "Канал-источник",
    NoWritePermission: "Нет прав на публикацию",
    NoMediaPermission: "Нет прав на медиа",
    NotResolved: "Не удалось открыть",
    RateLimited: "Ограничение Telegram",
    NotProcessed: "Не обработан",
    Pending: "В очереди",
};

const STATUS_LABELS: Record<RepostImportStatus, string> = {
    Pending: "В очереди",
    InProgress: "Выполняется",
    CooldownWait: "Пауза из-за ограничений Telegram",
    Completed: "Завершено",
    Failed: "Остановлено с ошибкой",
};

interface AddFromDiscoverDialogProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    /** selected — выбранные чекбоксами каналы, filter — все каналы под текущие фильтры */
    mode: "selected" | "filter";
    selectedChannelIds: string[];
    filter: DiscoverFilterValues;
    /** Сколько каналов нашлось по фильтру на странице Discover */
    matchedCount: number;
    job: RepostImportJobResponse | undefined;
    onJobStarted: (jobId: string) => void;
    onJobCleared: () => void;
}

function OutcomeIcon({outcome}: {outcome: AddDestinationOutcome}) {
    if (outcome === "Added") {
        return <CheckCircle2 className="h-4 w-4 text-green-600 flex-shrink-0"/>;
    }

    if (outcome === "Pending") {
        return <Clock className="h-4 w-4 text-muted-foreground flex-shrink-0"/>;
    }

    if (outcome === "RateLimited") {
        return <AlertTriangle className="h-4 w-4 text-amber-500 flex-shrink-0"/>;
    }

    return <XCircle className="h-4 w-4 text-muted-foreground flex-shrink-0"/>;
}

function JobProgress({job}: {job: RepostImportJobResponse}) {
    const processed = job.totalCount - job.pendingCount;
    const percent = job.totalCount > 0 ? Math.round((processed / job.totalCount) * 100) : 100;

    return (
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
                <Progress value={percent}/>
                <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
                    <span>Добавлено: {job.addedCount}</span>
                    <span>Пропущено: {job.skippedCount}</span>
                    <span>В очереди: {job.pendingCount}</span>
                </div>
            </div>

            {job.status === "CooldownWait" && (
                <p className="text-sm text-amber-600">
                    Telegram ограничил сессию
                    {job.retryAfterSeconds
                        ? `, обработка продолжится примерно через ${Math.ceil(job.retryAfterSeconds / 60)} мин`
                        : ", обработка продолжится позже"}
                </p>
            )}

            <ScrollArea className="min-h-0 min-w-0 flex-1 pr-3">
                <div className="space-y-2">
                    {job.results.map((item) => (
                        <div
                            key={item.discoveredChannelId}
                            className="flex items-start gap-2 text-sm border-b pb-2 last:border-b-0"
                        >
                            <OutcomeIcon outcome={item.outcome}/>
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
    );
}

export function AddFromDiscoverDialog({
    open,
    onOpenChange,
    mode,
    selectedChannelIds,
    filter,
    matchedCount,
    job,
    onJobStarted,
    onJobCleared,
}: AddFromDiscoverDialogProps) {
    const [settingsId, setSettingsId] = useState<string>("");
    const [autoJoin, setAutoJoin] = useState(true);

    const {data: settingsData, isLoading: isSettingsLoading} = useGetApiV1RepostSettings({
        query: {enabled: open},
    });
    const settings = settingsData?.items ?? [];

    const isJobRunning = job != null
        && job.status !== RepostImportStatus.Completed
        && job.status !== RepostImportStatus.Failed;
    const tooManyChannels = mode === "selected" && selectedChannelIds.length > MAX_SELECTED_CHANNELS;

    function handleJobCreated(response: RepostImportJobResponse) {
        onJobStarted(response.jobId);
        toast.success(`Каналов в задаче: ${response.totalCount}`, {
            description: response.pendingCount > 0
                ? "Каналы добавляются по одному, прогресс виден здесь и на странице Discover"
                : "Ни один канал не потребовал обращения к Telegram — задача уже завершена",
        });
    }

    function handleJobError(error: ProblemDetails) {
        toast.error("Не удалось создать задачу", {
            description: error.title || "Проверьте настройки репоста и попробуйте ещё раз",
        });
    }

    const {mutate: addSelected, isPending: isAddingSelected} =
        usePostApiV1RepostSettingsSettingsIdDestinationsFromDiscover({
            mutation: {onSuccess: handleJobCreated, onError: handleJobError},
        });
    const {mutate: addByFilter, isPending: isAddingByFilter} =
        usePostApiV1RepostSettingsSettingsIdDestinationsFromDiscoverFilter({
            mutation: {onSuccess: handleJobCreated, onError: handleJobError},
        });

    const isPending = isAddingSelected || isAddingByFilter;

    function handleSubmit() {
        if (mode === "filter") {
            addByFilter({settingsId, data: {...filter, autoJoin}});

            return;
        }

        addSelected({
            settingsId,
            data: {discoveredChannelIds: selectedChannelIds, autoJoin},
        });
    }

    function handleOpenChange(isOpen: boolean) {
        if (!isOpen && isPending) {
            return;
        }

        onOpenChange(isOpen);
    }

    function handleFinish() {
        onJobCleared();
        onOpenChange(false);
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[560px] max-h-[85dvh] grid-rows-[auto_minmax(0,1fr)_auto]">
                <DialogHeader>
                    <DialogTitle>Добавить в репост</DialogTitle>
                    <DialogDescription>
                        {job != null
                            ? "Каналы добавляются по одному с паузами, чтобы Telegram не ограничил аккаунт. "
                              + "Задачу можно свернуть — она продолжит работать"
                            : mode === "filter"
                                ? `По текущим фильтрам найдено каналов: ${matchedCount.toLocaleString()}. `
                                  + `В задачу уйдёт не больше ${MAX_FILTER_CHANNELS}: уже добавленные `
                                  + "и заведомо недоступные каналы пропускаются"
                                : `Выбрано каналов: ${selectedChannelIds.length}. Каналы получат общие настройки `
                                  + "выбранного репоста, дальше их можно настроить по отдельности"}
                    </DialogDescription>
                </DialogHeader>

                {job != null ? (
                    <JobProgress job={job}/>
                ) : (
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label htmlFor="repost-settings">Настройки репоста *</Label>
                            <Select value={settingsId} onValueChange={setSettingsId} disabled={isPending}>
                                <SelectTrigger id="repost-settings">
                                    <SelectValue placeholder={
                                        isSettingsLoading ? "Загрузка..." : "Выберите настройки репоста"
                                    }/>
                                </SelectTrigger>
                                <SelectContent>
                                    {settings.map((item) => (
                                        <SelectItem key={item.id} value={item.id}>
                                            {item.scheduleName} ({item.destinationsCount})
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            {!isSettingsLoading && settings.length === 0 && (
                                <p className="text-sm text-muted-foreground">
                                    Нет настроек репоста — создайте их на странице «Репосты»
                                </p>
                            )}
                        </div>

                        <div className="flex items-center justify-between gap-4">
                            <div className="space-y-0.5">
                                <Label htmlFor="auto-join">Вступать в канал</Label>
                                <p className="text-sm text-muted-foreground">
                                    Без вступления репост в канал не работает
                                </p>
                            </div>
                            <Switch
                                id="auto-join"
                                checked={autoJoin}
                                onCheckedChange={setAutoJoin}
                                disabled={isPending}
                            />
                        </div>

                        {tooManyChannels && (
                            <p className="text-sm text-destructive">
                                За один раз можно добавить не больше {MAX_SELECTED_CHANNELS} выбранных каналов.
                                Снимите лишние отметки или добавьте все каналы по фильтру
                            </p>
                        )}
                    </div>
                )}

                <DialogFooter>
                    {job != null ? (
                        isJobRunning ? (
                            <Button type="button" onClick={() => onOpenChange(false)}>
                                Свернуть
                            </Button>
                        ) : (
                            <Button type="button" onClick={handleFinish}>
                                Готово
                            </Button>
                        )
                    ) : (
                        <>
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => handleOpenChange(false)}
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button
                                type="button"
                                onClick={handleSubmit}
                                disabled={isPending || !settingsId || tooManyChannels
                                    || (mode === "selected" && selectedChannelIds.length === 0)
                                    || (mode === "filter" && matchedCount === 0)}
                            >
                                {isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        Создание задачи...
                                    </>
                                ) : (
                                    "Добавить"
                                )}
                            </Button>
                        </>
                    )}
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
