import { getJson, postJson, putJson } from "@/shared/api/client";
import type { UserDto, UserRole } from "@/shared/types/user";

export const usersQueryKey = ["users"] as const;

export type CreateUserRequest = {
  email: string;
  displayName: string;
  password: string;
  role: UserRole;
};

export type UpdateUserRequest = {
  displayName: string;
  role: UserRole;
  isActive: boolean;
  password: string | null;
};

export function listUsers(): Promise<UserDto[]> {
  return getJson<UserDto[]>("/api/users");
}

export function createUser(request: CreateUserRequest): Promise<UserDto> {
  return postJson<UserDto>("/api/users", request);
}

export function updateUser(id: string, request: UpdateUserRequest): Promise<UserDto> {
  return putJson<UserDto>(`/api/users/${id}`, request);
}
