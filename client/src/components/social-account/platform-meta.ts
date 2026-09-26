import type {SocialAccountStatus, SocialPlatform} from "@/api/endpoints/tgPosterAPI.schemas.ts";

export const PLATFORM_LABELS: Record<SocialPlatform, string> = {
    Bluesky: "Bluesky",
};

export const ACCOUNT_STATUS_LABELS: Record<SocialAccountStatus, string> = {
    Active: "Активен",
    NeedsReauth: "Нужно переподключить",
};
