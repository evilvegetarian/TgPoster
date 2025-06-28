import { Link, useLocation } from "react-router-dom"
import { Home, Bot, Apple } from "lucide-react"

import {
    Sidebar,
    SidebarContent,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarRail,
} from "@/components/ui/sidebar"

export function SideBar() {
    const location = useLocation()

    // Define navigation items based on your router paths
    const navItems = [
        {
            title: "Главная",
            path: "/",
            icon: Home,
        },
        {
            title: "Войти в аккаунт",
            path: "/login",
            icon: Apple
        }
    ]

    return (
        <Sidebar>
            <SidebarHeader className="flex items-center justify-between p-4">
                <div className="flex items-center gap-2">
                    <Bot className="h-6 w-6 text-primary" />
                    <span className="font-semibold text-lg">Telegram</span>
                </div>
            </SidebarHeader>
            <SidebarContent>
                <SidebarGroup>
                    <SidebarGroupLabel>Навигация</SidebarGroupLabel>
                    <SidebarGroupContent>
                        <SidebarMenu>
                            {navItems.map((item) => (
                                <SidebarMenuItem key={item.path}>
                                    <SidebarMenuButton
                                        asChild
                                        isActive={
                                            location.pathname === item.path ||
                                            (item.path !== "/" && location.pathname.startsWith(item.path))
                                        }
                                    >
                                        <Link to={item.path}>
                                            <item.icon className="h-4 w-4" />
                                            <span>{item.title}</span>
                                        </Link>
                                    </SidebarMenuButton>
                                </SidebarMenuItem>
                            ))}
                        </SidebarMenu>
                    </SidebarGroupContent>
                </SidebarGroup>
            </SidebarContent>
            <SidebarRail />
        </Sidebar>
    )
}