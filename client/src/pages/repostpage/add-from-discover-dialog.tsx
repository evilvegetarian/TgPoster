import {useState} from "react";
import {AlertTriangle, CheckCircle2, Loader2, XCircle} from "lucide-react";
import {toast} from "sonner";
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
import {ScrollArea} from "@/components/ui/scroll-area";
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select";
import {Switch} from "@/components/ui/switch";
import {
    useGetApiV1RepostSettings,
    usePostApiV1RepostSettingsSettingsIdDestinationsFromDiscover,
} from "@/api/endpoints/repost/repost.ts";
import type {
    AddDestinationOutcome,
    AddDestinationsFromDiscoverResponse,
} from "@/api/endpoints/tgPosterAPI.schemas.ts";

const MAX_CHANNELS = 20;

const OUTCOME_LABELS: Record<AddDestinationOutcome, string> = {
    Added: "Добавлен",
    AlreadyAdded: "Уже добавлен",
    SourceChannel: "Канал-источник",
    NoWritePermission: "Нет прав на публикацию",
    NoMediaPermission: "Нет прав на медиа",
    NotResolved: "Не удалось открыть",
    RateLimited: "Ограничение Telegram",
};

interface AddFromDiscoverDialogProps {
    selectedChannelIds: string[];
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onAdded: () => void;
}

function OutcomeIcon({outcome}: {outcome: AddDestinationOutcome}) {
    if (outcome === "Added") {
        return <CheckCircle2 className="h-4 w-4 text-green-600 flex-shrink-0"/>;
    }

    if (outcome === "RateLimited") {
        return <AlertTriangle className="h-4 w-4 text-amber-500 flex-shrink-0"/>;
    }

    return <XCircle className="h-4 w-4 text-muted-foreground flex-shrink-0"/>;
}

export function AddFromDiscoverDialog({
    selectedChannelIds,
    open,
    onOpenChange,
    onAdded,
}: AddFromDiscoverDialogProps) {
    const [settingsId, setSettingsId] = useState<string>("");
    const [autoJoin, setAutoJoin] = useState(true);
    const [result, setResult] = useState<AddDestinationsFromDiscoverResponse | null>(null);

    const {data: settingsData, isLoading: isSettingsLoading} = useGetApiV1RepostSettings({
        query: {enabled: open},
    });
    const settings = settingsData?.items ?? [];

    const tooManyChannels = selectedChannelIds.length > MAX_CHANNELS;

    const {mutate: addFromDiscover, isPending} = usePostApiV1RepostSettingsSettingsIdDestinationsFromDiscover({
        mutation: {
            onSuccess: (response) => {
                setResult(response);

                if (response.addedCount > 0) {
                    toast.success(`Добавлено каналов: ${response.addedCount}`, {
                        description: response.skippedCount > 0
                            ? `Пропущено: ${response.skippedCount}`
                            : undefined,
                    });
                    onAdded();
                } else {
                    toast.warning("Ни один канал не добавлен", {
                        description: "Проверьте причины в списке ниже",
                    });
                }

                if (response.rateLimited) {
                    toast.warning("Telegram ограничил аккаунт", {
                        description: "Часть каналов не обработана, повторите позже",
                    });
                }
            },
            onError: (error) => {
                toast.error("Ошибка добавления каналов", {
                    description: error.title || "Не удалось добавить каналы в репост",
                });
            },
        },
    });

    function handleSubmit() {
        addFromDiscover({
            settingsId,
            data: {discoveredChannelIds: selectedChannelIds, autoJoin},
        });
    }

    function handleOpenChange(isOpen: boolean) {
        if (!isOpen && isPending) {
            return;
        }

        if (!isOpen) {
            setResult(null);
        }

        onOpenChange(isOpen);
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[560px]">
                <DialogHeader>
                    <DialogTitle>Добавить в репост</DialogTitle>
                    <DialogDescription>
                        Выбрано каналов: {selectedChannelIds.length}. Каналы получат общие настройки
                        выбранного репоста, дальше их можно настроить по отдельности
                    </DialogDescription>
                </DialogHeader>

                {result ? (
                    <ScrollArea className="max-h-[320px] pr-3">
                        <div className="space-y-2">
                            {result.results.map((item) => (
                                <div
                                    key={item.discoveredChannelId}
                                    className="flex items-start gap-2 text-sm border-b pb-2 last:border-b-0"
                                >
                                    <OutcomeIcon outcome={item.outcome}/>
                                    <div className="min-w-0">
                                        <p className="truncate font-medium">{item.title}</p>
                                        <p className="text-xs text-muted-foreground">
                                            {OUTCOME_LABELS[item.outcome]}
                                            {item.error ? ` — ${item.error}` : ""}
                                        </p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </ScrollArea>
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
                                За один раз можно добавить не больше {MAX_CHANNELS} каналов
                            </p>
                        )}
                    </div>
                )}

                <DialogFooter>
                    {result ? (
                        <Button type="button" onClick={() => handleOpenChange(false)}>
                            Готово
                        </Button>
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
                                    || selectedChannelIds.length === 0}
                            >
                                {isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        Добавление...
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
