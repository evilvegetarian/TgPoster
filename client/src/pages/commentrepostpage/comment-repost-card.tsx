import {Badge} from "@/components/ui/badge";
import {Button} from "@/components/ui/button";
import {Card, CardContent, CardHeader, CardTitle, CardDescription} from "@/components/ui/card";
import {Switch} from "@/components/ui/switch";
import {Loader2, X} from "lucide-react";
import {toast} from "sonner";
import {
    useGetApiV1CommentRepostId,
    useDeleteApiV1CommentRepostId,
    usePutApiV1CommentRepostId,
    getGetApiV1CommentRepostQueryKey,
} from "@/api/endpoints/comment-repost/comment-repost.ts";
import type {CommentRepostItemDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {useQueryClient} from "@tanstack/react-query";

interface CommentRepostCardProps {
    settings: CommentRepostItemDto;
}

export function CommentRepostCard({settings}: CommentRepostCardProps) {
    const queryClient = useQueryClient();

    const {data: details, isLoading} = useGetApiV1CommentRepostId(settings.id);

    const {mutate: deleteSettings} = useDeleteApiV1CommentRepostId({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки удалены", {
                    description: "Настройки комментирующего репоста успешно удалены",
                });
                onRefresh();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить настройки",
                });
            },
        },
    });

    const {mutate: toggleActive, isPending: isToggling} = usePutApiV1CommentRepostId({
        mutation: {
            onSuccess: () => {
                toast.success("Статус обновлен");
                onRefresh();
            },
            onError: (error) => {
                toast.error("Ошибка обновления", {
                    description: error.title || "Не удалось обновить статус",
                });
            },
        },
    });

    function onRefresh() {
        void queryClient.invalidateQueries({queryKey: getGetApiV1CommentRepostQueryKey()});
    }

    function handleToggleActive() {
        toggleActive({
            id: settings.id,
            data: {isActive: !settings.isActive},
        });
    }

    function handleDelete() {
        deleteSettings({id: settings.id});
    }

    function formatDate(date?: string | null) {
        if (!date) return "—";
        return new Date(date).toLocaleString("ru-RU");
    }

    return (
        <Card className="relative hover:shadow-md transition-shadow">
            <Button
                variant="ghost"
                size="icon"
                className="absolute top-2 right-2 h-7 w-7 text-muted-foreground hover:text-destructive"
                onClick={handleDelete}
            >
                <X className="h-4 w-4"/>
            </Button>

            <CardHeader>
                <div className="flex items-center justify-between pr-8">
                    <div className="flex items-center gap-3">
                        <CardTitle className="text-xl">{settings.watchedChannel}</CardTitle>
                        <Badge variant={settings.isActive ? "default" : "secondary"}>
                            {settings.isActive ? "Активен" : "Неактивен"}
                        </Badge>
                    </div>
                    <Switch
                        checked={settings.isActive}
                        onCheckedChange={handleToggleActive}
                        disabled={isToggling}
                    />
                </div>
                <CardDescription>
                    Расписание: {settings.scheduleName}
                </CardDescription>
            </CardHeader>

            <CardContent>
                {isLoading ? (
                    <div className="flex items-center justify-center py-4">
                        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground"/>
                        <span className="ml-2 text-sm text-muted-foreground">
                            Загрузка деталей...
                        </span>
                    </div>
                ) : details ? (
                    <div className="grid grid-cols-2 gap-3 text-sm">
                        <div>
                            <span className="text-muted-foreground">Сессия:</span>{" "}
                            <span className="font-medium">{details.telegramSessionName || "—"}</span>
                        </div>
                        <div>
                            <span className="text-muted-foreground">Последняя проверка:</span>{" "}
                            <span className="font-medium">{formatDate(details.lastCheckDate)}</span>
                        </div>
                        <div>
                            <span className="text-muted-foreground">Последний пост:</span>{" "}
                            <span className="font-medium">{details.lastProcessedPostId ?? "—"}</span>
                        </div>
                        <div>
                            <span className="text-muted-foreground">Создано:</span>{" "}
                            <span className="font-medium">{formatDate(details.created)}</span>
                        </div>
                    </div>
                ) : null}
            </CardContent>
        </Card>
    );
}
