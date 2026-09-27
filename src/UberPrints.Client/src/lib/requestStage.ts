import { RequestStatusEnum } from '../types/api';

export type RequestStage = 'waiting' | 'printing' | 'pickup' | 'done' | 'rejected';

export type MainRequestStage = Exclude<RequestStage, 'rejected'>;

// Main track, in order. Rejected sits off the track.
export const MAIN_STAGES: MainRequestStage[] = ['waiting', 'printing', 'pickup', 'done'];

export const STAGE_LABELS: Record<RequestStage, string> = {
  waiting: 'Waiting',
  printing: 'Printing',
  pickup: 'Pickup',
  done: 'Done',
  rejected: 'Rejected',
};

// Literal class names so Tailwind picks them up.
export const STAGE_TEXT_CLASSES: Record<RequestStage, string> = {
  waiting: 'text-stage-waiting',
  printing: 'text-stage-printing',
  pickup: 'text-stage-pickup',
  done: 'text-stage-done',
  rejected: 'text-stage-rejected',
};

export const STAGE_BG_CLASSES: Record<RequestStage, string> = {
  waiting: 'bg-stage-waiting',
  printing: 'bg-stage-printing',
  pickup: 'bg-stage-pickup',
  done: 'bg-stage-done',
  rejected: 'bg-stage-rejected',
};

export const STATUS_LABELS: Record<RequestStatusEnum, string> = {
  [RequestStatusEnum.Pending]: 'Pending',
  [RequestStatusEnum.Accepted]: 'Accepted',
  [RequestStatusEnum.Rejected]: 'Rejected',
  [RequestStatusEnum.OnHold]: 'On hold',
  [RequestStatusEnum.Paused]: 'Paused',
  [RequestStatusEnum.WaitingForMaterials]: 'Waiting for materials',
  [RequestStatusEnum.Delivering]: 'Delivering',
  [RequestStatusEnum.WaitingForPickup]: 'Waiting for pickup',
  [RequestStatusEnum.Completed]: 'Completed',
};

const STATUS_STAGES: Record<RequestStatusEnum, RequestStage> = {
  [RequestStatusEnum.Pending]: 'waiting',
  [RequestStatusEnum.OnHold]: 'waiting',
  [RequestStatusEnum.WaitingForMaterials]: 'waiting',
  [RequestStatusEnum.Accepted]: 'printing',
  [RequestStatusEnum.Paused]: 'printing',
  [RequestStatusEnum.Delivering]: 'pickup',
  [RequestStatusEnum.WaitingForPickup]: 'pickup',
  [RequestStatusEnum.Completed]: 'done',
  [RequestStatusEnum.Rejected]: 'rejected',
};

export function getRequestStage(status: RequestStatusEnum): RequestStage {
  return STATUS_STAGES[status] ?? 'waiting';
}
