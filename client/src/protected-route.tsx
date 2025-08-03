import {Navigate, Outlet} from "react-router-dom";
import {Loader2} from "lucide-react";
import {useAuth} from "@/auth-context.tsx";

export function ProtectedRoute() {
    const {isAuthenticated, accessToken} = useAuth();

    if (accessToken === undefined) {
        return (
            <div className="flex h-screen items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin"/>
            </div>
        );
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" replace/>;
    }

    return <Outlet/>;
}

