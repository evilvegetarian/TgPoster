import { useState, useEffect } from "react"
import { X, Play, ChevronLeft, ChevronRight} from "lucide-react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import {type FileResponse, FileTypes} from "@/api/endpoints/tgPosterAPI.schemas.ts"
import { ScrollArea } from "@radix-ui/react-scroll-area"
import {ScrollBar} from "@/components/ui/scroll-area.tsx";

interface FilePreviewProps {
    file: FileResponse
    onRemove?: () => void
    showRemoveButton?: boolean
}

export function FilePreview({ file, onRemove, showRemoveButton = false }: FilePreviewProps) {
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [selectedPreviewIndex, setSelectedPreviewIndex] = useState(0)

    const isVideo = file.fileType === FileTypes.Video
    const previewFiles = file.previewFiles ?? []
    const thumbnailUrl = isVideo ? (previewFiles[0]?.url ?? file.url) : file.url

    useEffect(() => {
        if (isModalOpen) {
            setSelectedPreviewIndex(0)
        }
    }, [isModalOpen])

    const handlePrevious = () => {
        setSelectedPreviewIndex((prev) => (prev > 0 ? prev - 1 : previewFiles.length - 1))
    }

    const handleNext = () => {
        setSelectedPreviewIndex((prev) => (prev < previewFiles.length - 1 ? prev + 1 : 0))
    }

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
                            {isVideo ? "Кадры из видео" : "Просмотр изображения"}
                        </DialogTitle>
                    </DialogHeader>

                    <div className="flex-1 overflow-hidden flex flex-col bg-muted/30">
                        {isVideo ? (
                            <div className="flex flex-col h-full">
                                <div className="flex-1 relative flex items-center justify-center p-4 min-h-[300px]">
                                    {previewFiles.length > 0 ? (
                                        <>
                                            <img
                                                src={previewFiles[selectedPreviewIndex]?.url || "/placeholder.svg"}
                                                alt={`Frame ${selectedPreviewIndex + 1}`}
                                                className="max-w-full max-h-full object-contain rounded-md shadow-sm"
                                                crossOrigin="anonymous"
                                            />
                                            {previewFiles.length > 1 && (
                                                <>
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        onClick={handlePrevious}
                                                        className="absolute left-4 top-1/2 -translate-y-1/2 h-10 w-10 rounded-full bg-background/80 hover:bg-background shadow-sm"
                                                    >
                                                        <ChevronLeft className="h-6 w-6" />
                                                    </Button>
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        onClick={handleNext}
                                                        className="absolute right-4 top-1/2 -translate-y-1/2 h-10 w-10 rounded-full bg-background/80 hover:bg-background shadow-sm"
                                                    >
                                                        <ChevronRight className="h-6 w-6" />
                                                    </Button>
                                                </>
                                            )}
                                        </>
                                    ) : (
                                        <div className="text-muted-foreground">Нет доступных превью</div>
                                    )}
                                </div>
                                {previewFiles.length > 1 && (
                                    <div className="shrink-0 p-4 border-t bg-background">
                                        <ScrollArea className="w-full whitespace-nowrap">
                                            <div className="flex space-x-3 pb-2 w-max mx-auto">
                                                {previewFiles.map((preview, index) => (
                                                    <button
                                                        key={index}
                                                        onClick={() => setSelectedPreviewIndex(index)}
                                                        className={`relative w-24 aspect-video rounded-md overflow-hidden transition-all border-2 
                                                            ${selectedPreviewIndex === index
                                                            ? "border-primary ring-2 ring-primary/20 scale-105"
                                                            : "border-transparent opacity-70 hover:opacity-100"}`}
                                                    >
                                                        <img
                                                            src={preview.url || "/placeholder.svg"}
                                                            className="w-full h-full object-cover"
                                                            alt={`Thumbnail ${index}`}
                                                            loading="lazy"
                                                            crossOrigin="anonymous"
                                                        />
                                                    </button>
                                                ))}
                                            </div>
                                            <ScrollBar orientation="horizontal" />
                                        </ScrollArea>
                                    </div>
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