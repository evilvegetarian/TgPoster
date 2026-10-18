import {useState} from "react";
import {Link} from "react-router-dom";
import {Badge} from "@/components/ui/badge";
import {Button} from "@/components/ui/button";
import {Card, CardContent, CardHeader, CardTitle, CardDescription} from "@/components/ui/card";
import {Switch} from "@/components/ui/switch";
import {Loader2, Plus, ScrollText, Settings, X} from "lucide-react";
import {toast} from "sonner";
import {AddDestinationDialog} from "@/pages/repostpage/add-destination-dialog.tsx";
import {ConfirmActionDialog} from "@/pages/repostpage/confirm-action-dialog.tsx";
import {RepostDefaultsDialog} from "@/pages/repostpage/repost-defaults-dialog.tsx";
import {RepostDestinationsList} from "@/pages/repostpage/repost-destinations-list.tsx";
import {
    useGetApiV1RepostSettingsId,
    useDeleteApiV1RepostSettingsId,
    usePutApiV1RepostSettingsId,
    getGetApiV1RepostSettingsQueryKey,
} from "@/api/endpoints/repost/repost.ts";
import type {RepostSettingsItemDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {useQueryClient} from "@tanstack/react-query";

interface RepostSettingsCardProps {
    settings: RepostSettingsItemDto;
}

export function RepostSettingsCard({settings}: RepostSettingsCardProps) {
    const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
    const [isDefaultsDialogOpen, setIsDefaultsDialogOpen] = useState(false);
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);
    const queryClient = useQueryClient();

    const {data: detailedSettings, isLoading, refetch: refetchDetails} = useGetApiV1RepostSettingsId(
        settings.id
    );

    const {mutate: deleteSettings, isPending: isDeletingSettings} = useDeleteApiV1RepostSettingsId({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки удалены", {
                    description: "Настройки репоста успешно удалены",
                });
                setIsDeleteOpen(false);
                refreshList();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить настройки репоста",
                });
            },
        },
    });

    const {mutate: updateSettings, isPending: isUpdatingSettings} = usePutApiV1RepostSettingsId({
        mutation: {
            onSuccess: (_, variables) => {
                toast.success(variables.data.isActive ? "Репост включён" : "Репост выключен");
                handleChanged();
            },
            onError: (error) => {
                toast.error("Ошибка обновления", {
                    description: error.title || "Не удалось изменить статус репоста",
                });
            },
        },
    });

    function refreshList() {
        void queryClient.invalidateQueries({queryKey: getGetApiV1RepostSettingsQueryKey()});
    }

    function handleChanged() {
        void refetchDetails();
        refreshList();
    }

    function handleToggleSettings(isActive: boolean) {
        if (!detailedSettings) return;

        updateSettings({
            id: settings.id,
            data: {
                isActive,
                defaultDelayMinSeconds: detailedSettings.defaultDelayMinSeconds,
                defaultDelayMaxSeconds: detailedSettings.defaultDelayMaxSeconds,
                defaultRepostEveryNth: detailedSettings.defaultRepostEveryNth,
                defaultSkipProbability: detailedSettings.defaultSkipProbability,
                defaultMaxRepostsPerDay: detailedSettings.defaultMaxRepostsPerDay,
            },
        });
    }

    const destinations = detailedSettings?.destinations ?? [];

    return (
        <Card className="relative hover:shadow-md transition-shadow">
            <Button
                variant="ghost"
                size="icon"
                className="absolute top-2 right-2 h-7 w-7 text-muted-foreground hover:text-destructive"
                onClick={() => setIsDeleteOpen(true)}
                title="Удалить настройки репоста"
            >
                <X className="h-4 w-4"/>
            </Button>

            <CardHeader>
                <div className="flex items-center justify-between gap-4 pr-8">
                    <CardTitle className="text-xl">{settings.scheduleName}</CardTitle>
                    <label className="flex items-center gap-2 text-sm text-muted-foreground shrink-0">
                        {settings.isActive ? "Репост включён" : "Репост выключен"}
                        {isUpdatingSettings ? (
                            <Loader2 className="h-4 w-4 mx-2.5 animate-spin"/>
                        ) : (
                            <Switch
                                checked={settings.isActive}
                                onCheckedChange={handleToggleSettings}
                                disabled={!detailedSettings}
                            />
                        )}
                    </label>
                </div>
                <CardDescription>
                    Сессия: {detailedSettings?.telegramSessionName || "Загрузка..."}
                </CardDescription>
            </CardHeader>

            <CardContent className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="flex flex-wrap items-center gap-2">
                        <h4 className="text-sm font-semibold">Целевые каналы</h4>
                        <span className="text-sm text-muted-foreground">
                            включено{" "}
                            <span className="font-semibold text-foreground">{settings.activeDestinationsCount}</span>
                            {" "}из {settings.destinationsCount}
                        </span>
                        {settings.problemDestinationsCount > 0 && (
                            <Badge
                                variant="destructive"
                                className="text-xs"
                                title="Аккаунт забанен в чате или вышел из него"
                            >
                                проблемных: {settings.problemDestinationsCount}
                            </Badge>
                        )}
                    </div>
                    <div className="flex items-center gap-2">
                        <Link to={`/repost-logs?settingsId=${settings.id}`}>
                            <Button variant="outline" size="sm" className="gap-1">
                                <ScrollText className="h-3 w-3"/>
                                Логи
                            </Button>
                        </Link>
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setIsDefaultsDialogOpen(true)}
                            className="gap-1"
                            disabled={!detailedSettings}
                        >
                            <Settings className="h-3 w-3"/>
                            Общие настройки
                        </Button>
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
                    <RepostDestinationsList
                        settingsId={settings.id}
                        destinations={destinations}
                        onChanged={handleChanged}
                    />
                )}
            </CardContent>

            <AddDestinationDialog
                settingsId={settings.id}
                open={isAddDialogOpen}
                onOpenChange={setIsAddDialogOpen}
                onSuccess={handleChanged}
            />

            {detailedSettings && (
                <RepostDefaultsDialog
                    settings={detailedSettings}
                    open={isDefaultsDialogOpen}
                    onOpenChange={setIsDefaultsDialogOpen}
                    onSuccess={handleChanged}
                />
            )}

            <ConfirmActionDialog
                open={isDeleteOpen}
                onOpenChange={setIsDeleteOpen}
                title="Удалить настройки репоста?"
                description={
                    <>
                        Настройки для <strong>{settings.scheduleName}</strong> удалятся вместе со всеми
                        целевыми каналами ({settings.destinationsCount}). Это действие нельзя отменить.
                    </>
                }
                confirmLabel="Удалить"
                isPending={isDeletingSettings}
                onConfirm={() => deleteSettings({id: settings.id})}
            />
        </Card>
    );
}
