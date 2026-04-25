import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
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
import { Form } from "@/components/ui/form";
import {
    usePutApiV1ProxyId,
    getGetApiV1ProxyQueryKey,
} from "@/api/endpoints/proxy/proxy";
import { useQueryClient } from "@tanstack/react-query";
import { ProxyType, type ProxyResponse } from "@/api/endpoints/tgPosterAPI.schemas";
import {
    ProxyFormFields,
    proxyFormSchema,
    type ProxyFormValues,
} from "./proxy-form-fields";

interface ProxyEditDialogProps {
    proxy: ProxyResponse | null;
    onOpenChange: (open: boolean) => void;
}

export function ProxyEditDialog({ proxy, onOpenChange }: ProxyEditDialogProps) {
    const queryClient = useQueryClient();
    const open = proxy != null;

    const form = useForm<ProxyFormValues>({
        resolver: zodResolver(proxyFormSchema),
        defaultValues: {
            name: "",
            type: ProxyType.Socks5,
            host: "",
            port: 1080,
            username: "",
            password: "",
            secret: "",
        },
    });

    useEffect(() => {
        if (proxy) {
            form.reset({
                name: proxy.name ?? "",
                type: proxy.type ?? ProxyType.Socks5,
                host: proxy.host ?? "",
                port: proxy.port ?? 1080,
                username: proxy.username ?? "",
                password: proxy.password ?? "",
                secret: proxy.secret ?? "",
            });
        }
    }, [proxy, form]);

    const type = form.watch("type");

    const { mutate: updateProxy, isPending } = usePutApiV1ProxyId({
        mutation: {
            onSuccess: () => {
                toast.success("Прокси обновлён");
                queryClient.invalidateQueries({
                    queryKey: getGetApiV1ProxyQueryKey(),
                });
                onOpenChange(false);
            },
            onError: (error) => {
                toast.error("Ошибка обновления прокси", {
                    description: error.title || "Не удалось обновить",
                });
            },
        },
    });

    function onSubmit(values: ProxyFormValues) {
        if (!proxy?.id) return;
        updateProxy({
            id: proxy.id,
            data: {
                name: values.name,
                type: values.type,
                host: values.host,
                port: values.port,
                username: values.username || null,
                password: values.password || null,
                secret: values.type === ProxyType.MTProxy ? values.secret || null : null,
            },
        });
    }

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Изменить прокси</DialogTitle>
                    <DialogDescription>
                        После сохранения связанные сессии будут переподключены.
                    </DialogDescription>
                </DialogHeader>
                <Form {...form}>
                    <form
                        onSubmit={form.handleSubmit(onSubmit)}
                        className="space-y-4"
                    >
                        <ProxyFormFields control={form.control} type={type} />
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
    );
}
