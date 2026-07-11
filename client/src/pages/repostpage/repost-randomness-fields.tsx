import {z} from "zod";
import type {Control, UseFormReturn} from "react-hook-form";
import {
    FormControl,
    FormDescription,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import {Input} from "@/components/ui/input";

// Общие поля рандомизации репоста: используются и в настройках канала,
// и в общих настройках RepostSettings
export const repostRandomnessShape = {
    delayMinSeconds: z.coerce.number().int().min(0, "Не может быть отрицательным"),
    delayMaxSeconds: z.coerce.number().int().min(0, "Не может быть отрицательным"),
    repostEveryNth: z.coerce.number().int().min(1, "Минимум 1"),
    skipProbability: z.coerce.number().int().min(0, "Минимум 0").max(100, "Максимум 100"),
    maxRepostsPerDay: z.coerce.number().int().min(1, "Минимум 1").nullable(),
};

export const delayRangeCheck = (data: {delayMinSeconds: number; delayMaxSeconds: number}) =>
    data.delayMaxSeconds >= data.delayMinSeconds;

export const delayRangeError = {
    message: "Максимальная задержка не может быть меньше минимальной",
    path: ["delayMaxSeconds"],
};

export type RepostRandomnessValues = z.infer<z.ZodObject<typeof repostRandomnessShape>>;

interface RepostRandomnessFieldsProps<T extends RepostRandomnessValues> {
    form: UseFormReturn<T>;
    disabled: boolean;
}

export function RepostRandomnessFields<T extends RepostRandomnessValues>({
    form,
    disabled,
}: RepostRandomnessFieldsProps<T>) {
    const control = form.control as unknown as Control<RepostRandomnessValues>;

    return (
        <>
            <div className="grid grid-cols-2 gap-3">
                <FormField
                    control={control}
                    name="delayMinSeconds"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Мин. задержка (сек)</FormLabel>
                            <FormControl>
                                <Input
                                    type="number"
                                    min={0}
                                    {...field}
                                    disabled={disabled}
                                />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="delayMaxSeconds"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Макс. задержка (сек)</FormLabel>
                            <FormControl>
                                <Input
                                    type="number"
                                    min={0}
                                    {...field}
                                    disabled={disabled}
                                />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />
            </div>

            <FormField
                control={control}
                name="repostEveryNth"
                render={({field}) => (
                    <FormItem>
                        <FormLabel>Каждое N-е сообщение</FormLabel>
                        <FormControl>
                            <Input
                                type="number"
                                min={1}
                                {...field}
                                disabled={disabled}
                            />
                        </FormControl>
                        <FormDescription>
                            1 = каждое сообщение, 2 = каждое второе и т.д.
                        </FormDescription>
                        <FormMessage/>
                    </FormItem>
                )}
            />

            <FormField
                control={control}
                name="skipProbability"
                render={({field}) => (
                    <FormItem>
                        <FormLabel>Вероятность пропуска (%)</FormLabel>
                        <FormControl>
                            <Input
                                type="number"
                                min={0}
                                max={100}
                                {...field}
                                disabled={disabled}
                            />
                        </FormControl>
                        <FormDescription>
                            0 = не пропускать, 50 = пропускать половину
                        </FormDescription>
                        <FormMessage/>
                    </FormItem>
                )}
            />

            <FormField
                control={control}
                name="maxRepostsPerDay"
                render={({field}) => (
                    <FormItem>
                        <FormLabel>Макс. репостов в день</FormLabel>
                        <FormControl>
                            <Input
                                type="number"
                                min={1}
                                placeholder="Без лимита"
                                value={field.value ?? ""}
                                onChange={(e) => {
                                    const val = e.target.value;
                                    field.onChange(val === "" ? null : Number(val));
                                }}
                                disabled={disabled}
                            />
                        </FormControl>
                        <FormDescription>
                            Оставьте пустым для снятия лимита
                        </FormDescription>
                        <FormMessage/>
                    </FormItem>
                )}
            />
        </>
    );
}
