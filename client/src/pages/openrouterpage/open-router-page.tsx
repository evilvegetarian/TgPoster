import {CreateOpenRouterComponent} from "@/pages/openrouterpage/create-open-router-component.tsx";
import {OpenRouterListComponent} from "@/pages/openrouterpage/open-router-list-component.tsx";


export function OpenRouterPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <div className="mb-8">
                <div className="flex justify-between items-center">
                    <h1 className="text-3xl font-bold  gap-3">
                        Управление настройками Open Router
                    </h1>
                    <CreateOpenRouterComponent/>
                </div>
            </div>

            <div className="space-y-6">
                <OpenRouterListComponent/>
            </div>
        </div>
    )
}


