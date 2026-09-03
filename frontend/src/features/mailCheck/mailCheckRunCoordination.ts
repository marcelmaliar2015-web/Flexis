let manualRunActive = false;

export function setMailCheckManualRunActive(active: boolean): void {
  manualRunActive = active;
}

export function isMailCheckManualRunActive(): boolean {
  return manualRunActive;
}

export function prepareMailCheckManualRun(): void {
  setMailCheckManualRunActive(true);
}
