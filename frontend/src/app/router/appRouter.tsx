import { Navigate, createBrowserRouter } from "react-router-dom";
import { AppLayout } from "@/app/layout/AppLayout";
import { AuthenticatedLayout } from "@/app/layout/AuthenticatedLayout";
import { RequireAuth } from "@/app/router/routeGuards";
import { SignInPage } from "@/features/auth/SignInPage";
import { DashboardPage } from "@/features/dashboard/DashboardPage";
import { HealthPage } from "@/features/health/HealthPage";
import { HelpPage } from "@/features/help/HelpPage";
import { HomePage } from "@/features/home/HomePage";
import { JobApplicationPage } from "@/features/jobApplication/JobApplicationPage";
import { JobApplicationPipelineEntryPage } from "@/features/jobApplication/JobApplicationPipelineEntryPage";
import { SettingsPage } from "@/features/settings/SettingsPage";
import { appPaths } from "@/shared/config/paths";

export const appRouter = createBrowserRouter([
  {
    path: appPaths.home,
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
            element: <AuthenticatedLayout />,
            children: [
              {
                path: "dashboard",
                element: <DashboardPage />,
              },
              {
                path: "job-application",
                element: <JobApplicationPage />,
              },
              {
                path: "job-application/pipeline/:entryId",
                element: <JobApplicationPipelineEntryPage />,
              },
              {
                path: "settings",
                element: <SettingsPage />,
              },
              {
                path: "help",
                element: <HelpPage />,
              },
              {
                path: "health",
                element: <HealthPage />,
              },
              {
                path: "users",
                element: <Navigate to={appPaths.settings} replace />,
              },
            ],
          },
        ],
      },
    ],
  },
]);
