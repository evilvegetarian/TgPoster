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
    FormDescription,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { usePostApiV1TelegramSession, getGetApiV1TelegramSessionQueryKey } from "@/api/endpoints/telegram-session/telegram-session";
import { useGetApiV1Proxy, getGetApiV1ProxyQueryKey } from "@/api/endpoints/proxy/proxy";
import { useGetApiV1TelegramBot } from "@/api/endpoints/telegram-bot/telegram-bot";
import { useQueryClient } from "@tanstack/react-query";
import type { CreateTelegramSessionRequest } from "@/api/endpoints/tgPosterAPI.schemas";
import { ProxyCreateDialog } from "@/components/proxy/proxy-create-dialog";

const formSchema = z.object({
    apiId: z.string().min(1, "API ID обязателен"),
    apiHash: z.string().min(1, "API Hash обязателен"),
    phoneNumber: z.string().min(10, "Номер телефона должен содержать минимум 10 символов"),
    name: z.string().optional(),
    proxyId: z.string().uuid().nullable().optional(),
    notificationBotId: z.string().uuid().nullable().optional(),
});

type CreateTelegramAccountForm = z.infer<typeof formSchema>;

export function TelegramAccountCreateDialog() {
    const [open, setOpen] = useState(false);
    const [proxyCreateOpen, setProxyCreateOpen] = useState(false);
    const queryClient = useQueryClient();

    const { data: proxiesData } = useGetApiV1Proxy();
    const proxies = proxiesData?.items ?? [];

    const { data: botsData } = useGetApiV1TelegramBot();
    const bots = botsData?.items ?? [];

    const form = useForm<CreateTelegramAccountForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            apiId: "",
            apiHash: "",
            phoneNumber: "",
            name: "",
            proxyId: null,
            notificationBotId: null,
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
            proxyId: values.proxyId || null,
            notificationBotId: values.notificationBotId || null,
        };
        createAccount({ data: request });
    }

    function handleProxyCreated(proxyId: string) {
        queryClient.invalidateQueries({ queryKey: getGetApiV1ProxyQueryKey() });
        form.setValue("proxyId", proxyId);
    }

    return (
        <>
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
                            <FormField
                                control={form.control}
                                name="proxyId"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Прокси (опционально)</FormLabel>
                                        <Select
                                            onValueChange={(v) => {
                                                if (v === "__create__") {
                                                    setProxyCreateOpen(true);
                                                } else {
                                                    field.onChange(v === "__none__" ? null : v);
                                                }
                                            }}
                                            value={field.value ?? "__none__"}
                                        >
                                            <FormControl>
                                                <SelectTrigger>
                                                    <SelectValue placeholder="Без прокси" />
                                                </SelectTrigger>
                                            </FormControl>
                                            <SelectContent>
                                                <SelectItem value="__none__">Без прокси</SelectItem>
                                                {proxies.map((p) => (
                                                    <SelectItem key={p.id} value={p.id!}>
                                                        {p.name} — {p.host}:{p.port}
                                                    </SelectItem>
                                                ))}
                                                <SelectItem value="__create__">
                                                    + Создать новый прокси
                                                </SelectItem>
                                            </SelectContent>
                                        </Select>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="notificationBotId"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Бот для оповещений (опционально)</FormLabel>
                                        <Select
                                            onValueChange={(v) => field.onChange(v === "__none__" ? null : v)}
                                            value={field.value ?? "__none__"}
                                        >
                                            <FormControl>
                                                <SelectTrigger>
                                                    <SelectValue placeholder="Без оповещений" />
                                                </SelectTrigger>
                                            </FormControl>
                                            <SelectContent>
                                                <SelectItem value="__none__">Без оповещений</SelectItem>
                                                {bots.map((b) => (
                                                    <SelectItem key={b.id} value={b.id}>
                                                        {b.name}
                                                    </SelectItem>
                                                ))}
                                            </SelectContent>
                                        </Select>
                                        <FormDescription>
                                            Бот напишет в чат, если с аккаунтом возникнут проблемы
                                        </FormDescription>
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

            <ProxyCreateDialog
                triggerless
                open={proxyCreateOpen}
                onOpenChange={setProxyCreateOpen}
                onCreated={handleProxyCreated}
            />
        </>
    );
}
