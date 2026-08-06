interface AvatarProps {
  name: string;
  size?: number;
}

export function Avatar({ name, size = 24 }: AvatarProps) {
  const initial = name.trim().charAt(0).toUpperCase() || "?";

  return (
    <span
      className="inline-flex flex-shrink-0 items-center justify-center rounded-full font-semibold"
      style={{
        width: size,
        height: size,
        fontSize: size * 0.5,
        backgroundColor: "var(--fieldmap-trail)",
        color: "var(--fieldmap-paper-light)",
      }}
    >
      {initial}
    </span>
  );
}
