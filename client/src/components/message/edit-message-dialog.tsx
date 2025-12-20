import React from "react"
import {Edit, Upload, X} from "lucide-react"
import {Button} from "@/components/ui/button"
import {Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger} from "@/components/ui/dialog"
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

interface EditMessageDialogProps {
    message: MessageResponse,
    availableTimes?: string[] | null,
    onTimeSelect: (time: string) => void;
}

const formSchema = z.object({
    scheduleId: z.string({required_error: "Необходимо расписание.",}).min(1, "Необходимо расписание."),
    timePosting: z.string({required_error: "Необходимо выбрать дату.",}),
    textMessage: z.string().max(4096, "Текст должен быть не более 4096 символов").nullable().optional(),
    oldFiles: z.array(z.string()),
    newFiles: z.array(z.instanceof(File))
});

type EditMessageFormValues = z.infer<typeof formSchema>;

export function EditMessageDialog({message, availableTimes, onTimeSelect}: EditMessageDialogProps) {
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
        data: aiContentData,
        refetch: aiContentRefetch,
        isFetching: aiContentIsFetching
    } = useGetApiV1MessageMessageIdAiContent(
        message.id,
        {query: {enabled: false}}
    );

    const watchedOldFiles = form.watch('oldFiles');
    const watchedNewFiles = form.watch('newFiles');

    const {mutate: updateMutate, isPending: updateIsPending} = usePutApiV1MessageId({
        mutation: {
            onSuccess: () => {
                toast.success("Успех", {description: "Сообщение обновлено успешно"})
                form.reset();
            },
            onError: (err) => {
                toast.error("Ошибка", {description: err.title ?? "Не удалось обновить сообщение"})
            },
        },
    })

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files) {
            const currentFiles = form.getValues('newFiles');
            const addedFiles = Array.from(e.target.files);
            form.setValue('newFiles', [...currentFiles, ...addedFiles]);
        }
    }

    const removeOldFile = (fileId: string) => {
        const currentFiles = form.getValues('oldFiles');
        form.setValue('oldFiles', currentFiles.filter((id) => id !== fileId), {shouldDirty: true});
    }

    const removeNewFile = (index: number) => {
        const currentFiles = form.getValues('newFiles');
        form.setValue('newFiles', currentFiles.filter((_, i) => i !== index), {shouldDirty: true});
    }

    const handleTimeSuggestionClick = (time: string) => {
        form.setValue('timePosting', utcToLocalString(time), {shouldValidate: true});
        onTimeSelect(time);
    }

    const handleAIContent = async () => {
        await aiContentRefetch();
        if (aiContentData?.content) {
            form.setValue('textMessage', aiContentData.content, {shouldValidate: true});
        }
    };

    function onSubmit(values: EditMessageFormValues) {
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
    }

    return (
        <Dialog >
            <DialogTrigger asChild>
                <Button variant="ghost" size="sm">
                    <Edit className="h-4 w-4 mr-1"/>
                    Редактировать
                </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
                <DialogHeader>
                    <DialogTitle>Редактирование сообщения</DialogTitle>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 py-4">
                        <FormField
                            control={form.control}
                            name="textMessage"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Текстовое сообщение</FormLabel>
                                    <FormControl>
                                        <Textarea
                                            placeholder="Напишите ваше сообщение здесь..."
                                            className="resize-none"
                                            {...field}
                                            value={field.value?.toString()}
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />
                        <div>
                            <Badge onClick={handleAIContent} aria-disabled={aiContentIsFetching}
                                   className="cursor-pointer">
                                {aiContentIsFetching ? 'Генерация...' : 'Сгенерировать'}
                            </Badge>
                        </div>
                        <FormField
                            control={form.control}
                            name="timePosting"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Время публикации (местное время)</FormLabel>
                                    <FormControl>
                                        <Input
                                            id="time"
                                            type="datetime-local"
                                            {...field}
                                            required
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />
                        {availableTimes && availableTimes.length > 0 && (
                            <div className="space-y-2 rounded-md border p-3">
                                <Label>Свободное время по расписанию:</Label>
                                <div className="flex flex-wrap gap-2">
                                    {availableTimes.slice(0, 4).map((time) => (
                                        <Badge
                                            key={time}
                                            className="cursor-pointer"
                                            onClick={() => handleTimeSuggestionClick(time)}
                                        >
                                            {utcToShortLocalString(time)}
                                        </Badge>
                                    ))}
                                </div>
                            </div>
                        )}
                        <div className="space-y-2">
                            <Label>Файлы</Label>
                            {message.files && message.files.length > 0 && watchedOldFiles.length > 0 && (
                                <div className="space-y-2">
                                    <p className="text-sm text-muted-foreground">Существующие файлы:</p>
                                    <div className="grid grid-cols-4 gap-2">
                                        {message.files
                                            .filter((file) => watchedOldFiles.includes(file.id))
                                            .map((file) => (
                                                <FilePreview key={file.id} file={file} showRemoveButton
                                                             onRemove={() => removeOldFile(file.id)}/>
                                            ))}
                                    </div>
                                </div>
                            )}

                            {watchedNewFiles.length > 0 && (
                                <div className="space-y-2">
                                    <p className="text-sm text-muted-foreground">Новые файлы:</p>
                                    <div className="grid grid-cols-4 gap-2">
                                        {watchedNewFiles.map((file, index) => (
                                            <div key={index} className="relative group">
                                                <div
                                                    className="w-20 h-20 bg-muted rounded-lg flex items-center justify-center overflow-hidden">
                                                    <span
                                                        className="text-xs text-center p-1 break-all">{file.name}</span>
                                                </div>
                                                <Button
                                                    type="button"
                                                    variant="destructive"
                                                    size="icon"
                                                    className="absolute -top-2 -right-2 h-6 w-6 rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
                                                    onClick={() => removeNewFile(index)}
                                                >
                                                    <X className="h-3 w-3"/>
                                                </Button>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}

                            <div>
                                <Input
                                    type="file"
                                    multiple
                                    accept="image/*,video/*"
                                    onChange={handleFileChange}
                                    className="hidden"
                                    id="file-upload-edit"
                                    onClick={(e) => (e.currentTarget.value = "")}
                                />
                                <Label htmlFor="file-upload-edit" className="cursor-pointer">
                                    <div
                                        className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-4 text-center hover:border-muted-foreground/50 transition-colors">
                                        <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground"/>
                                        <p className="text-sm text-muted-foreground">Добавить новые файлы</p>
                                    </div>
                                </Label>
                            </div>
                        </div>

                        <div className="flex justify-end gap-2">
                            <Button type="button" variant="outline">
                                Отмена
                            </Button>
                            <Button type="submit" disabled={updateIsPending}>
                                {updateIsPending ? "Сохранение..." : "Сохранить"}
                            </Button>
                        </div>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}