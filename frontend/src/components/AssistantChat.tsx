"use client";

import { useEffect, useRef, useState } from "react";
import type { FormEvent, KeyboardEvent } from "react";
import { useLocale, useTranslations } from "next-intl";
import { MessageCircle, Send, Square, X } from "lucide-react";
import { Link, usePathname } from "@/i18n/navigation";
import { ApiError } from "@/lib/apiClient";
import { askAssistant } from "@/lib/assistantApi";
import type { AssistantHistoryMessage } from "@/lib/assistantApi";
import { getSpot } from "@/lib/spotsApi";
import type { SpotResponse } from "@/lib/spotsApi";
import { useAuthStore } from "@/store/useAuthStore";
import { useGeolocationStore } from "@/store/useGeolocationStore";
import type { Coordinates } from "@/store/useGeolocationStore";
import { CategoryIcon } from "@/components/CategoryIcon";
import { ErrorMessage } from "@/components/ErrorMessage";

// The backend caps both (Assistant:MaxQuestionLength / MaxHistoryMessages) and rejects more.
const MAX_QUESTION_LENGTH = 500;
const MAX_HISTORY_MESSAGES = 10;

// Off unless NEXT_PUBLIC_ASSISTANT_ENABLED=true, to match Assistant:Enabled on the backend
// (which answers 404 when it's off) — so a disabled assistant has no button at all.
const ASSISTANT_ENABLED = process.env.NEXT_PUBLIC_ASSISTANT_ENABLED === "true";

interface ChatMessage {
  id: number;
  role: "user" | "assistant";
  text: string;
  spots?: SpotResponse[];
  stopped?: boolean;
}

// The position once the browser has one, or null if it can't give one (denied, timed out,
// unsupported). An earlier failure gets one fresh attempt; a request already in flight is awaited.
function waitForLocation(): Promise<Coordinates | null> {
  const store = useGeolocationStore;
  const known = store.getState().coords;
  if (known) return Promise.resolve(known);

  store.getState().refreshLocation();
  if (store.getState().status !== "locating") return Promise.resolve(store.getState().coords);

  return new Promise((resolve) => {
    const unsubscribe = store.subscribe((state) => {
      if (state.status === "locating") return;
      unsubscribe();
      resolve(state.coords);
    });
  });
}

export function AssistantChat() {
  const status = useAuthStore((state) => state.status);
  if (!ASSISTANT_ENABLED || status !== "authenticated") return null;
  return <AssistantChatWidget />;
}

