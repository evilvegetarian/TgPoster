import {Card, CardContent} from "@/components/ui/card";
import {MessageSquareShare} from "lucide-react";
import {CommentRepostCard} from "@/pages/commentrepostpage/comment-repost-card.tsx";
import type {CommentRepostItemDto} from "@/api/endpoints/tgPosterAPI.schemas.ts";
import {CreateCommentRepostDialog} from "@/pages/commentrepostpage/create-comment-repost-dialog.tsx";

interface CommentRepostListProps {
    settings: CommentRepostItemDto[];
}

export function CommentRepostList({settings}: CommentRepostListProps) {
    if (settings.length === 0) {
        return (
            <Card className="text-center py-12">
                <CardContent className="pt-6">
                    <MessageSquareShare className="h-12 w-12 mx-auto text-muted-foreground mb-4"/>
                    <h3 className="text-lg font-semibold mb-2">Нет настроек комментирующего репоста</h3>
                    <p className="text-muted-foreground mb-4">
                        Создайте настройки для автоматического комментирования постов в отслеживаемых каналах
                    </p>
                    <CreateCommentRepostDialog/>
                </CardContent>
            </Card>
        );
    }

    return (
        <>
            {settings.map(setting => (
                <CommentRepostCard
                    key={setting.id}
                    settings={setting}
                />
            ))}
        </>
    );
}
