import {useState} from "react";
import {useQueryClient} from "@tanstack/react-query";
import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {
    getGetApiV1OpenRouterSettingQueryKey,
    usePostApiV1OpenRouterSetting
} from "@/api/endpoints/open-router-setting/open-router-setting.ts";
import {toast} from "sonner";
import {Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger} from "@/components/ui/dialog.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Loader2, Plus} from "lucide-react";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {Input} from "@/components/ui/input.tsx";
import {z} from "zod";

const formSchema = z.object({
    token: z.string().min(5, "Токен не может быть меньше 5 символов"),
    model: z.string().min(2, "Название модели слишком короткое"),
});

type FormValues = z.infer<typeof formSchema>;

export function CreateOpenRouterComponent() {
    const [open, setOpen] = useState(false);
    const queryClient = useQueryClient();

    const form = useForm<FormValues>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            model: "",
            token: "",
        }
    });

    const {mutate, isPending} = usePostApiV1OpenRouterSetting({
        mutation: {
            onSuccess: () => {
                toast.success(`Настройки OpenRouter успешно добавлены!`);
                queryClient.invalidateQueries({queryKey: getGetApiV1OpenRouterSettingQueryKey()});
                form.reset();
                setOpen(false);
            },
            onError: (error) => {
                console.error("Ошибка при добавлении OpenRouter:", error);
                toast.error("Ошибка", {description: error.title || "Что-то пошло не так"});
            }
        }
    });

    function onSubmit(values: FormValues) {
        mutate({data: values});
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button className="gap-2">
                    <Plus className="h-4 w-4"/>
                    Добавить
                </Button>
            </DialogTrigger>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>Добавить настройки OpenRouter</DialogTitle>
                </DialogHeader>
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="model"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Модель</FormLabel>
                                    <FormControl>
                                        <Input
                                            placeholder="gpt-4-turbo"
                                            {...field}
                                            disabled={isPending}
                                            className="font-mono text-sm"
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="token"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>OpenRouter API токен</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="password"
                                            placeholder="sk-or-..."
                                            {...field}
                                            disabled={isPending}
                                            className="font-mono text-sm"
                                        />
                                    </FormControl>
                                    <FormMessage/>
                                </FormItem>
                            )}
                        />

                        <DialogFooter className="gap-2 pt-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
                                disabled={isPending}
                            >
                                Отмена
                            </Button>
                            <Button type="submit" disabled={isPending}>
                                {isPending ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
                                        Добавление...
                                    </>
                                ) : (
                                    <>
                                        <Plus className="mr-2 h-4 w-4"/>
                                        Добавить
                                    </>
                                )}
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}