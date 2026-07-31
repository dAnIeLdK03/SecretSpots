"use client";

import { useEffect, useRef } from "react";

// Draws faint topographic contour rings behind the hero copy — the "Field
// Map" look, in place of a flat gradient. Purely decorative, so it's inert
// for reduced-motion users (it doesn't animate) and marked aria-hidden.
export function HeroContourBackground() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const draw = () => {
      const ctx = canvas.getContext("2d");
      if (!ctx) return;
      const dpr = window.devicePixelRatio || 1;
      const w = canvas.clientWidth;
      const h = canvas.clientHeight;
      canvas.width = w * dpr;
      canvas.height = h * dpr;
      ctx.setTransform(1, 0, 0, 1, 0, 0);
      ctx.scale(dpr, dpr);
      ctx.clearRect(0, 0, w, h);
      ctx.strokeStyle = "rgba(43, 42, 35, 0.16)";
      ctx.lineWidth = 1;

      const seeds: [number, number][] = [
        [w * 0.78, h * 0.28],
        [w * 0.12, h * 0.8],
        [w * 1.02, h * 0.9],
      ];

      seeds.forEach(([sx, sy], si) => {
        for (let r = 18; r < 260; r += 22) {
          ctx.beginPath();
          for (let a = 0; a <= 360; a += 6) {
            const rad = (a * Math.PI) / 180;
            const wobble = Math.sin(rad * 3 + si + r * 0.05) * 6 + Math.cos(rad * 2 + si) * 4;
            const x = sx + Math.cos(rad) * (r + wobble);
            const y = sy + Math.sin(rad) * (r + wobble) * 0.7;
            if (a === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
          }
          ctx.stroke();
        }
      });
    };

    draw();
    window.addEventListener("resize", draw);
    return () => window.removeEventListener("resize", draw);
  }, []);

  return <canvas ref={canvasRef} aria-hidden="true" className="absolute inset-0 h-full w-full" />;
}
