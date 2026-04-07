export interface ScriptDto {
  id: string;
  name: string;
  description?: string;
  defaultLanguage: string;
  paragraphs: ParagraphDto[];
  createdAt: string;
  updatedAt: string;
}

export interface ParagraphDto {
  text: string;
  language?: string;
  pauseBeforeSeconds: number;
  pauseAfterSeconds: number;
}

export interface JoinCallRequest {
  joinUrl: string;
  displayName?: string;
}

export interface JoinCallResponse {
  callId?: string;
  scenarioId?: string;
  callUri?: string;
}

export interface StartScriptRequest {
  callId: string;
  displayName: string;
  scriptId: string;
}
