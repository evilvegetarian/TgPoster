import {useState} from "react";
import {formatDistanceToNow} from "date-fns";
import {ru} from "date-fns/locale";
import {Loader2, RefreshCw, Settings, Trash2} from "lucide-react";
import {toast} from "sonner";
import {Badge} from "@/components/ui/badge";
import {Button} from "@/components/ui/button";
import {Switch} from "@/components/ui/switch";
import {ConfirmActionDialog} from "@/pages/repostpage/confirm-action-dialog.tsx";
import {DestinationSettingsDialog} from "@/pages/repostpage/destination-settings-dialog.tsx";
import {destinationTitle} from "@/pages/repostpage/repost-destination-utils.ts";
import {
    useDeleteApiV1RepostDestinationsId,
    usePostApiV1RepostDestinationsIdRefresh,
    usePutApiV1RepostDestinationsId,
} from "@/api/endpoints/repost/repost.ts";
import type {RepostDestinationDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {ChatStatus, ChatType} from "@/api/endpoints/tgPosterAPI.schemas.ts";

interface RepostDestinationRowProps {
    destination: RepostDestinationDto;
    onChanged: () => void;
}

function getChatStatusLabel(status: string | undefined): string {
    switch (status) {
        case ChatStatus.Active: return "Активен";
        case ChatStatus.Banned: return "Забанен";
        case ChatStatus.Left: return "Покинут";
        default: return "Неизвестно";
    }
}

function getChatStatusVariant(status: string | undefined): "default" | "destructive" | "secondary" | "outline" {
    switch (status) {
        case ChatStatus.Active: return "default";
        case ChatStatus.Banned: return "destructive";
        case ChatStatus.Left: return "secondary";
        default: return "outline";
    }
}

function getChatTypeLabel(type: string | undefined): string | null {
    switch (type) {
        case ChatType.Channel: return "Канал";
        case ChatType.Group: return "Группа";
        default: return null;
    }
}

function formatMemberCount(count: number): string {
    if (count >= 1_000_000) return `${(count / 1_000_000).toFixed(1)}M`;
    if (count >= 1_000) return `${(count / 1_000).toFixed(1)}K`;
    return count.toLocaleString();
}

function RepostActivity({destination}: {destination: RepostDestinationDto}) {
    const limit = destination.maxRepostsPerDay;
    const limitReached = limit != null && destination.repostsToday >= limit;
    const lastRepost = destination.lastRepostAt
        ? `репост ${formatDistanceToNow(new Date(destination.lastRepostAt), {addSuffix: true, locale: ru})}`
        : "репостов ещё не было";

    return (
        <>
            <span title={destination.lastRepostAt ? new Date(destination.lastRepostAt).toLocaleString("ru-RU") : undefined}>
                {lastRepost}
            </span>
            <span
                className={limitReached ? "text-amber-600 font-medium" : undefined}
                title="Считается с 00:00 UTC, так же как дневной лимит"
            >
                сегодня {destination.repostsToday}{limit != null && `/${limit}`}
            </span>
        </>
    );
}

export function RepostDestinationRow({destination, onChanged}: RepostDestinationRowProps) {
    const [isSettingsOpen, setIsSettingsOpen] = useState(false);
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);
    const typeLabel = getChatTypeLabel(destination.chatType);

    const {mutate: toggleDestination, isPending: isToggling} = usePutApiV1RepostDestinationsId({
        mutation: {
            onSuccess: () => {
                toast.success("Статус обновлен");
                onChanged();
            },
            onError: (error) => {
                toast.error("Ошибка обновления", {
                    description: error.title || "Не удалось обновить статус канала",
                });
            },
        },
    });

    const {mutate: deleteDestination, isPending: isDeleting} = useDeleteApiV1RepostDestinationsId({
        mutation: {
            onSuccess: () => {
                toast.success("Канал удален");
                setIsDeleteOpen(false);
                onChanged();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить канал",
                });
            },
        },
    });

    const {mutate: refreshDestination, isPending: isRefreshing} = usePostApiV1RepostDestinationsIdRefresh({
        mutation: {
            onSuccess: () => {
                toast.success("Информация обновлена");
                onChanged();
            },
            onError: (error) => {
                toast.error("Ошибка обновления", {
                    description: error.title || "Не удалось обновить информацию о канале",
                });
            },
        },
    });

    function handleToggle() {
        toggleDestination({
            id: destination.id,
            data: {
                isActive: !destination.isActive,
                delayMinSeconds: destination.delayMinSeconds ?? 0,
                delayMaxSeconds: destination.delayMaxSeconds ?? 0,
                repostEveryNth: destination.repostEveryNth ?? 1,
                skipProbability: destination.skipProbability ?? 0,
                maxRepostsPerDay: destination.maxRepostsPerDay,
            },
        });
    }

    return (
        <div className="flex items-center justify-between p-3 border rounded hover:bg-accent/50 transition-colors">
            <div className="flex items-center gap-3 flex-1 min-w-0">
                {destination.avatarBase64 ? (
                    <img
                        src={destination.avatarBase64}
                        alt=""
                        className="h-9 w-9 rounded-full object-cover shrink-0"
                    />
                ) : (
                    <div className="h-9 w-9 rounded-full bg-muted flex items-center justify-center text-xs font-medium shrink-0">
                        {destination.title?.[0] ?? "?"}
                    </div>
                )}

                <div className="flex flex-col min-w-0">
                    <div className="flex items-center gap-2">
                        <span className="text-sm font-medium truncate">
                            {destination.title ?? destination.chatId}
                        </span>
                        {destination.username && (
                            <a
                                href={`https://t.me/${destination.username}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="text-xs text-muted-foreground hover:underline shrink-0"
                            >
                                @{destination.username}
                            </a>
                        )}
                    </div>
                    <div className="flex flex-wrap items-center gap-x-2 text-xs text-muted-foreground">
                        {destination.memberCount != null && (
                            <span>{formatMemberCount(destination.memberCount)} подписчиков</span>
                        )}
                        {!destination.title && (
                            <span className="font-mono">{destination.chatId}</span>
                        )}
                        <RepostActivity destination={destination}/>
                    </div>
                </div>

                <div className="flex items-center gap-1.5 shrink-0">
                    {typeLabel && (
                        <Badge variant="outline" className="text-xs">
                            {typeLabel}
                        </Badge>
                    )}
                    <Badge variant={getChatStatusVariant(destination.chatStatus)} className="text-xs">
                        {getChatStatusLabel(destination.chatStatus)}
                    </Badge>
                    <Badge variant={destination.isActive ? "default" : "secondary"} className="text-xs">
                        {destination.isActive ? "Вкл" : "Выкл"}
                    </Badge>
                </div>
            </div>

            <div className="flex items-center gap-1 shrink-0 ml-2">
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-muted-foreground hover:text-foreground"
                    onClick={() => setIsSettingsOpen(true)}
                    title="Настроить репост"
                >
                    <Settings className="h-4 w-4"/>
                </Button>
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-muted-foreground hover:text-foreground"
                    onClick={() => refreshDestination({id: destination.id})}
                    disabled={isRefreshing}
                    title="Обновить информацию"
                >
                    <RefreshCw className={`h-4 w-4 ${isRefreshing ? "animate-spin" : ""}`}/>
                </Button>
                {isToggling ? (
                    <Loader2 className="h-4 w-4 mx-2.5 animate-spin text-muted-foreground"/>
                ) : (
                    <Switch
                        checked={destination.isActive}
                        onCheckedChange={handleToggle}
                        disabled={isDeleting}
                    />
                )}
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-muted-foreground hover:text-destructive"
                    onClick={() => setIsDeleteOpen(true)}
                    disabled={isDeleting}
                    title="Удалить канал"
                >
                    <Trash2 className="h-4 w-4"/>
                </Button>
            </div>

            <ConfirmActionDialog
                open={isDeleteOpen}
                onOpenChange={setIsDeleteOpen}
                title="Удалить канал из репоста?"
                description={<>Канал <strong>{destinationTitle(destination)}</strong> больше не будет получать репосты.</>}
                confirmLabel="Удалить"
                isPending={isDeleting}
                onConfirm={() => deleteDestination({id: destination.id})}
            />

            {isSettingsOpen && (
                <DestinationSettingsDialog
                    destination={destination}
                    open={true}
                    onOpenChange={setIsSettingsOpen}
                    onSuccess={onChanged}
                />
            )}
        </div>
    );
}
