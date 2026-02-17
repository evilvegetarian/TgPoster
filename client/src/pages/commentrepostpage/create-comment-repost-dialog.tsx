import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {z} from "zod";
import {Loader2, Plus} from "lucide-react";
import {toast} from "sonner";
import {Button} from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
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
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {Input} from "@/components/ui/input";
import {
    getGetApiV1CommentRepostQueryKey,
    usePostApiV1CommentRepost,
} from "@/api/endpoints/comment-repost/comment-repost.ts";
import {useGetApiV1Schedule} from "@/api/endpoints/schedule/schedule.ts";
import {useGetApiV1TelegramSession} from "@/api/endpoints/telegram-session/telegram-session.ts";
import {useState} from "react";
import {useQueryClient} from "@tanstack/react-query";

const createCommentRepostSchema = z.object({
    scheduleId: z.string().min(1, "Выберите расписание"),
    telegramSessionId: z.string().min(1, "Выберите Telegram сессию"),
    watchedChannel: z.string()
        .min(1, "Укажите канал для мониторинга")
        .max(256, "Идентификатор канала не может быть длиннее 256 символов"),
});

type FormValues = z.infer<typeof createCommentRepostSchema>;

export function CreateCommentRepostDialog() {
    const [open, setOpen] = useState(false);
    const form = useForm<FormValues>({
        resolver: zodResolver(createCommentRepostSchema),
        defaultValues: {
            scheduleId: "",
            telegramSessionId: "",
            watchedChannel: "",
        },
    });
    const queryClient = useQueryClient();
    const {data: schedulesData, isLoading: schedulesLoading} = useGetApiV1Schedule();
    const schedules = schedulesData?.items ?? [];

    const {data: sessionsData, isLoading: sessionsLoading} = useGetApiV1TelegramSession();
    const sessions = sessionsData?.items ?? [];

    const {mutate: createSettings, isPending} = usePostApiV1CommentRepost({
        mutation: {
            onSuccess: () => {
                toast.success("Настройки созданы", {
                    description: "Настройки комментирующего репоста успешно созданы.",
                });
                form.reset();
                setOpen(false);
                void queryClient.invalidateQueries({queryKey: getGetApiV1CommentRepostQueryKey()});
            },
            onError: (error) => {
                toast.error("Ошибка создания настроек", {
                    description: error.title || "Не удалось создать настройки комментирующего репоста",
                });
            },
        },
    });

    function onSubmit(values: FormValues) {
        createSettings({
            data: {
                scheduleId: values.scheduleId,
                telegramSessionId: values.telegramSessionId,
                watchedChannel: values.watchedChannel,
            },
        });
    }

    function handleOpenChange(isOpen: boolean) {
        if (!isOpen && !isPending) {
            form.reset();
        }
    }

    const isLoading = schedulesLoading || sessionsLoading;

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button className="gap-2">
                    <Plus className="h-4 w-4"/>
                    Создать настройку
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[520px]">
                <DialogHeader>
                    <DialogTitle>Создать комментирующий репост</DialogTitle>
                    <DialogDescription>
                        Выберите расписание, Telegram сессию и укажите канал для мониторинга
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="scheduleId"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Расписание *</FormLabel>
                                    <Select
                                        onValueChange={field.onChange}
                                        defaultValue={field.value}
                                        disabled={isLoading || isPending}
                                    >
                                        <FormControl>
                                            <SelectTrigger>
                                                <SelectValue placeholder={
                                                    isLoading ? "Загрузка..." : "Выберите расписание"
                                                }/>
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {isLoading ? (
                                                <SelectItem value="loading" disabled>
                                                    <Loader2 className="h-4 w-4 animate-spin"/>
                                                </SelectItem>
                                            ) : schedules.length === 0 ? (
                                                <SelectItem value="empty" disabled>
                                                    Нет доступных расписаний
                                                </SelectItem>
                                            ) : (
                                                schedules.map(schedule => (
                                                    <SelectItem key={schedule.id} value={schedule.id}>
                                                        {schedule.name}
                                                    </SelectItem>
                                                ))
                                            )}
                                        </SelectContent>
                                    </Select>
                                    <FormDescription>
                                        Расписание (наш канал-источник) для комментирования
                                    </FormDescription>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="telegramSessionId"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Telegram сессия *</FormLabel>
                                    <Select
                                        onValueChange={field.onChange}
                                        defaultValue={field.value}
                                        disabled={isLoading || isPending}
                                    >
                                        <FormControl>
                                            <SelectTrigger>
                                                <SelectValue placeholder={
                                                    isLoading ? "Загрузка..." : "Выберите сессию"
                                                }/>
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {isLoading ? (
                                                <SelectItem value="loading" disabled>
                                                    <Loader2 className="h-4 w-4 animate-spin"/>
                                                </SelectItem>
                                            ) : sessions.length === 0 ? (
                                                <SelectItem value="empty" disabled>
                                                    Нет активных сессий
                                                </SelectItem>
                                            ) : (
                                                sessions.map(session => (
                                                    <SelectItem key={session.id} value={session.id!}>
                                                        {session.name || session.phoneNumber || session.id}
                                                    </SelectItem>
                                                ))
                                            )}
                                        </SelectContent>
                                    </Select>
                                    <FormDescription>
                                        Telegram сессия для мониторинга и отправки комментариев
                                    </FormDescription>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="watchedChannel"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Отслеживаемый канал *</FormLabel>
                                    <FormControl>
                                        <Input
                                            placeholder="@channel или t.me/channel"
                                            disabled={isPending}
                                            {...field}
                                        />
                                    </FormControl>
                                    <FormDescription>
                                        Username или ссылка на канал, посты которого будут комментироваться
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
                            <Button type="submit" disabled={isPending || isLoading}>
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
