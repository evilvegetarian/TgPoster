import {AuthLayout} from "@/pages/auth-layout.tsx";
import {Link} from "react-router-dom";
import {Button} from "@/components/ui/button.tsx";
import {CheckCircle2} from "lucide-react";

export function RegistrationSuccessPage() {
    return (
        <AuthLayout
            title="Регистрация прошла успешно!"
            description="Ваш аккаунт был создан. Теперь вы можете войти в систему."
        >
            <div className="flex flex-col items-center justify-center space-y-6 text-center">
                <CheckCircle2 className="h-16 w-16 text-green-500"/>
                <p className="text-muted-foreground">
                    Вы можете войти, используя логин и пароль, которые вы только что указали.
                </p>
                <Button asChild className="w-full">
                    <Link to="/login">Перейти ко входу</Link>
                </Button>
            </div>
        </AuthLayout>
    );
}