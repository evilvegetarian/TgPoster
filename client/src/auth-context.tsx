import {createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useState} from 'react';
import {postApiV1AccountRefreshToken} from "@/api/endpoints/account/account";

interface AuthContextType {
    accessToken: string | null;
    refreshToken: string | null;
    isAuthenticated: boolean;
    login: (accessToken: string, refreshToken: string) => void;
    logout: () => void;
    refreshTokens: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function GetAccessToken() {
    return localStorage.getItem("accessToken");
}

export function GetRefreshToken() {
    return localStorage.getItem("refreshToken");
}


export function AuthProvider({children}: { children: ReactNode }) {
    const [accessToken, setAccessToken] = useState<string | null>(GetAccessToken);
    const [refreshToken, setRefreshToken] = useState<string | null>(GetRefreshToken);

    useEffect(() => {
        if (accessToken) {
            localStorage.setItem("accessToken", accessToken);
        } else {
            localStorage.removeItem("accessToken");
        }
    }, [accessToken]);

    useEffect(() => {
        if (refreshToken) {
            localStorage.setItem("refreshToken", refreshToken);
        } else {
            localStorage.removeItem("refreshToken");
        }
    }, [refreshToken]);

    const login = useCallback((newAccessToken: string, newRefreshToken: string) => {
        setAccessToken(newAccessToken);
        setRefreshToken(newRefreshToken);
    }, []);

    const logout = useCallback(() => {
        setAccessToken(null);
        setRefreshToken(null);
    }, []);

    const refreshTokens = useCallback(async (): Promise<boolean> => {
        if (!refreshToken) {
            logout();
            return false;
        }

        try {
            const response = await postApiV1AccountRefreshToken({
                refreshToken: refreshToken
            });

            if (response.accessToken && response.refreshToken) {
                setAccessToken(response.accessToken);
                setRefreshToken(response.refreshToken);
                return true;
            } else {
                logout();
                return false;
            }
        } catch (error) {
            logout();
            return false;
        }
    }, [refreshToken, logout]);

    const contextValue = useMemo(() => ({
        accessToken,
        refreshToken,
        isAuthenticated: !!accessToken,
        login,
        logout,
        refreshTokens,
    }), [accessToken, refreshToken, login, logout, refreshTokens]);

    return (
        <AuthContext.Provider value={contextValue}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
}
