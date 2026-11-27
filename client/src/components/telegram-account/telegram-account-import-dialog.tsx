import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Upload, FileUp } from "lucide-react";
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
    getGetApiV1TelegramSessionQueryKey,
    useGetApiV1TelegramSession,
    usePostApiV1TelegramSessionImport,
} from "@/api/endpoints/telegram-session/telegram-session";
import type { PostApiV1TelegramSessionImportBody } from "@/api/endpoints/tgPosterAPI.schemas";
import { useQueryClient } from "@tanstack/react-query";
import { useGetApiV1TelegramBot } from "@/api/endpoints/telegram-bot/telegram-bot";

const MANUAL_CREDENTIALS = "__manual__";

const formSchema = z
    .object({
        credentialsSource: z.string(),
        apiId: z.string().optional(),
        apiHash: z.string().optional(),
        sessionFile: z
            .instanceof(File, { message: "Файл сессии обязателен" })
            .refine((file) => file.size > 0, "Файл не должен быть пустым"),
        name: z.string().optional(),
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

type ImportTelegramAccountForm = z.infer<typeof formSchema>;

export function TelegramAccountImportDialog() {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();
    const fileInputRef = useRef<HTMLInputElement>(null);

    const { data: sessionsData } = useGetApiV1TelegramSession();
    const sessions = sessionsData?.items ?? [];
    const defaultCredentialsSource = sessions[0]?.id ?? MANUAL_CREDENTIALS;

    const { data: botsData } = useGetApiV1TelegramBot();
    const bots = botsData?.items ?? [];

    const form = useForm<ImportTelegramAccountForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            credentialsSource: MANUAL_CREDENTIALS,
            apiId: "",
            apiHash: "",
            sessionFile: undefined,
            name: "",
            notificationBotId: null,
        },
    });

    const credentialsSource = form.watch("credentialsSource");
    const showManualCredentials = sessions.length === 0 || credentialsSource === MANUAL_CREDENTIALS;

    useEffect(() => {
        if (
            open &&
            sessions.length > 0 &&
            !form.getFieldState("credentialsSource").isDirty &&
            form.getValues("credentialsSource") === MANUAL_CREDENTIALS
        ) {
            form.setValue("credentialsSource", sessions[0].id!);
        }
    }, [open, sessions, form]);

    const { mutate: importSession, isPending } =
        usePostApiV1TelegramSessionImport({
            mutation: {
                onSuccess: (data) => {
                    toast.success("Telegram сессия успешно импортирована!", {
                        description: `Телефон: ${data.phoneNumber}`,
                    });
                    handleOpenChange(false);
                    queryClient.invalidateQueries({
                        queryKey: getGetApiV1TelegramSessionQueryKey(),
                    });
                },
                onError: (error) => {
                    toast.error(
                        "Ошибка при импорте Telegram сессии",
                        {
                            description:
                                error.title ||
                                "Не удалось импортировать сессию",
                        }
                    );
                },
            },
        });

    function handleOpenChange(newOpen: boolean) {
        setOpen(newOpen);
        form.reset({
            credentialsSource: defaultCredentialsSource,
            apiId: "",
            apiHash: "",
            sessionFile: undefined,
            name: "",
            notificationBotId: null,
        });
        if (fileInputRef.current) {
            fileInputRef.current.value = "";
        }
    }

    function onSubmit(values: ImportTelegramAccountForm) {
        const isManual = sessions.length === 0 || values.credentialsSource === MANUAL_CREDENTIALS;
        const request: PostApiV1TelegramSessionImportBody = {
            ApiId: isManual ? values.apiId?.trim() : undefined,
            ApiHash: isManual ? values.apiHash?.trim() : undefined,
            CredentialsSessionId: isManual ? undefined : values.credentialsSource,
            SessionFile: values.sessionFile,
            Name: values.name || undefined,
            NotificationBotId: values.notificationBotId || undefined,
        };
        importSession({ data: request });
    }

    const selectedFile = form.watch("sessionFile");

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogTrigger asChild>
                <Button variant="outline">
                    <Upload className="h-4 w-4" />
                    Импорт из файла
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Импорт Telegram сессии</DialogTitle>
                    <DialogDescription>
                        Загрузите .session файл WTelegram для быстрого
                        подключения аккаунта
                    </DialogDescription>
                </DialogHeader>
                <Form {...form}>
                    <form
                        onSubmit={form.handleSubmit(onSubmit)}
                        className="space-y-4"
                    >
                        <FormField
                            control={form.control}
                            name="sessionFile"
                            render={({ field: { onChange } }) => (
                                <FormItem>
                                    <FormLabel>Файл сессии (.session)</FormLabel>
                                    <FormControl>
                                        <div
                                            className="border-2 border-dashed rounded-lg p-6 text-center cursor-pointer hover:border-primary transition-colors"
                                            onClick={() =>
                                                fileInputRef.current?.click()
                                            }
                                        >
                                            <input
                                                ref={fileInputRef}
                                                type="file"
                                                accept=".session"
                                                className="hidden"
                                                onChange={(e) => {
                                                    const file =
                                                        e.target.files?.[0];
                                                    if (file) {
                                                        onChange(file);
                                                    }
                                                }}
                                            />
                                            <FileUp className="h-8 w-8 mx-auto mb-2 text-muted-foreground" />
                                            {selectedFile ? (
                                                <p className="text-sm font-medium">
                                                    {selectedFile.name}
                                                    <span className="text-muted-foreground ml-2">
                                                        (
                                                        {(
                                                            selectedFile.size /
                                                            1024
                                                        ).toFixed(1)}{" "}
                                                        KB)
                                                    </span>
                                                </p>
                                            ) : (
                                                <p className="text-sm text-muted-foreground">
                                                    Нажмите для выбора файла или
                                                    перетащите сюда
                                                </p>
                                            )}
                                        </div>
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

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
                                                <Input
                                                    placeholder="123456"
                                                    {...field}
                                                />
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
                                                <Input
                                                    placeholder="abcdef123456..."
                                                    {...field}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </>
                        )}

                        <FormField
                            control={form.control}
                            name="name"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>
                                        Название (опционально)
                                    </FormLabel>
                                    <FormControl>
                                        <Input
                                            placeholder="Мой аккаунт"
                                            {...field}
                                        />
                                    </FormControl>
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
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button type="submit" disabled={isPending}>
                                {isPending ? "Импорт..." : "Импортировать"}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
