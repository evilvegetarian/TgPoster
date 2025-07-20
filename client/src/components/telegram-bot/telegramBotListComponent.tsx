import {
    getGetApiV1TelegramBotQueryKey,
    useDeleteApiV1TelegramBotId,
    useGetApiV1TelegramBot
} from "@/api/endpoints/telegram-bot/telegram-bot.ts";
import type {TelegramBotResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {Card, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {Badge} from "@/components/ui/badge.tsx";
import {Skeleton} from "@/components/ui/skeleton.tsx";
import {AlertCircle, Bot, Loader2, MoreVertical, Trash2, Users} from "lucide-react";
import {Button} from "@/components/ui/button.tsx";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu.tsx";
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
import {useEffect, useState} from "react";
import {toast} from "sonner";
import {useQueryClient} from "@tanstack/react-query";

export function TelegramBotListComponent() {
    const {data, error, isLoading, refetch} = useGetApiV1TelegramBot();
    const [botToDelete, setBotToDelete] = useState<TelegramBotResponse | null>(null);
    const queryClient = useQueryClient();

    const {mutate: deleteBot, isPending: isDeleting} = useDeleteApiV1TelegramBotId({
        mutation: {
            onSuccess: () => {
                toast.success("Telegram бот успешно удален!");
                setBotToDelete(null);
                queryClient.invalidateQueries({queryKey: getGetApiV1TelegramBotQueryKey()});
            },
            onError: (error) => {
                toast.error("Ошибка при удалении бота");
                console.error("Delete error:", error);
            }
        }
    });

    useEffect(() => {
    }, [refetch]);

    const handleDeleteClick = (bot: TelegramBotResponse) => {
        setBotToDelete(bot);
    };

    const handleDeleteConfirm = () => {
        if (botToDelete) {
            deleteBot({
                id: botToDelete.id
            });
        }
    };

    const handleDeleteCancel = () => {
        setBotToDelete(null);
    };

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            <Users className="h-5 w-5 text-green-500"/>
                            <CardTitle>Мои Telegram боты</CardTitle>
                        </div>
                        <Skeleton className="h-6 w-16"/>
                    </div>
                    <CardDescription>Загрузка списка ботов...</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {[...Array(3)].map((_, i) => (
                        <div key={i} className="flex items-center space-x-4 p-4 border rounded-lg">
                            <Skeleton className="h-10 w-10 rounded-full"/>
                            <div className="space-y-2 flex-1">
                                <Skeleton className="h-4 w-[200px]"/>
                                <Skeleton className="h-3 w-[100px]"/>
                            </div>
                            <Skeleton className="h-8 w-8"/>
                        </div>
                    ))}
                </CardContent>
            </Card>
        );
    }

    if (error) {
        return (
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-2">
                        <Users className="h-5 w-5 text-green-500"/>
                        <CardTitle>Мои Telegram боты</CardTitle>
                    </div>
                </CardHeader>
                <CardContent>
                    <div className="flex items-center gap-2 text-destructive bg-destructive/10 p-4 rounded-lg">
                        <AlertCircle className="h-5 w-5"/>
                        <span>Произошла ошибка при загрузке ботов</span>
                    </div>
                </CardContent>
            </Card>
        );
    }

    return (
        <>
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            <Users className="h-5 w-5 text-green-500"/>
                            <CardTitle>Мои Telegram боты</CardTitle>
                        </div>
                        <Badge variant="secondary">
                            {data?.length || 0} {data?.length === 1 ? 'бот' : 'ботов'}
                        </Badge>
                    </div>
                    <CardDescription>
                        Список всех подключенных Telegram ботов
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {data && data.length > 0 ? (
                        <div className="space-y-3">
                            {data.map((bot: TelegramBotResponse) => (
                                <div
                                    key={bot.id}
                                    className="flex items-center gap-4 p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                                >
                                    <div className="flex-shrink-0">
                                        <div
                                            className="h-10 w-10 bg-blue-100 rounded-full flex items-center justify-center">
                                            <Bot className="h-5 w-5 text-blue-600"/>
                                        </div>
                                    </div>
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-2">
                                            <h3 className="font-medium text-foreground truncate">
                                                {bot.name}
                                            </h3>
                                            <Badge variant="outline" className="text-xs">
                                                Активен
                                            </Badge>
                                        </div>
                                        <p className="text-sm text-muted-foreground">
                                            ID: {bot.id}
                                        </p>
                                    </div>
                                    <DropdownMenu>
                                        <DropdownMenuTrigger asChild>
                                            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                                                <MoreVertical className="h-4 w-4"/>
                                            </Button>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent align="end">
                                            <DropdownMenuItem
                                                className="text-destructive focus:text-destructive"
                                                onClick={() => handleDeleteClick(bot)}
                                                disabled={isDeleting}
                                            >
                                                <Trash2 className="mr-2 h-4 w-4"/>
                                                Удалить
                                            </DropdownMenuItem>
                                        </DropdownMenuContent>
                                    </DropdownMenu>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="text-center py-8">
                            <Bot className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                            <h3 className="text-lg font-medium text-foreground mb-2">
                                Боты не найдены
                            </h3>
                            <p className="text-muted-foreground mb-4">
                                Добавьте первого бота, чтобы начать работу
                            </p>
                        </div>
                    )}
                </CardContent>
            </Card>

            <AlertDialog open={!!botToDelete} onOpenChange={() => !isDeleting && setBotToDelete(null)}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Удалить Telegram бота?</AlertDialogTitle>
                        <AlertDialogDescription>
                            Вы уверены, что хотите удалить бота <strong>"{botToDelete?.name}"</strong>?
                            Это действие нельзя отменить.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel
                            onClick={handleDeleteCancel}
                            disabled={isDeleting}
                        >
                            Отмена
                        </AlertDialogCancel>
                        <AlertDialogAction
                            onClick={handleDeleteConfirm}
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
        </>
    );
}
