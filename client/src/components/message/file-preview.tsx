import {useState} from "react"
import {X, Play} from "lucide-react"
import {Button} from "@/components/ui/button"
import {type FileResponse, FileTypes} from "@/api/endpoints/tgPosterAPI.schemas"
import {MediaLightbox} from "./media-lightbox"

interface FilePreviewProps {
    file: FileResponse
    onRemove?: () => void
    showRemoveButton?: boolean
}

export function FilePreview({file, onRemove, showRemoveButton = false}: FilePreviewProps) {
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
                            <Play className="h-8 w-8 text-white fill-white opacity-90 drop-shadow-md"/>
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
                        <X className="h-3 w-3"/>
                    </Button>
                )}
            </div>
            <MediaLightbox file={file} open={isModalOpen} onOpenChange={setIsModalOpen}/>
        </>
    )
}
