import type { UserDto } from "@/shared/types/user";

export function userInitials(user: UserDto): string {
  const parts = user.displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return user.email.slice(0, 1).toUpperCase();
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 1).toUpperCase();
  }
  return `${parts[0].slice(0, 1)}${parts[1].slice(0, 1)}`.toUpperCase();
}
