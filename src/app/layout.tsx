import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Cultivation World Simulator",
  description: "Unity 6.3 Cultivation Xianxia Simulator — Project Workspace",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ru">
      <body className="antialiased">{children}</body>
    </html>
  );
}
