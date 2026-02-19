import { useState } from "react"
import { X, Play } from "lucide-react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import {type FileResponse, FileTypes} from "@/api/endpoints/tgPosterAPI.schemas.ts"

interface FilePreviewProps {
    file: FileResponse
    onRemove?: () => void
    showRemoveButton?: boolean
}

export function FilePreview({ file, onRemove, showRemoveButton = false }: FilePreviewProps) {
    const [isModalOpen, setIsModalOpen] = useState(false)

    const isVideo = file.fileType === FileTypes.Video
    const previewFiles = file.previewFiles ?? []
    const thumbnailUrl = isVideo ? (previewFiles[0]?.url ?? file.url) : file.url

    if (!thumbnailUrl) {
        return (
            <div className="w-20 h-20 bg-muted rounded-lg flex items-center justify-center border border-border">
                <div className="text-[10px] text-muted-foreground text-center px-1">Нет медиа</div>
            </div>
        )
    }

    const isTrimmed = isVideo && file.durationSeconds != null && file.durationSeconds >= 120

    return (
        <>
            <div className="relative group w-20 h-20">
                <div
                    className="w-full h-full rounded-lg overflow-hidden cursor-pointer bg-muted border border-border transition-all hover:ring-2 hover:ring-primary hover:border-transparent relative"
                    onClick={() => setIsModalOpen(true)}
                >
                    <img
                        src={thumbnailUrl || "/placeholder.svg"}
                        alt="File preview"
                        className="w-full h-full object-cover transition-transform group-hover:scale-110 duration-300"
                        crossOrigin="anonymous"
                    />
                    {isVideo && (
                        <div className="absolute inset-0 flex items-center justify-center bg-black/40 group-hover:bg-black/20 transition-colors">
                            <Play className="h-8 w-8 text-white fill-white opacity-90 drop-shadow-md" />
                        </div>
                    )}
                </div>

                {showRemoveButton && onRemove && (
                    <Button
                        variant="destructive"
                        size="icon"
                        className="absolute -top-2 -right-2 h-6 w-6 rounded-full opacity-0 scale-75 group-hover:opacity-100 group-hover:scale-100 transition-all shadow-md z-10"
                        onClick={(e) => {
                            e.stopPropagation()
                            onRemove()
                        }}
                    >
                        <X className="h-3 w-3" />
                    </Button>
                )}
            </div>
            <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
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
        </>
    )
}
