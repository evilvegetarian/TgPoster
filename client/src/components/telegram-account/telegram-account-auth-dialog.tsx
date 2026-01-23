import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Key } from "lucide-react";
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
import {
    usePostApiV1TelegramSessionIdStartAuth,
    usePostApiV1TelegramSessionIdVerifyCode,
    usePostApiV1TelegramSessionIdSendPassword,
    getGetApiV1TelegramSessionQueryKey,
} from "@/api/endpoints/telegram-session/telegram-session";
import { useQueryClient } from "@tanstack/react-query";

type AuthStep = "code" | "password" | "success";

const codeFormSchema = z.object({
    code: z.string().min(5, "Код должен содержать минимум 5 символов"),
});

const passwordFormSchema = z.object({
    password: z.string().min(1, "Пароль обязателен"),
});

type CodeForm = z.infer<typeof codeFormSchema>;
type PasswordForm = z.infer<typeof passwordFormSchema>;

interface TelegramAccountAuthDialogProps {
    accountId: string;
    accountName?: string;
}

export function TelegramAccountAuthDialog({ accountId, accountName }: TelegramAccountAuthDialogProps) {
    const [open, setOpen] = useState(false);
    const [authStep, setAuthStep] = useState<AuthStep>("code");
    const queryClient = useQueryClient();

    const codeForm = useForm<CodeForm>({
        resolver: zodResolver(codeFormSchema),
        defaultValues: {
            code: "",
        },
    });

    const passwordForm = useForm<PasswordForm>({
        resolver: zodResolver(passwordFormSchema),
        defaultValues: {
            password: "",
        },
    });

    const { mutate: startAuth, isPending: isStarting } = usePostApiV1TelegramSessionIdStartAuth({
        mutation: {
            onSuccess: () => {
                toast.success("Код отправлен на ваш Telegram");
                setAuthStep("code");
            },
            onError: (error) => {
                toast.error("Ошибка при начале авторизации", {
                    description: error.title || "Не удалось начать авторизацию",
                });
            },
        },
    });

    const { mutate: verifyCode, isPending: isVerifying } = usePostApiV1TelegramSessionIdVerifyCode({
        mutation: {
            onSuccess: () => {
                toast.success("Авторизация успешна!");
                setAuthStep("success");
                codeForm.reset();
                queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
                setTimeout(() => {
                    setOpen(false);
                    setAuthStep("code");
                }, 1500);
            },
            onError: (error) => {
                const errorMessage = error.title || "";
                if (errorMessage.includes("password") || errorMessage.includes("2FA")) {
                    toast.info("Требуется пароль двухфакторной аутентификации");
                    setAuthStep("password");
                } else {
                    toast.error("Ошибка при проверке кода", {
                        description: errorMessage || "Неверный код",
                    });
                }
            },
        },
    });

    const { mutate: sendPassword, isPending: isSendingPassword } = usePostApiV1TelegramSessionIdSendPassword({
        mutation: {
            onSuccess: () => {
                toast.success("Авторизация успешна!");
                setAuthStep("success");
                passwordForm.reset();
                queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
                setTimeout(() => {
                    setOpen(false);
                    setAuthStep("code");
                }, 1500);
            },
            onError: (error) => {
                toast.error("Ошибка при отправке пароля", {
                    description: error.title || "Неверный пароль",
                });
            },
        },
    });

    function handleStartAuth() {
        startAuth({ id: accountId });
    }

    function onCodeSubmit(values: CodeForm) {
        verifyCode({
            id: accountId,
            data: { code: values.code },
        });
    }

    function onPasswordSubmit(values: PasswordForm) {
        sendPassword({
            id: accountId,
            data: { password: values.password },
        });
    }

    function handleOpenChange(newOpen: boolean) {
        setOpen(newOpen);
        if (newOpen) {
            handleStartAuth();
        } else {
            setAuthStep("code");
            codeForm.reset();
            passwordForm.reset();
        }
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogTrigger asChild>
                <Button variant="outline" size="sm">
                    <Key className="h-4 w-4" />
                    Авторизовать
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Авторизация в Telegram</DialogTitle>
                    <DialogDescription>
                        {accountName ? `Аккаунт: ${accountName}` : ""}
                    </DialogDescription>
                </DialogHeader>

                {authStep === "code" && (
                    <Form {...codeForm}>
                        <form onSubmit={codeForm.handleSubmit(onCodeSubmit)} className="space-y-4">
                            <FormField
                                control={codeForm.control}
                                name="code"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Код подтверждения</FormLabel>
                                        <FormControl>
                                            <Input
                                                placeholder="12345"
                                                {...field}
                                                disabled={isStarting || isVerifying}
                                            />
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
                                    disabled={isStarting || isVerifying}
                                >
                                    Отмена
                                </Button>
                                <Button type="submit" disabled={isStarting || isVerifying}>
                                    {isVerifying ? "Проверка..." : "Подтвердить"}
                                </Button>
                            </DialogFooter>
                        </form>
                    </Form>
                )}

                {authStep === "password" && (
                    <Form {...passwordForm}>
                        <form onSubmit={passwordForm.handleSubmit(onPasswordSubmit)} className="space-y-4">
                            <FormField
                                control={passwordForm.control}
                                name="password"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Пароль 2FA</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="password"
                                                placeholder="Введите пароль"
                                                {...field}
                                                disabled={isSendingPassword}
                                            />
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
                                    disabled={isSendingPassword}
                                >
                                    Отмена
                                </Button>
                                <Button type="submit" disabled={isSendingPassword}>
                                    {isSendingPassword ? "Отправка..." : "Отправить"}
                                </Button>
                            </DialogFooter>
                        </form>
                    </Form>
                )}

                {authStep === "success" && (
                    <div className="flex flex-col items-center justify-center py-8">
                        <div className="text-green-600 text-lg font-semibold mb-2">
                            ✓ Успешная авторизация
                        </div>
                        <p className="text-sm text-muted-foreground">
                            Закрытие окна...
                        </p>
                    </div>
                )}
            </DialogContent>
        </Dialog>
    );
}
