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
import {Switch} from "@/components/ui/switch";
import {usePutApiV1RepostDestinationsId} from "@/api/endpoints/repost/repost.ts";
import type {RepostDestinationDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";

const settingsSchema = z.object({
    isActive: z.boolean(),
    delayMinSeconds: z.coerce.number().int().min(0, "Не может быть отрицательным"),
    delayMaxSeconds: z.coerce.number().int().min(0, "Не может быть отрицательным"),
    repostEveryNth: z.coerce.number().int().min(1, "Минимум 1"),
    skipProbability: z.coerce.number().int().min(0, "Минимум 0").max(100, "Максимум 100"),
    maxRepostsPerDay: z.coerce.number().int().min(1, "Минимум 1").nullable(),
}).refine(data => data.delayMaxSeconds >= data.delayMinSeconds, {
    message: "Максимальная задержка не может быть меньше минимальной",
    path: ["delayMaxSeconds"],
});

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

                        <div className="grid grid-cols-2 gap-3">
                            <FormField
                                control={form.control}
                                name="delayMinSeconds"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Мин. задержка (сек)</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="number"
                                                min={0}
                                                {...field}
                                                disabled={isPending}
                                            />
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="delayMaxSeconds"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Макс. задержка (сек)</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="number"
                                                min={0}
                                                {...field}
                                                disabled={isPending}
                                            />
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                        </div>

                        <FormField
                            control={form.control}
                            name="repostEveryNth"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Каждое N-е сообщение</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="number"
                                            min={1}
                                            {...field}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormDescription>
                                        1 = каждое сообщение, 2 = каждое второе и т.д.
                                    </FormDescription>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="skipProbability"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Вероятность пропуска (%)</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="number"
                                            min={0}
                                            max={100}
                                            {...field}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormDescription>
                                        0 = не пропускать, 50 = пропускать половину
                                    </FormDescription>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="maxRepostsPerDay"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Макс. репостов в день</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="number"
                                            min={1}
                                            placeholder="Без лимита"
                                            value={field.value ?? ""}
                                            onChange={(e) => {
                                                const val = e.target.value;
                                                field.onChange(val === "" ? null : Number(val));
                                            }}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormDescription>
                                        Оставьте пустым для снятия лимита
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
