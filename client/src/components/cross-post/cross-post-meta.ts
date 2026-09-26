import type {CrossPostFormat, CrossPostLinkTarget} from "@/api/endpoints/tgPosterAPI.schemas.ts";

interface FormatOption {
    value: CrossPostFormat;
    title: string;
    description: string;
}

interface LinkTargetOption {
    value: CrossPostLinkTarget;
    title: string;
}

export const FORMAT_OPTIONS: FormatOption[] = [
    {
        value: "Teaser",
        title: "Тизер",
        description: "Начало текста и «…», внизу призыв и ссылка — максимум переходов на пост",
    },
    {
        value: "Full",
        title: "Полный пост",
        description: "Весь текст как обычный пост, внизу ссылка на Telegram. Длинный текст уйдёт цепочкой",
    },
    {
        value: "Announcement",
        title: "Анонс",
        description: "Только картинка, призыв и ссылка — для визуальных каналов",
    },
];

export const LINK_TARGET_OPTIONS: LinkTargetOption[] = [
    {value: "Post", title: "На пост в канале"},
    {value: "Channel", title: "На канал"},
    {value: "Custom", title: "Своя ссылка (например, пригласительная)"},
    {value: "None", title: "Без ссылки"},
];
