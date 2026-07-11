import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import {z} from "zod";
import {Loader2} from "lucide-react";
import {toast} from "sonner";
import {Button} from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import {Form} from "@/components/ui/form";
import {usePutApiV1RepostSettingsId} from "@/api/endpoints/repost/repost.ts";
import type {RepostSettingsResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {
    RepostRandomnessFields,
    delayRangeCheck,
    delayRangeError,
    repostRandomnessShape,
} from "@/pages/repostpage/repost-randomness-fields.tsx";

const defaultsSchema = z.object(repostRandomnessShape).refine(delayRangeCheck, delayRangeError);

type FormValues = z.infer<typeof defaultsSchema>;

interface RepostDefaultsDialogProps {
    settings: RepostSettingsResponse;
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onSuccess: () => void;
}

export function RepostDefaultsDialog({
    settings,
    open,
    onOpenChange,
    onSuccess,
}: RepostDefaultsDialogProps) {
    const form = useForm<FormValues>({
        resolver: zodResolver(defaultsSchema),
        values: {
            delayMinSeconds: settings.defaultDelayMinSeconds,
            delayMaxSeconds: settings.defaultDelayMaxSeconds,
            repostEveryNth: settings.defaultRepostEveryNth,
            skipProbability: settings.defaultSkipProbability,
            maxRepostsPerDay: settings.defaultMaxRepostsPerDay,
        },
    });

    const {mutate: updateSettings, isPending} = usePutApiV1RepostSettingsId({
        mutation: {
            onSuccess: () => {
                toast.success("Общие настройки сохранены", {
                    description: "Они будут применены ко всем новым каналам",
                });
                onOpenChange(false);
                onSuccess();
            },
            onError: (error) => {
                toast.error("Ошибка сохранения", {
                    description: error.title || "Не удалось сохранить общие настройки",
                });
            },
        },
    });

    function onSubmit(values: FormValues) {
        updateSettings({
            id: settings.id,
            data: {
                isActive: settings.isActive,
                defaultDelayMinSeconds: values.delayMinSeconds,
                defaultDelayMaxSeconds: values.delayMaxSeconds,
                defaultRepostEveryNth: values.repostEveryNth,
                defaultSkipProbability: values.skipProbability,
                defaultMaxRepostsPerDay: values.maxRepostsPerDay,
            },
        });
    }

    function handleOpenChange(isOpen: boolean) {
        if (!isOpen && !isPending) {
            form.reset();
        }
        onOpenChange(isOpen);
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[520px]">
                <DialogHeader>
                    <DialogTitle>Общие настройки репоста</DialogTitle>
                    <DialogDescription>
                        Эти настройки копируются на каждый новый добавленный канал.
                        Уже добавленные каналы не изменяются — их можно настроить индивидуально.
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <RepostRandomnessFields form={form} disabled={isPending}/>

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
