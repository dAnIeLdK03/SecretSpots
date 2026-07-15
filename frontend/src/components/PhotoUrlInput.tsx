"use client";

interface PhotoUrlInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
}

export function PhotoUrlInput({ label, value, onChange, required = true }: PhotoUrlInputProps) {
  return (
    <label className="flex flex-col gap-1">
      <span className="text-sm text-zinc-600 dark:text-zinc-400">{label}</span>
      <input
        type="url"
        required={required}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="rounded border border-zinc-300 px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900"
      />
    </label>
  );
}
