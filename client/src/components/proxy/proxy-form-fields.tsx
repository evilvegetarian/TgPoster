import { z } from "zod";
import type { Control } from "react-hook-form";
import {
    Form,
    FormControl,
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
import { ProxyType } from "@/api/endpoints/tgPosterAPI.schemas";

export const proxyFormSchema = z
    .object({
        name: z.string().trim().min(1, "Имя обязательно").max(100),
        type: z.nativeEnum(ProxyType),
        host: z.string().trim().min(1, "Host обязателен"),
        port: z.coerce
            .number()
            .int()
            .min(1, "Порт от 1 до 65535")
            .max(65535, "Порт от 1 до 65535"),
        username: z.string().optional(),
        password: z.string().optional(),
        secret: z.string().optional(),
    })
    .superRefine((value, ctx) => {
        if (value.type === ProxyType.MTProxy && !value.secret?.trim()) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                path: ["secret"],
                message: "Для MTProxy секрет обязателен",
            });
        }
    });

export type ProxyFormValues = z.infer<typeof proxyFormSchema>;

export const PROXY_TYPE_LABELS: Record<ProxyType, string> = {
    [ProxyType.Socks5]: "SOCKS5",
    [ProxyType.Http]: "HTTP",
    [ProxyType.MTProxy]: "MTProxy",
};

export function ProxyFormFields({
    control,
    type,
}: {
    control: Control<ProxyFormValues>;
    type: ProxyType;
}) {
    const showAuth = type === ProxyType.Socks5 || type === ProxyType.Http;
    const showSecret = type === ProxyType.MTProxy;

    return (
        <>
            <FormField
                control={control}
                name="name"
                render={({ field }) => (
                    <FormItem>
                        <FormLabel>Имя</FormLabel>
                        <FormControl>
                            <Input placeholder="Мой VPS Хельсинки" {...field} />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                )}
            />
            <FormField
                control={control}
                name="type"
                render={({ field }) => (
                    <FormItem>
                        <FormLabel>Тип</FormLabel>
                        <Select onValueChange={field.onChange} value={field.value}>
                            <FormControl>
                                <SelectTrigger>
                                    <SelectValue />
                                </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                                <SelectItem value={ProxyType.Socks5}>SOCKS5</SelectItem>
                                <SelectItem value={ProxyType.Http}>HTTP</SelectItem>
                                <SelectItem value={ProxyType.MTProxy}>MTProxy</SelectItem>
                            </SelectContent>
                        </Select>
                        <FormMessage />
                    </FormItem>
                )}
            />
            <div className="grid grid-cols-2 gap-3">
                <FormField
                    control={control}
                    name="host"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Host</FormLabel>
                            <FormControl>
                                <Input placeholder="1.2.3.4" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <FormField
                    control={control}
                    name="port"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Port</FormLabel>
                            <FormControl>
                                <Input type="number" placeholder="1080" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
            </div>
            {showAuth && (
                <div className="grid grid-cols-2 gap-3">
                    <FormField
                        control={control}
                        name="username"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Username (опционально)</FormLabel>
                                <FormControl>
                                    <Input {...field} value={field.value ?? ""} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    <FormField
                        control={control}
                        name="password"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Password (опционально)</FormLabel>
                                <FormControl>
                                    <Input type="password" {...field} value={field.value ?? ""} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                </div>
            )}
            {showSecret && (
                <FormField
                    control={control}
                    name="secret"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Secret</FormLabel>
                            <FormControl>
                                <Input
                                    placeholder="ee... или dd..."
                                    {...field}
                                    value={field.value ?? ""}
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
            )}
        </>
    );
}

export const Forms = { Form };
