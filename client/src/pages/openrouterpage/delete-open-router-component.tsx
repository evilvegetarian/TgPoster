import {useState} from "react";
import {useQueryClient} from "@tanstack/react-query";
import {Loader2, Trash2} from "lucide-react";
import {toast} from "sonner";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog.tsx";
import {Button} from "@/components/ui/button.tsx";
import {
    getGetApiV1OpenRouterSettingQueryKey,
    useDeleteApiV1OpenRouterSettingId
} from "@/api/endpoints/open-router-setting/open-router-setting.ts";
import type {OpenRouterSettingResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";


export function DeleteOpenRouterComponent({ setting }: { setting: OpenRouterSettingResponse }) {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();

    const {mutate, isPending: isDeleting} = useDeleteApiV1OpenRouterSettingId({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки успешно удалены!");
                setOpen(false);
                queryClient.invalidateQueries({
                    queryKey: getGetApiV1OpenRouterSettingQueryKey()
                });
            },
            onError: (error) => {
                toast.error("Ошибка", {
                    description: error.title || 'Ошибка при удалении настроек'
                });
            }
        }
    });

    const handleDeleteConfirm = (e: React.MouseEvent) => {
        e.preventDefault();

        if (setting?.id) {
            mutate({id: setting.id});
        }
    };

    return (
        <AlertDialog open={open} onOpenChange={setOpen}>
            <AlertDialogTrigger asChild>
                <Button variant="destructive" size="icon" className="h-8 w-8">
                    <Trash2 className="h-4 w-4"/>
                </Button>
            </AlertDialogTrigger>

            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>Удалить настройки?</AlertDialogTitle>
                    <AlertDialogDescription>
                        Вы уверены, что хотите удалить модель <strong>"{setting?.model}"</strong>?
                        Это действие нельзя отменить.
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel disabled={isDeleting}>
                        Отмена
                    </AlertDialogCancel>

                    <AlertDialogAction
                        onClick={handleDeleteConfirm}
                        disabled={isDeleting}
                        className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                    >
                        {isDeleting ? (
                            <>
                                <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                Удаление...
                            </>
                        ) : (
                            <>
                                <Trash2 className="mr-2 h-4 w-4"/>
                                Удалить
                            </>
                        )}
                    </AlertDialogAction>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    );
}