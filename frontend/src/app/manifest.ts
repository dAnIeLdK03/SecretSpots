import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "SecretSpots",
    short_name: "SecretSpots",
    description: "Локален гид за скрити съкровища",
    start_url: "/",
    display: "standalone",
    background_color: "#ded9c6",
    theme_color: "#2b2a23",
    icons: [
      {
        src: "/favicon.ico",
        sizes: "any",
        type: "image/x-icon",
      },
      {
        src: "/icons/icon-192.png",
        sizes: "192x192",
        type: "image/png",
      },
      {
        src: "/icons/icon-512.png",
        sizes: "512x512",
        type: "image/png",
      },
    ],
  };
}
