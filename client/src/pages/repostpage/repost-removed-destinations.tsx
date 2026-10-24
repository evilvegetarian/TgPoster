import {useState} from "react";
import {format} from "date-fns";
import {ru} from "date-fns/locale";
import {Loader2, Trash2, Undo2} from "lucide-react";
import {toast} from "sonner";
import {Badge} from "@/components/ui/badge";
import {Button} from "@/components/ui/button";
import {ConfirmActionDialog} from "@/pages/repostpage/confirm-action-dialog.tsx";
import {destinationTitle} from "@/pages/repostpage/repost-destination-utils.ts";
import {
    usePostApiV1RepostSettingsSettingsIdDestinationsIdRestore,
    usePostApiV1RepostSettingsSettingsIdDestinationsPurge,
    usePutApiV1RepostDestinationsId,
} from "@/api/endpoints/repost/repost.ts";
import type {RepostDestinationDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {ChatStatus} from "@/api/endpoints/tgPosterAPI.schemas.ts";

function formatDate(value: string | null | undefined): string | null {
    return value ? format(new Date(value), "d MMM yyyy, HH:mm", {locale: ru}) : null;
}

function RemovedReason({destination}: {destination: RepostDestinationDto}) {
    const isBanned = destination.chatStatus === ChatStatus.Banned;
    const bannedAt = formatDate(destination.bannedAt);
    const deletedAt = formatDate(destination.deletedAt);

    return (
        <div className="flex flex-col gap-0.5 text-xs text-muted-foreground">
            <div className="flex flex-wrap items-center gap-1.5">
                {destination.deletedAt && <Badge variant="secondary" className="text-xs">Удалён</Badge>}
                {isBanned && <Badge variant="destructive" className="text-xs">Забанен</Badge>}
                {destination.chatStatus === ChatStatus.Left && (
                    <Badge variant="outline" className="text-xs">Нет в чате</Badge>
                )}
                {deletedAt && <span>удалён {deletedAt}</span>}
                {bannedAt && <span>бан {bannedAt}</span>}
                {!deletedAt && !isBanned && destination.chatStatus === ChatStatus.Left && (
                    <span>аккаунт не состоит в чате: выгнали, вышел или чат удалён</span>
                )}
            </div>
            {destination.banError && (
                <span className="font-mono text-red-600 truncate" title={destination.banError}>
                    {destination.banError}
                </span>
            )}
        </div>
    );
}

interface RemovedDestinationRowProps {
    settingsId: string;
    destination: RepostDestinationDto;
    onChanged: () => void;
}

function RemovedDestinationRow({settingsId, destination, onChanged}: RemovedDestinationRowProps) {
    const [isPurgeOpen, setIsPurgeOpen] = useState(false);
    const isRemoved = destination.deletedAt != null;

    const {mutate: restore, isPending: isRestoring} = usePostApiV1RepostSettingsSettingsIdDestinationsIdRestore({
        mutation: {
            onSuccess: () => {
                toast.success("Канал возвращён в список", {
                    description: "Он выключен: включите его, чтобы проверить доступ",
                });
                onChanged();
            },
            onError: (error) => {
                toast.error("Не удалось вернуть канал", {description: error.title || "Попробуйте ещё раз"});
            },
        },
    });

    const {mutate: enable, isPending: isEnabling} = usePutApiV1RepostDestinationsId({
        mutation: {
            onSuccess: () => {
                toast.success("Доступ подтверждён, канал снова в списке и включён");
                onChanged();
            },
            onError: (error) => {
                toast.error("Не удалось вернуть канал", {
                    description: error.title || "Telegram по-прежнему не пускает аккаунт в чат",
                });
            },
        },
    });

    const {mutate: purge, isPending: isPurging} = usePostApiV1RepostSettingsSettingsIdDestinationsPurge({
        mutation: {
            onSuccess: () => {
                toast.success("Канал удалён насовсем");
                setIsPurgeOpen(false);
                onChanged();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {description: error.title || "Не удалось удалить канал"});
            },
        },
    });

    function handleReturn() {
        if (isRemoved) {
            restore({settingsId, id: destination.id});
            return;
        }

        // Включение одного канала заново проверяет доступ и снимает бан, только если проверка прошла
        enable({
            id: destination.id,
            data: {
                isActive: true,
                delayMinSeconds: destination.delayMinSeconds ?? 0,
                delayMaxSeconds: destination.delayMaxSeconds ?? 0,
                repostEveryNth: destination.repostEveryNth ?? 1,
                skipProbability: destination.skipProbability ?? 0,
                maxRepostsPerDay: destination.maxRepostsPerDay,
            },
        });
    }

    const isBusy = isRestoring || isEnabling || isPurging;

    return (
        <div className="flex items-center justify-between gap-3 p-3 border rounded bg-muted/30">
            <div className="flex items-center gap-3 flex-1 min-w-0">
                {destination.avatarBase64 ? (
                    <img
                        src={destination.avatarBase64}
                        alt=""
                        className="h-9 w-9 rounded-full object-cover shrink-0 opacity-70"
                    />
                ) : (
                    <div className="h-9 w-9 rounded-full bg-muted flex items-center justify-center text-xs font-medium shrink-0">
                        {destination.title?.[0] ?? "?"}
                    </div>
                )}
                <div className="flex flex-col gap-1 min-w-0">
                    <div className="flex items-center gap-2 min-w-0">
                        <span className="text-sm font-medium truncate">{destinationTitle(destination)}</span>
                        {destination.username && (
                            <a
                                href={`https://t.me/${destination.username}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="text-xs text-muted-foreground hover:underline shrink-0"
                            >
                                @{destination.username}
                            </a>
                        )}
                    </div>
                    <RemovedReason destination={destination}/>
                </div>
            </div>
            <div className="flex items-center gap-1 shrink-0">
                <Button
                    variant="outline"
                    size="sm"
                    className="h-7 gap-1 text-xs"
                    disabled={isBusy}
                    onClick={handleReturn}
                    title={isRemoved
                        ? "Вернуть в общий список выключенным"
                        : "Проверить доступ заново: аккаунт вступит в чат, бан снимется, если Telegram пустит"}
                >
                    {isRestoring || isEnabling ? <Loader2 className="h-3 w-3 animate-spin"/> : <Undo2 className="h-3 w-3"/>}
                    Вернуть
                </Button>
                <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                    disabled={isBusy}
                    onClick={() => setIsPurgeOpen(true)}
                    title="Удалить насовсем"
                >
                    <Trash2 className="h-3.5 w-3.5"/>
                </Button>
            </div>

            <ConfirmActionDialog
                open={isPurgeOpen}
                onOpenChange={setIsPurgeOpen}
                title="Удалить канал насовсем?"
                description={
                    <>
                        <strong>{destinationTitle(destination)}</strong> пропадёт и из этого списка. Вернуть его можно
                        будет, только добавив заново.
                    </>
                }
                confirmLabel="Удалить насовсем"
                isPending={isPurging}
                onConfirm={() => purge({settingsId, data: {destinationIds: [destination.id]}})}
            />
        </div>
    );
}

interface RepostRemovedDestinationsProps {
    settingsId: string;
    destinations: RepostDestinationDto[];
    onChanged: () => void;
}

// Каналы, куда репост не дойдёт или которые убраны из списка: баны, чаты без аккаунта и удалённые вручную
export function RepostRemovedDestinations({settingsId, destinations, onChanged}: RepostRemovedDestinationsProps) {
    const [isPurgeAllOpen, setIsPurgeAllOpen] = useState(false);

    const {mutate: purgeAll, isPending: isPurgingAll} = usePostApiV1RepostSettingsSettingsIdDestinationsPurge({
        mutation: {
            onSuccess: (_, variables) => {
                toast.success(`Удалено насовсем: ${variables.data.destinationIds.length}`);
                setIsPurgeAllOpen(false);
                onChanged();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {description: error.title || "Не удалось удалить каналы"});
            },
        },
    });

    if (destinations.length === 0) {
        return (
            <p className="text-sm text-muted-foreground p-4 border rounded text-center">
                Банов и удалённых каналов нет
            </p>
        );
    }

    return (
        <div className="space-y-2">
            <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-xs text-muted-foreground">
                    Сюда репост не идёт. «Вернуть» заново проверяет доступ, удалённые вручную возвращаются выключенными
                </span>
                <Button
                    variant="outline"
                    size="sm"
                    className="h-7 gap-1 text-xs text-destructive hover:text-destructive"
                    disabled={isPurgingAll}
                    onClick={() => setIsPurgeAllOpen(true)}
                >
                    <Trash2 className="h-3 w-3"/>
                    Удалить насовсем ({destinations.length})
                </Button>
            </div>

            {destinations.map((dest) => (
                <RemovedDestinationRow key={dest.id} settingsId={settingsId} destination={dest} onChanged={onChanged}/>
            ))}

            <ConfirmActionDialog
                open={isPurgeAllOpen}
                onOpenChange={setIsPurgeAllOpen}
                title={`Удалить насовсем: ${destinations.length}?`}
                description="Все показанные каналы пропадут и из этого списка. Вернуть их можно будет, только добавив заново."
                confirmLabel="Удалить насовсем"
                isPending={isPurgingAll}
                onConfirm={() => purgeAll({settingsId, data: {destinationIds: destinations.map((d) => d.id)}})}
            />
        </div>
    );
}
