import {useEffect, useMemo, useRef, useState} from "react";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {z} from "zod";
import {useQueryClient} from "@tanstack/react-query";
import {toast} from "sonner";
import {Loader2, RefreshCw} from "lucide-react";
import {
    getGetApiV1ScheduleScheduleIdCrossPostTargetsQueryKey,
    usePostApiV1ScheduleScheduleIdCrossPostTargets,
    usePostApiV1ScheduleScheduleIdCrossPostTargetsPreview,
    usePutApiV1ScheduleScheduleIdCrossPostTargetsId,
} from "@/api/endpoints/cross-post-target/cross-post-target";
import type {
    CrossPostPreviewResponse,
    CrossPostTargetResponse,
    SocialAccountResponse,
} from "@/api/endpoints/tgPosterAPI.schemas";
import {PLATFORM_LABELS} from "@/components/social-account/platform-meta.ts";
import {FORMAT_OPTIONS, LINK_TARGET_OPTIONS} from "@/components/cross-post/cross-post-meta.ts";
import {cn} from "@/lib/utils";
import {Button} from "@/components/ui/button.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Textarea} from "@/components/ui/textarea.tsx";
import {Switch} from "@/components/ui/switch.tsx";
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select.tsx";
import {Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle} from "@/components/ui/dialog.tsx";

const formSchema = z
    .object({
        socialAccountId: z.string().min(1, "Выберите аккаунт"),
        format: z.enum(["Teaser", "Full", "Announcement"]),
        linkTarget: z.enum(["Post", "Channel", "Custom", "None"]),
        customLink: z.string(),
        callToAction: z.string().max(1000, "Не больше 1000 символов"),
        includeMedia: z.boolean(),
        includeParsed: z.boolean(),
        delayMinutes: z.number().int().min(0, "Не меньше 0").max(1440, "Не больше 1440"),
        isActive: z.boolean(),
    })
    .superRefine((values, ctx) => {
        if (values.linkTarget === "Custom") {
            const link = values.customLink.trim();
            if (!link || !/^https?:\/\//i.test(link)) {
                ctx.addIssue({
                    code: z.ZodIssueCode.custom,
                    path: ["customLink"],
                    message: "Нужен полный адрес http(s)://…",
                });
            }
        }

        const tooLongLine = values.callToAction.split("\n").some((line) => line.length > 200);
        if (tooLongLine) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                path: ["callToAction"],
                message: "Каждый вариант — не длиннее 200 символов",
            });
        }
    });

type FormValues = z.infer<typeof formSchema>;

interface CrossPostTargetDialogProps {
    scheduleId: string;
    target?: CrossPostTargetResponse;
    accounts: SocialAccountResponse[];
    open: boolean;
    onOpenChange: (open: boolean) => void;
}

