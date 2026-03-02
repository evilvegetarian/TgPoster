import {Dialog, DialogContent, DialogHeader, DialogTitle} from "@/components/ui/dialog"
import {type FileResponse, FileTypes} from "@/api/endpoints/tgPosterAPI.schemas"

interface MediaLightboxProps {
    file: FileResponse
    open: boolean
    onOpenChange: (open: boolean) => void
}

export function MediaLightbox({file, open, onOpenChange}: MediaLightboxProps) {
    const isVideo = file.fileType === FileTypes.Video
    const isTrimmed = isVideo && file.durationSeconds != null && file.durationSeconds >= 120

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-5xl w-[95vw] h-[90vh] md:h-auto p-0 flex flex-col gap-0 overflow-hidden bg-background">
                <DialogHeader className="p-4 border-b shrink-0 flex flex-row items-center justify-between space-y-0">
                    <DialogTitle className="text-lg font-medium">
                        {isVideo ? (
                            <span>
                                Просмотр видео
                                {isTrimmed && (
                                    <span className="text-sm text-muted-foreground ml-2">
                                        (превью: 1 мин из {Math.round(file.durationSeconds! / 60)} мин)
                                    </span>
                                )}
                            </span>
                        ) : "Просмотр изображения"}
                    </DialogTitle>
                </DialogHeader>

                <div className="flex-1 overflow-hidden flex flex-col bg-muted/30">
                    {isVideo ? (
                        <div className="flex-1 flex items-center justify-center p-4 min-h-[300px]">
                            {file.videoUrl ? (
                                <video
                                    src={file.videoUrl}
                                    controls
                                    autoPlay
                                    className="max-w-full max-h-[70vh] rounded-md shadow-sm"
                                    crossOrigin="anonymous"
                                />
                            ) : (
                                <div className="text-muted-foreground">Видео недоступно для просмотра</div>
                            )}
                        </div>
                    ) : (
                        <div className="w-full h-full flex items-center justify-center p-4 bg-dots-pattern">
                            <img
                                src={file.url || "/placeholder.svg"}
                                alt="Full size view"
                                className="max-w-full max-h-[80vh] object-contain rounded-lg shadow-lg"
                                crossOrigin="anonymous"
                            />
                        </div>
                    )}
                </div>
            </DialogContent>
        </Dialog>
    )
}
