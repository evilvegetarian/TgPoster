import { useState } from "react";
import { AlertCircle, Network, MoreVertical, Pencil, Trash2 } from "lucide-react";
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
    useGetApiV1Proxy,
    useDeleteApiV1ProxyId,
    getGetApiV1ProxyQueryKey,
} from "@/api/endpoints/proxy/proxy";
import { useQueryClient } from "@tanstack/react-query";
import type { ProxyResponse } from "@/api/endpoints/tgPosterAPI.schemas";
import { PROXY_TYPE_LABELS } from "./proxy-form-fields";
import { ProxyEditDialog } from "./proxy-edit-dialog";

export function ProxyListComponent() {
    const { data: proxiesData, error, isLoading } = useGetApiV1Proxy();
    const queryClient = useQueryClient();
    const [proxyToDelete, setProxyToDelete] = useState<ProxyResponse | null>(null);
    const [proxyToEdit, setProxyToEdit] = useState<ProxyResponse | null>(null);

    const { mutate: deleteProxy, isPending: isDeleting } = useDeleteApiV1ProxyId({
        mutation: {
            onSuccess: () => {
                toast.success("Прокси удалён");
                queryClient.invalidateQueries({
                    queryKey: getGetApiV1ProxyQueryKey(),
                });
                setProxyToDelete(null);
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить прокси",
                });
            },
        },
    });

    function handleDeleteConfirm() {
        if (proxyToDelete?.id) {
            deleteProxy({ id: proxyToDelete.id });
        }
    }

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Прокси</CardTitle>
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
                    <CardTitle>Прокси</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="flex items-center gap-2 text-red-600">
                        <AlertCircle className="h-5 w-5" />
                        <span>Ошибка загрузки прокси</span>
                    </div>
                </CardContent>
            </Card>
        );
    }

    const proxies = proxiesData?.items || [];
    const sessionsInUse = (proxyToDelete?.sessionsCount ?? 0) > 0;

    return (
        <>
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        Прокси
                        {proxies.length > 0 && (
                            <Badge variant="secondary">{proxies.length}</Badge>
                        )}
                    </CardTitle>
                    <CardDescription>
                        SOCKS5/HTTP/MTProxy для маршрутизации трафика Telegram-сессий
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {proxies.length === 0 ? (
                        <div className="text-center py-8 text-muted-foreground">
                            <Network className="h-12 w-12 mx-auto mb-2 opacity-50" />
                            <p>Нет добавленных прокси</p>
                            <p className="text-sm">
                                Добавьте прокси, если конкретный DC Telegram недоступен с сервера
                            </p>
                        </div>
                    ) : (
                        <div className="space-y-2">
                            {proxies.map((proxy) => (
                                <div
                                    key={proxy.id}
                                    className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
                                >
                                    <div className="flex items-center justify-center h-10 w-10 rounded-full bg-purple-100">
                                        <Network className="h-5 w-5 text-purple-600" />
                                    </div>

                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-2 flex-wrap">
                                            <h3 className="font-medium truncate">{proxy.name}</h3>
                                            <Badge variant="outline">
                                                {proxy.type !== undefined
                                                    ? PROXY_TYPE_LABELS[proxy.type]
                                                    : "?"}
                                            </Badge>
                                            {(proxy.sessionsCount ?? 0) > 0 && (
                                                <Badge variant="secondary">
                                                    {proxy.sessionsCount} сессий
                                                </Badge>
                                            )}
                                        </div>
                                        <p className="text-sm text-muted-foreground truncate font-mono">
                                            {proxy.host}:{proxy.port}
                                        </p>
                                        {proxy.created && (
                                            <p className="text-xs text-muted-foreground">
                                                Создан:{" "}
                                                {new Date(proxy.created).toLocaleDateString("ru-RU")}
                                            </p>
                                        )}
                                    </div>

                                    <DropdownMenu>
                                        <DropdownMenuTrigger asChild>
                                            <Button variant="ghost" size="sm">
                                                <MoreVertical className="h-4 w-4" />
                                            </Button>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent align="end">
                                            <DropdownMenuItem
                                                onClick={() => setProxyToEdit(proxy)}
                                            >
                                                <Pencil className="h-4 w-4 mr-2" />
                                                Изменить
                                            </DropdownMenuItem>
                                            <DropdownMenuItem
                                                onClick={() => setProxyToDelete(proxy)}
                                                className="text-red-600 focus:text-red-600"
                                            >
                                                <Trash2 className="h-4 w-4 mr-2" />
                                                Удалить
                                            </DropdownMenuItem>
                                        </DropdownMenuContent>
                                    </DropdownMenu>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            <AlertDialog
                open={!!proxyToDelete}
                onOpenChange={(open) => !open && setProxyToDelete(null)}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Удалить прокси?</AlertDialogTitle>
                        <AlertDialogDescription>
                            Прокси <strong>{proxyToDelete?.name}</strong> будет удалён.{" "}
                            {sessionsInUse && (
                                <>
                                    К нему привязано <strong>{proxyToDelete?.sessionsCount}</strong>{" "}
                                    сессий — они продолжат работать без прокси (напрямую).
                                </>
                            )}
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

            <ProxyEditDialog
                proxy={proxyToEdit}
                onOpenChange={(open) => !open && setProxyToEdit(null)}
            />
        </>
    );
}
