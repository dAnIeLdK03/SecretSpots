import { ApiError, apiFetchResponse } from "@/lib/apiClient";

export interface AssistantHistoryMessage {
  role: "user" | "assistant";
  text: string;
}

export interface AssistantQuestion {
  question: string;
  // Earlier turns of the conversation, oldest first (the backend caps how many it accepts).
  history: AssistantHistoryMessage[];
  locale: string;
  latitude?: number;
  longitude?: number;
}

export interface AssistantAnswer {
  answer: string;
  // Spots the answer recommends, in the order mentioned — render them from the app's own data.
  spotIds: string[];
}

export interface AssistantStreamHandlers {
  // What the assistant is doing right now ("Looking for spots around Plovdiv…").
  onStatus?: (status: string) => void;
  // The answer so far, growing as it streams in.
  onText?: (textSoFar: string) => void;
}

type AssistantEvent =
  | { type: "status"; text: string }
  | { type: "delta"; text: string }
  | { type: "done"; text: string; spotIds: string[] }
  | { type: "error"; text: string; code: string };

// Asks the assistant and follows its answer as it streams in. Resolves with the final answer;
// rejects with ApiError for a refused question (429 daily limit, 404 disabled, 400 invalid) or a
// model failure (503), and with an AbortError when `signal` is aborted — that's the Stop button:
// aborting closes the connection, which also stops the model on the server.
export async function askAssistant(
  question: AssistantQuestion,
  handlers: AssistantStreamHandlers = {},
  signal?: AbortSignal,
): Promise<AssistantAnswer> {
  const response = await apiFetchResponse("/assistant/chat", {
    method: "POST",
    body: JSON.stringify(question),
    headers: { Accept: "text/event-stream" },
    signal,
  });
  if (!response.body) {
    throw new Error("The assistant response has no body.");
  }

  const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
  let buffer = "";
  let text = "";
  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      buffer += value;

      // Events are separated by a blank line.
      let boundary = buffer.indexOf("\n\n");
      while (boundary >= 0) {
        const event = parseEvent(buffer.slice(0, boundary));
        buffer = buffer.slice(boundary + 2);
        boundary = buffer.indexOf("\n\n");
        if (!event) continue;

        switch (event.type) {
          case "status":
            handlers.onStatus?.(event.text);
            break;
          case "delta":
            text += event.text;
            handlers.onText?.(text);
            break;
          case "done":
            return { answer: event.text, spotIds: event.spotIds };
          case "error":
            throw new ApiError(503, { status: 503, detail: event.text, code: event.code });
        }
      }
    }
  } finally {
    reader.releaseLock();
  }

  throw new Error("The assistant stream ended without an answer.");
}

function parseEvent(block: string): AssistantEvent | null {
  const data = block
    .split("\n")
    .filter((line) => line.startsWith("data:"))
    .map((line) => line.slice("data:".length).trimStart())
    .join("\n");
  if (!data) return null;
  try {
    return JSON.parse(data) as AssistantEvent;
  } catch {
    return null;
  }
}
