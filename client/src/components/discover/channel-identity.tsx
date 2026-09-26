import {channelLink} from "@/components/discover/format"

interface ChannelAvatarProps {
    avatarUrl?: string | null
    name: string
    size?: "sm" | "md"
}

export function ChannelAvatar({avatarUrl, name, size = "sm"}: ChannelAvatarProps) {
    const dimension = size === "sm" ? "w-8 h-8 text-sm" : "w-10 h-10 text-base"
    return avatarUrl ? (
        <img src={avatarUrl} alt="" className={`${dimension} rounded-full object-cover shrink-0`}/>
    ) : (
        <div className={`${dimension} rounded-full bg-muted flex items-center justify-center text-muted-foreground font-semibold shrink-0`}>
            {name[0]?.toUpperCase() ?? "?"}
        </div>
    )
}

interface ChannelNameProps {
    title?: string | null
    username?: string | null
    tgUrl?: string | null
}

export function ChannelName({title, username, tgUrl}: ChannelNameProps) {
    const link = channelLink({tgUrl, username})
    const name = title ?? username ?? "Без названия"
    return (
        <div className="min-w-0">
            {link ? (
                <a href={link} target="_blank" rel="noopener noreferrer" className="font-medium truncate block hover:underline">
                    {name}
                </a>
            ) : (
                <p className="font-medium truncate">{name}</p>
            )}
            {username && <p className="text-xs text-muted-foreground truncate">@{username}</p>}
        </div>
    )
}
