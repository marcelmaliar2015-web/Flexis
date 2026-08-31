import { useQuery } from "@tanstack/react-query";
import { getJobFinancialBoard, jobFinancialQueryKey } from "@/shared/api/financial";
import { getGoogleConnection, googleConnectionQueryKey } from "@/shared/api/google";
import { getHealthStatus, healthQueryKey } from "@/shared/api/health";
import { jobApplicationLogsQueryKey, listJobApplicationLogs } from "@/shared/api/jobApplicationLogs";
import { getJobPipelineBoard, jobPipelineQueryKey } from "@/shared/api/pipeline";
import { listUsers, usersQueryKey } from "@/shared/api/users";
import { useAuth } from "@/shared/auth/AuthProvider";

export function useDashboardData() {
  const auth = useAuth();
  const isAdmin = auth.user?.role === "Admin";

  const healthQuery = useQuery({
    queryKey: healthQueryKey,
    queryFn: getHealthStatus,
  });
  const googleQuery = useQuery({
    queryKey: googleConnectionQueryKey,
    queryFn: getGoogleConnection,
  });
  const pipelineQuery = useQuery({
    queryKey: jobPipelineQueryKey,
    queryFn: getJobPipelineBoard,
  });
  const financialQuery = useQuery({
    queryKey: jobFinancialQueryKey,
    queryFn: getJobFinancialBoard,
  });
  const logsQuery = useQuery({
    queryKey: jobApplicationLogsQueryKey,
    queryFn: listJobApplicationLogs,
  });
  const usersQuery = useQuery({
    queryKey: usersQueryKey,
    queryFn: listUsers,
    enabled: isAdmin,
  });

  return {
    user: auth.user,
    isAdmin,
    healthQuery,
    googleQuery,
    pipelineQuery,
    financialQuery,
    logsQuery,
    usersQuery,
  };
}
