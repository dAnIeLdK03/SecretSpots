interface LiveAnnouncementProps {
  // Empty while there is nothing to say (e.g. still loading).
  message: string;
}

// Visually hidden text read out by screen readers whenever `message` changes — for updates that are
// obvious on screen but silent otherwise, like how many results a search returned. Must stay
// mounted: a live region only announces changes to an element that was already in the page.
export function LiveAnnouncement({ message }: LiveAnnouncementProps) {
  return (
    <p className="sr-only" aria-live="polite" aria-atomic="true">
      {message}
    </p>
  );
}
