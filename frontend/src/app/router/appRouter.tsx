import { createBrowserRouter } from "react-router-dom";
import { AppLayout } from "@/app/layout/AppLayout";
import { HealthPage } from "@/features/health/HealthPage";
import { HomePage } from "@/features/home/HomePage";

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
        path: "health",
        element: <HealthPage />,
      },
    ],
  },
]);
