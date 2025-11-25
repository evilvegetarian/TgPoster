import {Link, useLocation} from "react-router-dom"
import {
    Bot,
    BotIcon,
    CalendarDays,
    Home,
    LogIn, LogOut,
    MessageCircleMore,
    Settings,
    SignatureIcon,
    UserPlus
} from "lucide-react"

import {
    Sidebar,
    SidebarContent, SidebarFooter,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarRail,
} from "@/components/ui/sidebar"
import {useAuth} from "@/auth-context.tsx";

export function SideBar() {
    const location = useLocation()
    const {isAuthenticated} = useAuth()

    const navItems = isAuthenticated
        ? [
            {
                title: "Главная",
                path: "/",
                icon: Home,
            },
            {
                title: "Расписание",
                path: "/schedule",
                icon: CalendarDays
            },
            {
                title: "Телеграм бот",
                path: "/telegram-bot",
                icon: BotIcon
            },
            {
                title: "Подтверждение постов",
                path: "/approve-messages",
                icon: SignatureIcon
            },
            {
                title: "Посты",
                path: "/messages",
                icon: MessageCircleMore
            },
            {
                title: "Спарсить канал",
                path: "/parse-channel",
                icon: MessageCircleMore
            },
            {
                title: "OpenRouter",
                path: "/open-router",
                icon: MessageCircleMore
            }
        ]
        : [
            {
                title: "Войти",
                path: "/login",
                icon: LogIn,
            },
            {
                title: "Регистрация",
                path: "/register",
                icon: UserPlus,
            }
        ];

    return (
        <Sidebar>
            <SidebarHeader className="flex items-center justify-between p-4">
                <div className="flex items-center gap-2">
                    <Bot className="h-6 w-6 text-primary"/>
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
                                            <item.icon className="h-4 w-4"/>
                                            <span>{item.title}</span>
                                        </Link>
                                    </SidebarMenuButton>
                                </SidebarMenuItem>
                            ))}
                        </SidebarMenu>
                    </SidebarGroupContent>
                </SidebarGroup>
            </SidebarContent>
            <SidebarFooter className="p-4">
                <SidebarMenu>
                    <SidebarMenuItem>
                        <SidebarMenuButton asChild>
                            <Link to="/settings">
                                <Settings className="h-4 w-4"/>
                                <span>Настройки</span>
                            </Link>
                        </SidebarMenuButton>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                        <SidebarMenuButton asChild>
                            <Link to="/logout">
                                <LogOut className="h-4 w-4"/>
                                <span>Выход</span>
                            </Link>
                        </SidebarMenuButton>
                    </SidebarMenuItem>
                </SidebarMenu>
            </SidebarFooter>
            <SidebarRail/>
        </Sidebar>
    )
}