function AssistantChatWidget() {
  const t = useTranslations("Assistant");
  const tCommon = useTranslations("Common");
  const locale = useLocale();
  const pathname = usePathname();
  const requestLocation = useGeolocationStore((state) => state.requestLocation);

  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [liveStatus, setLiveStatus] = useState<string | null>(null);
  const [isAnswering, setIsAnswering] = useState(false);
  const [isLocating, setIsLocating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const abortRef = useRef<AbortController | null>(null);
  const nextIdRef = useRef(1);
  const openButtonRef = useRef<HTMLButtonElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const listRef = useRef<HTMLDivElement>(null);

  // Stop the model if the user leaves while it is still answering.
  useEffect(() => () => abortRef.current?.abort(), []);

  useEffect(() => {
    if (isOpen) inputRef.current?.focus();
  }, [isOpen]);

  useEffect(() => {
    const list = listRef.current;
    if (list) list.scrollTop = list.scrollHeight;
  }, [messages, liveStatus, error, isOpen]);

  function open() {
    // "Near me" questions need a position; a no-op if the app already asked for one.
    requestLocation();
    setIsOpen(true);
  }

  function close() {
    setIsOpen(false);
    openButtonRef.current?.focus();
  }

  function handlePanelKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Escape") {
      event.stopPropagation();
      close();
    }
  }

  function updateMessage(id: number, patch: Partial<ChatMessage>) {
    setMessages((current) => current.map((m) => (m.id === id ? { ...m, ...patch } : m)));
  }

  function describeError(err: unknown): string {
    if (err instanceof ApiError) {
      if (err.status === 429) return t("limitReached");
      if (err.status === 404) return t("unavailable");
      if (err.status === 503 && err.problem.detail) return err.problem.detail;
    }
    return t("genericError");
  }

  async function send(question: string) {
    const trimmed = question.trim();
    if (!trimmed || isAnswering) return;

    const history: AssistantHistoryMessage[] = messages
      .filter((m) => m.text && !m.stopped)
      .slice(-MAX_HISTORY_MESSAGES)
      .map((m) => ({ role: m.role, text: m.text }));
    const answerId = nextIdRef.current + 1;
    setMessages((current) => [
      ...current,
      { id: nextIdRef.current, role: "user", text: trimmed },
      { id: answerId, role: "assistant", text: "" },
    ]);
    nextIdRef.current += 2;
    setInput("");
    setError(null);
    setLiveStatus(t("thinking"));
    setIsAnswering(true);

    // Read at send time, not from the last render: the position may have arrived since.
    const coords = useGeolocationStore.getState().coords;
    const controller = new AbortController();
    abortRef.current = controller;
    try {
      const answer = await askAssistant(
        {
          question: trimmed,
          history,
          locale,
          latitude: coords?.lat,
          longitude: coords?.lng,
        },
        {
          onStatus: setLiveStatus,
          onText: (text) => updateMessage(answerId, { text }),
        },
        controller.signal,
      );
      updateMessage(answerId, { text: answer.answer });
      setLiveStatus(null);
      setIsAnswering(false);

      // Recommended places are shown from the app's own data, not from the model's text.
      const found = await Promise.all(answer.spotIds.map((id) => getSpot(id, controller.signal).catch(() => null)));
      const spots = found.filter((s): s is SpotResponse => s !== null);
      if (spots.length > 0) updateMessage(answerId, { spots });
    } catch (err) {
      if (controller.signal.aborted) {
        // Stop pressed: keep what arrived, or drop the empty bubble.
        setMessages((current) =>
          current.flatMap((m) => (m.id !== answerId ? [m] : m.text ? [{ ...m, stopped: true }] : [])),
        );
      } else {
        setMessages((current) => current.filter((m) => m.id !== answerId));
        setError(describeError(err));
      }
    } finally {
      if (abortRef.current === controller) abortRef.current = null;
      setLiveStatus(null);
      setIsAnswering(false);
    }
  }

  // A "near me" suggestion is only sent with a position: without one the model has nothing to
  // measure from, so the user is told how to proceed instead.
  async function sendNearMe(question: string) {
    if (isAnswering || isLocating) return;
    setError(null);
    setIsLocating(true);
    setLiveStatus(t("locating"));
    const position = await waitForLocation();
    setIsLocating(false);
    setLiveStatus(null);
    if (!position) {
      setError(t("locationNeeded"));
      return;
    }
    await send(question);
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    void send(input);
  }

  function handleInputKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    // Enter sends, Shift+Enter adds a line (not while an IME is composing).
    if (event.key === "Enter" && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault();
      void send(input);
    }
  }

  function startNewChat() {
    abortRef.current?.abort();
    setMessages([]);
    setError(null);
    setLiveStatus(null);
    setIsAnswering(false);
  }

  // The map page has its own "add a spot" button in this corner; sit above it there.
  const bottomClass = pathname === "/map" ? "bottom-28" : "bottom-6";
  const panelBottomClass = pathname === "/map" ? "bottom-44" : "bottom-24";

  const suggestions = [
    { text: t("suggestion1"), nearMe: false },
    { text: t("suggestion2"), nearMe: true },
    { text: t("suggestion3"), nearMe: false },
  ];

  return (
    <>
      {isOpen ? (
        <div
          role="dialog"
          aria-label={t("title")}
          onKeyDown={handlePanelKeyDown}
          className={`fixed right-4 left-4 z-40 flex h-[32rem] max-h-[calc(100dvh-10rem)] flex-col overflow-hidden rounded-lg border border-contour bg-paper-light text-ink shadow-xl sm:right-6 sm:left-auto sm:w-96 ${panelBottomClass}`}
        >
          <div className="flex items-center justify-between border-b border-contour px-4 py-3">
            <h2 className="text-sm font-semibold">{t("title")}</h2>
            <div className="flex items-center gap-1">
              {messages.length > 0 ? (
                <button
                  type="button"
                  onClick={startNewChat}
                  className="rounded px-2 py-1 text-xs text-dim hover:text-ink"
                >
                  {t("newChat")}
                </button>
              ) : null}
              <button
                type="button"
                onClick={close}
                aria-label={tCommon("close")}
                className="rounded p-1 text-dim hover:text-ink"
              >
                <X size={18} aria-hidden="true" />
              </button>
            </div>
          </div>

          <div ref={listRef} role="log" aria-live="polite" className="flex flex-1 flex-col gap-3 overflow-y-auto p-4">
            {messages.length === 0 ? (
              <div className="flex flex-col gap-3 text-sm">
                <p className="text-dim">{t("emptyHint")}</p>
                <div className="flex flex-col items-start gap-2">
                  {suggestions.map((suggestion) => (
                    <button
                      key={suggestion.text}
                      type="button"
                      disabled={isLocating}
                      onClick={() => void (suggestion.nearMe ? sendNearMe(suggestion.text) : send(suggestion.text))}
                      className="rounded-full border border-contour px-3 py-1.5 text-left hover:bg-black/5 disabled:opacity-50 dark:hover:bg-white/5"
                    >
                      {suggestion.text}
                    </button>
                  ))}
                </div>
              </div>
            ) : null}

            {messages.map((message) => (
              <ChatBubble key={message.id} message={message} onNavigate={close} />
            ))}

            {liveStatus ? <p className="text-xs text-dim italic">{liveStatus}</p> : null}
            {error ? <ErrorMessage>{error}</ErrorMessage> : null}
          </div>

          <form onSubmit={handleSubmit} className="border-t border-contour p-3">
            <div className="flex items-end gap-2">
              <textarea
                ref={inputRef}
                value={input}
                onChange={(event) => setInput(event.target.value)}
                onKeyDown={handleInputKeyDown}
                maxLength={MAX_QUESTION_LENGTH}
                rows={1}
                placeholder={t("placeholder")}
                aria-label={t("placeholder")}
                className="max-h-28 min-h-10 flex-1 resize-none rounded border border-contour bg-paper px-3 py-2 text-sm"
              />
              {isAnswering ? (
                <button
                  type="button"
                  onClick={() => abortRef.current?.abort()}
                  aria-label={t("stop")}
                  title={t("stop")}
                  className="flex size-10 shrink-0 items-center justify-center rounded-full bg-ink text-paper-light"
                >
                  <Square size={16} fill="currentColor" aria-hidden="true" />
                </button>
              ) : (
                <button
                  type="submit"
                  disabled={!input.trim()}
                  aria-label={t("send")}
                  title={t("send")}
                  className="flex size-10 shrink-0 items-center justify-center rounded-full bg-trail text-on-trail disabled:opacity-50"
                >
                  <Send size={16} aria-hidden="true" />
                </button>
              )}
            </div>
            <p className="mt-2 text-xs text-dim">{t("disclaimer")}</p>
          </form>
        </div>
      ) : null}

      <button
        ref={openButtonRef}
        type="button"
        onClick={isOpen ? close : open}
        aria-label={t("openLabel")}
        aria-expanded={isOpen}
        className={`fixed right-6 z-40 flex size-14 items-center justify-center rounded-full bg-trail text-on-trail shadow-lg ${bottomClass}`}
      >
        {isOpen ? <X size={24} aria-hidden="true" /> : <MessageCircle size={24} aria-hidden="true" />}
      </button>
    </>
  );
}

