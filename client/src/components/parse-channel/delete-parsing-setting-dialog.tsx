import {useState} from "react";
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
import {useDeleteApiV1ParseChannelId} from "@/api/endpoints/parse-channel/parse-channel.ts";
import type {ParseChannelResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";

interface DeleteParsingSettingDialogProps {
    setting: ParseChannelResponse;
    refetch: () => void;
}

export function DeleteParsingSettingDialog({setting, refetch}: DeleteParsingSettingDialogProps) {
    const [open, setOpen] = useState(false);

    const {mutate, isPending: isDeleting} = useDeleteApiV1ParseChannelId({
        mutation: {
            onSuccess: () => {
                toast.success("Настройка удалена", {
                    description: "Настройка парсинга успешно удалена",
                });
                setOpen(false);
                refetch();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить настройку парсинга",
                });
            },
        },
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
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-muted-foreground hover:text-destructive"
                >
                    <Trash2 className="h-4 w-4"/>
                </Button>
            </AlertDialogTrigger>

            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>Удалить настройку парсинга?</AlertDialogTitle>
                    <AlertDialogDescription>
                        Вы уверены, что хотите удалить настройку парсинга для
                        канала <strong>"{setting.channel}"</strong>?
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
