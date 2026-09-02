self.addEventListener("push", (event) => {
  if (!event.data) return;

  const data = event.data.json();
  event.waitUntil(
    self.registration.showNotification(data.title, {
      body: data.body,
      data: { relatedSpotId: data.relatedSpotId },
    }),
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();

  const spotId = event.notification.data && event.notification.data.relatedSpotId;
  const url = spotId ? `/spots/${spotId}` : "/";

  event.waitUntil(
    self.clients.matchAll({ type: "window", includeUncontrolled: true }).then((clientsArr) => {
      for (const client of clientsArr) {
        if ("focus" in client) {
          client.navigate(url);
          return client.focus();
        }
      }
      if (self.clients.openWindow) {
        return self.clients.openWindow(url);
      }
    }),
  );
});
