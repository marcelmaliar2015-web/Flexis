export type GoogleConnectionStatus = {
  configured: boolean;
  connected: boolean;
  googleEmail: string | null;
  connectedAt: string | null;
  capabilities: string[];
};

export type GoogleConnectStart = {
  authorizationUrl: string;
};
