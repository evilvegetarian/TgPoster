import {useState} from "react";
import {useQueryClient} from "@tanstack/react-query";
import {toast} from "sonner";
import {AlertCircle, Loader2, MoreVertical, Share2, Trash2} from "lucide-react";
import {
    getGetApiV1SocialAccountsQueryKey,
    useDeleteApiV1SocialAccountsId,
    useGetApiV1SocialAccounts,
} from "@/api/endpoints/social-account/social-account";
import type {SocialAccountResponse} from "@/api/endpoints/tgPosterAPI.schemas";
import {ACCOUNT_STATUS_LABELS, PLATFORM_LABELS} from "@/components/social-account/platform-meta.ts";
import {Card, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {Badge} from "@/components/ui/badge.tsx";
import {Skeleton} from "@/components/ui/skeleton.tsx";
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

export function SocialAccountList() {
    const {data: accounts, isLoading, error} = useGetApiV1SocialAccounts();
    const queryClient = useQueryClient();
    const [accountToDelete, setAccountToDelete] = useState<SocialAccountResponse | null>(null);

    const {mutate: deleteAccount, isPending: isDeleting} = useDeleteApiV1SocialAccountsId({
        mutation: {
            onSuccess: () => {
                toast.success("Аккаунт удалён");
                setAccountToDelete(null);
                queryClient.invalidateQueries({queryKey: getGetApiV1SocialAccountsQueryKey()});
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить аккаунт",
                });
            },
        },
    });

    const handleDeleteConfirm = () => {
        if (accountToDelete) {
            deleteAccount({id: accountToDelete.id});
        }
    };

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-2">
                        <Share2 className="h-5 w-5 text-primary"/>
                        <CardTitle>Мои аккаунты</CardTitle>
                    </div>
                    <CardDescription>Загрузка списка аккаунтов...</CardDescription>
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
                        <Share2 className="h-5 w-5 text-primary"/>
                        <CardTitle>Мои аккаунты</CardTitle>
                    </div>
                </CardHeader>
                <CardContent>
                    <div className="flex items-center gap-2 text-destructive bg-destructive/10 p-4 rounded-lg">
                        <AlertCircle className="h-5 w-5"/>
                        <span>Произошла ошибка при загрузке аккаунтов</span>
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
                            <Share2 className="h-5 w-5 text-primary"/>
                            <CardTitle>Мои аккаунты</CardTitle>
                        </div>
                        <Badge variant="secondary">
                            {accounts?.length || 0}
                        </Badge>
                    </div>
                    <CardDescription>
                        Подключённые аккаунты соцсетей
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {accounts && accounts.length > 0 ? (
                        <div className="space-y-3">
                            {accounts.map((account) => (
                                <div
                                    key={account.id}
                                    className="flex items-center gap-4 p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                                >
                                    <div className="flex-1 min-w-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <span className="font-medium text-foreground truncate">
                                                {PLATFORM_LABELS[account.platform]}
                                            </span>
                                            <span className="text-sm text-muted-foreground truncate">
                                                {account.name}
                                            </span>
                                            <Badge
                                                variant={account.status === "Active" ? "outline" : "destructive"}
                                                className="text-xs"
                                            >
                                                {ACCOUNT_STATUS_LABELS[account.status]}
                                            </Badge>
                                        </div>
                                        {account.lastError && (
                                            <p className="text-xs text-muted-foreground mt-1 truncate">
                                                {account.lastError}
                                            </p>
                                        )}
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
                                                onClick={() => setAccountToDelete(account)}
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
                            <Share2 className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                            <h3 className="text-lg font-medium text-foreground mb-2">
                                Пока нет подключённых аккаунтов
                            </h3>
                            <p className="text-muted-foreground">
                                Подключите аккаунт, чтобы посты уходили в соцсети
                            </p>
                        </div>
                    )}
                </CardContent>
            </Card>

            <AlertDialog
                open={!!accountToDelete}
                onOpenChange={() => !isDeleting && setAccountToDelete(null)}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Удалить аккаунт?</AlertDialogTitle>
                        <AlertDialogDescription>
                            Кросс-постинг в него из всех расписаний прекратится.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel
                            onClick={() => setAccountToDelete(null)}
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
