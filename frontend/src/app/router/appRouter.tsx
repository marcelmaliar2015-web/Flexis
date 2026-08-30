import { createBrowserRouter } from "react-router-dom";
import { AppLayout } from "@/app/layout/AppLayout";
import { RequireAdmin, RequireAuth } from "@/app/router/routeGuards";
import { SignInPage } from "@/features/auth/SignInPage";
import { HealthPage } from "@/features/health/HealthPage";
import { HomePage } from "@/features/home/HomePage";
import { UsersPage } from "@/features/users/UsersPage";

export const appRouter = createBrowserRouter([
  {
    path: "/",
    element: <AppLayout />,
    children: [
      {
        index: true,
        element: <HomePage />,
      },
      {
        path: "sign-in",
        element: <SignInPage />,
      },
      {
        element: <RequireAuth />,
        children: [
          {
            path: "health",
            element: <HealthPage />,
          },
          {
            element: <RequireAdmin />,
            children: [
              {
                path: "users",
                element: <UsersPage />,
              },
            ],
          },
        ],
      },
    ],
  },
]);
