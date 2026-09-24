"use client";

import { useEffect, useId, useState } from "react";
import { useTranslations } from "next-intl";
import { searchSpots } from "@/lib/spotsApi";
import type { SpotSearchResult } from "@/lib/spotsApi";
import { CategoryIcon } from "./CategoryIcon";

const MAX_RESULTS = 5;
const DEBOUNCE_MS = 300;

interface MapSearchBoxProps {
  onSelect: (spot: SpotSearchResult) => void;
}

export function MapSearchBox({ onSelect }: MapSearchBoxProps) {
  const t = useTranslations("Spots");
  const listId = useId();
  const [term, setTerm] = useState("");
  const [results, setResults] = useState<SpotSearchResult[]>([]);
  const [searched, setSearched] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const trimmed = term.trim();
    if (!trimmed) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- clearing derived results when the input is emptied
      setResults([]);
      setSearched(false);
      return;
    }

    const controller = new AbortController();
    const timeoutId = setTimeout(() => {
      searchSpots({ q: trimmed, page: 1, pageSize: MAX_RESULTS }, controller.signal)
        .then((page) => {
          setResults(page.items);
          setSearched(true);
        })
        .catch(() => {
          if (controller.signal.aborted) return;
          setResults([]);
          setSearched(true);
        });
    }, DEBOUNCE_MS);

    return () => {
      clearTimeout(timeoutId);
      controller.abort();
    };
  }, [term]);

  function handleSelect(spot: SpotSearchResult) {
    setOpen(false);
    setTerm(spot.name);
    onSelect(spot);
  }

  const showList = open && term.trim() !== "" && searched;

  return (
    <div className="relative w-44 max-w-[calc(100vw-2rem)]">
      <input
        type="search"
        value={term}
        onChange={(e) => {
          setTerm(e.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={(e) => {
          if (e.key === "Escape") setOpen(false);
          if (e.key === "Enter" && results[0]) {
            e.preventDefault();
            handleSelect(results[0]);
          }
        }}
        placeholder={t("mapSearchPlaceholder")}
        aria-label={t("mapSearchPlaceholder")}
        aria-controls={listId}
        aria-expanded={showList}
        role="combobox"
        aria-autocomplete="list"
        className="w-full rounded px-3 py-1.5 text-sm shadow placeholder:opacity-60"
        style={{ backgroundColor: "#f1eddc", color: "#2b2a23" }}
      />

      {showList ? (
        <ul
          id={listId}
          role="listbox"
          className="absolute top-full right-0 left-0 mt-1 overflow-hidden rounded shadow-lg"
          style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
        >
          {results.length === 0 ? (
            <li className="px-3 py-2 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {t("mapSearchNoResults")}
            </li>
          ) : (
            results.map((spot) => (
              <li key={spot.id} role="option" aria-selected="false">
                <button
                  type="button"
                  onClick={() => handleSelect(spot)}
                  className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-black/5 dark:hover:bg-white/5"
                >
                  <CategoryIcon category={spot.category} size={14} />
                  <span className="truncate">{spot.name}</span>
                </button>
              </li>
            ))
          )}
        </ul>
      ) : null}
    </div>
  );
}
