import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { usePostApiV1TelegramSession, getGetApiV1TelegramSessionQueryKey } from "@/api/endpoints/telegram-session/telegram-session";
import { useQueryClient } from "@tanstack/react-query";
import type { CreateTelegramSessionRequest } from "@/api/endpoints/tgPosterAPI.schemas";

const formSchema = z.object({
    apiId: z.string().min(1, "API ID обязателен"),
    apiHash: z.string().min(1, "API Hash обязателен"),
    phoneNumber: z.string().min(10, "Номер телефона должен содержать минимум 10 символов"),
    name: z.string().optional(),
});

type CreateTelegramAccountForm = z.infer<typeof formSchema>;

export function TelegramAccountCreateDialog() {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();

    const form = useForm<CreateTelegramAccountForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            apiId: "",
            apiHash: "",
            phoneNumber: "",
            name: "",
        },
    });

    const { mutate: createAccount, isPending } = usePostApiV1TelegramSession({
        mutation: {
            onSuccess: () => {
                toast.success("Telegram аккаунт успешно добавлен!");
                form.reset();
                setOpen(false);
                queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
            },
            onError: (error) => {
                toast.error("Ошибка при добавлении Telegram аккаунта", {
                    description: error.title || "Не удалось добавить аккаунт",
                });
            },
        },
    });

    function onSubmit(values: CreateTelegramAccountForm) {
        const request: CreateTelegramSessionRequest = {
            apiId: values.apiId,
            apiHash: values.apiHash,
            phoneNumber: values.phoneNumber,
            name: values.name || null,
        };
        createAccount({ data: request });
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button>
                    <Plus className="h-4 w-4" />
                    Добавить аккаунт
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Добавить Telegram аккаунт</DialogTitle>
                    <DialogDescription>
                        Введите данные для подключения к Telegram аккаунту
                    </DialogDescription>
                </DialogHeader>
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="apiId"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>API ID</FormLabel>
                                    <FormControl>
                                        <Input placeholder="123456" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="apiHash"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>API Hash</FormLabel>
                                    <FormControl>
                                        <Input placeholder="abcdef123456..." {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="phoneNumber"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Номер телефона</FormLabel>
                                    <FormControl>
                                        <Input placeholder="+79991234567" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="name"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Название (опционально)</FormLabel>
                                    <FormControl>
                                        <Input placeholder="Мой аккаунт" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <DialogFooter>
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button type="submit" disabled={isPending}>
                                {isPending ? "Добавление..." : "Добавить"}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
