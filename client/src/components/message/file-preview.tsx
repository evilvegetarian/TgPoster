import {useState} from "react"
import {X} from "lucide-react"
import {Dialog, DialogContent, DialogHeader, DialogTitle} from "@/components/ui/dialog"
import {Button} from "@/components/ui/button"
import type {FileResponse} from "@/api/endpoints/tgPosterAPI.schemas"
import {useGetApiOptionsFileType} from "@/api/endpoints/options/options.ts";

interface FilePreviewProps {
    file: FileResponse
    onRemove?: () => void
    showRemoveButton?: boolean
}

export function FilePreview({file, onRemove, showRemoveButton = false}: FilePreviewProps) {
    const {data: fileTypes, isLoading} = useGetApiOptionsFileType();
    const targetFileType = fileTypes?.find(x => x.value === file.fileType);
    const [isModalOpen, setIsModalOpen] = useState(false)
    if (isLoading) {
        return <div>Загрузка...</div>;
    }

    const mainFileUrl = file.url;

    if (!mainFileUrl) {
        return (
            <div className="w-20 h-20 bg-muted rounded-lg flex items-center justify-center">
                <div className="text-xs text-muted-foreground">Нет файла</div>
            </div>
        )
    }

    return (
        <>
            <div className="relative group">
                {targetFileType!.name}
                <div
                    className="w-20 h-20 rounded-lg overflow-hidden cursor-pointer hover:opacity-80 transition-opacity"
                    onClick={() => setIsModalOpen(true)}
                >
                    <img
                        src={mainFileUrl || "/placeholder.svg"}
                        alt="File preview"
                        className="w-full h-full object-cover"
                        crossOrigin="anonymous"
                    />
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
                        <X className="h-3 w-3"/>
                    </Button>
                )}
            </div>

            <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
                <DialogContent className="max-w-4xl">
                    <DialogHeader>
                        <DialogTitle>Просмотр файла</DialogTitle>
                    </DialogHeader>
                    <div className="space-y-5">
                        <div className="flex justify-center">
                            <img
                                src={mainFileUrl! || "/placeholder.svg"}
                                alt="File"
                                className="max-h-200 rounded-lg"
                                crossOrigin="anonymous"
                            />
                        </div>
                    </div>
                </DialogContent>
            </Dialog>
        </>
    )
}
