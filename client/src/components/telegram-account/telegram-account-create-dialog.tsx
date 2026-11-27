import { useEffect, useRef, useState } from "react";
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
import {
    useGetApiV1TelegramSession,
    usePostApiV1TelegramSession,
    usePostApiV1TelegramSessionIdVerifyCode,
    usePostApiV1TelegramSessionIdSendPassword,
    getGetApiV1TelegramSessionQueryKey,
} from "@/api/endpoints/telegram-session/telegram-session";
import { useGetApiV1Proxy, getGetApiV1ProxyQueryKey } from "@/api/endpoints/proxy/proxy";
import { useGetApiV1TelegramBot } from "@/api/endpoints/telegram-bot/telegram-bot";
import { useQueryClient } from "@tanstack/react-query";
import type { CreateTelegramSessionRequest } from "@/api/endpoints/tgPosterAPI.schemas";
import { ProxyCreateDialog } from "@/components/proxy/proxy-create-dialog";

type WizardStep = "form" | "code" | "password" | "success";

const MANUAL_CREDENTIALS = "__manual__";

const formSchema = z
    .object({
        credentialsSource: z.string(),
        apiId: z.string().optional(),
        apiHash: z.string().optional(),
        phoneNumber: z.string().min(10, "Номер телефона должен содержать минимум 10 символов"),
        name: z.string().optional(),
        proxyId: z.string().uuid().nullable().optional(),
        notificationBotId: z.string().uuid().nullable().optional(),
    })
    .superRefine((values, ctx) => {
        if (values.credentialsSource === MANUAL_CREDENTIALS) {
            if (!values.apiId?.trim()) {
                ctx.addIssue({
                    code: z.ZodIssueCode.custom,
                    message: "API ID обязателен",
                    path: ["apiId"],
                });
            }
            if (!values.apiHash?.trim()) {
                ctx.addIssue({
                    code: z.ZodIssueCode.custom,
                    message: "API Hash обязателен",
                    path: ["apiHash"],
                });
            }
        }
    });

const codeFormSchema = z.object({
    code: z.string().min(3, "Код должен содержать минимум 3 символа"),
});

const passwordFormSchema = z.object({
    password: z.string().min(1, "Пароль обязателен"),
});

type CreateTelegramAccountForm = z.infer<typeof formSchema>;
type CodeForm = z.infer<typeof codeFormSchema>;
type PasswordForm = z.infer<typeof passwordFormSchema>;

