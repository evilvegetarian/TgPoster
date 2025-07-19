import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Plus, Settings,  Filter, Eye, EyeOff, Edit, Loader2 } from "lucide-react"
import { toast } from "sonner"

import {
    useGetApiV1ParseChannel,
    usePostApiV1ParseChannel,
    usePutApiV1ParseChannelId
} from "@/api/endpoints/parse-channel/parse-channel.ts";
import type {
    CreateParseChannelRequest,
    ParseChannelsResponse, UpdateParseChannelRequest
} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {AddParsingSettingsDialog} from "@/components/parse-channel/add-parsing-settings-dialog.tsx";
import {EditParsingSettingsDialog} from "@/components/parse-channel/edit-parsing-settings-dialog.tsx";

export  function ParseChannelPage() {
    const [isAddDialogOpen, setIsAddDialogOpen] = useState(false)
    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
    const [editingSettings, setEditingSettings] = useState<ParseChannelsResponse | null>(null)

    const { data: settings = [], isLoading, error, refetch } = useGetApiV1ParseChannel()

    const createMutation = usePostApiV1ParseChannel({
        mutation: {
            onSuccess: (data) => {
                toast.success("Настройка создана", {
                    description: `Настройка парсинга создана с ID: ${data.id}`,
                })
                 refetch()
                setIsAddDialogOpen(false)
            },
            onError: (error) => {
                toast.error("Ошибка создания", error?.message || "Не удалось создать настройку парсинга")
            },
        },
    })

    const updateMutation = usePutApiV1ParseChannelId({
        mutation: {
            onSuccess: () => {
                toast.success("Настройка обновлена", {
                    description: "Настройка парсинга успешно обновлена",
                })
                refetch()
                setIsEditDialogOpen(false)
                setEditingSettings(null)
            },
            onError: (error) => {
                toast.error("Ошибка обновления", error?.message || "Не удалось обновить настройку парсинга")
            },
        },
    })

    const handleAddSettings = (newSettings: CreateParseChannelRequest) => {
        createMutation.mutate({ data: newSettings })
    }

    const handleEditSettings = (updatedSettings: UpdateParseChannelRequest) => {
        if (editingSettings) {
            updateMutation.mutate({
                id: editingSettings.id,
                data: updatedSettings,
            })
        }
    }

    const openEditDialog = (settings: ParseChannelsResponse) => {
        setEditingSettings(settings)
        setIsEditDialogOpen(true)
    }

    const getStatusBadge = (status: string | null, isActive: boolean) => {
        if (!isActive) {
            return <Badge variant="secondary">Неактивен</Badge>
        }

        switch (status) {
            case "active":
                return (
                    <Badge variant="default" className="bg-green-500">
                        Активен
                    </Badge>
                )
            case "paused":
                return <Badge variant="secondary">Приостановлен</Badge>
            default:
                return <Badge variant="outline">{status || "Неизвестно"}</Badge>
        }
    }

    if (isLoading) {
        return (
            <div className="container mx-auto p-6 max-w-6xl">
                <div className="flex items-center justify-center py-12">
                    <Loader2 className="h-8 w-8 animate-spin" />
                    <span className="ml-2">Загрузка настроек парсинга...</span>
                </div>
            </div>
        )
    }

    if (error) {
        return (
            <div className="container mx-auto p-6 max-w-6xl">
                <div className="text-center py-12">
                    <p className="text-red-500 mb-4">Ошибка загрузки настроек парсинга</p>
                    <p className="text-sm text-muted-foreground mb-4">{error?.title || "Произошла неизвестная ошибка"}</p>
                    <Button onClick={() => refetch()} className="mt-4">
                        Попробовать снова
                    </Button>
                </div>
            </div>
        )
    }

    return (
        <div className="container mx-auto p-6 max-w-6xl">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h1 className="text-3xl font-bold">Настройки парсинга</h1>
                    <p className="text-muted-foreground mt-2">
                        Управляйте настройками парсинга каналов и расписаниями публикаций
                    </p>
                </div>
                <Button onClick={() => setIsAddDialogOpen(true)} className="gap-2">
                    <Plus className="h-4 w-4" />
                    Добавить настройку
                </Button>
            </div>

            <div className="grid gap-6">
                {settings.length === 0 ? (
                    <Card className="text-center py-12">
                        <CardContent>
                            <Settings className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                            <h3 className="text-lg font-semibold mb-2">Нет настроек парсинга</h3>
                            <p className="text-muted-foreground mb-4">Создайте первую настройку для начала парсинга каналов</p>
                            <Button onClick={() => setIsAddDialogOpen(true)} className="gap-2">
                                <Plus className="h-4 w-4" />
                                Добавить настройку
                            </Button>
                        </CardContent>
                    </Card>
                ) : (
                    settings.map((setting) => (
                        <Card key={setting.id} className="hover:shadow-md transition-shadow">
                            <CardHeader>
                                <div className="flex items-center justify-between">
                                    <div className="flex items-center gap-3">
                                        <CardTitle className="text-xl">{setting.channel || "Канал не указан"}</CardTitle>
                                        {getStatusBadge(setting.status, setting.isActive)}
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Button variant="outline" size="sm" onClick={() => openEditDialog(setting)} className="gap-1">
                                            <Edit className="h-3 w-3" />
                                            Редактировать
                                        </Button>
                                    </div>
                                </div>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                                    <div className="flex items-center gap-2">
                                        {setting.deleteText ? (
                                            <EyeOff className="h-4 w-4 text-red-500" />
                                        ) : (
                                            <Eye className="h-4 w-4 text-green-500" />
                                        )}
                                        <span className="text-sm">{setting.deleteText ? "Без текста" : "С текстом"}</span>
                                    </div>

                                    <div className="flex items-center gap-2">
                                        {setting.deleteMedia ? (
                                            <EyeOff className="h-4 w-4 text-red-500" />
                                        ) : (
                                            <Eye className="h-4 w-4 text-green-500" />
                                        )}
                                        <span className="text-sm">{setting.deleteMedia ? "Без медиа" : "С медиа"}</span>
                                    </div>

                                    <div className="flex items-center gap-2">
                                        <Settings className="h-4 w-4 text-blue-500" />
                                        <span className="text-sm">
                      {setting.needVerifiedPosts ? "Требует подтверждения" : "Автопубликация"}
                    </span>
                                    </div>

                                    {setting.avoidWords && setting.avoidWords.length > 0 && (
                                        <div className="flex items-center gap-2">
                                            <Filter className="h-4 w-4 text-orange-500" />
                                            <span className="text-sm">Фильтры: {setting.avoidWords.length}</span>
                                        </div>
                                    )}
                                </div>

                                {setting.avoidWords && setting.avoidWords.length > 0 && (
                                    <div>
                                        <p className="text-sm font-medium mb-2">Исключаемые слова:</p>
                                        <div className="flex flex-wrap gap-1">
                                            {setting.avoidWords.map((word, index) => (
                                                <Badge key={index} variant="outline" className="text-xs">
                                                    {word}
                                                </Badge>
                                            ))}
                                        </div>
                                    </div>
                                )}

                                {(setting.dateFrom || setting.dateTo) && (
                                    <div className="text-sm text-muted-foreground">
                                        <span className="font-medium">Период парсинга: </span>
                                        {setting.dateFrom && <span>с {new Date(setting.dateFrom).toLocaleDateString("ru-RU")}</span>}
                                        {setting.dateFrom && setting.dateTo && <span> </span>}
                                        {setting.dateTo && <span>до {new Date(setting.dateTo).toLocaleDateString("ru-RU")}</span>}
                                    </div>
                                )}
                            </CardContent>
                        </Card>
                    ))
                )}
            </div>

            <AddParsingSettingsDialog
                open={isAddDialogOpen}
                onOpenChange={setIsAddDialogOpen}
                onSubmit={handleAddSettings}
                isLoading={createMutation.isPending}
            />

            {editingSettings && (
                <EditParsingSettingsDialog
                    open={isEditDialogOpen}
                    onOpenChange={setIsEditDialogOpen}
                    onSubmit={handleEditSettings}
                    initialData={editingSettings}
                    isLoading={updateMutation.isPending}
                />
            )}
        </div>
    )
}
