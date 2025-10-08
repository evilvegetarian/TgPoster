import { useAuth } from "@/auth-context.tsx";
import { useEffect } from "react";

export function LogOutPage() {
    const { logout } = useAuth();

    useEffect(() => {
        logout();
    }, [logout]);

    return (
        <div>
            <p>Logging out...</p>
        </div>
    );
}