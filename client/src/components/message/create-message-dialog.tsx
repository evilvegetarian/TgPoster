import {useState} from "react";
import {Loader2, Plus} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger} from "@/components/ui/dialog";
import {Textarea} from "@/components/ui/textarea";
import {Input} from "@/components/ui/input";
import {toast} from "sonner";
import {usePostApiV1Message} from "@/api/endpoints/message/message.ts";
import {z} from "zod";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form";
import {utcToLocalString} from "@/utils/convertLocalToIsoTime.tsx";
import {TimeSuggestions} from "@/components/message/time-suggestions.tsx";
import {MediaUploader} from "@/components/message/media-uploader.tsx";

const formSchema = z.object({
    scheduleId: z.string().min(1),
    timePosting: z.string({ required_error: "Выберите время публикации" }),
    textMessage: z.string().optional(),
    files: z.array(z.instanceof(File))
});

type CreateMessageFormValues = z.infer<typeof formSchema>;

interface CreateMessageDialogProps {
    scheduleId: string;
    availableTimes?: string[] | null;
    onTimeSelect: (time: string) => void;
    onSuccess?: () => void;
}

export function CreateMessageDialog({ scheduleId, availableTimes, onTimeSelect, onSuccess }: CreateMessageDialogProps) {
    const [open, setOpen] = useState(false);

    const form = useForm<CreateMessageFormValues>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            scheduleId: scheduleId,
            textMessage: "",
            timePosting: "",
            files: []
        }
    });

    const createMessage = usePostApiV1Message({
        mutation: {
            onSuccess: () => {
                toast.success("Успешно", { description: "Сообщение поставлено в очередь" });
                setOpen(false);
                form.reset();
                onSuccess?.();
            },
            onError: (error) => {
                toast.error("Ошибка", { description: error.title || "Не удалось создать сообщение" });
            },
        },
    });

    const onSubmit = (values: CreateMessageFormValues) => {
        const utcTimeForServer = new Date(values.timePosting).toISOString();
        createMessage.mutate({
            data: {
                ScheduleId: values.scheduleId,
                TimePosting: utcTimeForServer,
                TextMessage: values.textMessage || undefined,
                Files: values.files.length > 0 ? values.files : undefined,
            },
        });
    };

    const handleTimeSuggestionClick = (time: string) => {
        form.setValue('timePosting', utcToLocalString(time), { shouldValidate: true, shouldDirty: true });
        onTimeSelect(time);
    };

    const handleFilesAdded = (newFiles: File[]) => {
        const currentFiles = form.getValues('files');
        form.setValue('files', [...currentFiles, ...newFiles], { shouldValidate: true });
    };

    const handleFileRemove = (index: number) => {
        const currentFiles = form.getValues('files');
        form.setValue('files', currentFiles.filter((_, i) => i !== index));
    };

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button>
                    <Plus className="h-4 w-4 mr-2" />
                    Создать сообщение
                </Button>
            </DialogTrigger>

            <DialogContent className="max-w-2xl sm:max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Создание нового сообщения</DialogTitle>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 py-2">

                        <FormField
                            control={form.control}
                            name="textMessage"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Текст сообщения</FormLabel>
                                    <FormControl>
                                        <Textarea
                                            placeholder="Введите текст поста..."
                                            className="resize-y min-h-[120px]"
                                            {...field}
                                        />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <div className="space-y-3">
                            <FormField
                                control={form.control}
                                name="timePosting"
                                render={({ field }) => (
                                    <FormItem className="flex flex-col">
                                        <FormLabel>Время публикации</FormLabel>
                                        <FormControl>
                                            <Input
                                                id="time-create"
                                                type="datetime-local"
                                                {...field}
                                                className="w-[240px]"
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            <TimeSuggestions
                                availableTimes={availableTimes}
                                onSelect={handleTimeSuggestionClick}
                            />
                        </div>

                        <MediaUploader
                            files={form.watch('files')}
                            onFilesAdded={handleFilesAdded}
                            onFileRemove={handleFileRemove}
                            disabled={createMessage.isPending}
                        />

                        <DialogFooter>
                            <Button type="button" variant="outline" onClick={() => setOpen(false)}>
                                Отмена
                            </Button>
                            <Button type="submit" disabled={createMessage.isPending}>
                                {createMessage.isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                        Создание...
                                    </>
                                ) : (
                                    "Создать"
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}