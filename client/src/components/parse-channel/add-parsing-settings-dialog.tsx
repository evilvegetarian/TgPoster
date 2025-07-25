"use client"

import type React from "react"
import {useState} from "react"
import {Button} from "@/components/ui/button"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import {Input} from "@/components/ui/input"
import {Label} from "@/components/ui/label"
import {Switch} from "@/components/ui/switch"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"
import {Badge} from "@/components/ui/badge"
import {AlertCircle, Loader2, X} from "lucide-react"
import {toast} from "sonner"
import type {CreateParseChannelRequest} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {useGetApiV1Schedule} from "@/api/endpoints/schedule/schedule.ts";

interface AddParsingSettingsDialogProps {
    open: boolean
    onOpenChange: (open: boolean) => void
    onSubmit: (settings: CreateParseChannelRequest) => void
    isLoading?: boolean
}

export function AddParsingSettingsDialog({
                                             open,
                                             onOpenChange,
                                             onSubmit,
                                             isLoading = false,
                                         }: AddParsingSettingsDialogProps) {
    const [formData, setFormData] = useState<CreateParseChannelRequest>({
        channel: "",
        alwaysCheckNewPosts: true,
        scheduleId: "",
        deleteText: false,
        deleteMedia: false,
        avoidWords: [],
        needVerifiedPosts: false,
        dateFrom: "",
        dateTo: "",
    })

    const [newAvoidWord, setNewAvoidWord] = useState("")

    // Загружаем расписания
    const {data: schedules = [], isLoading: schedulesLoading, error: schedulesError} = useGetApiV1Schedule()
    const getUtcString = (dateValue: string | Date | null | undefined): string | null => {
        if (!dateValue) {
            return null;
        }
        return new Date(dateValue).toISOString();
    };
    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault()

        if (!formData.channel || !formData.scheduleId) {
            toast.error("Ошибка валидации", {
                description: "Канал и расписание обязательны для заполнения",
            })
            return
        }

        const settings: CreateParseChannelRequest = {
            ...formData,
            channel: formData.channel.startsWith("@") ? formData.channel : `@${formData.channel}`,
            avoidWords: formData.avoidWords && formData.avoidWords.length > 0 ? formData.avoidWords : [],
            dateFrom: getUtcString(formData.dateFrom),
            dateTo: getUtcString(formData.dateTo),
        }

        onSubmit(settings)
    }

    const resetForm = () => {
        setFormData({
            channel: "",
            alwaysCheckNewPosts: true,
            scheduleId: "",
            deleteText: false,
            deleteMedia: false,
            avoidWords: [],
            needVerifiedPosts: false,
            dateFrom: "",
            dateTo: "",
        })
        setNewAvoidWord("")
    }

    const handleOpenChange = (newOpen: boolean) => {
        if (!newOpen && !isLoading) {
            resetForm()
        }
        onOpenChange(newOpen)
    }

    const addAvoidWord = () => {
        if (newAvoidWord.trim() && !formData.avoidWords?.includes(newAvoidWord.trim())) {
            setFormData({
                ...formData,
                avoidWords: [...formData.avoidWords ?? [], newAvoidWord.trim()],
            })
            setNewAvoidWord("")
            toast.success("Слово добавлено", {
                description: `"${newAvoidWord.trim()}" добавлено в список исключений`,
            })
        } else if (formData.avoidWords?.includes(newAvoidWord.trim())) {
            toast.error("Слово уже существует", {
                description: "Это слово уже есть в списке исключений",
            })
        }
    }

    const removeAvoidWord = (word: string) => {
        setFormData({
            ...formData,
            avoidWords: formData.avoidWords?.filter((w) => w !== word),
        })
        toast.info("Слово удалено", {
            description: `"${word}" удалено из списка исключений`,
        })
    }

    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === "Enter") {
            e.preventDefault()
            addAvoidWord()
        }
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Добавить настройку парсинга</DialogTitle>
                    <DialogDescription>Создайте новую настройку для парсинга канала</DialogDescription>
                </DialogHeader>

                <form onSubmit={handleSubmit} className="space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="channel">Канал для парсинга *</Label>
                            <Input
                                id="channel"
                                placeholder="@channel_name"
                                value={formData.channel?.valueOf()}
                                onChange={(e) => setFormData({...formData, channel: e.target.value})}
                                disabled={isLoading}
                                required
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="schedule">Расписание *</Label>
                            {schedulesError ? (
                                <div className="flex items-center gap-2 p-2 text-sm text-red-600 bg-red-50 rounded-md">
                                    <AlertCircle className="h-4 w-4"/>
                                    <span>Ошибка загрузки расписаний</span>
                                </div>
                            ) : (
                                <Select
                                    value={formData.scheduleId}
                                    onValueChange={(value) => setFormData({...formData, scheduleId: value})}
                                    disabled={isLoading || schedulesLoading}
                                    required
                                >
                                    <SelectTrigger>
                                        <SelectValue
                                            placeholder={schedulesLoading ? "Загрузка..." : "Выберите расписание"}/>
                                    </SelectTrigger>
                                    <SelectContent>
                                        {schedulesLoading ? (
                                            <SelectItem value="loading" disabled>
                                                <div className="flex items-center gap-2">
                                                    <Loader2 className="h-4 w-4 animate-spin"/>
                                                    Загрузка расписаний...
                                                </div>
                                            </SelectItem>
                                        ) : schedules.length === 0 ? (
                                            <SelectItem value="empty" disabled>
                                                Нет доступных расписаний
                                            </SelectItem>
                                        ) : (
                                            schedules
                                                .filter((schedule) => schedule.isActive)
                                                .map((schedule) => (
                                                    <SelectItem key={schedule.id} value={schedule.id}>
                                                        <div className="flex flex-col">
                                                            <span>{schedule.name}</span>
                                                            {schedule.name && (
                                                                <span
                                                                    className="text-xs text-muted-foreground">{schedule.name}</span>
                                                            )}
                                                        </div>
                                                    </SelectItem>
                                                ))
                                        )}
                                    </SelectContent>
                                </Select>
                            )}
                        </div>
                    </div>

                    <div className="space-y-4">
                        <div className="flex items-center justify-between">
                            <div className="space-y-0.5">
                                <Label>Всегда проверять новые посты</Label>
                                <p className="text-sm text-muted-foreground">Раз в день будет проверять новые посты</p>
                            </div>
                            <Switch
                                checked={formData.alwaysCheckNewPosts}
                                onCheckedChange={(checked) => setFormData({...formData, alwaysCheckNewPosts: checked})}
                                disabled={isLoading}
                            />
                        </div>

                        <div className="flex items-center justify-between">
                            <div className="space-y-0.5">
                                <Label>Удалять текст</Label>
                                <p className="text-sm text-muted-foreground">Оставлять только картинки</p>
                            </div>
                            <Switch
                                checked={formData.deleteText}
                                onCheckedChange={(checked) => setFormData({...formData, deleteText: checked})}
                                disabled={isLoading}
                            />
                        </div>

                        <div className="flex items-center justify-between">
                            <div className="space-y-0.5">
                                <Label>Удалять медиа</Label>
                                <p className="text-sm text-muted-foreground">Оставлять только текст</p>
                            </div>
                            <Switch
                                checked={formData.deleteMedia}
                                onCheckedChange={(checked) => setFormData({...formData, deleteMedia: checked})}
                                disabled={isLoading}
                            />
                        </div>

                        <div className="flex items-center justify-between">
                            <div className="space-y-0.5">
                                <Label>Требовать подтверждение</Label>
                                <p className="text-sm text-muted-foreground">Подтверждать посты перед публикацией</p>
                            </div>
                            <Switch
                                checked={formData.needVerifiedPosts}
                                onCheckedChange={(checked) => setFormData({...formData, needVerifiedPosts: checked})}
                                disabled={isLoading}
                            />
                        </div>
                    </div>

                    <div className="space-y-2">
                        <Label>Исключаемые слова</Label>
                        <div className="flex gap-2">
                            <Input
                                placeholder="Добавить слово для исключения"
                                value={newAvoidWord}
                                onChange={(e) => setNewAvoidWord(e.target.value)}
                                onKeyPress={handleKeyPress}
                                disabled={isLoading}
                            />
                            <Button type="button" onClick={addAvoidWord} variant="outline" disabled={isLoading}>
                                Добавить
                            </Button>
                        </div>
                        {(formData.avoidWords?.length ?? 0) > 0 && (
                            <div className="flex flex-wrap gap-2 mt-2">
                                {formData.avoidWords?.map((word, index) => (
                                    <Badge key={index} variant="secondary" className="gap-1">
                                        {word}
                                        <X className="h-3 w-3 cursor-pointer"
                                           onClick={() => !isLoading && removeAvoidWord(word)}/>
                                    </Badge>
                                ))}
                            </div>
                        )}
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="dateFrom">Дата начала парсинга</Label>
                            <Input
                                id="dateFrom"
                                type="date"
                                value={formData.dateFrom?.valueOf()}
                                onChange={(e) => setFormData({...formData, dateFrom: e.target.value})}
                                disabled={isLoading}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="dateTo">Дата окончания парсинга</Label>
                            <Input
                                id="dateTo"
                                type="date"
                                value={formData.dateTo?.valueOf()}
                                onChange={(e) => setFormData({...formData, dateTo: e.target.value})}
                                disabled={isLoading}
                            />
                        </div>
                    </div>

                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}
                                disabled={isLoading}>
                            Отмена
                        </Button>
                        <Button type="submit" disabled={isLoading || schedulesLoading}>
                            {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin"/>}
                            Создать настройку
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
