import {Button} from "@/components/ui/button";
import {Loader2} from "lucide-react";
import {RepostSettingsList} from "@/pages/repostpage/repost-settings-list.tsx";
import {CreateRepostSettingsDialog} from "@/pages/repostpage/create-repost-settings-dialog.tsx";
import {
    useGetApiV1RepostSettings,
} from "@/api/endpoints/repost/repost.ts";

export function RepostSettingsPage() {

    const {data, isLoading, error, refetch} = useGetApiV1RepostSettings();
    const settings = data?.items ?? [];


    if (isLoading) {
        return (
            <div className="container mx-auto p-6 max-w-6xl">
                <div className="flex items-center justify-center py-12">
                    <Loader2 className="h-8 w-8 animate-spin"/>
                    <span className="ml-2">Загрузка настроек репоста...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="container mx-auto p-6 max-w-6xl">
                <div className="text-center py-12">
                    <p className="text-red-500 mb-4">Ошибка загрузки настроек репоста</p>
                    <p className="text-sm text-muted-foreground mb-4">
                        {error?.title || "Произошла неизвестная ошибка"}
                    </p>
                    <Button onClick={() => refetch()}>Попробовать снова</Button>
                </div>
            </div>
        );
    }

    return (
        <div className="container mx-auto p-6 max-w-6xl">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h1 className="text-3xl font-bold">Настройки репоста</h1>
                    <p className="text-muted-foreground mt-2">
                        Управляйте автоматическим репостом сообщений в другие каналы
                    </p>
                </div>
                <CreateRepostSettingsDialog/>
            </div>

            <div className="grid gap-6">
                <RepostSettingsList
                    settings={settings}
                />
            </div>


        </div>
    );
}
