import {type FileResponse, FileTypes} from "@/api/endpoints/tgPosterAPI.schemas"
import {MediaAlbumItem} from "./media-album-item"

interface MediaAlbumProps {
    files: FileResponse[]
}

export function MediaAlbum({files}: MediaAlbumProps) {
    const mediaFiles = files.filter(f => f.fileType === FileTypes.Image || f.fileType === FileTypes.Video)

    if (mediaFiles.length === 0) return null

    if (mediaFiles.length === 1) {
        return (
            <div className="rounded-lg overflow-hidden">
                <MediaAlbumItem file={mediaFiles[0]} className="w-full max-h-[400px]"/>
            </div>
        )
    }

    if (mediaFiles.length === 2) {
        return (
            <div className="grid grid-cols-2 gap-0.5 rounded-lg overflow-hidden h-[280px]">
                <MediaAlbumItem file={mediaFiles[0]} className="h-full"/>
                <MediaAlbumItem file={mediaFiles[1]} className="h-full"/>
            </div>
        )
    }

    if (mediaFiles.length === 3) {
        return (
            <div className="grid grid-cols-3 gap-0.5 rounded-lg overflow-hidden h-[320px]">
                <div className="col-span-2 h-full">
                    <MediaAlbumItem file={mediaFiles[0]} className="h-full w-full"/>
                </div>
                <div className="flex flex-col gap-0.5 h-full">
                    <MediaAlbumItem file={mediaFiles[1]} className="flex-1 min-h-0"/>
                    <MediaAlbumItem file={mediaFiles[2]} className="flex-1 min-h-0"/>
                </div>
            </div>
        )
    }

    const [first, ...rest] = mediaFiles
    return (
        <div className="space-y-0.5 rounded-lg overflow-hidden">
            <div className="h-[280px]">
                <MediaAlbumItem file={first} className="w-full h-full"/>
            </div>
            <div className="grid grid-cols-3 gap-0.5">
                {rest.map((file) => (
                    <div key={file.id} className="aspect-square">
                        <MediaAlbumItem file={file} className="w-full h-full"/>
                    </div>
                ))}
            </div>
        </div>
    )
}
