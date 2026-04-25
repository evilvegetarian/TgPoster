import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
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
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {
    usePutApiV1TelegramSessionId,
    getGetApiV1TelegramSessionQueryKey,
} from "@/api/endpoints/telegram-session/telegram-session";
import { useGetApiV1Proxy, getGetApiV1ProxyQueryKey } from "@/api/endpoints/proxy/proxy";
import { useQueryClient } from "@tanstack/react-query";
import type { TelegramSessionResponse } from "@/api/endpoints/tgPosterAPI.schemas";
import { ProxyCreateDialog } from "@/components/proxy/proxy-create-dialog";

const formSchema = z.object({
    name: z.string().optional(),
    isActive: z.boolean(),
    proxyId: z.string().uuid().nullable().optional(),
});

type EditForm = z.infer<typeof formSchema>;

interface TelegramAccountEditDialogProps {
    account: TelegramSessionResponse | null;
    onOpenChange: (open: boolean) => void;
}

export function TelegramAccountEditDialog({ account, onOpenChange }: TelegramAccountEditDialogProps) {
    const queryClient = useQueryClient();
    const open = account != null;
    const [proxyCreateOpen, setProxyCreateOpen] = useState(false);

    const { data: proxiesData } = useGetApiV1Proxy();
    const proxies = proxiesData?.items ?? [];

    const form = useForm<EditForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            name: "",
            isActive: true,
            proxyId: null,
        },
    });

    useEffect(() => {
        if (account) {
            form.reset({
                name: account.name ?? "",
                isActive: account.isActive ?? true,
                proxyId: account.proxyId ?? null,
            });
        }
    }, [account, form]);

    const { mutate: updateAccount, isPending } = usePutApiV1TelegramSessionId({
        mutation: {
            onSuccess: () => {
                toast.success("Аккаунт обновлён");
                queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
                onOpenChange(false);
            },
            onError: (error) => {
                toast.error("Ошибка обновления аккаунта", {
                    description: error.title || "Не удалось обновить",
                });
            },
        },
    });

    function onSubmit(values: EditForm) {
        if (!account?.id) return;
        updateAccount({
            id: account.id,
            data: {
                name: values.name || null,
                isActive: values.isActive,
                proxyId: values.proxyId || null,
            },
        });
    }

    function handleProxyCreated(proxyId: string) {
        queryClient.invalidateQueries({ queryKey: getGetApiV1ProxyQueryKey() });
        form.setValue("proxyId", proxyId);
    }

    return (
        <>
            <Dialog open={open} onOpenChange={onOpenChange}>
                <DialogContent className="sm:max-w-[500px]">
                    <DialogHeader>
                        <DialogTitle>Изменить аккаунт</DialogTitle>
                        <DialogDescription>
                            {account?.name || account?.phoneNumber}
                        </DialogDescription>
                    </DialogHeader>
                    <Form {...form}>
                        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                            <FormField
                                control={form.control}
                                name="name"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Название (опционально)</FormLabel>
                                        <FormControl>
                                            <Input placeholder="Мой аккаунт" {...field} value={field.value ?? ""} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="isActive"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Статус</FormLabel>
                                        <Select
                                            onValueChange={(v) => field.onChange(v === "true")}
                                            value={field.value ? "true" : "false"}
                                        >
                                            <FormControl>
                                                <SelectTrigger>
                                                    <SelectValue />
                                                </SelectTrigger>
                                            </FormControl>
                                            <SelectContent>
                                                <SelectItem value="true">Активен</SelectItem>
                                                <SelectItem value="false">Неактивен</SelectItem>
                                            </SelectContent>
                                        </Select>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="proxyId"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Прокси</FormLabel>
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
                            <DialogFooter>
                                <Button
                                    type="button"
                                    variant="outline"
                                    onClick={() => onOpenChange(false)}
                                    disabled={isPending}
                                >
                                    Отмена
                                </Button>
                                <Button type="submit" disabled={isPending}>
                                    {isPending ? "Сохранение..." : "Сохранить"}
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
