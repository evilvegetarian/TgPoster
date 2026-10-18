import {useState} from "react";
import {Power, PowerOff, Search, Trash2} from "lucide-react";
import {toast} from "sonner";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Tabs, TabsList, TabsTrigger} from "@/components/ui/tabs";
import {ConfirmActionDialog} from "@/pages/repostpage/confirm-action-dialog.tsx";
import {RepostDestinationRow} from "@/pages/repostpage/repost-destination-row.tsx";
import {canBeEnabled, isProblemDestination} from "@/pages/repostpage/repost-destination-utils.ts";
import {
    usePostApiV1RepostSettingsSettingsIdDestinationsDelete,
    usePutApiV1RepostSettingsSettingsIdDestinationsActive,
} from "@/api/endpoints/repost/repost.ts";
import type {
    RepostDestinationDto,
    SetRepostDestinationsActiveResponse,
} from "@/api/endpoints/tgPosterAPI.schemas.ts";

type DestinationFilter = "all" | "active" | "inactive" | "problem";

const FILTERS: {value: DestinationFilter; label: string; match: (d: RepostDestinationDto) => boolean}[] = [
    {value: "all", label: "Все", match: () => true},
    {value: "active", label: "Вкл", match: (d) => d.isActive},
    {value: "inactive", label: "Выкл", match: (d) => !d.isActive},
    {value: "problem", label: "Проблемные", match: isProblemDestination},
];

function matchesSearch(destination: RepostDestinationDto, query: string): boolean {
    if (!query) return true;
    return (destination.title?.toLowerCase().includes(query) ?? false)
        || (destination.username?.toLowerCase().includes(query) ?? false)
        || String(destination.chatId).includes(query);
}

function showEnableResult(result: SetRepostDestinationsActiveResponse) {
    const skipped = result.notMemberCount + result.noPermissionCount;
    if (skipped === 0) {
        toast.success(`Включено каналов: ${result.updatedCount}`);
        return;
    }

    const reasons: string[] = [];
    if (result.notMemberCount > 0) {
        reasons.push(`${result.notMemberCount} — аккаунт не состоит в чате, включите их по одному: тогда аккаунт вступит заново`);
    }
    if (result.noPermissionCount > 0) {
        reasons.push(`${result.noPermissionCount} — нет прав на отправку сообщений или медиа`);
    }

    toast.warning(`Включено ${result.updatedCount}, пропущено ${skipped}`, {
        description: reasons.join(". "),
        duration: 10000,
    });
}

interface RepostDestinationsListProps {
    settingsId: string;
    destinations: RepostDestinationDto[];
    onChanged: () => void;
}

export function RepostDestinationsList({settingsId, destinations, onChanged}: RepostDestinationsListProps) {
    const [filter, setFilter] = useState<DestinationFilter>("all");
    const [search, setSearch] = useState("");
    const [isBulkDeleteOpen, setIsBulkDeleteOpen] = useState(false);

    const query = search.trim().toLowerCase();
    const activeFilter = FILTERS.find((f) => f.value === filter) ?? FILTERS[0];
    const visible = destinations.filter((d) => activeFilter.match(d) && matchesSearch(d, query));
    const toEnable = visible.filter(canBeEnabled);
    const toDisable = visible.filter((d) => d.isActive);

    const {mutate: setActive, isPending: isSwitching} = usePutApiV1RepostSettingsSettingsIdDestinationsActive({
        mutation: {
            onSuccess: (result, variables) => {
                if (variables.data.isActive) {
                    showEnableResult(result);
                } else {
                    toast.success(`Выключено каналов: ${result.updatedCount}`);
                }
                onChanged();
            },
            onError: (error) => {
                toast.error("Ошибка переключения каналов", {
                    description: error.title || "Не удалось изменить статус каналов",
                });
            },
        },
    });

    const {mutate: deleteMany, isPending: isDeletingMany} = usePostApiV1RepostSettingsSettingsIdDestinationsDelete({
        mutation: {
            onSuccess: (_, variables) => {
                toast.success(`Удалено каналов: ${variables.data.destinationIds.length}`);
                setIsBulkDeleteOpen(false);
                onChanged();
            },
            onError: (error) => {
                toast.error("Ошибка удаления", {
                    description: error.title || "Не удалось удалить каналы",
                });
            },
        },
    });

    const isBulkPending = isSwitching || isDeletingMany;

    function handleSetActive(targets: RepostDestinationDto[], isActive: boolean) {
        setActive({settingsId, data: {destinationIds: targets.map((d) => d.id), isActive}});
    }

    function handleDeleteVisible() {
        deleteMany({settingsId, data: {destinationIds: visible.map((d) => d.id)}});
    }

    return (
        <div className="space-y-3">
            <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                <Tabs value={filter} onValueChange={(value) => setFilter(value as DestinationFilter)}>
                    <TabsList>
                        {FILTERS.map((f) => (
                            <TabsTrigger key={f.value} value={f.value} className="text-xs">
                                {f.label} ({destinations.filter(f.match).length})
                            </TabsTrigger>
                        ))}
                    </TabsList>
                </Tabs>
                <div className="relative md:w-64">
                    <Search className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground"/>
                    <Input
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        placeholder="Название, @username или ID"
                        className="h-8 pl-8 text-sm"
                    />
                </div>
            </div>

            {visible.length > 0 && (
                <div className="flex flex-wrap items-center gap-2">
                    <span className="text-xs text-muted-foreground mr-1">
                        Показано {visible.length} из {destinations.length}:
                    </span>
                    <Button
                        variant="outline"
                        size="sm"
                        className="h-7 gap-1 text-xs"
                        disabled={toEnable.length === 0 || isBulkPending}
                        onClick={() => handleSetActive(toEnable, true)}
                        title="Включаются только каналы, где аккаунт уже состоит: в чаты он не вступает"
                    >
                        <Power className="h-3 w-3"/>
                        Включить ({toEnable.length})
                    </Button>
                    <Button
                        variant="outline"
                        size="sm"
                        className="h-7 gap-1 text-xs"
                        disabled={toDisable.length === 0 || isBulkPending}
                        onClick={() => handleSetActive(toDisable, false)}
                    >
                        <PowerOff className="h-3 w-3"/>
                        Выключить ({toDisable.length})
                    </Button>
                    <Button
                        variant="outline"
                        size="sm"
                        className="h-7 gap-1 text-xs text-destructive hover:text-destructive"
                        disabled={isBulkPending}
                        onClick={() => setIsBulkDeleteOpen(true)}
                    >
                        <Trash2 className="h-3 w-3"/>
                        Удалить ({visible.length})
                    </Button>
                </div>
            )}

            {visible.length === 0 ? (
                <p className="text-sm text-muted-foreground p-4 border rounded text-center">
                    Под фильтр не подходит ни один канал
                </p>
            ) : (
                <div className="space-y-2">
                    {visible.map((dest) => (
                        <RepostDestinationRow key={dest.id} destination={dest} onChanged={onChanged}/>
                    ))}
                </div>
            )}

            <ConfirmActionDialog
                open={isBulkDeleteOpen}
                onOpenChange={setIsBulkDeleteOpen}
                title={`Удалить каналы: ${visible.length}?`}
                description="Удаляются все каналы, показанные по текущему фильтру и поиску. Репосты в них прекратятся."
                confirmLabel="Удалить"
                isPending={isDeletingMany}
                onConfirm={handleDeleteVisible}
            />
        </div>
    );
}
