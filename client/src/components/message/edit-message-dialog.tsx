import type React from "react"
import {useEffect, useState} from "react"
import {Upload, X} from "lucide-react"
import {Button} from "@/components/ui/button"
import {Dialog, DialogContent, DialogHeader, DialogTitle} from "@/components/ui/dialog"
import {Input} from "@/components/ui/input"
import {Label} from "@/components/ui/label"
import {FilePreview} from "./file-preview"
import type {MessageResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {usePutApiV1MessageId} from "@/api/endpoints/message/message.ts";
import {Textarea} from "@/components/ui/textarea.tsx";
import {toast} from "sonner";
import {Badge} from "@/components/ui/badge.tsx";
import {format} from "date-fns"
import {ru} from "date-fns/locale";


interface EditMessageDialogProps {
    message: MessageResponse | null,
    isOpen: boolean,
    onClose: () => void,
    availableTimes?: string[] | null
}

const utcToLocalDatetimeString = (utcString: string): string => {
    const date = new Date(utcString);
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
        .toISOString()
        .slice(0, 16);
};

export function EditMessageDialog({message, isOpen, onClose, availableTimes}: EditMessageDialogProps) {
    const [textMessage, setTextMessage] = useState("")
    const [timePosting, setTimePosting] = useState("")
    const [oldFiles, setOldFiles] = useState<string[]>([])
    const [newFiles, setNewFiles] = useState<File[]>([])
    const [times, setTimes] = useState<string[]>([])

    const updateMessage = usePutApiV1MessageId({
        mutation: {
            onSuccess: () => {
                toast.success("Успех", {description: "Сообщение обновлено успешно"})
                onClose()
            },
            onError: () => {
                toast.error("Ошибка", {description: "Не удалось обновить сообщение"})
            },
        },
    })

    useEffect(() => {
        if (message) {
            setTextMessage(message.textMessage || "")
            setTimePosting(message.timePosting
                ? utcToLocalDatetimeString(message.timePosting)
                : "");
            setOldFiles(message.files?.map((f) => f.id) || [])
            setNewFiles([])
            setTimes(availableTimes || [])
        }
    }, [message, availableTimes])

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()

        if (!message || !timePosting) {
            toast.error("Ошибка", {description: "Укажите время публикации"})
            return
        }

        updateMessage.mutate({
            id: message.id,
            data: {
                ScheduleId: message.scheduleId,
                TimePosting: new Date(timePosting).toISOString(),
                TextMessage: textMessage || undefined,
                OldFiles: oldFiles.length > 0 ? oldFiles : undefined,
                NewFiles: newFiles.length > 0 ? newFiles : undefined,
            },
        })
    }

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files) {
            setNewFiles((prev) => [...prev, ...Array.from(e.target.files!)])
        }
    }

    const removeOldFile = (fileId: string) => {
        setOldFiles((prev) => prev.filter((id) => id !== fileId))
    }

    const removeNewFile = (index: number) => {
        setNewFiles((prev) => prev.filter((_, i) => i !== index))
    }

    const addTime = (localTimeString: string) => {
        setTimePosting(localTimeString)

        const selectedUtcTime = new Date(localTimeString).toISOString();
        setTimes(availableTimes?.filter(a => new Date(a).getTime() !== new Date(selectedUtcTime).getTime()) || [])
    }

    if (!message)
        return null

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent className="max-w-2xl">
                <DialogHeader>
                    <DialogTitle>Редактирование сообщения</DialogTitle>
                </DialogHeader>

                <form onSubmit={handleSubmit} className="space-y-4">
                    <div className="space-y-2">
                        <Label htmlFor="text">Текст сообщения</Label>
                        <Textarea
                            id="text"
                            value={textMessage}
                            onChange={(e) => setTextMessage(e.target.value)}
                            placeholder="Введите текст сообщения..."
                            rows={4}
                        />
                    </div>

                    <div className="space-y-2">
                        <Label htmlFor="time">Время публикации (местное время)</Label>
                        <Input
                            id="time"
                            type="datetime-local"
                            value={timePosting}
                            onChange={(e) => setTimePosting(e.target.value)}
                            required
                        />
                    </div>

                    {/* Блок с подсказками по времени */}
                    {times.length > 0 && (
                        <div className="space-y-2 rounded-md border p-3">
                            <Label>Свободное время по расписанию:</Label>
                            <div className="flex flex-wrap gap-2">
                                {times.slice(0, 4).map((time) => (
                                    <Badge
                                        key={time}
                                        variant={message.timePosting === time ? "default" : "secondary"}
                                        className="cursor-pointer"
                                        onClick={() => addTime(utcToLocalDatetimeString(time))}
                                    >
                                        {format(new Date(time), "dd MMM HH:mm", {locale: ru})}
                                    </Badge>
                                ))}
                            </div>
                        </div>
                    )}

                    <div className="space-y-2">
                        <Label>Файлы</Label>

                        {/* Существующие файлы */}
                        {message.files && message.files.length > 0 && (
                            <div className="space-y-2">
                                <p className="text-sm text-muted-foreground">Существующие файлы:</p>
                                <div className="grid grid-cols-4 gap-2">
                                    {message.files
                                        .filter((file) => oldFiles.includes(file.id))
                                        .map((file) => (
                                            <FilePreview key={file.id} file={file} showRemoveButton
                                                         onRemove={() => removeOldFile(file.id)}/>
                                        ))}
                                </div>
                            </div>
                        )}

                        {/* Новые файлы */}
                        {newFiles.length > 0 && (
                            <div className="space-y-2">
                                <p className="text-sm text-muted-foreground">Новые файлы:</p>
                                <div className="grid grid-cols-4 gap-2">
                                    {newFiles.map((file, index) => (
                                        <div key={index} className="relative group">
                                            <div
                                                className="w-20 h-20 bg-muted rounded-lg flex items-center justify-center">
                                                <span className="text-xs text-center p-1">{file.name}</span>
                                            </div>
                                            <Button
                                                type="button"
                                                variant="destructive"
                                                size="icon"
                                                className="absolute -top-2 -right-2 h-6 w-6 rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
                                                onClick={() => removeNewFile(index)}
                                            >
                                                <X className="h-3 w-3"/>
                                            </Button>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}

                        {/* Добавление новых файлов */}
                        <div>
                            <Input
                                type="file"
                                multiple
                                accept="image/*,video/*"
                                onChange={handleFileChange}
                                className="hidden"
                                id="file-upload-edit"
                            />
                            <Label htmlFor="file-upload-edit" className="cursor-pointer">
                                <div
                                    className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-4 text-center hover:border-muted-foreground/50 transition-colors">
                                    <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground"/>
                                    <p className="text-sm text-muted-foreground">Добавить новые файлы</p>
                                </div>
                            </Label>
                        </div>
                    </div>

                    <div className="flex justify-end gap-2">
                        <Button type="button" variant="outline" onClick={onClose}>
                            Отмена
                        </Button>
                        <Button type="submit" disabled={updateMessage.isPending}>
                            {updateMessage.isPending ? "Сохранение..." : "Сохранить"}
                        </Button>
                    </div>
                </form>
            </DialogContent>
        </Dialog>
    )
}