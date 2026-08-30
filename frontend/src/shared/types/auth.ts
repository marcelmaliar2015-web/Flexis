import type { UserDto } from "@/shared/types/user";

export type SignInRequest = {
  email: string;
  password: string;
};

export type SignInResultDto = {
  accessToken: string;
  user: UserDto;
};
