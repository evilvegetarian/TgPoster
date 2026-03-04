import { useState, useRef } from "react";
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
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { getGetApiV1TelegramSessionQueryKey } from "@/api/endpoints/telegram-session/telegram-session";
import { usePostApiV1TelegramSessionImport } from "@/api/endpoints/telegram-session/import-telegram-session";
import type { ImportTelegramSessionRequest } from "@/api/endpoints/telegram-session/import-telegram-session";
import { useQueryClient } from "@tanstack/react-query";

const formSchema = z.object({
    apiId: z.string().min(1, "API ID обязателен"),
    apiHash: z.string().min(1, "API Hash обязателен"),
    sessionFile: z
        .instanceof(File, { message: "Файл сессии обязателен" })
        .refine((file) => file.size > 0, "Файл не должен быть пустым"),
    name: z.string().optional(),
});

type ImportTelegramAccountForm = z.infer<typeof formSchema>;

export function TelegramAccountImportDialog() {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();
    const fileInputRef = useRef<HTMLInputElement>(null);

    const form = useForm<ImportTelegramAccountForm>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            apiId: "",
            apiHash: "",
            sessionFile: undefined,
            name: "",
        },
    });

    const { mutate: importSession, isPending } =
        usePostApiV1TelegramSessionImport({
            mutation: {
                onSuccess: (data) => {
                    toast.success("Telegram сессия успешно импортирована!", {
                        description: `Телефон: ${data.phoneNumber}`,
                    });
                    form.reset();
                    if (fileInputRef.current) {
                        fileInputRef.current.value = "";
                    }
                    setOpen(false);
                    queryClient.invalidateQueries({
                        queryKey: getGetApiV1TelegramSessionQueryKey(),
                    });
                },
                onError: (error) => {
                    toast.error(
                        "Ошибка при импорте Telegram сессии",
                        {
                            description:
                                (error as { title?: string })?.title ||
                                "Не удалось импортировать сессию",
                        }
                    );
                },
            },
        });

    function onSubmit(values: ImportTelegramAccountForm) {
        const request: ImportTelegramSessionRequest = {
            apiId: values.apiId,
            apiHash: values.apiHash,
            sessionFile: values.sessionFile,
            name: values.name || null,
        };
        importSession({ data: request });
    }

    const selectedFile = form.watch("sessionFile");

    return (
        <Dialog open={open} onOpenChange={setOpen}>
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
                                {isPending ? "Импорт..." : "Импортировать"}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
