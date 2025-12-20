import React from "react";
import {Label} from "@/components/ui/label.tsx";
import {Button} from "@/components/ui/button.tsx";
import {Upload, X} from "lucide-react";
import {Input} from "@/components/ui/input.tsx";

interface MediaUploaderProps {
    files: File[];
    onFilesAdded: (files: File[]) => void;
    onFileRemove: (index: number) => void;
    disabled?: boolean;
}

export function MediaUploader({files, onFilesAdded, onFileRemove, disabled}: MediaUploaderProps) {
    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files?.length) {
            onFilesAdded(Array.from(e.target.files));
        }
    };

    return (
        <div className="space-y-3">
            <Label>Медиафайлы</Label>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                {files.map((file, index) => (
                    <div key={`new-${index}`} className="relative group aspect-square">
                        <div
                            className="w-full h-full bg-muted rounded-lg border flex flex-col items-center justify-center overflow-hidden p-2 text-center">
                            {file.type.startsWith('image/') ? (
                                <img
                                    src={URL.createObjectURL(file)}
                                    alt="preview"
                                    className="w-full h-full object-cover"
                                    onLoad={(e) => URL.revokeObjectURL(e.currentTarget.src)}
                                />
                            ) : (
                                <span className="text-xs text-muted-foreground break-all line-clamp-3">
                                    {file.name}
                                </span>
                            )}
                        </div>
                        <Button
                            type="button"
                            variant="destructive"
                            size="icon"
                            className="absolute -top-2 -right-2 h-6 w-6 rounded-full shadow-md opacity-0 group-hover:opacity-100 transition-opacity"
                            onClick={() => onFileRemove(index)}
                            disabled={disabled}
                        >
                            <X className="h-3 w-3"/>
                        </Button>
                    </div>
                ))}

                <label
                    className={`cursor-pointer aspect-square border-2 border-dashed border-muted-foreground/25 rounded-lg flex flex-col items-center justify-center hover:border-primary/50 hover:bg-muted/50 transition-all ${disabled ? 'opacity-50 pointer-events-none' : ''}`}>
                    <Upload className="h-6 w-6 mb-2 text-muted-foreground"/>
                    <span className="text-xs text-muted-foreground">Добавить</span>
                    <Input
                        type="file"
                        multiple
                        accept="image/*,video/*"
                        onChange={handleFileChange}
                        className="hidden"
                        disabled={disabled}
                        onClick={(e) => (e.currentTarget.value = "")}
                    />
                </label>
            </div>
        </div>
    );
}