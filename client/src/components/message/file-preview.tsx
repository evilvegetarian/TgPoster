"use client"

import { useState } from "react"
import { X, Play } from "lucide-react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import type { FileResponse } from "@/api/endpoints/tgPosterAPI.schemas"
import {useGetApiV1FileId} from "@/api/endpoints/file/file.ts";

interface FilePreviewProps {
    file: FileResponse
    onRemove?: () => void
    showRemoveButton?: boolean
}

export function FilePreview({ file, onRemove, showRemoveButton = false }: FilePreviewProps) {
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [currentPreviewIndex, setCurrentPreviewIndex] = useState(0)

    const { data: mainFileBlob } = useGetApiV1FileId(file.fileCacheId || "", {
        query: { enabled: !!file.fileCacheId },
    })

    const previewIds = file.previewCacheIds || []
    const { data: preview1Blob } = useGetApiV1FileId(previewIds[0] || "", {
        query: { enabled: !!previewIds[0] },
    })
    const { data: preview2Blob } = useGetApiV1FileId(previewIds[1] || "", {
        query: { enabled: !!previewIds[1] },
    })
    const { data: preview3Blob } = useGetApiV1FileId(previewIds[2] || "", {
        query: { enabled: !!previewIds[2] },
    })

    const mainFileUrl = mainFileBlob ? URL.createObjectURL(mainFileBlob) : null
    const previewUrls = [
        preview1Blob ? URL.createObjectURL(preview1Blob) : null,
        preview2Blob ? URL.createObjectURL(preview2Blob) : null,
        preview3Blob ? URL.createObjectURL(preview3Blob) : null,
    ].filter(Boolean) as string[]

    const isVideo = file.fileType === 1 // FileTypes.NUMBER_1 для видео
    const displayUrl = isVideo && previewUrls.length > 0 ? previewUrls[0] : mainFileUrl

    if (!displayUrl) {
        return (
            <div className="w-20 h-20 bg-muted rounded-lg flex items-center justify-center">
                <div className="animate-pulse text-xs">Загрузка...</div>
            </div>
        )
    }

    return (
        <>
            <div className="relative group">
                <div
                    className="w-20 h-20 rounded-lg overflow-hidden cursor-pointer hover:opacity-80 transition-opacity"
                    onClick={() => setIsModalOpen(true)}
                >
                    <img
                        src={displayUrl || "/placeholder.svg"}
                        alt="File preview"
                        className="w-full h-full object-cover"
                        crossOrigin="anonymous"
                    />
                    {isVideo && (
                        <div className="absolute inset-0 flex items-center justify-center bg-black/20">
                            <Play className="h-6 w-6 text-white" />
                        </div>
                    )}
                    {isVideo && previewUrls.length > 1 && (
                        <div className="absolute bottom-1 right-1 bg-black/60 text-white text-xs px-1 rounded">
                            +{previewUrls.length - 1}
                        </div>
                    )}
                </div>

                {showRemoveButton && onRemove && (
                    <Button
                        variant="destructive"
                        size="icon"
                        className="absolute -top-2 -right-2 h-6 w-6 rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
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
                <DialogContent className="max-w-4xl">
                    <DialogHeader>
                        <DialogTitle>Просмотр файла</DialogTitle>
                    </DialogHeader>

                    <div className="space-y-4">
                        {isVideo && previewUrls.length > 0 ? (
                            <div className="space-y-4">
                                <div className="flex justify-center">
                                    <img
                                        src={previewUrls[currentPreviewIndex] || "/placeholder.svg"}
                                        alt={`Preview ${currentPreviewIndex + 1}`}
                                        className="max-h-96 rounded-lg"
                                        crossOrigin="anonymous"
                                    />
                                </div>

                                {previewUrls.length > 1 && (
                                    <div className="flex justify-center gap-2">
                                        {previewUrls.map((url, index) => (
                                            <button
                                                key={index}
                                                onClick={() => setCurrentPreviewIndex(index)}
                                                className={`w-16 h-16 rounded-lg overflow-hidden border-2 ${
                                                    currentPreviewIndex === index ? "border-primary" : "border-transparent"
                                                }`}
                                            >
                                                <img
                                                    src={url || "/placeholder.svg"}
                                                    alt={`Preview ${index + 1}`}
                                                    className="w-full h-full object-cover"
                                                    crossOrigin="anonymous"
                                                />
                                            </button>
                                        ))}
                                    </div>
                                )}
                            </div>
                        ) : (
                            <div className="flex justify-center">
                                <img
                                    src={mainFileUrl! || "/placeholder.svg"}
                                    alt="File"
                                    className="max-h-96 rounded-lg"
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
