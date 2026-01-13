import { api } from "../lib/api";
import type { AuthResultDto, LoginRequestDto, SignUpRequestDto } from "../types/auth";

function errorMessage(e: any): string {
    return e?.response?.data ?? e?.message ?? "Request failed";
}

export const authService = {
    async login(dto: LoginRequestDto): Promise<AuthResultDto> {
        try {
            const res = await api.post<AuthResultDto>("/auth/login", dto);
            return res.data;
        } catch (e) {
            throw new Error(errorMessage(e));
        }
    },

    async signup(dto: SignUpRequestDto): Promise<AuthResultDto> {
        try {
            const res = await api.post<AuthResultDto>("/auth/signup", dto);
            return res.data;
        } catch (e) {
            throw new Error(errorMessage(e));
        }
    },
};