export function TelegramAccountCreateDialog() {
    const [open, setOpen] = useState(false);
    const [step, setStep] = useState<WizardStep>("form");
    const [createdSessionId, setCreatedSessionId] = useState<string | null>(null);
    const [proxyCreateOpen, setProxyCreateOpen] = useState(false);
    const closeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
    const queryClient = useQueryClient();

    const { data: sessionsData } = useGetApiV1TelegramSession();
    const sessions = sessionsData?.items ?? [];
    const defaultCredentialsSource = sessions[0]?.id ?? MANUAL_CREDENTIALS;

    const { data: proxiesData } = useGetApiV1Proxy();
    const proxies = proxiesData?.items ?? [];

    const { data: botsData } = useGetApiV1TelegramBot();
    const bots = botsData?.items ?? [];

    const form = useForm<CreateTelegramAccountForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            credentialsSource: MANUAL_CREDENTIALS,
            apiId: "",
            apiHash: "",
            phoneNumber: "",
            name: "",
            proxyId: null,
            notificationBotId: null,
        },
    });

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

    const credentialsSource = form.watch("credentialsSource");
    const showManualCredentials = sessions.length === 0 || credentialsSource === MANUAL_CREDENTIALS;

    useEffect(() => {
        if (
            open &&
            step === "form" &&
            sessions.length > 0 &&
            !form.getFieldState("credentialsSource").isDirty &&
            form.getValues("credentialsSource") === MANUAL_CREDENTIALS
        ) {
            form.setValue("credentialsSource", sessions[0].id!);
        }
    }, [open, step, sessions, form]);

    useEffect(() => {
        return () => {
            if (closeTimerRef.current) {
                clearTimeout(closeTimerRef.current);
            }
        };
    }, []);

    function scheduleClose() {
        if (closeTimerRef.current) {
            clearTimeout(closeTimerRef.current);
        }
        closeTimerRef.current = setTimeout(() => {
            handleOpenChange(false);
        }, 1500);
    }

    const { mutate: createAccount, isPending: isCreating } = usePostApiV1TelegramSession({
        mutation: {
            onSuccess: (data) => {
                queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
                setCreatedSessionId(data.id ?? null);

                if (data.authStatus === "already_authorized") {
                    toast.success("Telegram аккаунт добавлен и авторизован!");
                    setStep("success");
                    scheduleClose();
                } else if (data.authStatus === "awaiting_password") {
                    toast.info("Требуется пароль двухфакторной аутентификации");
                    setStep("password");
                } else {
                    toast.success("Код подтверждения отправлен в Telegram");
                    setStep("code");
                }
            },
            onError: (error) => {
                toast.error("Ошибка при добавлении Telegram аккаунта", {
                    description: error.title || "Не удалось добавить аккаунт",
                });
            },
        },
    });

    const { mutate: verifyCode, isPending: isVerifying } = usePostApiV1TelegramSessionIdVerifyCode({
        mutation: {
            onSuccess: (data) => {
                if (data.success) {
                    toast.success("Авторизация успешна!");
                    setStep("success");
                    codeForm.reset();
                    queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
                    scheduleClose();
                } else if (data.requiresPassword) {
                    toast.info("Требуется пароль двухфакторной аутентификации");
                    setStep("password");
                } else {
                    toast.error("Ошибка при проверке кода", {
                        description:
                            data.message === "invalid_code"
                                ? "Неверный код подтверждения"
                                : data.message || "Не удалось подтвердить код",
                    });
                }
            },
            onError: (error) => {
                toast.error("Ошибка при проверке кода", {
                    description: error.title || "Неверный код",
                });
            },
        },
    });

    const { mutate: sendPassword, isPending: isSendingPassword } = usePostApiV1TelegramSessionIdSendPassword({
        mutation: {
            onSuccess: (data) => {
                if (data.success) {
                    toast.success("Авторизация успешна!");
                    setStep("success");
                    passwordForm.reset();
                    queryClient.invalidateQueries({ queryKey: getGetApiV1TelegramSessionQueryKey() });
                    scheduleClose();
                } else {
                    toast.error("Неверный пароль", {
                        description: data.message || "Проверьте пароль и попробуйте снова",
                    });
                }
            },
            onError: (error) => {
                toast.error("Ошибка при отправке пароля", {
                    description: error.title || "Неверный пароль",
                });
            },
        },
    });

    function handleOpenChange(newOpen: boolean) {
        if (closeTimerRef.current) {
            clearTimeout(closeTimerRef.current);
            closeTimerRef.current = null;
        }
        setOpen(newOpen);
        setStep("form");
        setCreatedSessionId(null);
        form.reset({
            credentialsSource: defaultCredentialsSource,
            apiId: "",
            apiHash: "",
            phoneNumber: "",
            name: "",
            proxyId: null,
            notificationBotId: null,
        });
        codeForm.reset();
        passwordForm.reset();
    }

    function onSubmit(values: CreateTelegramAccountForm) {
        const isManual = sessions.length === 0 || values.credentialsSource === MANUAL_CREDENTIALS;
        const request: CreateTelegramSessionRequest = {
            apiId: isManual ? values.apiId?.trim() || null : null,
            apiHash: isManual ? values.apiHash?.trim() || null : null,
            credentialsSessionId: isManual ? null : values.credentialsSource,
            phoneNumber: values.phoneNumber,
            name: values.name || null,
            proxyId: values.proxyId || null,
            notificationBotId: values.notificationBotId || null,
        };
        createAccount({ data: request });
    }

    function onCodeSubmit(values: CodeForm) {
        if (!createdSessionId) return;
        verifyCode({
            id: createdSessionId,
            data: { code: values.code },
        });
    }

    function onPasswordSubmit(values: PasswordForm) {
        if (!createdSessionId) return;
        sendPassword({
            id: createdSessionId,
            data: { password: values.password },
        });
    }

    function handleProxyCreated(proxyId: string) {
        queryClient.invalidateQueries({ queryKey: getGetApiV1ProxyQueryKey() });
        form.setValue("proxyId", proxyId);
    }

    return (
        <>
            <Dialog open={open} onOpenChange={handleOpenChange}>
                <DialogTrigger asChild>
                    <Button>
                        <Plus className="h-4 w-4" />
                        Добавить аккаунт
                    </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-[500px]">
                    <DialogHeader>
                        <DialogTitle>
                            {step === "form" && "Добавить Telegram аккаунт"}
                            {step === "code" && "Подтверждение входа"}
                            {step === "password" && "Двухфакторная аутентификация"}
                            {step === "success" && "Аккаунт подключён"}
                        </DialogTitle>
                        <DialogDescription>
                            {step === "form" && "Введите данные для подключения к Telegram аккаунту"}
                            {step === "code" && "Введите код подтверждения, отправленный в Telegram"}
                            {step === "password" && "Введите облачный пароль 2FA от аккаунта"}
                            {step === "success" && "Telegram аккаунт готов к работе"}
                        </DialogDescription>
                    </DialogHeader>

                    {step === "form" && (
                        <Form {...form}>
                            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                                {sessions.length > 0 && (
                                    <FormField
                                        control={form.control}
                                        name="credentialsSource"
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel>API-ключи</FormLabel>
                                                <Select
                                                    onValueChange={field.onChange}
                                                    value={field.value}
                                                >
                                                    <FormControl>
                                                        <SelectTrigger>
                                                            <SelectValue placeholder="Выберите источник API-ключей" />
                                                        </SelectTrigger>
                                                    </FormControl>
                                                    <SelectContent>
                                                        {sessions.map((s) => (
                                                            <SelectItem key={s.id} value={s.id!}>
                                                                {s.name
                                                                    ? `${s.name} (${s.phoneNumber})`
                                                                    : s.phoneNumber}
                                                            </SelectItem>
                                                        ))}
                                                        <SelectItem value={MANUAL_CREDENTIALS}>
                                                            Ввести вручную
                                                        </SelectItem>
                                                    </SelectContent>
                                                </Select>
                                                <FormDescription>
                                                    Можно использовать API ID и API Hash от уже добавленного аккаунта
                                                </FormDescription>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                )}

                                {showManualCredentials && (
                                    <>
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
                                    </>
                                )}

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
                                        onClick={() => handleOpenChange(false)}
                                        disabled={isCreating}
                                    >
                                        Отмена
                                    </Button>
                                    <Button type="submit" disabled={isCreating}>
                                        {isCreating ? "Отправка кода..." : "Продолжить"}
                                    </Button>
                                </DialogFooter>
                            </form>
                        </Form>
                    )}

                    {step === "code" && (
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
                                                    disabled={isVerifying}
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
                                        onClick={() => handleOpenChange(false)}
                                        disabled={isVerifying}
                                    >
                                        Закрыть
                                    </Button>
                                    <Button type="submit" disabled={isVerifying}>
                                        {isVerifying ? "Проверка..." : "Подтвердить"}
                                    </Button>
                                </DialogFooter>
                            </form>
                        </Form>
                    )}

                    {step === "password" && (
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
                                        onClick={() => handleOpenChange(false)}
                                        disabled={isSendingPassword}
                                    >
                                        Закрыть
                                    </Button>
                                    <Button type="submit" disabled={isSendingPassword}>
                                        {isSendingPassword ? "Отправка..." : "Отправить"}
                                    </Button>
                                </DialogFooter>
                            </form>
                        </Form>
                    )}

                    {step === "success" && (
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

            <ProxyCreateDialog
                triggerless
                open={proxyCreateOpen}
                onOpenChange={setProxyCreateOpen}
                onCreated={handleProxyCreated}
            />
        </>
    );
}
