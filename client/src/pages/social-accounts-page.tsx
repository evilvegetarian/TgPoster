import {Share2} from "lucide-react";
import {Card, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {SocialAccountList} from "@/components/social-account/social-account-list.tsx";
import {BlueskyConnectDialog} from "@/components/social-account/bluesky-connect-dialog.tsx";

export function SocialAccountsPage() {
    return (
        <div className="container mx-auto py-10">
            <div className="flex items-center gap-4 mb-8">
                <Share2 className="h-8 w-8 text-primary"/>
                <div>
                    <h1 className="text-3xl font-bold">Соцсети</h1>
                    <p className="text-muted-foreground">
                        Подключите аккаунты, чтобы посты из Telegram автоматически уходили в соцсети
                        и приводили подписчиков в канал
                    </p>
                </div>
            </div>

            <div className="grid gap-6 lg:grid-cols-[1fr,400px]">
                <SocialAccountList/>

                <Card>
                    <CardHeader>
                        <CardTitle>Подключить</CardTitle>
                        <CardDescription>
                            Добавьте аккаунт соцсети, в который будут уходить посты
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <BlueskyConnectDialog/>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
