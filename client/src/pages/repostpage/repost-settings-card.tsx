import {useState} from "react";
import {Badge} from "@/components/ui/badge";
import {Button} from "@/components/ui/button";
import {Card, CardContent, CardHeader, CardTitle, CardDescription} from "@/components/ui/card";
import {Switch} from "@/components/ui/switch";
import {Loader2, Plus, RefreshCw, Trash2, X} from "lucide-react";
import {toast} from "sonner";
import {AddDestinationDialog} from "@/pages/repostpage/add-destination-dialog.tsx";
import {
    useGetApiV1RepostSettingsId,
    useDeleteApiV1RepostDestinationsId,
    usePutApiV1RepostDestinationsId,
    useDeleteApiV1RepostSettingsId,
    getGetApiV1RepostSettingsQueryKey,
    usePostApiV1RepostDestinationsIdRefresh,
} from "@/api/endpoints/repost/repost.ts";
import type {RepostSettingsItemDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {ChatStatus, ChatType} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {useQueryClient} from "@tanstack/react-query";

interface RepostSettingsCardProps {
    settings: RepostSettingsItemDto;
}

function getChatStatusLabel(status: number): string {
    switch (status) {
        case ChatStatus.Active: return "Активен";
        case ChatStatus.Banned: return "Забанен";
        case ChatStatus.Left: return "Покинут";
        default: return "Неизвестно";
    }
}

function getChatStatusVariant(status: number): "default" | "destructive" | "secondary" | "outline" {
    switch (status) {
        case ChatStatus.Active: return "default";
        case ChatStatus.Banned: return "destructive";
        case ChatStatus.Left: return "secondary";
        default: return "outline";
    }
}

function getChatTypeLabel(type: number): string | null {
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

export function RepostSettingsCard({settings}: RepostSettingsCardProps) {
    const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
    const queryClient = useQueryClient();
    const {mutate: deleteSettings} = useDeleteApiV1RepostSettingsId({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки удалены", {
                    description: "Настройки репоста успешно удалены",
                });
                onRefresh();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить настройки репоста",
                });
            },
        },
    });
    const {data: detailedSettings, isLoading, refetch: refetchDetails} = useGetApiV1RepostSettingsId(
        settings.id
    );

    const {mutate: toggleDestination, isPending: isToggling} = usePutApiV1RepostDestinationsId({
        mutation: {
            onSuccess: () => {
                toast.success("Статус обновлен");
                void refetchDetails();
                onRefresh();
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
                void refetchDetails();
                onRefresh();
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
                void refetchDetails();
            },
            onError: (error) => {
                toast.error("Ошибка обновления", {
                    description: error.title || "Не удалось обновить информацию о канале",
                });
            },
        },
    });

    function handleToggleDestination(destinationId: string, currentStatus: boolean) {
        toggleDestination({
            id: destinationId,
            data: {isActive: !currentStatus},
        });
    }

    function onRefresh() {
        void queryClient.invalidateQueries({queryKey: getGetApiV1RepostSettingsQueryKey()});
    }

    function handleDeleteDestination(destinationId: string) {
        deleteDestination({id: destinationId});
    }

    function handleDeleteSettings(settingsId: string) {
        deleteSettings({id: settingsId});
    }

    function handleRefreshDestination(destinationId: string) {
        refreshDestination({id: destinationId});
    }

    function handleDestinationAdded() {
        void refetchDetails();
    }

    const destinations = detailedSettings?.destinations ?? [];

    return (
        <Card className="relative hover:shadow-md transition-shadow">
            <Button
                variant="ghost"
                size="icon"
                className="absolute top-2 right-2 h-7 w-7 text-muted-foreground hover:text-destructive"
                onClick={() => handleDeleteSettings(settings.id)}
            >
                <X className="h-4 w-4"/>
            </Button>

            <CardHeader>
                <div className="flex items-center justify-between pr-8">
                    <div className="flex items-center gap-3">
                        <CardTitle className="text-xl">{settings.scheduleName}</CardTitle>
                        <Badge variant={settings.isActive ? "default" : "secondary"}>
                            {settings.isActive ? "Активен" : "Неактивен"}
                        </Badge>
                    </div>
                </div>
                <CardDescription>
                    Сессия: {detailedSettings?.telegramSessionName || "Загрузка..."}
                </CardDescription>
            </CardHeader>

            <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                    <h4 className="text-sm font-semibold">
                        Целевые каналы ({settings.destinationsCount})
                    </h4>
                    <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setIsAddDialogOpen(true)}
                        className="gap-1"
                    >
                        <Plus className="h-3 w-3"/>
                        Добавить канал
                    </Button>
                </div>

                {isLoading ? (
                    <div className="flex items-center justify-center py-4">
                        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground"/>
                        <span className="ml-2 text-sm text-muted-foreground">
                            Загрузка каналов...
                        </span>
                    </div>
                ) : destinations.length === 0 ? (
                    <p className="text-sm text-muted-foreground p-4 border rounded text-center">
                        Целевые каналы не добавлены.{" "}
                        <Button
                            variant="link"
                            className="p-0 h-auto"
                            onClick={() => setIsAddDialogOpen(true)}
                        >
                            Добавить первый канал
                        </Button>
                    </p>
                ) : (
                    <div className="space-y-2">
                        {destinations.map(dest => {
                            const typeLabel = getChatTypeLabel(dest.chatType);
                            return (
                                <div
                                    key={dest.id}
                                    className="flex items-center justify-between p-3 border rounded hover:bg-accent/50 transition-colors"
                                >
                                    <div className="flex items-center gap-3 flex-1 min-w-0">
                                        {dest.avatarBase64 ? (
                                            <img
                                                src={dest.avatarBase64}
                                                alt=""
                                                className="h-9 w-9 rounded-full object-cover shrink-0"
                                            />
                                        ) : (
                                            <div className="h-9 w-9 rounded-full bg-muted flex items-center justify-center text-xs font-medium shrink-0">
                                                {dest.title?.[0] ?? "?"}
                                            </div>
                                        )}

                                        <div className="flex flex-col min-w-0">
                                            <div className="flex items-center gap-2">
                                                <span className="text-sm font-medium truncate">
                                                    {dest.title ?? dest.chatId}
                                                </span>
                                                {dest.username && (
                                                    <a
                                                        href={`https://t.me/${dest.username}`}
                                                        target="_blank"
                                                        rel="noopener noreferrer"
                                                        className="text-xs text-muted-foreground hover:underline shrink-0"
                                                    >
                                                        @{dest.username}
                                                    </a>
                                                )}
                                            </div>
                                            <div className="flex items-center gap-2 text-xs text-muted-foreground">
                                                {dest.memberCount != null && (
                                                    <span>{formatMemberCount(dest.memberCount)} подписчиков</span>
                                                )}
                                                {!dest.title && (
                                                    <span className="font-mono">{dest.chatId}</span>
                                                )}
                                            </div>
                                        </div>

                                        <div className="flex items-center gap-1.5 shrink-0">
                                            {typeLabel && (
                                                <Badge variant="outline" className="text-xs">
                                                    {typeLabel}
                                                </Badge>
                                            )}
                                            <Badge variant={getChatStatusVariant(dest.chatStatus)} className="text-xs">
                                                {getChatStatusLabel(dest.chatStatus)}
                                            </Badge>
                                            <Badge variant={dest.isActive ? "default" : "secondary"} className="text-xs">
                                                {dest.isActive ? "Вкл" : "Выкл"}
                                            </Badge>
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-1 shrink-0 ml-2">
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className="h-8 w-8 text-muted-foreground hover:text-foreground"
                                            onClick={() => handleRefreshDestination(dest.id)}
                                            disabled={isRefreshing}
                                            title="Обновить информацию"
                                        >
                                            <RefreshCw className={`h-4 w-4 ${isRefreshing ? "animate-spin" : ""}`}/>
                                        </Button>
                                        <Switch
                                            checked={dest.isActive}
                                            onCheckedChange={() =>
                                                handleToggleDestination(dest.id, dest.isActive)
                                            }
                                            disabled={isToggling || isDeleting}
                                        />
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className="h-8 w-8 text-muted-foreground hover:text-destructive"
                                            onClick={() => handleDeleteDestination(dest.id)}
                                            disabled={isDeleting}
                                        >
                                            <Trash2 className="h-4 w-4"/>
                                        </Button>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                )}
            </CardContent>

            <AddDestinationDialog
                settingsId={settings.id}
                open={isAddDialogOpen}
                onOpenChange={setIsAddDialogOpen}
                onSuccess={handleDestinationAdded}
            />
        </Card>
    );
}
