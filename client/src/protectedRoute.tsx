import {Navigate, Outlet} from "react-router-dom";
import {Loader2} from "lucide-react";
import {useAuth} from "@/authContext.tsx";

export function ProtectedRoute() {
    const { isAuthenticated, token } = useAuth();

    if (token === undefined) {
        console.log("перешел1");
        return (
            <div className="flex h-screen items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin" />
            </div>
        );
    }
    console.log("перешел2");

    if (!isAuthenticated) {
        console.log("перешел3");
        return <Navigate to="/login" replace />;
    }

    return <Outlet />;
}

