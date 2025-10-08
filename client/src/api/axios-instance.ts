/// <reference types="vite/client" />
import Axios, {type AxiosRequestConfig, HttpStatusCode} from 'axios';
import {postApiV1AccountRefreshToken} from "@/api/endpoints/account/account.ts";
import {GetAccessToken} from "@/auth-context.tsx";

const API_URL ='';

export const axiosInstance = Axios.create({
    baseURL: API_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

let isRefreshing = false;
let failedQueue: Array<{
    resolve: (value?: string | null) => void;
    reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });

    failedQueue = [];
};

axiosInstance.interceptors.request.use((config) => {
    const token = GetAccessToken();
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

axiosInstance.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;
        if (error.code === "ERR_CANCELED") {
            return Promise.resolve({status: 499});
        }

        if (error.response?.status === HttpStatusCode.Unauthorized) {
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({resolve, reject});
                })
                    .then(token => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return axiosInstance(originalRequest);
                    })
                    .catch(err => {
                        return Promise.reject(err);
                    });
            }

            originalRequest._retry = true;
            isRefreshing = true;

            const refreshToken = localStorage.getItem('refreshToken');

            if (!refreshToken) {
                processQueue(error, null);
                localStorage.removeItem('accessToken');
                localStorage.removeItem('refreshToken');
                window.location.href = '/login';
                return Promise.reject(error);
            }

            try {
                const response = await postApiV1AccountRefreshToken({
                    refreshToken: refreshToken
                });

                if (response.accessToken && response.refreshToken) {
                    localStorage.setItem('accessToken', response.accessToken);
                    localStorage.setItem('refreshToken', response.refreshToken);

                    processQueue(null, response.accessToken);

                    originalRequest.headers.Authorization = `Bearer ${response.accessToken}`;
                    return axiosInstance(originalRequest);
                } else {
                    throw new Error('Invalid refresh response');
                }
            } catch (refreshError) {
                processQueue(refreshError, null);

                localStorage.removeItem('accessToken');
                localStorage.removeItem('refreshToken');
                window.location.href = '/login';

                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

        console.error('API Error:', error);
        if (error.response?.data?.title) {
            console.error('Error details:', error.response.data.title);
        }

        return Promise.reject(error);
    }
);

export const customInstance = <T>(
    config: AxiosRequestConfig
): Promise<T> => {
    return axiosInstance(config).then((response) => response.data);
};

export const fileInstance = <T>(
    config: AxiosRequestConfig,
): Promise<T> => {
    return axiosInstance({...config, responseType: 'blob'})
        .then((response) => response.data);
};
