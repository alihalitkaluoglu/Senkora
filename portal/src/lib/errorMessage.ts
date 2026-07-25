/**
 * API hatalarindan okunabilir mesaj cikarir.
 * Hem ApiResponse formatini hem ProblemDetails formatini destekler.
 */
export function apiError(err: unknown, fallback = "Islem basarisiz."): string {
  const e = err as {
    response?: {
      status?: number;
      data?: {
        errors?: string[] | Record<string, string[]>;
        message?: string;
        title?: string;
        detail?: string;
      };
    };
    message?: string;
  };

  const d = e?.response?.data;

  // ApiResponse.Fail → errors: ["mesaj"]
  if (Array.isArray(d?.errors) && d.errors.length > 0) {
    return String(d.errors[0]);
  }

  // ValidationProblemDetails → errors: { field: ["msg"] }
  if (d?.errors && !Array.isArray(d.errors)) {
    const first = Object.values(d.errors)[0];
    if (Array.isArray(first) && first.length > 0) return String(first[0]);
  }

  if (d?.message) return d.message;
  if (d?.title && d.title !== "An error occurred.") return d.title;
  if (d?.detail) return String(d.detail).split("\n")[0];

  if (e?.response?.status === 401) return "Oturum suresi doldu. Tekrar giris yapin.";
  if (e?.response?.status === 403) return "Bu islem icin yetkiniz yok.";
  if (e?.response?.status === 404) return "Kayit bulunamadi.";

  if (e?.message) return e.message;
  return fallback;
}
