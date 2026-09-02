import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useAuth } from "@/shared/auth/AuthProvider";
import { setUnauthorizedHandler } from "@/shared/auth/unauthorized";

export function SessionLifecycle() {
  const auth = useAuth();
  const queryClient = useQueryClient();

  useEffect(() => {
    setUnauthorizedHandler(() => {
      auth.signOut();
      queryClient.clear();
    });

    return () => {
      setUnauthorizedHandler(null);
    };
  }, [auth.signOut, queryClient]);

  return null;
}
