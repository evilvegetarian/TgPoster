import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
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
import { Form } from "@/components/ui/form";
import {
    usePostApiV1Proxy,
    getGetApiV1ProxyQueryKey,
} from "@/api/endpoints/proxy/proxy";
import { useQueryClient } from "@tanstack/react-query";
import { ProxyType } from "@/api/endpoints/tgPosterAPI.schemas";
import {
    ProxyFormFields,
    proxyFormSchema,
    type ProxyFormValues,
} from "./proxy-form-fields";

interface ProxyCreateDialogProps {
    open?: boolean;
    onOpenChange?: (open: boolean) => void;
    onCreated?: (proxyId: string) => void;
    triggerless?: boolean;
}

export function ProxyCreateDialog({
    open: controlledOpen,
    onOpenChange,
    onCreated,
    triggerless,
}: ProxyCreateDialogProps = {}) {
    const [internalOpen, setInternalOpen] = useState(false);
    const open = controlledOpen ?? internalOpen;
    const setOpen = onOpenChange ?? setInternalOpen;

    const queryClient = useQueryClient();

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

    const type = form.watch("type");

    useEffect(() => {
        if (!open) form.reset();
    }, [open, form]);

    const { mutate: createProxy, isPending } = usePostApiV1Proxy({
        mutation: {
            onSuccess: (data) => {
                toast.success("Прокси создан");
                queryClient.invalidateQueries({
                    queryKey: getGetApiV1ProxyQueryKey(),
                });
                setOpen(false);
                if (data?.id) onCreated?.(data.id);
            },
            onError: (error) => {
                toast.error("Ошибка создания прокси", {
                    description: error.title || "Не удалось создать прокси",
                });
            },
        },
    });

    function onSubmit(values: ProxyFormValues) {
        createProxy({
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
        <Dialog open={open} onOpenChange={setOpen}>
            {!triggerless && (
                <DialogTrigger asChild>
                    <Button>
                        <Plus className="h-4 w-4" />
                        Добавить прокси
                    </Button>
                </DialogTrigger>
            )}
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Создать прокси</DialogTitle>
                    <DialogDescription>
                        SOCKS5/HTTP/MTProxy для маршрутизации трафика Telegram-сессии.
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
                                onClick={() => setOpen(false)}
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button type="submit" disabled={isPending}>
                                {isPending ? "Создание..." : "Создать"}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
