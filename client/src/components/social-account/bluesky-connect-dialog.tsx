import {useState} from "react";
import {useQueryClient} from "@tanstack/react-query";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {z} from "zod";
import {toast} from "sonner";
import {Plus, Loader2} from "lucide-react";
import {
    getGetApiV1SocialAccountsQueryKey,
    usePostApiV1SocialAccountsBluesky,
} from "@/api/endpoints/social-account/social-account";
import {Button} from "@/components/ui/button.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog.tsx";

const formSchema = z.object({
    handle: z.string().min(3, "Handle не может быть короче 3 символов").max(253, "Handle слишком длинный"),
    appPassword: z.string().min(8, "Пароль приложения не может быть короче 8 символов").max(64, "Пароль приложения слишком длинный"),
});

type BlueskyConnectForm = z.infer<typeof formSchema>;

export function BlueskyConnectDialog() {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();

    const form = useForm<BlueskyConnectForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            handle: "",
            appPassword: "",
        },
    });

    const {mutate, isPending} = usePostApiV1SocialAccountsBluesky({
        mutation: {
            onSuccess: () => {
                toast.success("Bluesky подключён");
                form.reset();
                setOpen(false);
                queryClient.invalidateQueries({queryKey: getGetApiV1SocialAccountsQueryKey()});
            },
            onError: (error) => {
                toast.error("Не удалось подключить", {
                    description: error.title || "Проверьте handle и пароль приложения",
                });
            },
        },
    });

    const onSubmit = (values: BlueskyConnectForm) => {
        mutate({data: values});
    };

    const handleOpenChange = (newOpen: boolean) => {
        if (!isPending) {
            setOpen(newOpen);
            if (!newOpen) {
                form.reset();
            }
        }
    };

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogTrigger asChild>
                <Button className="w-full gap-2">
                    <Plus className="h-4 w-4"/>
                    Подключить Bluesky
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Подключить Bluesky</DialogTitle>
                    <DialogDescription>
                        Введите handle аккаунта и пароль приложения Bluesky
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="handle"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Handle</FormLabel>
                                    <FormControl>
                                        <Input
                                            placeholder="name.bsky.social"
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
                            name="appPassword"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Пароль приложения</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="password"
                                            placeholder="xxxx-xxxx-xxxx-xxxx"
                                            {...field}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <p className="text-sm text-muted-foreground">
                            Создайте пароль приложения в Bluesky: Настройки → Конфиденциальность и
                            безопасность → Пароли приложений. Основной пароль не вводите.{" "}
                            <a
                                href="https://bsky.app/settings/app-passwords"
                                target="_blank"
                                rel="noreferrer"
                                className="text-blue-600 hover:underline"
                            >
                                Открыть настройки
                            </a>
                        </p>

                        <DialogFooter className="gap-2">
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
                                        Подключение...
                                    </>
                                ) : (
                                    "Подключить"
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
