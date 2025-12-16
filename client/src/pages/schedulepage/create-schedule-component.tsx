import {useState} from "react";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {z} from "zod";
import {toast} from "sonner";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger, // Добавлено для полноты примера
} from "@/components/ui/dialog.tsx";
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Input} from "@/components/ui/input.tsx";
import {ArrowLeft, Loader2, Plus} from "lucide-react";
import {useGetApiV1TelegramBot} from "@/api/endpoints/telegram-bot/telegram-bot.ts";
import {getGetApiV1ScheduleQueryKey, usePostApiV1Schedule} from "@/api/endpoints/schedule/schedule.ts";
import {useQueryClient} from "@tanstack/react-query";

const formSchema = z.object({
    name: z.string()
        .min(2, "Название должно быть не менее 2 символов")
        .max(30, "Название должно быть не более 30 символов"),
    channel: z.string().min(5, "ID или @имя канала должно быть не менее 5 символов"),
    telegramBotId: z.string({
        required_error: "Необходимо выбрать Telegram бота.",
    }).min(1, "Необходимо выбрать Telegram бота."),
});

type CreateScheduleFormValues = z.infer<typeof formSchema>;

export function CreateScheduleComponent() {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();

    const form = useForm<CreateScheduleFormValues>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            name: "",
            channel: "",
            telegramBotId: "",
        },
    });

    const {data: telegramBots = [], isLoading: botsLoading} = useGetApiV1TelegramBot();
    const {mutate: createScheduleMutate, isPending} = usePostApiV1Schedule({
        mutation: {
            onSuccess: () => {
                toast.success("Расписание успешно создано");
                form.reset();
                setOpen(false);
                queryClient.invalidateQueries({queryKey: getGetApiV1ScheduleQueryKey()});
            },
            onError: (error) => {
                toast.error("Ошибка", {
                    description: error.title || "Ошибка при создании расписания",
                });
            },
        },
    });

    function onSubmit(values: CreateScheduleFormValues) {
        createScheduleMutate({data: values});
    }

    const handleCloseDialog = (isOpen: boolean) => {
        if (!isOpen) {
            form.reset();
        }
        setOpen(isOpen);
    }

    return (
        <Dialog open={open} onOpenChange={handleCloseDialog}>
            <DialogTrigger asChild>
                <Button>
                    <Plus className="mr-2 h-4 w-4"/>
                    Создать расписание
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[520px]">
                <DialogHeader>
                    <div className="flex items-center gap-3">
                        <Button variant="outline" size="icon" onClick={() => setOpen(false)}>
                            <ArrowLeft className="h-4 w-4"/>
                        </Button>
                        <div className="text-left">
                            <DialogTitle className="text-2xl font-bold leading-tight">
                                Создание расписания
                            </DialogTitle>
                            <DialogDescription>
                                Укажите основную информацию для нового расписания
                            </DialogDescription>
                        </div>
                    </div>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 py-4">
                        <FormField
                            control={form.control}
                            name="name"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Название расписания</FormLabel>
                                    <FormControl>
                                        <Input placeholder="Например, 'Утренние новости'" {...field} />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="channel"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Канал Telegram</FormLabel>
                                    <FormControl>
                                        <Input placeholder="@channel_name или ссылка на канал" {...field} />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="telegramBotId"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Telegram бот</FormLabel>
                                    <Select onValueChange={field.onChange} defaultValue={field.value}
                                            disabled={botsLoading}>
                                        <FormControl>
                                            <SelectTrigger>
                                                <SelectValue placeholder="Выберите бота для отправки"/>
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {botsLoading ? (
                                                <div className="p-2 text-sm text-muted-foreground">Загрузка...</div>
                                            ) : (
                                                telegramBots.map((bot) => (
                                                    <SelectItem key={bot.id} value={bot.id}>
                                                        {bot.name || `Бот ${bot.id}`}
                                                    </SelectItem>
                                                ))
                                            )}
                                        </SelectContent>
                                    </Select>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <DialogFooter className="flex flex-col-reverse gap-2 pt-4 sm:flex-row sm:justify-end">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button type="submit" disabled={isPending || botsLoading}>
                                {isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        Создание...
                                    </>
                                ) : (
                                    "Создать"
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}