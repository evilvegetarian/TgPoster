import {useState} from "react"
import {Play} from "lucide-react"
import {type FileResponse, FileTypes} from "@/api/endpoints/tgPosterAPI.schemas"
import {MediaLightbox} from "./media-lightbox"

interface MediaAlbumItemProps {
    file: FileResponse
    className?: string
}

export function MediaAlbumItem({file, className = ""}: MediaAlbumItemProps) {
    const [isModalOpen, setIsModalOpen] = useState(false)

    const isVideo = file.fileType === FileTypes.Video
    const previewFiles = file.previewFiles ?? []
    const thumbnailUrl = isVideo ? (previewFiles[0]?.url ?? file.url) : file.url

    if (!thumbnailUrl) {
        return (
            <div className={`bg-muted flex items-center justify-center ${className}`}>
                <span className="text-xs text-muted-foreground">Нет медиа</span>
            </div>
        )
    }

    return (
        <>
            <div
                className={`relative cursor-pointer overflow-hidden group ${className}`}
                onClick={() => setIsModalOpen(true)}
            >
                <img
                    src={thumbnailUrl}
                    alt="Media"
                    className="w-full h-full object-cover transition-transform group-hover:scale-105 duration-300"
                    crossOrigin="anonymous"
                />
                {isVideo && (
                    <div className="absolute inset-0 flex items-center justify-center bg-black/30 group-hover:bg-black/15 transition-colors">
                        <Play className="h-10 w-10 text-white fill-white opacity-90 drop-shadow-lg"/>
                    </div>
                )}
            </div>
            <MediaLightbox file={file} open={isModalOpen} onOpenChange={setIsModalOpen}/>
        </>
    )
}
