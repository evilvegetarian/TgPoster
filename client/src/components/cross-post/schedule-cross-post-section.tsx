import {useState} from "react";
import {Link} from "react-router-dom";
import {useQueryClient} from "@tanstack/react-query";
import {toast} from "sonner";
import {AlertCircle, Loader2, Plus, Settings, Share2, Trash2} from "lucide-react";
import {
    getGetApiV1ScheduleScheduleIdCrossPostTargetsQueryKey,
    useDeleteApiV1ScheduleScheduleIdCrossPostTargetsId,
    useGetApiV1ScheduleScheduleIdCrossPostTargets,
    usePutApiV1ScheduleScheduleIdCrossPostTargetsId,
} from "@/api/endpoints/cross-post-target/cross-post-target";
import {useGetApiV1SocialAccounts} from "@/api/endpoints/social-account/social-account";
import type {CrossPostTargetResponse} from "@/api/endpoints/tgPosterAPI.schemas";
import {ACCOUNT_STATUS_LABELS, PLATFORM_LABELS} from "@/components/social-account/platform-meta.ts";
import {FORMAT_OPTIONS} from "@/components/cross-post/cross-post-meta.ts";
import {CrossPostTargetDialog} from "@/components/cross-post/cross-post-target-dialog.tsx";
import {Card, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Badge} from "@/components/ui/badge.tsx";
import {Switch} from "@/components/ui/switch.tsx";
import {Skeleton} from "@/components/ui/skeleton.tsx";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";

function formatTitle(format: CrossPostTargetResponse["format"]): string {
    return FORMAT_OPTIONS.find((option) => option.value === format)?.title ?? format;
}

