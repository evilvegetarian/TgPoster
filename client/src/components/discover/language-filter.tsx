import {useGetApiV1DiscoverLanguages} from "@/api/endpoints/discover/discover"
import {Select, SelectContent, SelectItem, SelectTrigger, SelectValue} from "@/components/ui/select"

const allLanguages = "__all__"
const names = new Intl.DisplayNames("ru", {type: "language", fallback: "none"})

function languageLabel(code: string) {
    try {
        const name = names.of(code)
        if (name) return `${name[0].toLocaleUpperCase("ru")}${name.slice(1)} (${code.toUpperCase()})`
    } catch {
        // В старых записях модель могла вернуть значение вне формата ISO
    }
    return code.toUpperCase()
}

export function LanguageFilter({value, onChange}: {value?: string; onChange: (code?: string) => void}) {
    const {data: languages} = useGetApiV1DiscoverLanguages()

    return (
        <Select value={value ?? allLanguages} onValueChange={(code) => onChange(code === allLanguages ? undefined : code)}>
            <SelectTrigger aria-label="Язык публикаций">
                <SelectValue placeholder="Все языки"/>
            </SelectTrigger>
            <SelectContent>
                <SelectItem value={allLanguages}>Все языки</SelectItem>
                {languages?.map((code) => <SelectItem key={code} value={code}>{languageLabel(code)}</SelectItem>)}
            </SelectContent>
        </Select>
    )
}
