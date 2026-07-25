"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function Root() {
  const router = useRouter();
  useEffect(() => {
    const t = localStorage.getItem("senkora_token");
    router.replace(t ? "/dashboard" : "/login");
  }, [router]);
  return (
    <div style={{ minHeight: "100vh", background: "#030712", display: "flex",
      alignItems: "center", justifyContent: "center" }}>
      <div style={{ width: 24, height: 24, border: "2px solid #2563eb",
        borderTopColor: "transparent", borderRadius: "50%", animation: "spin 0.8s linear infinite" }} />
      <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
}
