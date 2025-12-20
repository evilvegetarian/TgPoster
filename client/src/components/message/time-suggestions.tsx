import {Badge} from "@/components/ui/badge.tsx";
import {utcToShortLocalString} from "@/utils/convertLocalToIsoTime.tsx";

interface TimeSuggestionsProps {
    availableTimes?: string[] | null;
    onSelect: (time: string) => void;
    maxItems?: number;
}

export function TimeSuggestions({availableTimes, onSelect, maxItems = 7}: TimeSuggestionsProps) {
    if (!availableTimes || availableTimes.length === 0) return null;

    return (
        <div
            className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground p-3 bg-muted/40 rounded-lg border border-dashed">
            <span>Свободные слоты:</span>
            {availableTimes.slice(0, maxItems).map((time) => (
                <Badge
                    key={time}
                    variant="secondary"
                    className="cursor-pointer hover:bg-primary hover:text-primary-foreground transition-colors"
                    onClick={() => onSelect(time)}
                >
                    {utcToShortLocalString(time)}
                </Badge>
            ))}
        </div>
    );
}