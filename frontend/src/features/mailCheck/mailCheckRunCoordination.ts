let manualRunActive = false;
let autoCheckAbort: AbortController | null = null;

export function setMailCheckManualRunActive(active: boolean): void {
  manualRunActive = active;
}

export function isMailCheckManualRunActive(): boolean {
  return manualRunActive;
}

export function bindMailCheckAutoAbort(controller: AbortController | null): void {
  autoCheckAbort = controller;
}

export function abortMailCheckAutoRun(): void {
  autoCheckAbort?.abort();
  autoCheckAbort = null;
}

export function prepareMailCheckManualRun(): void {
  abortMailCheckAutoRun();
  setMailCheckManualRunActive(true);
}
