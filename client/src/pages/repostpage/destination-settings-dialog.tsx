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
} from "@/components/ui/form";
import {Switch} from "@/components/ui/switch";
import {usePutApiV1RepostDestinationsId} from "@/api/endpoints/repost/repost.ts";
import type {RepostDestinationDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {
    RepostRandomnessFields,
    delayRangeCheck,
    delayRangeError,
    repostRandomnessShape,
} from "@/pages/repostpage/repost-randomness-fields.tsx";

const settingsSchema = z.object({
    isActive: z.boolean(),
    ...repostRandomnessShape,
}).refine(delayRangeCheck, delayRangeError);

type FormValues = z.infer<typeof settingsSchema>;

interface DestinationSettingsDialogProps {
    destination: RepostDestinationDto;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onSuccess: () => void;
}

export function DestinationSettingsDialog({
    destination,
    open,
    onOpenChange,
    onSuccess,
}: DestinationSettingsDialogProps) {
    const form = useForm<FormValues>({
        resolver: zodResolver(settingsSchema),
        values: {
            isActive: destination.isActive,
            delayMinSeconds: destination.delayMinSeconds ?? 0,
            delayMaxSeconds: destination.delayMaxSeconds ?? 0,
            repostEveryNth: destination.repostEveryNth ?? 1,
            skipProbability: destination.skipProbability ?? 0,
            maxRepostsPerDay: destination.maxRepostsPerDay ?? null,
        },
    });

    const {mutate: updateDestination, isPending} = usePutApiV1RepostDestinationsId({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки сохранены", {
                    description: `Настройки канала "${destination.title ?? destination.chatId}" обновлены`,
                });
                onOpenChange(false);
                onSuccess();
            },
            onError: (error) => {
                toast.error("Ошибка сохранения", {
                    description: error.title || "Не удалось сохранить настройки канала",
                });
            },
        },
    });

    function onSubmit(values: FormValues) {
        updateDestination({
            id: destination.id,
            data: {
                isActive: values.isActive,
                delayMinSeconds: values.delayMinSeconds,
                delayMaxSeconds: values.delayMaxSeconds,
                repostEveryNth: values.repostEveryNth,
                skipProbability: values.skipProbability,
                maxRepostsPerDay: values.maxRepostsPerDay,
            },
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
            <DialogContent className="sm:max-w-[520px]">
                <DialogHeader>
                    <DialogTitle>Настройки репоста</DialogTitle>
                    <DialogDescription>
                        {destination.title ?? `Канал ${destination.chatId}`}
                        {destination.username && ` (@${destination.username})`}
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="isActive"
                            render={({field}) => (
                                <FormItem className="flex items-center justify-between rounded-lg border p-3">
                                    <div>
                                        <FormLabel>Активен</FormLabel>
                                        <FormDescription>
                                            Включить/выключить репост в этот канал
                                        </FormDescription>
                                    </div>
                                    <FormControl>
                                        <Switch
                                            checked={field.value}
                                            onCheckedChange={field.onChange}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                </FormItem>
                            )}
                        />

                        <RepostRandomnessFields form={form} disabled={isPending}/>

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
                                        Сохранение...
                                    </>
                                ) : (
                                    "Сохранить"
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