function ChatBubble({ message, onNavigate }: { message: ChatMessage; onNavigate: () => void }) {
  const t = useTranslations("Assistant");
  const tCategory = useTranslations("Spots.category");
  const isUser = message.role === "user";
  if (!isUser && !message.text) return null;

  return (
    <div className={`flex flex-col gap-2 ${isUser ? "items-end" : "items-start"}`}>
      <p
        className={`max-w-[85%] rounded-lg px-3 py-2 text-sm break-words whitespace-pre-wrap ${
          isUser ? "bg-trail text-on-trail" : "bg-card text-ink"
        }`}
      >
        {message.text}
      </p>
      {message.stopped ? <p className="text-xs text-dim italic">{t("stopped")}</p> : null}
      {message.spots?.map((spot) => (
        <Link
          key={spot.id}
          href={`/spots/${spot.id}`}
          onClick={onNavigate}
          className="flex w-full max-w-[85%] items-center gap-3 overflow-hidden rounded-lg border border-contour bg-paper hover:bg-black/5 dark:hover:bg-white/5"
        >
          {spot.photoUrls[0] ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={spot.photoUrls[0]} alt="" className="size-16 shrink-0 object-cover" />
          ) : null}
          <span className="min-w-0 flex-1 py-2 pr-3">
            <span className="block truncate text-sm font-medium">{spot.name}</span>
            <span className="flex items-center gap-1 text-xs text-dim">
              <CategoryIcon category={spot.category} size={12} />
              {tCategory(spot.category)}
            </span>
          </span>
        </Link>
      ))}
    </div>
  );
}
