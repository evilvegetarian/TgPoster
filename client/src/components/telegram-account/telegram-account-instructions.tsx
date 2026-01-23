import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";

export function TelegramAccountInstructions() {
    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle>Как получить API credentials</CardTitle>
                    <CardDescription>
                        Инструкция по получению API ID и API Hash
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div>
                        <h3 className="font-semibold mb-2">Шаг 1: Войдите в Telegram</h3>
                        <p className="text-sm text-muted-foreground">
                            Перейдите на сайт{" "}
                            <a
                                href="https://my.telegram.org"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="text-blue-600 hover:underline"
                            >
                                my.telegram.org
                            </a>{" "}
                            и авторизуйтесь с помощью номера телефона
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold mb-2">Шаг 2: Создайте приложение</h3>
                        <p className="text-sm text-muted-foreground">
                            Перейдите в раздел "API development tools" и заполните форму создания приложения:
                        </p>
                        <ul className="text-sm text-muted-foreground list-disc list-inside mt-2 space-y-1">
                            <li>App title: любое название</li>
                            <li>Short name: короткое имя приложения</li>
                            <li>Platform: выберите подходящую платформу</li>
                        </ul>
                    </div>

                    <div>
                        <h3 className="font-semibold mb-2">Шаг 3: Скопируйте данные</h3>
                        <p className="text-sm text-muted-foreground">
                            После создания приложения вы получите:
                        </p>
                        <ul className="text-sm text-muted-foreground list-disc list-inside mt-2 space-y-1">
                            <li><strong>API ID</strong> - числовой идентификатор</li>
                            <li><strong>API Hash</strong> - строка из букв и цифр</li>
                        </ul>
                    </div>

                    <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
                        <p className="text-sm text-yellow-800">
                            <strong>Важно:</strong> Храните API credentials в безопасности и не передавайте их третьим лицам.
                        </p>
                    </div>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Дополнительная информация</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <div>
                        <h4 className="font-semibold text-foreground mb-1">Авторизация</h4>
                        <p>
                            После добавления аккаунта необходимо авторизоваться. Нажмите кнопку "Авторизовать" и введите код, полученный в Telegram.
                        </p>
                    </div>
                    <div>
                        <h4 className="font-semibold text-foreground mb-1">Двухфакторная аутентификация</h4>
                        <p>
                            Если у вас включена 2FA, после ввода кода потребуется ввести пароль облачного доступа.
                        </p>
                    </div>
                    <div>
                        <h4 className="font-semibold text-foreground mb-1">Статус аккаунта</h4>
                        <p>
                            Активный аккаунт готов к использованию для постинга. Неактивный требует повторной авторизации.
                        </p>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
