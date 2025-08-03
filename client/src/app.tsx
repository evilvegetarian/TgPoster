import {Outlet} from "react-router-dom";
import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {ReactQueryDevtools} from "@tanstack/react-query-devtools";
import {AuthProvider} from "@/auth-context.tsx";

const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: 1,
            staleTime: 1000 * 60 * 5,
        },
    },
});

export function App() {
    return (
        <AuthProvider>
            <QueryClientProvider client={queryClient}>
                <Outlet/>
                <ReactQueryDevtools initialIsOpen={false}/>
            </QueryClientProvider>
        </AuthProvider>
    );
}

