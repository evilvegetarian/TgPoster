import {useState} from "react";
import {getGetApiV1TelegramBotQueryKey, usePostApiV1TelegramBot} from "@/api/endpoints/telegram-bot/telegram-bot.ts";
import type {CreateTelegramBotRequest} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {z} from "zod";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Button} from "@/components/ui/button.tsx";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog.tsx";
import {Bot, Loader2, Plus} from "lucide-react";
import {toast} from "sonner";
import {useQueryClient} from "@tanstack/react-query";

const formSchema = z.object({
    token: z.string().min(5, "Токен не может быть меньше 5 символов"),
})
type CreateTelegramBotForm = z.infer<typeof formSchema>;

export function TelegramBotCreateDialog() {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();

    const form = useForm<CreateTelegramBotForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            token: "",
        }
    })

    const {mutate, isPending, error} = usePostApiV1TelegramBot({
        mutation: {
            onSuccess: () => {
                toast.success(`Бот  успешно добавлен!`);
                form.reset();
                setOpen(false);
                queryClient.invalidateQueries({queryKey: getGetApiV1TelegramBotQueryKey()});
            },
            onError: (error) => {
                const errorMessage = error?.title || "Ошибка при добавлении бота";
                toast.error(errorMessage);
            }
        }
    });

    function onSubmit(values: CreateTelegramBotRequest) {
        mutate({data: values})
    }

    const handleOpenChange = (newOpen: boolean) => {
        if (!isPending) {
            setOpen(newOpen)
            if (!newOpen) {
                form.reset()
            }
        }
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogTrigger asChild>
                <Button className="gap-2">
                    <Plus className="h-4 w-4"/>
                    Добавить бота
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <div className="flex items-center gap-2">
                        <Bot className="h-5 w-5 text-blue-500"/>
                        <DialogTitle>Добавить Telegram бота</DialogTitle>
                    </div>
                    <DialogDescription>
                        Введите токен вашего Telegram бота для добавления в систему
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="token"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Токен бота</FormLabel>
                                    <FormControl>
                                        <Input
                                            placeholder="7603344018:AsGopCfN81rr-a1hxswulYrsak3gaqdKI"
                                            {...field}
                                            disabled={isPending}
                                            className="font-mono text-sm"
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        {error && (
                            <div className="text-sm font-medium text-destructive bg-destructive/10 p-3 rounded-md">
                                {error?.title || "Произошла ошибка при добавлении бота"}
                            </div>
                        )}

                        <DialogFooter className="gap-2">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
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
                                    <>
                                        <Plus className="mr-2 h-4 w-4"/>
                                        Добавить
                                    </>
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
