import axios from "axios";
import { getToken, clearAuthStorage } from "./storage";

export const api = axios.create({
    baseURL: "https://client.i9t24.online/api",
    headers: {
        "Content-Type": "application/json",
    },
});

api.interceptors.request.use((config) => {
    const token = getToken();
    if (token) {
        config.headers = config.headers ?? {};
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

api.interceptors.response.use(
    (res) => res,
    (err) => {
        if (err?.response?.status === 401) {
            clearAuthStorage();
            window.location.href = "/login";
        }
        return Promise.reject(err);
    }
);
