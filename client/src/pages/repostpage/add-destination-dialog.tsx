import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {z} from "zod";
import {Loader2} from "lucide-react";
import {toast} from "sonner";
import {Button} from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import {
    Form,
    FormControl,
    FormDescription,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import {Input} from "@/components/ui/input";
import {usePostApiV1RepostSettingsSettingsIdDestinations} from "@/api/endpoints/repost/repost.ts";

const addDestinationSchema = z.object({
    chatIdentifier: z.string()
        .min(1, "Укажите канал")
});

type FormValues = z.infer<typeof addDestinationSchema>;

interface AddDestinationDialogProps {
    settingsId: string;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onSuccess: () => void;
}

export function AddDestinationDialog({
    settingsId,
    open,
    onOpenChange,
    onSuccess,
}: AddDestinationDialogProps) {
    const form = useForm<FormValues>({
        resolver: zodResolver(addDestinationSchema),
        defaultValues: {
            chatIdentifier: "",
        },
    });

    const {mutate: addDestination, isPending} = usePostApiV1RepostSettingsSettingsIdDestinations({
        mutation: {
            onSuccess: () => {
                toast.success("Канал добавлен", {
                    description: "Целевой канал успешно добавлен к настройкам репоста",
                });
                form.reset();
                onOpenChange(false);
                onSuccess();
            },
            onError: (error) => {
                toast.error("Ошибка добавления канала", {
                    description: error.title || "Не удалось добавить целевой канал",
                });
            },
        },
    });

    function onSubmit(values: FormValues) {
        addDestination({
            settingsId,
            data: {chatIdentifier: values.chatIdentifier},
        });
    }

    function handleOpenChange(isOpen: boolean) {
        if (!isOpen && !isPending) {
            form.reset();
        }
        onOpenChange(isOpen);
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[480px]">
                <DialogHeader>
                    <DialogTitle>Добавить целевой канал</DialogTitle>
                    <DialogDescription>
                        Укажите ID или @username канала для автоматического репоста сообщений
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="chatIdentifier"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Канал *</FormLabel>
                                    <FormControl>
                                        <Input
                                            placeholder="@channel или -1001234567890"
                                            {...field}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormDescription>
                                        Введите @username канала или его числовой ID
                                    </FormDescription>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <DialogFooter>
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => handleOpenChange(false)}
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button type="submit" disabled={isPending}>
                                {isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        Добавление...
                                    </>
                                ) : (
                                    "Добавить"
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
