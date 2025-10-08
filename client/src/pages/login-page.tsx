import {useForm} from "react-hook-form";
import {zodResolver} from "@hookform/resolvers/zod";
import * as z from "zod";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {CardFooter} from "@/components/ui/card";
import {Loader2} from "lucide-react";
import {usePostApiV1AccountSignIn} from "@/api/endpoints/account/account";
import {AuthLayout} from "./auth-layout.tsx";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {Link} from "react-router-dom";
import {useAuth} from "@/auth-context.tsx";
import {toast} from "sonner";
import type {SignInRequest} from "@/api/endpoints/tgPosterAPI.schemas.ts";

const formSchema = z.object({
    login: z.string().min(5, "Логин должен быть не менее 5 символов"),
    password: z.string().min(5, "Пароль должен быть не менее 5 символов"),
});
type LoginFormValues = z.infer<typeof formSchema>;

export function LoginPage() {
    const {login} = useAuth();

    const form = useForm<LoginFormValues>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            login: "",
            password: "",
        },
    });

    const {mutate, isPending} = usePostApiV1AccountSignIn({
        mutation: {
            onSuccess: (data) => {
                if (data.accessToken && data.refreshToken) {
                    login(data.accessToken, data.refreshToken);
                }
            },
            onError: (error) => {
                console.log('Error object:', error);
                console.log('Error title:', error.title);
                toast.error('Ошибка', {
                    description: error.title || "Не удалось войти в аккаунт"
                });
            }
        }
    });

    function onSubmit(values: SignInRequest) {
        mutate({data: values});
    }

    return (
        <AuthLayout
            title="Вход в аккаунт"
            description="Введите свои данные, чтобы войти в свою учетную запись"
        >
            <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                    <FormField
                        control={form.control}
                        name="login"
                        render={({field}) => (
                            <FormItem>
                                <FormLabel>Логин или Email</FormLabel>
                                <FormControl>
                                    <Input placeholder="m@example.com" {...field} disabled={isPending}/>
                                </FormControl>
                                <FormMessage/>
                            </FormItem>
                        )}
                    />
                    <FormField
                        control={form.control}
                        name="password"
                        render={({field}) => (
                            <FormItem>
                                <div className="flex items-center">
                                    <FormLabel>Пароль</FormLabel>
                                    <a href="/forgot-password"
                                       className="ml-auto inline-block text-sm underline-offset-4 hover:underline">
                                        Забыли пароль?
                                    </a>
                                </div>
                                <FormControl>
                                    <Input type="password" {...field} disabled={isPending}/>
                                </FormControl>
                                <FormMessage/>
                            </FormItem>
                        )}
                    />

                    <CardFooter className="flex-col gap-4 p-0 pt-2">
                        <Button type="submit" className="w-full" disabled={isPending}>
                            {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin"/>}
                            Войти
                        </Button>
                        <Button variant="outline" className="w-full" disabled={isPending}>
                            Войти через Google
                        </Button>
                        <div className="mt-4 text-center text-sm">
                            Нет аккаунта?{" "}
                            <Link to="/register" className="underline">
                                Регистрация
                            </Link>
                        </div>
                    </CardFooter>
                </form>
            </Form>
        </AuthLayout>
    );
}
