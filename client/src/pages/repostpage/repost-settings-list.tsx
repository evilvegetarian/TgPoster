import {Card, CardContent} from "@/components/ui/card";
import { Repeat2} from "lucide-react";

import {RepostSettingsCard} from "@/pages/repostpage/repost-settings-card.tsx";
import type {RepostSettingsItemDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {CreateRepostSettingsDialog} from "@/pages/repostpage/create-repost-settings-dialog.tsx";

interface RepostSettingsListProps {
    settings: RepostSettingsItemDto[];

}

export function RepostSettingsList({settings}: RepostSettingsListProps) {
    if (settings.length === 0) {
        return (
            <Card className="text-center py-12">
                <CardContent className="pt-6">
                    <Repeat2 className="h-12 w-12 mx-auto text-muted-foreground mb-4"/>
                    <h3 className="text-lg font-semibold mb-2">Нет настроек репоста</h3>
                    <p className="text-muted-foreground mb-4">
                        Создайте настройки для автоматического репоста сообщений в другие каналы
                    </p>
                    <CreateRepostSettingsDialog/>
                </CardContent>
            </Card>
        );
    }

    return (
        <>
            {settings.map(setting => (
                <RepostSettingsCard
                    settings={setting}
                />
            ))}
        </>
    );
}
