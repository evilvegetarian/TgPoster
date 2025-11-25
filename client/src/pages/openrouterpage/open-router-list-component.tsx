import {useGetApiV1OpenRouterSetting} from "@/api/endpoints/open-router-setting/open-router-setting.ts";
import {Card, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card.tsx";
import {AlertCircle, Bot,Users} from "lucide-react";
import {Skeleton} from "@/components/ui/skeleton.tsx";
import {Badge} from "@/components/ui/badge.tsx";
import type {OpenRouterSettingResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";

import {DeleteOpenRouterComponent} from "@/pages/openrouterpage/delete-open-router-component.tsx";

export function OpenRouterListComponent() {
    const {data, error, isLoading} = useGetApiV1OpenRouterSetting();

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            <Users className="h-5 w-5 text-green-500"/>
                            <CardTitle>Мои настройки</CardTitle>
                        </div>
                        <Skeleton className="h-6 w-16"/>
                    </div>
                    <CardDescription>Загрузка списка настроек...</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {[...Array(3)].map((_, i) => (
                        <div key={i} className="flex items-center space-x-4 p-4 border rounded-lg">
                            <Skeleton className="h-10 w-10 rounded-full"/>
                            <div className="space-y-2 flex-1">
                                <Skeleton className="h-4 w-[200px]"/>
                                <Skeleton className="h-3 w-[100px]"/>
                            </div>
                            <Skeleton className="h-8 w-8"/>
                        </div>
                    ))}
                </CardContent>
            </Card>
        );
    }

    if (error) {
        return (
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-2">
                        <Users className="h-5 w-5 text-green-500"/>
                        <CardTitle>Мои настройки</CardTitle>
                    </div>
                </CardHeader>
                <CardContent>
                    <div className="flex items-center gap-2 text-destructive bg-destructive/10 p-4 rounded-lg">
                        <AlertCircle className="h-5 w-5"/>
                        <span>Произошла ошибка при загрузке настроек</span>
                    </div>
                </CardContent>
            </Card>
        );
    }

    return (
        <Card>
            <CardHeader>
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                        <Users className="h-5 w-5 text-green-500"/>
                        <CardTitle>Мои настройки</CardTitle>
                    </div>
                    <Badge variant="secondary">
                        {data?.openRouterSettingResponses.length || 0} {data?.openRouterSettingResponses.length === 1 ? 'настройки' : 'настроек'}
                    </Badge>
                </div>
                <CardDescription>
                    Список всех настроек
                </CardDescription>
            </CardHeader>
            <CardContent>
                {data?.openRouterSettingResponses && data.openRouterSettingResponses.length > 0 ? (
                    data.openRouterSettingResponses.map((open: OpenRouterSettingResponse) => (
                        <div
                            key={open.id}
                            className="flex items-center gap-4 p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                        >
                            <div className="flex-shrink-0">
                                <div
                                    className="h-10 w-10 bg-blue-100 rounded-full flex items-center justify-center">
                                    <Bot className="h-5 w-5 text-blue-600"/>
                                </div>
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2">
                                    <h3 className="font-medium text-foreground truncate">
                                        {open.model}
                                    </h3>
                                    <Badge variant="outline" className="text-xs">
                                        Активен
                                    </Badge>
                                </div>
                            </div>
                            <DeleteOpenRouterComponent setting={open}/>
                        </div>

                    ))
                ) : (
                    <div className="text-center py-8">
                        <Bot className="h-12 w-12 text-muted-foreground mx-auto mb-4"/>
                        <h3 className="text-lg font-medium text-foreground mb-2">
                            Настройки OpenRouter нет
                        </h3>
                        <p className="text-muted-foreground mb-4">
                            Добавьте настройку, чтобы начать работу
                        </p>
                    </div>
                )}
            </CardContent>
        </Card>
    )
}