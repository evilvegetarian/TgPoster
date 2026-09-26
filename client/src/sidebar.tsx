import {Link, useLocation} from "react-router-dom"
import {
    BarChart3,
    Bot,
    BotIcon,
    CalendarDays,
    Home,
    LogIn, LogOut,
    MessageCircleMore,
    MessageSquareShare,
    Network,
    Repeat2,
    ScrollText,
    Settings,
    Share2,
    SignatureIcon,
    Smartphone,
    Tags,
    Telescope,
    UserPlus,
    Youtube
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
            },
            {
                title: "YouTube Аккаунты",
                path: "/youtube-accounts",
                icon: Youtube
            },
            {
                title: "Соцсети",
                path: "/social-accounts",
                icon: Share2
            },
            {
                title: "Telegram Аккаунты",
                path: "/telegram-accounts",
                icon: Smartphone
            },
            {
                title: "Прокси",
                path: "/proxies",
                icon: Network
            },
            {
                title: "Настройки репоста",
                path: "/repost-settings",
                icon: Repeat2
            },
            {
                title: "Логи репостов",
                path: "/repost-logs",
                icon: ScrollText
            },
            {
                title: "Комментирующий репост",
                path: "/comment-repost",
                icon: MessageSquareShare
            },
            {
                title: "Discover",
                path: "/discover",
                icon: Telescope
            },
            {
                title: "Статистика Discover",
                path: "/discover/stats",
                icon: BarChart3
            },
            {
                title: "Классификация каналов",
                path: "/discover/classification",
                icon: Tags
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
                                            (item.path !== "/"
                                                && location.pathname.startsWith(item.path + "/")
                                                && !navItems.some((other) => other.path === location.pathname))
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
