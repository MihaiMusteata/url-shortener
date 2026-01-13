export type MinimalUserDto = {
    id: string;
    email: string;
    username: string;
    firstName: string;
    lastName: string;
    role: string;
};

export type AuthResultDto = {
    accessToken: string;
    expiresAtUtc: string; // ISO
    user: MinimalUserDto;
};

export type LoginRequestDto = {
    email: string;
    password: string;
};

export type SignUpRequestDto = {
    email: string;
    username: string;
    firstName: string;
    lastName: string;
    password: string;
};
