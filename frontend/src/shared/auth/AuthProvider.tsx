import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { setAccessToken } from "@/shared/api/accessToken";
import { getCurrentUser, signIn as requestSignIn } from "@/shared/api/auth";
import { isApiError } from "@/shared/api/errors";
import { readStoredAccessToken, writeStoredAccessToken } from "@/shared/auth/tokenStorage";
import type { SignInRequest } from "@/shared/types/auth";
import type { UserDto } from "@/shared/types/user";

type AuthContextValue = {
  user: UserDto | null;
  isReady: boolean;
  signIn: (request: SignInRequest) => Promise<void>;
  signOut: () => void;
  replaceUser: (user: UserDto) => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

type AuthProviderProps = {
  children: ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [isReady, setIsReady] = useState(false);

  const applySession = useCallback((token: string | null, nextUser: UserDto | null) => {
    setAccessToken(token);
    writeStoredAccessToken(token);
    setUser(nextUser);
  }, []);

  useEffect(() => {
    const stored = readStoredAccessToken();
    if (!stored) {
      setIsReady(true);
      return;
    }

    setAccessToken(stored);
    void getCurrentUser()
      .then((current) => {
        setUser(current);
      })
      .catch((error: unknown) => {
        if (isApiError(error) && error.status === 401) {
          applySession(null, null);
          return;
        }
        applySession(null, null);
      })
      .finally(() => {
        setIsReady(true);
      });
  }, [applySession]);

  const signIn = useCallback(
    async (request: SignInRequest) => {
      const result = await requestSignIn(request);
      applySession(result.accessToken, result.user);
    },
    [applySession],
  );

  const signOut = useCallback(() => {
    applySession(null, null);
  }, [applySession]);

  const replaceUser = useCallback((nextUser: UserDto) => {
    setUser(nextUser);
  }, []);

  const value = useMemo(
    () => ({
      user,
      isReady,
      signIn,
      signOut,
      replaceUser,
    }),
    [user, isReady, signIn, signOut, replaceUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return value;
}
