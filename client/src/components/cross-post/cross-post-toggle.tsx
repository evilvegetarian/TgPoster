import type {MessageCrossPostFormat} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {FORMAT_OPTIONS} from "@/components/cross-post/cross-post-meta.ts";
import {Label} from "@/components/ui/label.tsx";
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select.tsx";
import {Switch} from "@/components/ui/switch.tsx";

const MESSAGE_FORMAT_OPTIONS: { value: MessageCrossPostFormat; title: string }[] = [
    {value: "Inherit", title: "Как в настройках расписания"},
    ...FORMAT_OPTIONS.map((option) => ({value: option.value, title: option.title})),
];

interface CrossPostToggleProps {
    id: string;
    enabled: boolean;
    onEnabledChange: (enabled: boolean) => void;
    format: MessageCrossPostFormat;
    onFormatChange: (format: MessageCrossPostFormat) => void;
    disabled?: boolean;
}

export function CrossPostToggle({id, enabled, onEnabledChange, format, onFormatChange, disabled}: CrossPostToggleProps) {
    return (
        <div className="rounded-lg border p-4 space-y-3">
            <div className="flex items-start justify-between gap-3">
                <div className="space-y-1">
                    <Label htmlFor={id}>Кросс-постинг в соцсети</Label>
                    <p className="text-xs text-muted-foreground">
                        После публикации в Telegram пост уйдёт в соцсети, подключённые к расписанию
                    </p>
                </div>
                <Switch
                    id={id}
                    checked={enabled}
                    onCheckedChange={onEnabledChange}
                    disabled={disabled}
                />
            </div>

            {enabled && (
                <div className="space-y-2">
                    <Label htmlFor={`${id}-format`}>Формат для этого поста</Label>
                    <Select
                        value={format}
                        onValueChange={(value) => onFormatChange(value as MessageCrossPostFormat)}
                        disabled={disabled}
                    >
                        <SelectTrigger id={`${id}-format`} className="w-full">
                            <SelectValue/>
                        </SelectTrigger>
                        <SelectContent>
                            {MESSAGE_FORMAT_OPTIONS.map((option) => (
                                <SelectItem key={option.value} value={option.value}>
                                    {option.title}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
            )}
        </div>
    );
}
