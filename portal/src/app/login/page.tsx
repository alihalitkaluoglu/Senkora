"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { authApi } from "@/lib/api";
import { useAuthStore } from "@/stores/useAuthStore";

export default function LoginPage() {
  const router = useRouter();
  const setAuth = useAuthStore((s) => s.setAuth);
  const [email, setEmail] = useState("admin@senkora.io");
  const [password, setPassword] = useState("Admin@Senkora2024!");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(""); setLoading(true);
    try {
      const res = await authApi.login(email, password);
      const d = res.data.data!;
      setAuth(d.accessToken, { userId: d.userId, email: d.email,
        fullName: d.fullName, roles: d.roles });
      router.push("/dashboard");
    } catch (err: unknown) {
      const e = err as { response?: { data?: { errors?: string[] } } };
      setError(e?.response?.data?.errors?.[0] ?? "Giriş başarısız.");
    } finally { setLoading(false); }
  }

  return (
    <div style={{ minHeight: "100vh", background: "#030712", display: "flex",
      alignItems: "center", justifyContent: "center", padding: 16 }}>
      <style>{`
        .inp { width:100%; background:#1f2937; border:1px solid #374151; border-radius:10px;
          padding:10px 14px; font-size:14px; color:white; outline:none; }
        .inp:focus { border-color:#2563eb; box-shadow:0 0 0 3px rgba(37,99,235,0.15); }
        .btn-primary { width:100%; background:#2563eb; color:white; border:none;
          border-radius:10px; padding:11px; font-size:14px; font-weight:600;
          cursor:pointer; transition:background 0.15s; }
        .btn-primary:hover:not(:disabled) { background:#1d4ed8; }
        .btn-primary:disabled { opacity:0.6; cursor:not-allowed; }
      `}</style>

      <div style={{ width: "100%", maxWidth: 380 }}>
        {/* Logo */}
        <div style={{ textAlign: "center", marginBottom: 32 }}>
          <div style={{ display: "inline-flex", alignItems: "center", gap: 10, marginBottom: 8 }}>
            <div style={{ width: 38, height: 38, background: "#2563eb", borderRadius: 10,
              display: "flex", alignItems: "center", justifyContent: "center",
              color: "white", fontWeight: 800, fontSize: 18 }}>S</div>
            <span style={{ fontSize: 24, fontWeight: 700, color: "white" }}>Senkora</span>
          </div>
          <div style={{ color: "#6b7280", fontSize: 13 }}>Enterprise Integration Platform</div>
        </div>

        {/* Card */}
        <div style={{ background: "#111827", border: "1px solid #1f2937", borderRadius: 16,
          padding: 28, boxShadow: "0 25px 50px rgba(0,0,0,0.4)" }}>
          <h1 style={{ fontSize: 18, fontWeight: 600, marginBottom: 20, color: "white" }}>
            Giriş Yap
          </h1>
          {error && (
            <div style={{ background: "rgba(239,68,68,0.1)", border: "1px solid rgba(239,68,68,0.3)",
              borderRadius: 8, padding: "10px 14px", color: "#fca5a5", fontSize: 13, marginBottom: 16 }}>
              {error}
            </div>
          )}
          <form onSubmit={submit}>
            <div style={{ marginBottom: 14 }}>
              <label style={{ display: "block", fontSize: 12, color: "#9ca3af",
                fontWeight: 500, marginBottom: 6 }}>E-posta</label>
              <input className="inp" type="email" value={email}
                onChange={e => setEmail(e.target.value)} required />
            </div>
            <div style={{ marginBottom: 20 }}>
              <label style={{ display: "block", fontSize: 12, color: "#9ca3af",
                fontWeight: 500, marginBottom: 6 }}>Şifre</label>
              <input className="inp" type="password" value={password}
                onChange={e => setPassword(e.target.value)} required />
            </div>
            <button className="btn-primary" type="submit" disabled={loading}>
              {loading ? "Giriş yapılıyor..." : "Giriş Yap"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
