export type UserRole = "Admin" | "User" | "Viewer";

export type UserDto = {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
};
