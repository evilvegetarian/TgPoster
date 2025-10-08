import type React from "react"
import {useState} from "react"
import {Plus, Upload, X} from "lucide-react"
import {format} from "date-fns"
import {Button} from "@/components/ui/button"
import {Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger} from "@/components/ui/dialog"
import {Textarea} from "@/components/ui/textarea"
import {Input} from "@/components/ui/input"
import {Label} from "@/components/ui/label"

import {toast} from "sonner"
import {usePostApiV1Message} from "@/api/endpoints/message/message.ts";
import {ru} from "date-fns/locale"
import {Badge} from "@/components/ui/badge.tsx";

interface CreateMessageDialogProps {
    scheduleId: string,
    availableTimes?: string[] | null,
    onTimeSelect: (time: string) => void
}

const utcToLocalDatetimeString = (utcString: string): string => {
    const date = new Date(utcString);
    const localISOTime = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
        .toISOString()
        .slice(0, 16);
    return localISOTime;
};

export function CreateMessageDialog({scheduleId, availableTimes, onTimeSelect}: CreateMessageDialogProps) {
    const [isOpen, setIsOpen] = useState(false)
    const [textMessage, setTextMessage] = useState("")
    const [timePosting, setTimePosting] = useState("")
    const [files, setFiles] = useState<File[]>([])

    const createMessage = usePostApiV1Message({
        mutation: {
            onSuccess: () => {
                toast.success("Сообщение создано успешно")
                setTextMessage("")
                setTimePosting("")
                setFiles([])
                setIsOpen(false)
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title || "Не удалось создать сообщение"})
            },
        },
    })

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()

        if (!timePosting) {
            toast.error("Ошибка", {description: "Укажите время публикации"})
            return
        }

        createMessage.mutate({
            data: {
                ScheduleId: scheduleId,
                TimePosting: timePosting,
                TextMessage: textMessage || undefined,
                Files: files.length > 0 ? files : undefined,
            },
        })
    }

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files) {
            setFiles((prev) => [...prev, ...Array.from(e.target.files!)])
        }
    }

    const removeFile = (index: number) => {
        setFiles((prev) => prev.filter((_, i) => i !== index))
    }

    return (
        <Dialog open={isOpen} onOpenChange={setIsOpen}>
            <DialogTrigger asChild>
                <Button>
                    <Plus className="h-4 w-4 mr-2"/>
                    Создать новое сообщение
                </Button>
            </DialogTrigger>

            <DialogContent className="max-w-2xl">
                <DialogHeader>
                    <DialogTitle>Создание нового сообщения</DialogTitle>
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
                        <Label htmlFor="time">Время публикации</Label>
                        <Input
                            id="time"
                            type="datetime-local"
                            value={timePosting}
                            onChange={(e) => setTimePosting(e.target.value)}
                            required
                        />
                    </div>
                    {availableTimes && availableTimes.length > 0 && (
                        <div className="space-y-2 rounded-md border p-3">
                            <Label>Свободное время по расписанию:</Label>
                            <div className="flex flex-wrap gap-2">
                                {availableTimes.slice(0, 4).map((time) => (
                                    <Badge
                                        key={time}
                                        variant="secondary"
                                        className="cursor-pointer"
                                        onClick={() => {
                                            setTimePosting(utcToLocalDatetimeString(time))
                                            onTimeSelect(time)
                                        }}
                                    >
                                        {format(new Date(time), "dd MMM HH:mm", {locale: ru})}
                                    </Badge>
                                ))}
                            </div>
                        </div>
                    )}
                    <div className="space-y-2">
                        <Label>Файлы</Label>
                        <div className="space-y-2">
                            <Input
                                type="file"
                                multiple
                                accept="image/*,video/*"
                                onChange={handleFileChange}
                                className="hidden"
                                id="file-upload"
                            />
                            <Label htmlFor="file-upload" className="cursor-pointer">
                                <div
                                    className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-4 text-center hover:border-muted-foreground/50 transition-colors">
                                    <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground"/>
                                    <p className="text-sm text-muted-foreground">Нажмите для выбора файлов</p>
                                </div>
                            </Label>

                            {files.length > 0 && (
                                <div className="grid grid-cols-4 gap-2">
                                    {files.map((file, index) => (
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
                                                onClick={() => removeFile(index)}
                                            >
                                                <X className="h-3 w-3"/>
                                            </Button>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>

                    <div className="flex justify-end gap-2">
                        <Button type="button" variant="outline" onClick={() => setIsOpen(false)}>
                            Отмена
                        </Button>
                        <Button type="submit" disabled={createMessage.isPending}>
                            {createMessage.isPending ? "Создание..." : "Создать"}
                        </Button>
                    </div>
                </form>
            </DialogContent>
        </Dialog>
    )
}