export function CrossPostTargetDialog({scheduleId, target, accounts, open, onOpenChange}: CrossPostTargetDialogProps) {
    const queryClient = useQueryClient();
    const isEdit = !!target;

    const [preview, setPreview] = useState<CrossPostPreviewResponse[]>([]);
    const [previewError, setPreviewError] = useState<string | null>(null);
    const previewTimer = useRef<number | null>(null);

    const defaultValues = useMemo<FormValues>(() => target ? {
        socialAccountId: target.socialAccountId,
        format: target.format,
        linkTarget: target.linkTarget,
        customLink: target.customLink ?? "",
        callToAction: target.callToAction ?? "",
        includeMedia: target.includeMedia,
        includeParsed: target.includeParsed,
        delayMinutes: target.delayMinutes,
        isActive: target.isActive,
    } : {
        socialAccountId: "",
        format: "Teaser",
        linkTarget: "Post",
        customLink: "",
        callToAction: "",
        includeMedia: true,
        includeParsed: false,
        delayMinutes: 0,
        isActive: true,
    }, [target]);

    const form = useForm<FormValues>({
        resolver: zodResolver(formSchema),
        defaultValues,
    });

    const watched = form.watch();

    const {mutate: previewMutate, isPending: previewLoading} = usePostApiV1ScheduleScheduleIdCrossPostTargetsPreview({
        mutation: {
            onSuccess: (data) => {
                setPreview(data);
                setPreviewError(null);
            },
            onError: (error) => {
                setPreview([]);
                setPreviewError(error.title || "Не удалось построить предпросмотр");
            },
        },
    });

    const {mutate: createTarget, isPending: isCreating} = usePostApiV1ScheduleScheduleIdCrossPostTargets({
        mutation: {
            onSuccess: () => {
                toast.success("Сохранено");
                queryClient.invalidateQueries({queryKey: getGetApiV1ScheduleScheduleIdCrossPostTargetsQueryKey(scheduleId)});
                onOpenChange(false);
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title || "Не удалось сохранить настройку"});
            },
        },
    });

    const {mutate: updateTarget, isPending: isUpdating} = usePutApiV1ScheduleScheduleIdCrossPostTargetsId({
        mutation: {
            onSuccess: () => {
                toast.success("Сохранено");
                queryClient.invalidateQueries({queryKey: getGetApiV1ScheduleScheduleIdCrossPostTargetsQueryKey(scheduleId)});
                onOpenChange(false);
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title || "Не удалось сохранить настройку"});
            },
        },
    });

    const isPending = isCreating || isUpdating;

    useEffect(() => {
        if (open) {
            form.reset(defaultValues);
            setPreview([]);
            setPreviewError(null);
        }
    }, [open, defaultValues, form]);

    useEffect(() => {
        if (!open || !watched.socialAccountId) {
            return;
        }

        previewTimer.current = window.setTimeout(() => {
            const values = form.getValues();
            if (!values.socialAccountId) {
                return;
            }
            previewMutate({
                scheduleId,
                data: {
                    socialAccountId: values.socialAccountId,
                    format: values.format,
                    linkTarget: values.linkTarget,
                    customLink: values.customLink.trim() ? values.customLink.trim() : null,
                    callToAction: values.callToAction.trim() ? values.callToAction : null,
                },
            });
        }, 600);

        return () => {
            if (previewTimer.current !== null) {
                window.clearTimeout(previewTimer.current);
                previewTimer.current = null;
            }
        };
    }, [open, scheduleId, watched.socialAccountId, watched.format, watched.linkTarget, watched.customLink, watched.callToAction, form, previewMutate]);

    const runPreview = () => {
        const values = form.getValues();
        if (!values.socialAccountId) {
            setPreview([]);
            setPreviewError(null);
            return;
        }
        previewMutate({
            scheduleId,
            data: {
                socialAccountId: values.socialAccountId,
                format: values.format,
                linkTarget: values.linkTarget,
                customLink: values.customLink.trim() ? values.customLink.trim() : null,
                callToAction: values.callToAction.trim() ? values.callToAction : null,
            },
        });
    };

    const onSubmit = (values: FormValues) => {
        const common = {
            format: values.format,
            linkTarget: values.linkTarget,
            customLink: values.customLink.trim() ? values.customLink.trim() : null,
            callToAction: values.callToAction.trim() ? values.callToAction : null,
            includeMedia: values.includeMedia,
            includeParsed: values.includeParsed,
            delayMinutes: values.delayMinutes,
        };

        if (isEdit && target) {
            updateTarget({
                scheduleId,
                id: target.id,
                data: {...common, isActive: values.isActive},
            });
        } else {
            createTarget({
                scheduleId,
                data: {...common, socialAccountId: values.socialAccountId},
            });
        }
    };

    const handleOpenChange = (isOpen: boolean) => {
        if (!isPending) {
            onOpenChange(isOpen);
        }
    };


    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>{isEdit ? "Настройка кросс-постинга" : "Добавить соцсеть"}</DialogTitle>
                    <DialogDescription>
                        Настройте, как посты этого канала будут публиковаться в соцсети
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        {isEdit && target ? (
                            <div className="space-y-2">
                                <span className="text-sm font-medium">Аккаунт</span>
                                <div className="rounded-md border px-3 py-2 text-sm text-muted-foreground">
                                    {PLATFORM_LABELS[target.platform]} — {target.accountName}
                                </div>
                            </div>
                        ) : (
                            <FormField
                                control={form.control}
                                name="socialAccountId"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Аккаунт соцсети</FormLabel>
                                        <Select value={field.value} onValueChange={field.onChange} disabled={isPending}>
                                            <FormControl>
                                                <SelectTrigger className="w-full">
                                                    <SelectValue placeholder="Выберите аккаунт"/>
                                                </SelectTrigger>
                                            </FormControl>
                                            <SelectContent>
                                                {accounts.map((account) => (
                                                    <SelectItem key={account.id} value={account.id}>
                                                        {PLATFORM_LABELS[account.platform]} — {account.name}
                                                    </SelectItem>
                                                ))}
                                            </SelectContent>
                                        </Select>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                        )}

                        <FormField
                            control={form.control}
                            name="format"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Формат</FormLabel>
                                    <div className="grid gap-2 sm:grid-cols-3">
                                        {FORMAT_OPTIONS.map((option) => (
                                            <button
                                                key={option.value}
                                                type="button"
                                                onClick={() => field.onChange(option.value)}
                                                disabled={isPending}
                                                className={cn(
                                                    "rounded-lg border p-3 text-left transition-colors disabled:opacity-50",
                                                    field.value === option.value
                                                        ? "border-primary bg-primary/5"
                                                        : "hover:bg-muted/50",
                                                )}
                                            >
                                                <div className="text-sm font-medium">{option.title}</div>
                                                <div className="text-xs text-muted-foreground">{option.description}</div>
                                            </button>
                                        ))}
                                    </div>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="linkTarget"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Куда ведёт ссылка</FormLabel>
                                    <Select value={field.value} onValueChange={field.onChange} disabled={isPending}>
                                        <FormControl>
                                            <SelectTrigger className="w-full">
                                                <SelectValue/>
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {LINK_TARGET_OPTIONS.map((option) => (
                                                <SelectItem key={option.value} value={option.value}>
                                                    {option.title}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        {watched.linkTarget === "Custom" && (
                            <FormField
                                control={form.control}
                                name="customLink"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Своя ссылка</FormLabel>
                                        <FormControl>
                                            <Input placeholder="https://t.me/..." {...field} disabled={isPending}/>
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                        )}

                        <FormField
                            control={form.control}
                            name="callToAction"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Призыв</FormLabel>
                                    <FormControl>
                                        <Textarea
                                            rows={3}
                                            placeholder="Читать полностью в Telegram 👇"
                                            {...field}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormDescription>
                                        Можно несколько вариантов — по одному на строку, для каждого поста выбирается
                                        случайный. Пусто — стандартный призыв
                                    </FormDescription>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="includeMedia"
                            render={({field}) => (
                                <FormItem className="flex items-center justify-between rounded-lg border p-3">
                                    <FormLabel>Прикладывать картинки</FormLabel>
                                    <FormControl>
                                        <Switch checked={field.value} onCheckedChange={field.onChange} disabled={isPending}/>
                                    </FormControl>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="includeParsed"
                            render={({field}) => (
                                <FormItem className="flex items-center justify-between rounded-lg border p-3">
                                    <div>
                                        <FormLabel>Кросс-постить посты из парсера каналов</FormLabel>
                                        <FormDescription>
                                            Чужой контент может привести к блокировке аккаунта в соцсети
                                        </FormDescription>
                                    </div>
                                    <FormControl>
                                        <Switch checked={field.value} onCheckedChange={field.onChange} disabled={isPending}/>
                                    </FormControl>
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="delayMinutes"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Задержка после публикации в Telegram, мин</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="number"
                                            min={0}
                                            max={1440}
                                            value={field.value}
                                            onChange={(event) => field.onChange(event.target.value === "" ? 0 : Number(event.target.value))}
                                            disabled={isPending}
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        {isEdit && (
                            <FormField
                                control={form.control}
                                name="isActive"
                                render={({field}) => (
                                    <FormItem className="flex items-center justify-between rounded-lg border p-3">
                                        <FormLabel>Связка активна</FormLabel>
                                        <FormControl>
                                            <Switch checked={field.value} onCheckedChange={field.onChange} disabled={isPending}/>
                                        </FormControl>
                                    </FormItem>
                                )}
                            />
                        )}

                        <div className="space-y-3 rounded-lg border p-4">
                            <div className="flex items-center justify-between gap-2">
                                <span className="text-sm font-medium">Предпросмотр</span>
                                <Button
                                    type="button"
                                    variant="outline"
                                    size="sm"
                                    onClick={runPreview}
                                    disabled={previewLoading || !watched.socialAccountId}
                                >
                                    {previewLoading ? (
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                    ) : (
                                        <RefreshCw className="mr-2 h-4 w-4"/>
                                    )}
                                    Обновить предпросмотр
                                </Button>
                            </div>

                            {previewError && <p className="text-sm text-destructive">{previewError}</p>}

                            {!previewError && preview.length === 0 && (
                                <p className="text-sm text-muted-foreground">
                                    {watched.socialAccountId
                                        ? "Предпросмотр обновится автоматически"
                                        : "Выберите аккаунт, чтобы увидеть предпросмотр"}
                                </p>
                            )}

                            <div className="space-y-4">
                                {preview.map((item) => (
                                    <div key={item.socialAccountId} className="space-y-2">
                                        <div className="text-sm font-medium">
                                            {PLATFORM_LABELS[item.platform]} — {item.accountName}
                                        </div>
                                        {item.parts.map((part, index) => (
                                            <div key={index} className="rounded-md bg-muted/40 p-2">
                                                <div className="mb-1 flex items-center justify-between text-xs">
                                                    <span className="text-muted-foreground">
                                                        {item.parts.length > 1 ? `${index + 1}/${item.parts.length}` : "Пост"}
                                                    </span>
                                                    <span
                                                        className={part.length > part.limit ? "font-medium text-destructive" : "text-muted-foreground"}>
                                                        {part.length}/{part.limit}
                                                    </span>
                                                </div>
                                                <pre
                                                    className="whitespace-pre-wrap break-words font-sans text-sm">{part.text}</pre>
                                            </div>
                                        ))}
                                        {item.warnings.length > 0 && (
                                            <ul className="list-disc pl-5 text-xs text-amber-600">
                                                {item.warnings.map((warning, index) => (
                                                    <li key={index}>{warning}</li>
                                                ))}
                                            </ul>
                                        )}
                                    </div>
                                ))}
                            </div>
                        </div>

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
                                {isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        Сохранение...
                                    </>
                                ) : (
                                    "Сохранить"
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}
