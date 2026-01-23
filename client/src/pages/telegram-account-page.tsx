import { TelegramAccountListComponent } from "@/components/telegram-account/telegram-account-list-component";
import { TelegramAccountCreateDialog } from "@/components/telegram-account/telegram-account-create-dialog";
import { TelegramAccountInstructions } from "@/components/telegram-account/telegram-account-instructions";

export default function TelegramAccountPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-7xl">
            <div className="mb-8 flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold">Telegram аккаунты</h1>
                    <p className="text-muted-foreground mt-2">
                        Управляйте Telegram аккаунтами для постинга сообщений
                    </p>
                </div>
                <TelegramAccountCreateDialog />
            </div>

            <div className="grid gap-6 lg:grid-cols-[1fr,400px]">
                <div>
                    <TelegramAccountListComponent />
                </div>

                <div>
                    <TelegramAccountInstructions />
                </div>
            </div>
        </div>
    );
}
