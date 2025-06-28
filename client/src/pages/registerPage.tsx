import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { CardFooter } from "@/components/ui/card"
import {AuthLayout} from "@/pages/authLayout.tsx";

export function RegisterPage() {
    return (
        <AuthLayout
            title="Создания аккаунта"
            description="Введите свои данные ниже, чтобы создать новую учетную запись"
        >
            <form>
                <div className="flex flex-col gap-6">
                    <div className="grid gap-2">
                        <Label htmlFor="name">Ник</Label>
                        <Input id="name" placeholder="John Doe" required />
                    </div>
                    <div className="grid gap-2">
                        <Label htmlFor="email">Почта</Label>
                        <Input id="email" type="email" placeholder="m@example.com" required />
                    </div>
                    <div className="grid gap-2">
                        <Label htmlFor="password">Пароль</Label>
                        <Input id="password" type="password" required />
                    </div>
                </div>
                <CardFooter className="flex-col gap-4 pt-6 px-0">
                    <Button type="submit" className="w-full">
                        Создайте аккаунт
                    </Button>
                    <div className="mt-4 text-center text-sm">
                        Уже есть аккаунт?{" "}
                        <a href="/login" className="underline">
                            Войти
                        </a>
                    </div>
                </CardFooter>
            </form>
        </AuthLayout>
    )
}
