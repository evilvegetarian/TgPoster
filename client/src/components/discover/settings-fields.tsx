import {Badge} from "@/components/ui/badge"
import {Checkbox} from "@/components/ui/checkbox"
import {Input} from "@/components/ui/input"
import type {DiscoverSessionOption} from "@/api/endpoints/tgPosterAPI.schemas"

// Классификатор и поиск каналов отдают сессии в одинаковом виде
type SessionOption = Pick<DiscoverSessionOption, "id" | "name" | "phoneNumber" | "isActive" | "isAuthorized" | "isOwn">

export function NumberInput({value, onChange, min, max, disabled}: {
    value: number
    onChange: (value: number) => void
    min: number
    max: number
    disabled?: boolean
}) {
    return (
        <Input
            type="number"
            min={min}
            max={max}
            value={value}
            onChange={(event) => onChange(event.target.value === "" ? min : Number(event.target.value))}
            disabled={disabled}
            className="w-32"
        />
    )
}

function SessionStateBadges({session}: {session: SessionOption}) {
    return (
        <>
            {!session.isActive && (
                <Badge variant="outline" className="shrink-0 border-amber-300 text-amber-700 font-normal">неактивна</Badge>
            )}
            {!session.isAuthorized && (
                <Badge variant="outline" className="shrink-0 border-amber-300 text-amber-700 font-normal">не авторизована</Badge>
            )}
        </>
    )
}

// Свои сессии отмечаются галочками; чужие, уже назначенные этой задаче, видны, но менять их нельзя
export function SessionsPicker({sessions, value, onChange, disabled}: {
    sessions: SessionOption[]
    value: string[]
    onChange: (value: string[]) => void
    disabled?: boolean
}) {
    const own = sessions.filter((s) => s.isOwn)
    const foreign = sessions.filter((s) => !s.isOwn)

    const toggle = (id: string, checked: boolean) =>
        onChange(checked ? [...value, id] : value.filter((x) => x !== id))

    return (
        <div className="space-y-1.5">
            {own.length === 0 && (
                <p className="text-sm text-muted-foreground">
                    У вас нет Telegram-сессий — добавьте их на странице «Telegram Аккаунты»
                </p>
            )}
            {own.map((session) => (
                <label
                    key={session.id}
                    className="flex items-center gap-3 rounded-md border px-3 py-2 cursor-pointer hover:bg-accent/40"
                >
                    <Checkbox
                        checked={value.includes(session.id)}
                        onCheckedChange={(checked) => toggle(session.id, checked === true)}
                        disabled={disabled}
                    />
                    <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium truncate">{session.name || session.phoneNumber || "Без названия"}</p>
                        {session.name && session.phoneNumber && (
                            <p className="text-xs text-muted-foreground truncate">{session.phoneNumber}</p>
                        )}
                    </div>
                    <SessionStateBadges session={session}/>
                </label>
            ))}
            {foreign.map((session) => (
                <div
                    key={session.id}
                    className="flex items-center gap-3 rounded-md border border-dashed px-3 py-2 text-muted-foreground"
                    title="Сессия другого пользователя: снять назначение может только владелец"
                >
                    <Checkbox checked disabled/>
                    <p className="min-w-0 flex-1 text-sm truncate">{session.name || "Без названия"}</p>
                    <Badge variant="secondary" className="shrink-0 font-normal">другого пользователя</Badge>
                    <SessionStateBadges session={session}/>
                </div>
            ))}
        </div>
    )
}
