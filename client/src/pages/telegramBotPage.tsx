import {TelegramBotListComponent} from "@/components/telegram-bot/telegramBotListComponent.tsx";
import {TelegramBotCreateDialog} from "@/components/telegram-bot/telegramBotCreateDialog.tsx";

export function TelegramBotPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="mb-8">
                <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-3">
                        <h1 className="text-3xl font-bold text-foreground">
                            Управление Telegram ботами
                        </h1>
                    </div>
                    <div className="flex items-center gap-2">
                        <TelegramBotCreateDialog/>
                    </div>
                </div>
                <p className="text-muted-foreground">
                    Добавляйте и управляйте вашими Telegram ботами
                </p>
            </div>

            <div className="space-y-6">
                <TelegramBotListComponent/>
            </div>
        </div>
    );
}

