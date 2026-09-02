"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { getErrorMessage } from "@/lib/apiClient";
import { getPushPublicKey, subscribeToPush, unsubscribeFromPush } from "@/lib/pushApi";

type Status = "checking" | "unsupported" | "blocked" | "enabled" | "disabled";

// The VAPID public key arrives as a base64url string — PushManager.subscribe() needs it as raw
// bytes in a Uint8Array.
function urlBase64ToUint8Array(base64Url: string): Uint8Array<ArrayBuffer> {
  const padding = "=".repeat((4 - (base64Url.length % 4)) % 4);
  const base64 = (base64Url + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  const bytes = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i++) {
    bytes[i] = raw.charCodeAt(i);
  }
  return bytes;
}

export function PushNotificationsToggle() {
  const t = useTranslations("Settings");
  const [status, setStatus] = useState<Status>("checking");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!("serviceWorker" in navigator) || !("PushManager" in window)) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- reflecting browser capability, no user event to attach to
      setStatus("unsupported");
      return;
    }
    if (Notification.permission === "denied") {
      setStatus("blocked");
      return;
    }

    navigator.serviceWorker
      .getRegistration()
      .then((registration) => registration?.pushManager.getSubscription())
      .then((subscription) => setStatus(subscription ? "enabled" : "disabled"))
      .catch(() => setStatus("disabled"));
  }, []);

  async function handleEnable() {
    setError(null);
    setBusy(true);
    try {
      const registration = await navigator.serviceWorker.register("/sw.js");
      await navigator.serviceWorker.ready;

      const permission = await Notification.requestPermission();
      if (permission !== "granted") {
        setStatus(permission === "denied" ? "blocked" : "disabled");
        return;
      }

      const { publicKey } = await getPushPublicKey();
      const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey),
      });

      const json = subscription.toJSON();
      await subscribeToPush(json.endpoint!, json.keys!.p256dh, json.keys!.auth);
      setStatus("enabled");
    } catch (err) {
      setError(getErrorMessage(err, t("pushError")));
    } finally {
      setBusy(false);
    }
  }

  async function handleDisable() {
    setError(null);
    setBusy(true);
    try {
      const registration = await navigator.serviceWorker.getRegistration();
      const subscription = await registration?.pushManager.getSubscription();
      if (subscription) {
        await unsubscribeFromPush(subscription.endpoint);
        await subscription.unsubscribe();
      }
      setStatus("disabled");
    } catch (err) {
      setError(getErrorMessage(err, t("pushError")));
    } finally {
      setBusy(false);
    }
  }

  if (status === "unsupported" || status === "checking") {
    return null;
  }

  return (
    <div className="flex flex-col gap-2 rounded-2xl p-5" style={{ backgroundColor: "var(--fieldmap-card)" }}>
      <h2 className="text-sm font-semibold uppercase" style={{ color: "var(--fieldmap-dim)" }}>
        {t("pushSectionTitle")}
      </h2>
      <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {t("pushSectionDescription")}
      </p>
      {status === "blocked" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("pushBlocked")}
        </p>
      ) : (
        <button
          onClick={status === "enabled" ? handleDisable : handleEnable}
          disabled={busy}
          className="w-fit rounded border px-4 py-2 text-sm disabled:opacity-50"
          style={{ borderColor: "var(--fieldmap-contour)" }}
        >
          {status === "enabled" ? t("pushDisableButton") : t("pushEnableButton")}
        </button>
      )}
      {error ? <p className="text-sm text-red-700">{error}</p> : null}
    </div>
  );
}