export function ScheduleCrossPostSection({scheduleId}: {scheduleId: string}) {
    const queryClient = useQueryClient();
    const {data: targetsData, isLoading} = useGetApiV1ScheduleScheduleIdCrossPostTargets(scheduleId);
    const {data: accountsData} = useGetApiV1SocialAccounts();

    const targets = targetsData ?? [];
    const accounts = accountsData ?? [];

    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [editingTarget, setEditingTarget] = useState<CrossPostTargetResponse | null>(null);
    const [targetToDelete, setTargetToDelete] = useState<CrossPostTargetResponse | null>(null);

    const invalidateTargets = () => {
        queryClient.invalidateQueries({queryKey: getGetApiV1ScheduleScheduleIdCrossPostTargetsQueryKey(scheduleId)});
    };

    const {mutate: updateTarget, isPending: isUpdating} = usePutApiV1ScheduleScheduleIdCrossPostTargetsId({
        mutation: {
            onSuccess: () => {
                invalidateTargets();
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title || "Не удалось обновить связку"});
            },
        },
    });

    const {mutate: deleteTarget, isPending: isDeleting} = useDeleteApiV1ScheduleScheduleIdCrossPostTargetsId({
        mutation: {
            onSuccess: () => {
                toast.success("Связка удалена");
                setTargetToDelete(null);
                invalidateTargets();
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title || "Не удалось удалить связку"});
            },
        },
    });

    const availableAccounts = accounts.filter((account) => !targets.some((target) => target.socialAccountId === account.id));

    const handleToggleActive = (target: CrossPostTargetResponse, isActive: boolean) => {
        updateTarget({
            scheduleId,
            id: target.id,
            data: {
                isActive,
                format: target.format,
                linkTarget: target.linkTarget,
                customLink: target.customLink ?? null,
                callToAction: target.callToAction ?? null,
                includeMedia: target.includeMedia,
                includeParsed: target.includeParsed,
                delayMinutes: target.delayMinutes,
            },
        });
    };

    const openCreateDialog = () => {
        setEditingTarget(null);
        setIsDialogOpen(true);
    };

    const openEditDialog = (target: CrossPostTargetResponse) => {
        setEditingTarget(target);
        setIsDialogOpen(true);
    };

    return (
        <Card>
            <CardHeader>
                <CardTitle className="flex items-center gap-2">
                    <Share2 className="h-5 w-5"/>
                    Кросс-постинг
                </CardTitle>
                <CardDescription>
                    Посты этого канала после публикации в Telegram уйдут в выбранные соцсети
                </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
                {isLoading ? (
                    <div className="space-y-2">
                        {[...Array(2)].map((_, index) => (
                            <Skeleton key={index} className="h-16 w-full"/>
                        ))}
                    </div>
                ) : targets.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                        Пока ни одна соцсеть не подключена к этому расписанию
                    </p>
                ) : (
                    <div className="space-y-2">
                        {targets.map((target) => (
                            <div key={target.id} className="flex items-center gap-3 rounded-lg border p-3">
                                <div className="min-w-0 flex-1 space-y-1">
                                    <div className="flex flex-wrap items-center gap-2">
                                        <span className="font-medium">{PLATFORM_LABELS[target.platform]}</span>
                                        <span className="truncate text-sm text-muted-foreground">
                                            {target.accountName}
                                        </span>
                                        <Badge variant="secondary" className="text-xs">
                                            {formatTitle(target.format)}
                                        </Badge>
                                        {target.accountStatus === "NeedsReauth" && (
                                            <Badge variant="destructive" className="text-xs">
                                                <AlertCircle className="mr-1 h-3 w-3"/>
                                                {ACCOUNT_STATUS_LABELS[target.accountStatus]}
                                            </Badge>
                                        )}
                                    </div>
                                </div>
                                <Switch
                                    checked={target.isActive}
                                    onCheckedChange={(checked) => handleToggleActive(target, checked)}
                                    disabled={isUpdating}
                                />
                                <Button
                                    type="button"
                                    variant="outline"
                                    size="sm"
                                    onClick={() => openEditDialog(target)}
                                    disabled={isUpdating}
                                >
                                    <Settings className="h-4 w-4"/>
                                </Button>
                                <Button
                                    type="button"
                                    variant="outline"
                                    size="sm"
                                    onClick={() => setTargetToDelete(target)}
                                    disabled={isDeleting}
                                >
                                    <Trash2 className="h-4 w-4"/>
                                </Button>
                            </div>
                        ))}
                    </div>
                )}

                {availableAccounts.length > 0 ? (
                    <Button type="button" variant="outline" onClick={openCreateDialog}>
                        <Plus className="mr-2 h-4 w-4"/>
                        Добавить соцсеть
                    </Button>
                ) : (
                    <p className="text-sm text-muted-foreground">
                        Сначала подключите аккаунт в разделе{" "}
                        <Link to="/social-accounts" className="text-primary hover:underline">
                            Соцсети
                        </Link>
                    </p>
                )}
            </CardContent>

            <CrossPostTargetDialog
                scheduleId={scheduleId}
                target={editingTarget ?? undefined}
                accounts={availableAccounts}
                open={isDialogOpen}
                onOpenChange={(open) => {
                    setIsDialogOpen(open);
                    if (!open) {
                        setEditingTarget(null);
                    }
                }}
            />

            <AlertDialog
                open={!!targetToDelete}
                onOpenChange={() => !isDeleting && setTargetToDelete(null)}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Удалить связку?</AlertDialogTitle>
                        <AlertDialogDescription>
                            Посты этого канала перестанут публиковаться в{" "}
                            {targetToDelete ? `${PLATFORM_LABELS[targetToDelete.platform]} — ${targetToDelete.accountName}` : "соцсеть"}.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel onClick={() => setTargetToDelete(null)} disabled={isDeleting}>
                            Отмена
                        </AlertDialogCancel>
                        <AlertDialogAction
                            onClick={() => targetToDelete && deleteTarget({scheduleId, id: targetToDelete.id})}
                            disabled={isDeleting}
                            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                        >
                            {isDeleting ? (
                                <><Loader2 className="mr-2 h-4 w-4 animate-spin"/>Удаление...</>
                            ) : (
                                <><Trash2 className="mr-2 h-4 w-4"/>Удалить</>
                            )}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </Card>
    );
}
