import {Button} from "@/components/ui/button";
import {Loader2} from "lucide-react";
import {CommentRepostList} from "@/pages/commentrepostpage/comment-repost-list.tsx";
import {CreateCommentRepostDialog} from "@/pages/commentrepostpage/create-comment-repost-dialog.tsx";
import {useGetApiV1CommentRepost} from "@/api/endpoints/comment-repost/comment-repost.ts";

export function CommentRepostPage() {
    const {data, isLoading, error, refetch} = useGetApiV1CommentRepost();
    const settings = data?.items ?? [];

    if (isLoading) {
        return (
            <div className="container mx-auto p-6 max-w-6xl">
                <div className="flex items-center justify-center py-12">
                    <Loader2 className="h-8 w-8 animate-spin"/>
                    <span className="ml-2">Загрузка настроек комментирующего репоста...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="container mx-auto p-6 max-w-6xl">
                <div className="text-center py-12">
                    <p className="text-red-500 mb-4">Ошибка загрузки настроек</p>
                    <p className="text-sm text-muted-foreground mb-4">
                        {error?.title || "Произошла неизвестная ошибка"}
                    </p>
                    <Button onClick={() => refetch()}>Попробовать снова</Button>
                </div>
            </div>
        );
    }

    return (
        <div className="container mx-auto p-6 max-w-6xl">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h1 className="text-3xl font-bold">Комментирующий репост</h1>
                    <p className="text-muted-foreground mt-2">
                        Автоматическое комментирование постов в отслеживаемых каналах
                    </p>
                </div>
                <CreateCommentRepostDialog/>
            </div>

            <div className="grid gap-6">
                <CommentRepostList settings={settings}/>
            </div>
        </div>
    );
}
