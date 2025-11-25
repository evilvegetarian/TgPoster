import {useEffect, useState} from "react";
import {
    useGetApiV1OpenRouterSetting,
    usePatchApiV1OpenRouterSettingIdScheduleScheduleId
} from "@/api/endpoints/open-router-setting/open-router-setting.ts";
import {useGetApiV1PromptSettingId, usePostApiV1PromptSetting} from "@/api/endpoints/prompt-setting/prompt-setting.ts";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {toast} from "sonner";
import type {CreatePromptSettingRequest} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {Dialog, DialogContent, DialogFooter, DialogTitle, DialogTrigger} from "@/components/ui/dialog.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select.tsx";
import {Input} from "@/components/ui/input.tsx";
import {Loader2, Plus} from "lucide-react";
import z from "zod";

const formSchema = z.object({
    video: z.string().min(2, "Название модели слишком короткое"),
    photo: z.string().min(2, "Название модели слишком короткое"),
    text: z.string().min(2, "Название модели слишком короткое"),
    scheduleId: z.string().min(2, "Название модели слишком короткое"),
});

type FormValues = z.infer<typeof formSchema>;

export function EditIiComponent({
                                    scheduleId,
                                    openRouterId,
                                    promptId
                                }: {
    scheduleId: string;
    openRouterId: string | null | undefined;
    promptId: string | null | undefined;
}) {
    const [open, setOpen] = useState(false);

    // 1. Состояние для выбранного роутера, инициализируем пропсом
    const [newOpenRouter, setNewOpenRouter] = useState<string | undefined>(
        openRouterId || undefined
    );

    // 2. Получаем список роутеров
    const {data: openrouters, isLoading: isloadingopenrouters} = useGetApiV1OpenRouterSetting();

    // 3. ИСПРАВЛЕНИЕ: Хук всегда должен вызываться на верхнем уровне.
    // Используем опцию query: {enabled: ... }, если это Orval/TanStack Query,
    // либо просто передаем null/пустую строку, если библиотека это обрабатывает.
    const {data: promptData, isLoading: isloadingprompt} = useGetApiV1PromptSettingId(
        promptId || "",
        {query: {enabled: !!promptId}} // Важно: отключаем запрос, если нет ID
    );

    const form = useForm<FormValues>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            scheduleId: scheduleId,
            text: "",  // Начальные пустые значения
            photo: "",
            video: ""
        }
    });

    // 4. РЕШЕНИЕ ЗАДАЧИ: Как только пришли данные promptData, обновляем форму
    useEffect(() => {
        if (promptData && open) {
            form.reset({
                scheduleId: scheduleId, // Сохраняем scheduleId
                text: promptData.textPrompt || "", // Маппинг данных с бэка на поля формы
                photo: promptData.picturePrompt || "",
                video: promptData.videoPrompt || ""
            });
        }
        // Если promptId нет (режим создания), можно сбросить форму к пустым значениям
        else if (!promptId && open) {
            form.reset({
                scheduleId: scheduleId,
                text: "",
                photo: "",
                video: ""
            });
        }
    }, [promptData, form, scheduleId, open, promptId]);

    // Обновляем стейт селекта, если изменился пропс openRouterId (например, при открытии другого элемента)
    useEffect(() => {
        setNewOpenRouter(openRouterId || undefined);
    }, [openRouterId, open]);


    const {mutate: mutatePatch} = usePatchApiV1OpenRouterSettingIdScheduleScheduleId({
        mutation: {
            onSuccess: () => setOpen(false),
            onError: (error) => {
                toast.error("Ошибка", {description: error.title || "Ошибка настройки"});
            }
        }
    });

    const {mutate, isPending} = usePostApiV1PromptSetting({
        mutation: {
            onSuccess: () => {
                toast.success(`Настройки успешно добавлены!`);
                form.reset();
                setOpen(false);
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title || "Ошибка сохранения"});
            }
        }
    });

    function onSubmit(value: FormValues) {
        const appData: CreatePromptSettingRequest = {
            textPrompt: value.text,
            photoPrompt: value.photo,
            videoPrompt: value.video,
            scheduleId: value.scheduleId,
        };

        // Логика: если это редактирование, возможно нужен другой endpoint (PUT/PATCH)
        // но пока оставляем как в оригинале mutate (POST)
        mutate({data: appData});

        if (newOpenRouter && newOpenRouter !== openRouterId) {
            mutatePatch({id: newOpenRouter, scheduleId: value.scheduleId});
        }
    }

    const isLoadingData = isloadingprompt || isloadingopenrouters;

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button className="gap-2">AI</Button>
            </DialogTrigger>
            <DialogContent>
                <DialogTitle>
                    {promptId ? "Редактировать" : "Добавить"}
                </DialogTitle>
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)}>
                        <div className="mb-4">
                            <Select
                                value={newOpenRouter}
                                disabled={isLoadingData}
                                onValueChange={(value) => setNewOpenRouter(value)}
                            >
                                <SelectTrigger className="w-full">
                                    <SelectValue placeholder="Выберете модель"/>
                                </SelectTrigger>
                                <SelectContent>
                                    {openrouters?.openRouterSettingResponses?.map((router) => (
                                        <SelectItem key={router.id} value={router.id || ""}>
                                            {router.model}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>
                        <fieldset disabled={isloadingprompt} className="space-y-4">
                            <FormField
                                control={form.control}
                                name="text"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Промпт для текста {isloadingprompt && <span
                                            className="text-xs text-muted-foreground">(Загрузка...)</span>}</FormLabel>
                                        <FormControl>
                                            <Input
                                                placeholder="Prompt for text"
                                                {...field}
                                                className="font-mono text-sm"
                                            />
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="photo"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Промпт для фото</FormLabel>
                                        <FormControl>
                                            <Input
                                                placeholder="Prompt for picture"
                                                {...field}
                                                className="font-mono text-sm"
                                            />
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="video"
                                render={({field}) => (
                                    <FormItem>
                                        <FormLabel>Промпт для видео</FormLabel>
                                        <FormControl>
                                            <Input
                                                placeholder="Prompt for video"
                                                {...field}
                                                className="font-mono text-sm"
                                            />
                                        </FormControl>
                                        <FormMessage/>
                                    </FormItem>
                                )}
                            />
                        </fieldset>

                        <DialogFooter className="gap-2 mt-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button type="submit" disabled={isPending || isLoadingData}>
                                {isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        Сохранение...
                                    </>
                                ) : (
                                    <>
                                        <Plus className="mr-2 h-4 w-4"/>
                                        Сохранить
                                    </>
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}