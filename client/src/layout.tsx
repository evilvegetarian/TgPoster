import { Outlet } from "react-router-dom"
import { SidebarInset, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { Toaster } from "@/components/ui/sonner.tsx"
import {SideBar} from "@/sidebar.tsx";

export function Layout() {
    return (
        <SidebarProvider>
            <SideBar/>
            <SidebarInset className="flex-1">
                <header className="flex h-16 items-center border-b px-6">
                    <div className="flex items-center gap-4">
                        <SidebarTrigger />
                    </div>
                </header>
                <main className="p-6">
                    <Outlet />
                </main>
            </SidebarInset>
            <Toaster />
        </SidebarProvider>
    )
}