import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { CardFooter } from "@/components/ui/card"
import {AuthLayout} from "@/pages/authLayout.tsx";
import {usePostApiV1AccountSignOn} from "@/api/endpoints/account/account.ts";
import {z} from "zod";
import {zodResolver} from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from "@/components/ui/form.tsx";
import {Loader2} from "lucide-react";

const formSchema = z.object({
    login : z.string().min(5, "Логин должен быть не менее 5 символов").max(30,"Логин должен быть не более 30 символов"),
    password : z.string().min(5, "Пароль должен быть не менее 5 символов"),
})
type RegisterForm=z.infer<typeof formSchema>;

export function RegisterPage() {
const form = useForm<RegisterForm>({
    resolver: zodResolver(formSchema),
    defaultValues: {
        login: "",
        password: ""
    }
})

    const {mutate, isPending, error}=usePostApiV1AccountSignOn(
        {
            mutation:{
                onSuccess: (data) => {
                    console.log(data)
                },
                onError:(error)=>{
                    console.log("asdas" ,error.title )
                }
            }
        }
    );

    function onSubmit(values: RegisterForm) {
mutate({data:values})
    }

    return (
        <AuthLayout
            title="Создания аккаунта"
            description="Введите свои данные ниже, чтобы создать новую учетную запись"
        >
            <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                    <FormField
                        control={form.control}
                        name="login"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Логин</FormLabel>
                                <FormControl>
                                    <Input placeholder="Username" {...field} disabled={isPending} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="password"
                        render={({ field }) => (
                            <FormItem>
                                <div className="flex items-center">
                                    <FormLabel>Пароль</FormLabel>
                                </div>
                                <FormControl>
                                    <Input type="password" {...field} disabled={isPending} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    {error && (
                        <p className="text-sm font-medium text-destructive">{error?.detail || "Неверный логин или пароль"}</p>
                    )}
                    <CardFooter className="flex-col gap-4 p-0 pt-2">
                        <Button type="submit" className="w-full" disabled={isPending}>
                            {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                            Регистрация
                        </Button>
                        <div className="mt-4 text-center text-sm">
                            Уже есть аккаунт?{" "}
                            <a href="/login" className="underline">
                                Войти
                            </a>
                        </div>
                    </CardFooter>
                    </form>
            </Form>
        </AuthLayout>
    )
}
