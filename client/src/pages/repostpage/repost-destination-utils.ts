import type {RepostDestinationDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {ChatStatus} from "@/api/endpoints/tgPosterAPI.schemas.ts";

// Проблемный — аккаунт забанен в чате или его там нет: репост туда не дойдёт, канал живёт во вкладке «Баны и удалённые»
export function isProblemDestination(destination: RepostDestinationDto): boolean {
    return destination.chatStatus === ChatStatus.Banned || destination.chatStatus === ChatStatus.Left;
}

export function destinationTitle(destination: RepostDestinationDto): string {
    return destination.title ?? (destination.username ? `@${destination.username}` : String(destination.chatId));
}

// Те же каналы, которым бэкенд проверяет членство при массовом включении
export function canBeEnabled(destination: RepostDestinationDto): boolean {
    return !destination.isActive || isProblemDestination(destination);
}
