import {TelegramBotGetComponent} from "@/components/telegramBot/telegramBotGetComponent.tsx";
import {Settings} from "lucide-react";
import {useState} from "react";
import {TelegramBotCreateDialog} from "@/components/telegramBot/telegramBotCreateDialog.tsx";

export function TelegramBotPage() {
    const [refreshKey, setRefreshKey] = useState(0);

    const handleBotCreated = () => {
        setRefreshKey(prev => prev + 1);
    };

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="mb-8">
                <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-3">
                        <div className="h-8 w-8 bg-blue-100 rounded-lg flex items-center justify-center">
                            <Settings className="h-5 w-5 text-blue-600" />
                        </div>
                        <h1 className="text-3xl font-bold text-foreground">
                            Управление Telegram ботами
                        </h1>
                    </div>
                    <div className="flex items-center gap-2">
                        <TelegramBotCreateDialog onSuccess={handleBotCreated} />
                    </div>
                </div>
                <p className="text-muted-foreground">
                    Добавляйте и управляйте вашими Telegram ботами
                </p>
            </div>

            <div className="space-y-6">
                <TelegramBotGetComponent shouldRefresh={refreshKey}/>
            </div>
        </div>
    );
}
