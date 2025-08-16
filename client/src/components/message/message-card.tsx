import { useState } from "react"
import { Edit, Clock } from "lucide-react"
import { format } from "date-fns"
import { ru } from "date-fns/locale"
import { Card, CardContent } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Badge } from "@/components/ui/badge"
import { FilePreview } from "./file-preview"
import { EditMessageDialog } from "./edit-message-dialog"
import type { MessageResponse } from "@/api/endpoints/tgPosterAPI.schemas"

interface MessageCardProps {
    message: MessageResponse
    isSelected: boolean
    onSelectionChange: (selected: boolean) => void
}

export function MessageCard({ message, isSelected, onSelectionChange }: MessageCardProps) {
    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)

    const getStatusBadge = (needApprove: boolean, canApprove: boolean) => {
        if (needApprove && !canApprove) {
            return <Badge variant="secondary">Ожидает подтверждения</Badge>
        }
        if (needApprove && canApprove) {
            return <Badge variant="outline">Готов к подтверждению</Badge>
        }
        return <Badge variant="default">Подтверждено</Badge>
    }

    return (
        <>
            <Card className={`transition-colors ${isSelected ? "ring-2 ring-primary" : ""}`}>
                <CardContent className="p-4">
                    <div className="flex items-start gap-3">
                        {/* Checkbox для выбора */}
                        <Checkbox checked={isSelected} onCheckedChange={onSelectionChange} className="mt-1" />

                        <div className="flex-1 space-y-3">
                            {/* Заголовок со статусом и кнопкой редактирования */}
                            <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                    {getStatusBadge(message.needApprove, message.canApprove)}
                                    <div className="flex items-center gap-1 text-sm text-muted-foreground">
                                        <Clock className="h-3 w-3" />
                                        {message.timePosting && format(new Date(message.timePosting), "dd.MM.yyyy HH:mm", { locale: ru })}
                                    </div>
                                </div>
                                <Button variant="ghost" size="sm" onClick={() => setIsEditDialogOpen(true)}>
                                    <Edit className="h-4 w-4 mr-1" />
                                    Редактировать
                                </Button>
                            </div>

                            {/* Текст сообщения */}
                            {message.textMessage && (
                                <div className="prose prose-sm max-w-none">
                                    <p className="text-sm leading-relaxed">{message.textMessage}</p>
                                </div>
                            )}

                            {/* Файлы */}
                            {message.files && message.files.length > 0 && (
                                <div className="space-y-2">
                                    <p className="text-sm font-medium text-muted-foreground">Файлы ({message.files.length}):</p>
                                    <div className="flex flex-wrap gap-2">
                                        {message.files.map((file) => (
                                            <FilePreview key={file.id} file={file} />
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                </CardContent>
            </Card>

            <EditMessageDialog message={message} isOpen={isEditDialogOpen} onClose={() => setIsEditDialogOpen(false)} />
        </>
    )
}
