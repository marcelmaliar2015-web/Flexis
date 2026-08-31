import { getJson, postJson, putJson } from "@/shared/api/client";
import type { SignInRequest, SignInResultDto } from "@/shared/types/auth";
import type { UserDto } from "@/shared/types/user";

export function signIn(request: SignInRequest): Promise<SignInResultDto> {
  return postJson<SignInResultDto>("/api/auth/sign-in", request);
}

export function getCurrentUser(): Promise<UserDto> {
  return getJson<UserDto>("/api/auth/me");
}

export type UpdateCurrentUserRequest = {
  displayName: string;
  password: string | null;
};

export function updateCurrentUser(request: UpdateCurrentUserRequest): Promise<UserDto> {
  return putJson<UserDto>("/api/auth/me", request);
}
