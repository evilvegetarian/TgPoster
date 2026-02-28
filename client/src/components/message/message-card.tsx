import {useState, useRef, useEffect} from "react"
import {Clock, Youtube} from "lucide-react"
import {format} from "date-fns"
import {ru} from "date-fns/locale"
import {Card, CardContent} from "@/components/ui/card"
import {Checkbox} from "@/components/ui/checkbox"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {FilePreview} from "./file-preview"
import {EditMessageDialog} from "./edit-message-dialog"
import type {MessageResponse} from "@/api/endpoints/tgPosterAPI.schemas"
import {usePostApiV1YoutubeMessageId} from "@/api/endpoints/you-tube-account/you-tube-account"
import {toast} from "sonner"

interface MessageCardProps {
    message: MessageResponse,
    isSelected: boolean,
    onSelectionChange: (selected: boolean) => void,
    availableTimes?: string[] | null,
    onTimeSelect: (time: string) => void
}

export function MessageCard({message, isSelected, onSelectionChange, availableTimes, onTimeSelect}: MessageCardProps) {
    const [isExpanded, setIsExpanded] = useState(false)
    const [isClamped, setIsClamped] = useState(false)
    const textRef = useRef<HTMLParagraphElement>(null)

    useEffect(() => {
        const el = textRef.current
        if (el) {
            setIsClamped(el.scrollHeight > el.clientHeight)
        }
    }, [message.textMessage])

    const {mutate: publishVideo, isPending: isPublishing} = usePostApiV1YoutubeMessageId({
        mutation: {
            onSuccess: () => {
                toast.success("Видео успешно отправлено на публикацию")
            },
            onError: () => {
                toast.error("Ошибка при отправке видео")
            }
        }
    })

    const getStatusBadge = (needApprove: boolean, canApprove: boolean, isSent: boolean) => {
        if (needApprove && !canApprove) {
            return <Badge variant="secondary" className="bg-gray-200 text-gray-700 hover:bg-gray-200">Ожидает подтверждения</Badge>
        }
        if (needApprove && canApprove) {
            return <Badge variant="default" className="bg-blue-600 hover:bg-blue-700">Готов к подтверждению</Badge>
        }
        if (isSent) {
            return <Badge variant="outline" className="text-blue-600 border-blue-600">Отправлено</Badge>
        }
        return <Badge variant="default" className="bg-green-600 hover:bg-green-700">Ожидает отправки</Badge>
    }

    return (
        <>
            <Card className={`transition-colors ${isSelected ? "ring-2 ring-primary" : ""}`}>
                <CardContent className="p-4">
                    <div className="flex items-start gap-3">
                        <Checkbox checked={isSelected} onCheckedChange={onSelectionChange} className="mt-1"/>

                        <div className="flex-1 space-y-3">
                            <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                    {getStatusBadge(message.needApprove, message.canApprove, message.isSent)}
                                    <div className="flex items-center gap-1 text-sm text-muted-foreground">
                                        <Clock className="h-3 w-3"/>
                                        {message.timePosting && format(new Date(message.timePosting), "dd.MM.yyyy HH:mm", {locale: ru})}
                                    </div>
                                </div>
                                <div className="flex items-center gap-2">
                                    {message.hasVideo && message.hasYouTubeAccount && (
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            className="gap-2"
                                            onClick={() => publishVideo({messageId: message.id})}
                                            disabled={isPublishing}
                                        >
                                            <Youtube className="h-4 w-4"/>
                                            {isPublishing ? "Публикация..." : "Опубликовать видео"}
                                        </Button>
                                    )}
                                    <EditMessageDialog
                                        availableTimes={availableTimes}
                                        message={message}
                                        onTimeSelect={onTimeSelect}
                                    />
                                </div>
                            </div>

                            {message.textMessage && (
                                <div className="prose prose-sm max-w-none">
                                    <p
                                        ref={textRef}
                                        className={`text-sm leading-relaxed ${!isExpanded ? "line-clamp-3" : ""}`}
                                    >
                                        {message.textMessage}
                                    </p>
                                    {isClamped && (
                                        <button
                                            type="button"
                                            onClick={() => setIsExpanded(!isExpanded)}
                                            className="text-xs text-primary hover:underline mt-1"
                                        >
                                            {isExpanded ? "Свернуть" : "Показать полностью"}
                                        </button>
                                    )}
                                </div>
                            )}

                            {message.files && message.files.length > 0 && (
                                <div className="space-y-2">
                                    <p className="text-sm font-medium text-muted-foreground">Файлы
                                        ({message.files.length}):</p>
                                    <div className="flex flex-wrap gap-2">
                                        {message.files.map((file) => (
                                            <FilePreview key={file.id} file={file}/>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                </CardContent>
            </Card>


        </>
    )
}
