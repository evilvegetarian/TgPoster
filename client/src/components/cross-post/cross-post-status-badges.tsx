import type {ReactNode} from "react";
import {useQueryClient} from "@tanstack/react-query";
import {RotateCcw} from "lucide-react";
import {toast} from "sonner";
import {usePostApiV1MessageIdCrossPostsCrossPostIdRetry} from "@/api/endpoints/message/message.ts";
import type {CrossPostStatus, CrossPostStatusResponse, MessageResponse} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {PLATFORM_LABELS} from "@/components/social-account/platform-meta.ts";
import {Badge} from "@/components/ui/badge.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Tooltip, TooltipContent, TooltipTrigger} from "@/components/ui/tooltip.tsx";

const STATUS_LABELS: Record<CrossPostStatus, string> = {
    Pending: "в очереди",
    InProgress: "публикуется",
    Published: "опубликовано",
    Failed: "ошибка",
    Skipped: "пропущено",
};

function renderStatus(crossPost: CrossPostStatusResponse) {
    const label = `${PLATFORM_LABELS[crossPost.platform]}: ${STATUS_LABELS[crossPost.status]}`;

    if (crossPost.status === "Published") {
        const badge = <Badge variant="outline" className="text-green-600 border-green-600">{label}</Badge>;
        return crossPost.externalUrl ? (
            <a href={crossPost.externalUrl} target="_blank" rel="noreferrer">{badge}</a>
        ) : badge;
    }

    if (crossPost.status === "Failed") {
        return <Badge variant="destructive">{label}</Badge>;
    }

    if (crossPost.status === "Skipped") {
        return <Badge variant="outline">{label}</Badge>;
    }

    return <Badge variant="secondary">{label}</Badge>;
}

function withHint(node: ReactNode, hint?: string | null) {
    if (!hint) {
        return node;
    }

    return (
        <Tooltip>
            <TooltipTrigger asChild>{node}</TooltipTrigger>
            <TooltipContent className="max-w-xs">{hint}</TooltipContent>
        </Tooltip>
    );
}

export function CrossPostStatusBadges({message}: { message: MessageResponse }) {
    const queryClient = useQueryClient();

    const {mutate: retry, isPending} = usePostApiV1MessageIdCrossPostsCrossPostIdRetry({
        mutation: {
            onSuccess: () => {
                toast.success("Кросс-пост поставлен в очередь");
                queryClient.invalidateQueries({queryKey: ["/api/v1/message"]});
            },
            onError: (error) => {
                toast.error("Не удалось повторить", {description: error.title});
            },
        },
    });

    if (message.crossPostEnabled === false) {
        return <Badge variant="outline">Без кросс-постинга</Badge>;
    }

    const crossPosts = message.crossPosts ?? [];
    if (crossPosts.length === 0) {
        return null;
    }

    return (
        <>
            {crossPosts.map((crossPost) => {
                const canRetry = crossPost.status === "Failed" || crossPost.status === "Skipped";

                return (
                    <span key={crossPost.id} className="inline-flex items-center gap-1">
                        {withHint(renderStatus(crossPost), crossPost.error)}
                        {canRetry && (
                            <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                className="h-6 w-6"
                                title="Повторить"
                                onClick={() => retry({id: message.id, crossPostId: crossPost.id})}
                                disabled={isPending}
                            >
                                <RotateCcw className="h-3 w-3"/>
                            </Button>
                        )}
                    </span>
                );
            })}
        </>
    );
}
