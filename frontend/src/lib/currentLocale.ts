let currentLocale = "bg";

export function setCurrentLocale(locale: string): void {
  currentLocale = locale;
}

export function getCurrentLocale(): string {
  return currentLocale;
}
