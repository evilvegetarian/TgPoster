import {useState, useRef, useEffect} from "react"
import {Check, Clock, Loader2, RotateCw, Trash2, Youtube} from "lucide-react"
import {format} from "date-fns"
import {ru} from "date-fns/locale"
import {useQueryClient} from "@tanstack/react-query"
import {Card, CardContent} from "@/components/ui/card"
import {Checkbox} from "@/components/ui/checkbox"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger
} from "@/components/ui/alert-dialog"
import {MediaAlbum} from "./media-album"
import {EditMessageDialog} from "./edit-message-dialog"
import {CrossPostStatusBadges} from "@/components/cross-post/cross-post-status-badges.tsx"
import {Tooltip, TooltipContent, TooltipTrigger} from "@/components/ui/tooltip"
import {MessageDeliveryStatus, RetryMode} from "@/api/endpoints/tgPosterAPI.schemas"
import type {MessageResponse} from "@/api/endpoints/tgPosterAPI.schemas"
import {usePostApiV1YoutubeMessageId} from "@/api/endpoints/you-tube-account/you-tube-account"
import {
    useDeleteApiV1Message,
    useDeleteApiV1MessageIdChannel,
    usePatchApiV1Message,
    usePostApiV1MessageRetry
} from "@/api/endpoints/message/message"
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
    const queryClient = useQueryClient()

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

    const {mutate: deleteMessage, isPending: isDeleting} = useDeleteApiV1Message({
        mutation: {
            onSuccess: () => {
                toast.success("Сообщение удалено")
                queryClient.invalidateQueries({queryKey: ["/api/v1/message"]})
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title ?? "Не удалось удалить сообщение"})
            }
        }
    })

    const {mutate: deleteFromChannel, isPending: isDeletingFromChannel} = useDeleteApiV1MessageIdChannel({
        mutation: {
            onSuccess: () => {
                toast.success("Пост удалён из канала")
                queryClient.invalidateQueries({queryKey: ["/api/v1/message"]})
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title ?? "Не удалось удалить пост из канала"})
            }
        }
    })

    const {mutate: retryMessage, isPending: isRetrying} = usePostApiV1MessageRetry({
        mutation: {
            onSuccess: (response) => {
                const retried = response.items[0]
                toast.success("Повтор запланирован", {
                    description: retried
                        ? `Новое время отправки: ${format(new Date(retried.timePosting), "dd.MM.yyyy HH:mm", {locale: ru})}`
                        : undefined
                })
                queryClient.invalidateQueries({queryKey: ["/api/v1/message"]})
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title ?? "Не удалось повторить отправку"})
            }
        }
    })

    const {mutate: approveMessage, isPending: isApproving} = usePatchApiV1Message({
        mutation: {
            onSuccess: () => {
                toast.success("Пост подтверждён")
                queryClient.invalidateQueries({queryKey: ["/api/v1/message"]})
            },
            onError: (error) => {
                toast.error("Ошибка", {description: error.title ?? "Не удалось подтвердить пост"})
            }
        }
    })

    const isFailed = message.deliveryStatus === MessageDeliveryStatus.Failed
    const canBeApproved = message.needApprove && !message.isSent
    const isTimePassed = !!message.timePosting && new Date(message.timePosting).getTime() < Date.now()

    const getStatusBadge = (needApprove: boolean, canApprove: boolean, isSent: boolean) => {
        if (message.deliveryStatus === MessageDeliveryStatus.Failed) {
            const badge = <Badge variant="destructive">Ошибка отправки</Badge>
            if (!message.sendError) return badge
            return (
                <Tooltip>
                    <TooltipTrigger asChild>{badge}</TooltipTrigger>
                    <TooltipContent className="max-w-xs">{message.sendError}</TooltipContent>
                </Tooltip>
            )
        }
        if (message.deliveryStatus === MessageDeliveryStatus.Sending) {
            return <Badge variant="default" className="bg-amber-600 hover:bg-amber-600">Отправляется</Badge>
        }
        if (message.deliveryStatus === MessageDeliveryStatus.Cancelled) {
            return <Badge variant="secondary" className="bg-gray-200 text-gray-700 hover:bg-gray-200">Отменён</Badge>
        }
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
                                <div className="flex flex-wrap items-center gap-2">
                                    {getStatusBadge(message.needApprove, message.canApprove, message.isSent)}
                                    <CrossPostStatusBadges message={message}/>
                                    <div className="flex items-center gap-1 text-sm text-muted-foreground">
                                        <Clock className="h-3 w-3"/>
                                        {message.timePosting && format(new Date(message.timePosting), "dd.MM.yyyy HH:mm", {locale: ru})}
                                    </div>
                                </div>
                                <div className="flex flex-wrap items-center justify-end gap-2">
                                    {isFailed && (
                                        <>
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                className="gap-2"
                                                disabled={isRetrying}
                                                onClick={() => retryMessage({data: {ids: [message.id], mode: RetryMode.Now}})}
                                            >
                                                <RotateCw className="h-4 w-4"/>
                                                Повторить сейчас
                                            </Button>
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                disabled={isRetrying}
                                                onClick={() => retryMessage({data: {ids: [message.id], mode: RetryMode.NextFreeSlot}})}
                                            >
                                                В свободный слот
                                            </Button>
                                        </>
                                    )}
                                    {canBeApproved && (isTimePassed ? (
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <Button
                                                    size="sm"
                                                    className="gap-2"
                                                    disabled={isApproving}
                                                    onClick={() => approveMessage({data: {messagesIds: [message.id]}})}
                                                >
                                                    <Check className="h-4 w-4"/>
                                                    Подтвердить
                                                </Button>
                                            </TooltipTrigger>
                                            <TooltipContent className="max-w-xs">
                                                Время прошло — после подтверждения пост встанет в ближайший свободный слот
                                            </TooltipContent>
                                        </Tooltip>
                                    ) : (
                                        <Button
                                            size="sm"
                                            className="gap-2"
                                            disabled={isApproving}
                                            onClick={() => approveMessage({data: {messagesIds: [message.id]}})}
                                        >
                                            <Check className="h-4 w-4"/>
                                            Подтвердить
                                        </Button>
                                    ))}
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
                                    <AlertDialog>
                                        <AlertDialogTrigger asChild>
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                className="text-destructive hover:text-destructive"
                                                disabled={isDeleting || isDeletingFromChannel}
                                            >
                                                {isDeleting || isDeletingFromChannel
                                                    ? <Loader2 className="h-4 w-4 mr-1 animate-spin"/>
                                                    : <Trash2 className="h-4 w-4 mr-1"/>}
                                                Удалить
                                            </Button>
                                        </AlertDialogTrigger>
                                        <AlertDialogContent>
                                            <AlertDialogHeader>
                                                <AlertDialogTitle>Удалить сообщение?</AlertDialogTitle>
                                                <AlertDialogDescription>
                                                    {message.isSent
                                                        ? `Пост уже опубликован в канале. ${message.canDeleteFromChannel && message.deleteFromChannelUntil
                                                            ? `Удалить из канала можно до ${format(new Date(message.deleteFromChannelUntil), "dd.MM.yyyy HH:mm", {locale: ru})}. `
                                                            : ""}«Убрать только из списка» не удалит его из канала.`
                                                        : "Это действие нельзя отменить. Сообщение будет удалено безвозвратно"}
                                                </AlertDialogDescription>
                                            </AlertDialogHeader>
                                            <AlertDialogFooter>
                                                <AlertDialogCancel>Отмена</AlertDialogCancel>
                                                {message.isSent ? (
                                                    <>
                                                        <AlertDialogAction
                                                            className="bg-secondary text-secondary-foreground hover:bg-secondary/80"
                                                            onClick={() => deleteMessage({data: [message.id]})}
                                                        >
                                                            Убрать только из списка
                                                        </AlertDialogAction>
                                                        {message.canDeleteFromChannel ? (
                                                            <AlertDialogAction
                                                                onClick={() => deleteFromChannel({id: message.id})}
                                                            >
                                                                Удалить из канала
                                                            </AlertDialogAction>
                                                        ) : (
                                                            <Tooltip>
                                                                <TooltipTrigger asChild>
                                                                    <span tabIndex={0}>
                                                                        <Button disabled>Удалить из канала</Button>
                                                                    </span>
                                                                </TooltipTrigger>
                                                                <TooltipContent className="max-w-xs">
                                                                    {message.deleteFromChannelUntil
                                                                        ? "Прошло больше 48 часов — Telegram не даёт боту удалить пост"
                                                                        : "Пост опубликован до обновления — удалите его в Telegram вручную"}
                                                                </TooltipContent>
                                                            </Tooltip>
                                                        )}
                                                    </>
                                                ) : (
                                                    <AlertDialogAction onClick={() => deleteMessage({data: [message.id]})}>
                                                        Удалить
                                                    </AlertDialogAction>
                                                )}
                                            </AlertDialogFooter>
                                        </AlertDialogContent>
                                    </AlertDialog>
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
                                <MediaAlbum files={message.files}/>
                            )}
                        </div>
                    </div>
                </CardContent>
            </Card>


        </>
    )
}
