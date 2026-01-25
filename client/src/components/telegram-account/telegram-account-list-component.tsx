import { useState } from "react";
import { AlertCircle, Smartphone, Trash2, MoreVertical } from "lucide-react";
import { toast } from "sonner";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
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
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import {
    useGetApiV1TelegramSession,
    useDeleteApiV1TelegramSessionId,
    getGetApiV1TelegramSessionQueryKey,
} from "@/api/endpoints/telegram-session/telegram-session";
import { useQueryClient } from "@tanstack/react-query";
import type { TelegramSessionResponse } from "@/api/endpoints/tgPosterAPI.schemas";
import { TelegramSessionStatus } from "@/api/endpoints/tgPosterAPI.schemas";
import { TelegramAccountAuthDialog } from "./telegram-account-auth-dialog";

function getStatusConfig(status?: string) {
    switch (status) {
        case TelegramSessionStatus.Authorized:
            return { label: "Авторизован", variant: "default" as const, color: "text-green-600" };
        case TelegramSessionStatus.AwaitingCode:
            return { label: "Ожидает код", variant: "secondary" as const, color: "text-yellow-600" };
        case TelegramSessionStatus.CodeSent:
            return { label: "Код отправлен", variant: "secondary" as const, color: "text-blue-600" };
        case TelegramSessionStatus.AwaitingPassword:
            return { label: "Ожидает пароль", variant: "secondary" as const, color: "text-orange-600" };
        case TelegramSessionStatus.Failed:
            return { label: "Ошибка", variant: "destructive" as const, color: "text-red-600" };
        default:
            return { label: "Неизвестно", variant: "outline" as const, color: "text-gray-600" };
    }
}

export function TelegramAccountListComponent() {
    const { data: sessionsData, error, isLoading } = useGetApiV1TelegramSession();
    const queryClient = useQueryClient();
    const [accountToDelete, setAccountToDelete] = useState<TelegramSessionResponse | null>(null);

    const { mutate: deleteAccount, isPending: isDeleting } = useDeleteApiV1TelegramSessionId({
        mutation: {
            onSuccess: () => {
                toast.success("Telegram аккаунт успешно удален!");
                queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
                setAccountToDelete(null);
            },
            onError: (error) => {
                toast.error("Ошибка при удалении аккаунта", {
                    description: error.title || "Не удалось удалить аккаунт",
                });
            },
        },
    });

    function handleDeleteConfirm() {
        if (accountToDelete?.id) {
            deleteAccount({ id: accountToDelete.id });
        }
    }

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Telegram аккаунты</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="space-y-3">
                        {[1, 2, 3].map((i) => (
                            <div key={i} className="flex items-center gap-3 p-3">
                                <Skeleton className="h-10 w-10 rounded-full" />
                                <div className="flex-1 space-y-2">
                                    <Skeleton className="h-4 w-32" />
                                    <Skeleton className="h-3 w-24" />
                                </div>
                                <Skeleton className="h-8 w-20" />
                            </div>
                        ))}
                    </div>
                </CardContent>
            </Card>
        );
    }

    if (error) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Telegram аккаунты</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="flex items-center gap-2 text-red-600">
                        <AlertCircle className="h-5 w-5" />
                        <span>Ошибка загрузки аккаунтов</span>
                    </div>
                </CardContent>
            </Card>
        );
    }

    const accounts = sessionsData?.items || [];

    return (
        <>
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        Telegram аккаунты
                        {accounts.length > 0 && (
                            <Badge variant="secondary">{accounts.length}</Badge>
                        )}
                    </CardTitle>
                    <CardDescription>
                        Управляйте Telegram аккаунтами для постинга
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {accounts.length === 0 ? (
                        <div className="text-center py-8 text-muted-foreground">
                            <Smartphone className="h-12 w-12 mx-auto mb-2 opacity-50" />
                            <p>Нет добавленных аккаунтов</p>
                            <p className="text-sm">Добавьте первый аккаунт для начала работы</p>
                        </div>
                    ) : (
                        <div className="space-y-2">
                            {accounts.map((account) => (
                                <div
                                    key={account.id}
                                    className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                                >
                                    <div className="flex items-center justify-center h-10 w-10 rounded-full bg-blue-100">
                                        <Smartphone className="h-5 w-5 text-blue-600" />
                                    </div>

                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-2 flex-wrap">
                                            <h3 className="font-medium truncate">
                                                {account.name || account.phoneNumber}
                                            </h3>
                                            <Badge
                                                variant={account.isActive ? "default" : "secondary"}
                                            >
                                                {account.isActive ? "Активен" : "Неактивен"}
                                            </Badge>
                                            <Badge variant={getStatusConfig(account.status).variant}>
                                                {getStatusConfig(account.status).label}
                                            </Badge>
                                        </div>
                                        <p className="text-sm text-muted-foreground truncate">
                                            {account.phoneNumber}
                                        </p>
                                        {account.created && (
                                            <p className="text-xs text-muted-foreground">
                                                Добавлен: {new Date(account.created).toLocaleDateString("ru-RU")}
                                            </p>
                                        )}
                                    </div>

                                    <div className="flex items-center gap-2">
                                        {account.status !== TelegramSessionStatus.Authorized && (
                                            <TelegramAccountAuthDialog
                                                accountId={account.id!}
                                                accountName={account.name || account.phoneNumber}
                                            />
                                        )}

                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild>
                                                <Button variant="ghost" size="sm">
                                                    <MoreVertical className="h-4 w-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end">
                                                <DropdownMenuItem
                                                    onClick={() => setAccountToDelete(account)}
                                                    className="text-red-600 focus:text-red-600"
                                                >
                                                    <Trash2 className="h-4 w-4 mr-2" />
                                                    Удалить
                                                </DropdownMenuItem>
                                            </DropdownMenuContent>
                                        </DropdownMenu>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            <AlertDialog
                open={!!accountToDelete}
                onOpenChange={(open) => !open && setAccountToDelete(null)}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Удалить аккаунт?</AlertDialogTitle>
                        <AlertDialogDescription>
                            Вы уверены, что хотите удалить аккаунт{" "}
                            <strong>{accountToDelete?.name || accountToDelete?.phoneNumber}</strong>?
                            Это действие нельзя отменить.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel disabled={isDeleting}>Отмена</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={handleDeleteConfirm}
                            disabled={isDeleting}
                            className="bg-red-600 hover:bg-red-700"
                        >
                            {isDeleting ? "Удаление..." : "Удалить"}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </>
    );
}
