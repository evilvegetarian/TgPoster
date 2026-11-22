import {useState} from "react"
import {useQueryClient} from "@tanstack/react-query"
import {toast} from "sonner"
import {
    getGetApiV1DiscoverQueryKey,
    useGetApiV1DiscoverIdSimilar,
    usePostApiV1DiscoverIdSimilar,
} from "@/api/endpoints/discover/discover"
import {ChannelName} from "@/components/discover/channel-identity"
import {Badge} from "@/components/ui/badge"
import {Button} from "@/components/ui/button"
import {Checkbox} from "@/components/ui/checkbox"
import {Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle} from "@/components/ui/dialog"

interface SimilarChannelsDialogProps {
    sourceId: string
    sourceName: string
    onClose: () => void
}

export function SimilarChannelsDialog({sourceId, sourceName, onClose}: SimilarChannelsDialogProps) {
    const [selected, setSelected] = useState<number[]>([])
    const queryClient = useQueryClient()
    const {data, isPending, isError, refetch} = useGetApiV1DiscoverIdSimilar(sourceId)
    const {mutate: save, isPending: isSaving} = usePostApiV1DiscoverIdSimilar({
        mutation: {
            onSuccess: async (count) => {
                setSelected([])
                toast.success(count ? `Добавлено в Discover: ${count}` : "Выбранные площадки уже в Discover")
                await Promise.all([refetch(), queryClient.invalidateQueries({queryKey: getGetApiV1DiscoverQueryKey()})])
            },
            onError: (error) => {
                toast.error("Не удалось сохранить площадки", {description: error.title})
                void refetch()
            },
        },
    })
    const channels = data?.channels ?? []
    const selectedIds = channels.flatMap(c =>
        !c.discoverId && c.telegramId != null && selected.includes(c.telegramId) ? [c.telegramId] : [])

    return (
        <Dialog open onOpenChange={(open) => { if (!open) onClose() }}>
            <DialogContent className="sm:max-w-xl">
                <DialogHeader>
                    <DialogTitle>Похожие площадки</DialogTitle>
                    <DialogDescription>
                        {data?.source === "Telegram" ? `Рекомендации Telegram для «${sourceName}»`
                            : data ? `Совпадения по тематике в Discover для «${sourceName}»` : sourceName}
                    </DialogDescription>
                </DialogHeader>
                {isPending ? (
                    <p role="status" className="text-sm text-muted-foreground">Ищем похожие площадки…</p>
                ) : isError ? (
                    <div role="alert" className="space-y-2 text-sm">
                        <p>Не удалось загрузить похожие площадки.</p>
                        <Button size="sm" variant="outline" onClick={() => void refetch()}>Повторить</Button>
                    </div>
                ) : channels.length === 0 ? (
                    <p className="text-sm text-muted-foreground">Похожих площадок пока нет.</p>
                ) : (
                    <ul className="max-h-[52vh] overflow-y-auto divide-y">
                        {channels.map(channel => {
                            const id = channel.telegramId
                            return (
                                <li key={channel.discoverId ?? id} className="flex items-center gap-3 py-2.5">
                                    {!channel.discoverId && id != null && (
                                        <Checkbox aria-label={`Выбрать ${channel.title ?? channel.username}`}
                                                  checked={selected.includes(id)} disabled={isSaving}
                                                  onCheckedChange={checked => setSelected(prev => checked === true
                                                      ? [...prev, id] : prev.filter(value => value !== id))}/>
                                    )}
                                    <div className="flex-1 min-w-0 text-sm">
                                        <ChannelName title={channel.title} username={channel.username} tgUrl={channel.tgUrl}/>
                                        {channel.participantsCount != null && (
                                            <span className="text-xs text-muted-foreground">
                                                Участников: {channel.participantsCount.toLocaleString("ru-RU")}
                                            </span>
                                        )}
                                    </div>
                                    {channel.discoverId && <Badge variant="secondary">В Discover</Badge>}
                                </li>
                            )
                        })}
                    </ul>
                )}
                <DialogFooter>
                    <Button variant="outline" onClick={onClose}>Закрыть</Button>
                    {channels.some(c => !c.discoverId && c.telegramId != null) && (
                        <Button disabled={isSaving || isError || selectedIds.length === 0}
                                onClick={() => save({id: sourceId, data: {telegramIds: selectedIds}})}>
                            {isSaving ? "Сохраняем…" : `Сохранить выбранные (${selectedIds.length})`}
                        </Button>
                    )}
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}
