import {Card, CardContent, CardDescription, CardHeader, CardTitle} from "@/components/ui/card";
import type {ReactNode} from "react";

interface AuthLayoutProps {
    title: string;
    description: string;
    children: ReactNode;
}

export function AuthLayout({title, description, children}: AuthLayoutProps) {
    return (
        <div className="flex items-center justify-center min-h-screen bg-gray-100 dark:bg-gray-900">
            <Card className="w-full max-w-sm">
                <CardHeader>
                    <CardTitle className="flex items-center justify-center">{title}</CardTitle>
                    <CardDescription className="flex items-center justify-center">{description}</CardDescription>
                </CardHeader>
                <CardContent>
                    {children}
                </CardContent>
            </Card>
        </div>
    )
}