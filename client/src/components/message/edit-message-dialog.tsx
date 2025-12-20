import React, {useState} from "react"
import {Edit, Upload, X, Sparkles, Loader2} from "lucide-react" // Добавил иконки для красоты
import {Button} from "@/components/ui/button"
import {Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter} from "@/components/ui/dialog"
import {Input} from "@/components/ui/input"
import {Label} from "@/components/ui/label"
import {FilePreview} from "./file-preview"
import type {MessageResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {useGetApiV1MessageMessageIdAiContent, usePutApiV1MessageId} from "@/api/endpoints/message/message.ts";
import {toast} from "sonner";
import {Badge} from "@/components/ui/badge.tsx";
import {z} from "zod";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {utcToLocalString, utcToShortLocalString} from "@/utils/convertLocalToIsoTime.tsx";
import {Textarea} from "@/components/ui/textarea.tsx";

const formSchema = z.object({
    scheduleId: z.string().min(1, "Необходимо расписание"),
    timePosting: z.string({required_error: "Выберите дату"}),
    textMessage: z.string().max(4096).nullable().optional(),
    oldFiles: z.array(z.string()),
    newFiles: z.array(z.instanceof(File))
});

type EditMessageFormValues = z.infer<typeof formSchema>;

interface EditMessageDialogProps {
    message: MessageResponse,
    availableTimes?: string[] | null,
    onTimeSelect: (time: string) => void;
    onSuccess?: () => void;
}

export function EditMessageDialog({message, availableTimes, onTimeSelect, onSuccess}: EditMessageDialogProps) {
    const [open, setOpen] = useState(false); // 1. Контролируемый диалог

    const form = useForm<EditMessageFormValues>({
        resolver: zodResolver(formSchema),
        values: {
            textMessage: message?.textMessage ?? "",
            timePosting: utcToLocalString(message?.timePosting),
            scheduleId: message?.scheduleId,
            oldFiles: message.files?.map(x => x.id) ?? [],
            newFiles: []
        }
    });

    const {
        refetch: aiContentRefetch,
        isFetching: aiContentIsFetching
    } = useGetApiV1MessageMessageIdAiContent(
        message.id,
        {query: {enabled: false}}
    );

    const {mutate: updateMutate, isPending: updateIsPending} = usePutApiV1MessageId({
        mutation: {
            onSuccess: () => {
                toast.success("Сохранено", {description: "Сообщение успешно обновлено"});
                setOpen(false);
                form.reset();
                onSuccess?.();
            },
            onError: (err) => {
                toast.error("Ошибка", {description: err.title ?? "Не удалось обновить сообщение"});
            },
        },
    });

    const watchedOldFiles = form.watch('oldFiles');
    const watchedNewFiles = form.watch('newFiles');

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files?.length) {
            const currentFiles = form.getValues('newFiles');
            // Можно добавить проверку на дубликаты по имени/размеру здесь
            form.setValue('newFiles', [...currentFiles, ...Array.from(e.target.files)]);
        }
    };

    const removeNewFile = (index: number) => {
        const currentFiles = form.getValues('newFiles');
        form.setValue('newFiles', currentFiles.filter((_, i) => i !== index));
    };

    const removeOldFile = (fileId: string) => {
        const currentFiles = form.getValues('oldFiles');
        form.setValue('oldFiles', currentFiles.filter((id) => id !== fileId));
    };

    const handleTimeSuggestionClick = (time: string) => {
        form.setValue('timePosting', utcToLocalString(time), {shouldValidate: true, shouldDirty: true});
        onTimeSelect(time);
    };

    const handleAIContent = async () => {
        const {data} = await aiContentRefetch();
        if (data?.content) {
            form.setValue('textMessage', data.content, {shouldValidate: true, shouldDirty: true});
            toast.info("Текст сгенерирован");
        } else {
            toast.warning("AI не вернул контент");
        }
    };

    const onSubmit = (values: EditMessageFormValues) => {
        updateMutate({
            id: message.id,
            data: {
                ScheduleId: values.scheduleId,
                TimePosting: new Date(values.timePosting).toISOString(),
                TextMessage: values.textMessage || undefined,
                OldFiles: values.oldFiles,
                NewFiles: values.newFiles
            }
        });
    };

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button variant="ghost" size="sm">
                    <Edit className="h-4 w-4 mr-1"/>
                    Редактировать
                </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl sm:max-h-[85vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Редактирование сообщения</DialogTitle>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 py-2">

                        {/* Блок текста и AI */}
                        <div className="space-y-2">
                            <FormField
                                control={form.control}
                                name="textMessage"
                                render={({field}) => (
                                    <FormItem>
                                        <div className="flex justify-between items-center mb-1">
                                            <FormLabel>Сообщение</FormLabel>
                                            {/* 4. Button вместо Badge для интерактивности */}
                                            <Button
                                                variant="outline"
                                                type="button"
                                                className="h-6 text-xs gap-1 border-violet-200 hover:bg-violet-50 text-violet-700"
                                                onClick={handleAIContent}
                                                disabled={aiContentIsFetching}
                                            >
                                                {aiContentIsFetching ? <Loader2 className="h-3 w-3 animate-spin"/> :
                                                    <Sparkles className="h-3 w-3"/>}
                                                {aiContentIsFetching ? 'Генерация...' : 'AI Rewrite'}
                                            </Button>
                                        </div>
                                        <FormControl>
                                            <Textarea
                                                placeholder="Введите текст поста..."
                                                className="min-h-[120px] resize-y"
                                                {...field}
                                                value={field.value?.toString() ?? ''}
                                            />
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                        </div>

                        <div className="space-y-3">
                            <FormField
                                control={form.control}
                                name="timePosting"
                                render={({field}) => (
                                    <FormItem className="flex flex-col">
                                        <FormLabel>Время публикации</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="datetime-local"
                                                {...field}
                                                className="w-[240px]"
                                            />
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />

                            {availableTimes && availableTimes.length > 0 && (
                                <div
                                    className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground p-3 bg-muted/40 rounded-lg border border-dashed">
                                    <span>Свободные слоты:</span>
                                    {availableTimes.slice(0, 4).map((time) => (
                                        <Badge
                                            key={time}
                                            variant="secondary"
                                            className="cursor-pointer hover:bg-primary hover:text-primary-foreground transition-colors"
                                            onClick={() => handleTimeSuggestionClick(time)}
                                        >
                                            {utcToShortLocalString(time)}
                                        </Badge>
                                    ))}
                                </div>
                            )}
                        </div>

                        <div className="space-y-3">
                            <Label>Медиафайлы</Label>

                            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                                {message.files?.filter(f => watchedOldFiles.includes(f.id)).map((file) => (
                                    <FilePreview
                                        key={file.id}
                                        file={file}
                                        showRemoveButton
                                        onRemove={() => removeOldFile(file.id)}
                                    />
                                ))}

                                {watchedNewFiles.map((file, index) => (
                                    <div key={`new-${index}`} className="relative group aspect-square">
                                        <div
                                            className="w-full h-full bg-muted rounded-lg border flex flex-col items-center justify-center overflow-hidden p-2 text-center">
                                            {file.type.startsWith('image/') ? (
                                                <img
                                                    src={URL.createObjectURL(file)}
                                                    alt="preview"
                                                    className="w-full h-full object-cover"
                                                    onLoad={(e) => URL.revokeObjectURL(e.currentTarget.src)} // Память!
                                                />
                                            ) : (
                                                <span
                                                    className="text-xs text-muted-foreground break-all line-clamp-3">{file.name}</span>
                                            )}
                                        </div>
                                        <Button
                                            type="button"
                                            variant="destructive"
                                            size="icon"
                                            className="absolute -top-2 -right-2 h-6 w-6 rounded-full shadow-md opacity-0 group-hover:opacity-100 transition-opacity"
                                            onClick={() => removeNewFile(index)}
                                        >
                                            <X className="h-3 w-3"/>
                                        </Button>
                                    </div>
                                ))}

                                <label
                                    className="cursor-pointer aspect-square border-2 border-dashed border-muted-foreground/25 rounded-lg flex flex-col items-center justify-center hover:border-primary/50 hover:bg-muted/50 transition-all">
                                    <Upload className="h-6 w-6 mb-2 text-muted-foreground"/>
                                    <span className="text-xs text-muted-foreground">Добавить</span>
                                    <Input
                                        type="file"
                                        multiple
                                        accept="image/*,video/*"
                                        onChange={handleFileChange}
                                        className="hidden"
                                        onClick={(e) => (e.currentTarget.value = "")}
                                    />
                                </label>
                            </div>
                        </div>

                        <DialogFooter>
                            <Button type="button" variant="outline" onClick={() => setOpen(false)}>
                                Отмена
                            </Button>
                            <Button type="submit" disabled={updateIsPending}>
                                {updateIsPending && <Loader2 className="mr-2 h-4 w-4 animate-spin"/>}
                                Сохранить изменения
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}