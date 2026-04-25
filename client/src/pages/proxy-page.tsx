import { ProxyListComponent } from "@/components/proxy/proxy-list-component";
import { ProxyCreateDialog } from "@/components/proxy/proxy-create-dialog";

export default function ProxyPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-7xl">
            <div className="mb-8 flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold">Прокси</h1>
                    <p className="text-muted-foreground mt-2">
                        Маршрутизация трафика Telegram-сессий через SOCKS5/HTTP/MTProxy
                    </p>
                </div>
                <ProxyCreateDialog />
            </div>
            <ProxyListComponent />
        </div>
    );
}
