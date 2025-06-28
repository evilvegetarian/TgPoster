import Axios, {type AxiosRequestConfig} from 'axios';

const API_URL = import.meta.env.VITE_API_URL;
export const axiosInstance = Axios.create({
    baseURL: API_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// токен авторизации
axiosInstance.interceptors.request.use((config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

axiosInstance.interceptors.response.use(
    (response) => response, // Просто возвращаем успешный ответ
    (error) => {
        if (error.response?.status === 401) {
            console.error('Unauthorized! Redirecting to login...');
            // window.location.href = '/login';
        }
        console.error(error)
        console.error(error.response.data.title);
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

export default customInstance;

