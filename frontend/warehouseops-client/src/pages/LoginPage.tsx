import { useState, type FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import {
  AlertTriangle,
  LockKeyhole,
  LogIn,
  ShieldCheck,
  UserRound,
} from "lucide-react";
import { z } from "zod";
import { useAuth } from "../auth/AuthContext";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const loginSchema = z.object({
  email: z.string().trim().email("Enter a valid email address."),
  password: z.string().min(1, "Password is required."),
});

type LoginFormValues = z.infer<typeof loginSchema>;

const demoAccounts = [
  {
    role: "Admin",
    email: "admin@warehouseops.local",
    password: "Admin123!",
    description: "Full access to products, operations and change history.",
  },
  {
    role: "WarehouseStaff",
    email: "staff@warehouseops.local",
    password: "Staff123!",
    description: "Can handle stock, orders, shipments and incidents.",
  },
  {
    role: "Manager",
    email: "manager@warehouseops.local",
    password: "Manager123!",
    description: "Can view dashboard, operations and change history.",
  },
];

export default function LoginPage() {
  const navigate = useNavigate();
  const { login, isAuthenticated } = useAuth();

  const [formValues, setFormValues] = useState<LoginFormValues>({
    email: "admin@warehouseops.local",
    password: "Admin123!",
  });
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  function handleChange(field: keyof LoginFormValues, value: string) {
    setFormValues((current) => ({
      ...current,
      [field]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setErrorMessage("");

    const result = loginSchema.safeParse(formValues);

    if (!result.success) {
      const nextValidationErrors: Record<string, string> = {};

      result.error.issues.forEach((issue) => {
        const path = issue.path.join(".");

        if (path) {
          nextValidationErrors[path] = issue.message;
        }
      });

      setValidationErrors(nextValidationErrors);
      return;
    }

    setValidationErrors({});
    setIsSubmitting(true);

    try {
      await login(result.data);
      navigate("/", { replace: true });
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Could not log in."));
    } finally {
      setIsSubmitting(false);
    }
  }

  function useDemoAccount(email: string, password: string) {
    setFormValues({
      email,
      password,
    });
    setValidationErrors({});
    setErrorMessage("");
  }

  return (
    <div className="min-h-screen bg-slate-100 px-6 py-10">
      <div className="mx-auto grid max-w-6xl gap-6 lg:grid-cols-[1.1fr_0.9fr] lg:items-center">
        <section className="rounded-3xl bg-slate-950 p-8 text-white shadow-sm">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-blue-500 p-3">
              <ShieldCheck size={28} />
            </div>

            <div>
              <p className="text-sm font-medium text-blue-300">WarehouseOps</p>
              <h1 className="text-2xl font-bold">Internal logistics system</h1>
            </div>
          </div>

          <p className="mt-6 max-w-xl text-sm leading-6 text-slate-300">
            Sign in to manage products, stock, customers, orders, shipments, incidents and change history based on your role.
          </p>

          <div className="mt-8 grid gap-3">
            {demoAccounts.map((account) => (
              <button
                key={account.email}
                type="button"
                onClick={() => useDemoAccount(account.email, account.password)}
                className="rounded-2xl border border-slate-800 bg-slate-900 p-4 text-left transition hover:border-blue-500"
              >
                <p className="text-sm font-semibold text-white">{account.role}</p>
                <p className="mt-1 text-xs text-slate-400">{account.description}</p>
                <p className="mt-3 text-xs text-blue-300">{account.email}</p>
              </button>
            ))}
          </div>
        </section>

        <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="mb-6">
            <div className="mb-4 inline-flex rounded-xl bg-blue-50 p-3 text-blue-600">
              <LogIn size={24} />
            </div>

            <h2 className="text-2xl font-bold text-slate-950">Sign in</h2>
            <p className="mt-2 text-sm leading-6 text-slate-500">
              Use one of the demo accounts to test role-based access.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Email</span>
              <div className="mt-2 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2">
                <UserRound size={17} className="text-slate-400" />
                <input
                  type="email"
                  value={formValues.email}
                  onChange={(event) => handleChange("email", event.target.value)}
                  className="w-full border-0 bg-transparent text-sm text-slate-950 outline-none"
                />
              </div>
            </label>

            {validationErrors.email && (
              <p className="text-sm text-red-600">{validationErrors.email}</p>
            )}

            <label className="block">
              <span className="text-sm font-medium text-slate-700">Password</span>
              <div className="mt-2 flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2">
                <LockKeyhole size={17} className="text-slate-400" />
                <input
                  type="password"
                  value={formValues.password}
                  onChange={(event) => handleChange("password", event.target.value)}
                  className="w-full border-0 bg-transparent text-sm text-slate-950 outline-none"
                />
              </div>
            </label>

            {validationErrors.password && (
              <p className="text-sm text-red-600">{validationErrors.password}</p>
            )}

            {errorMessage && (
              <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                <div className="flex gap-2">
                  <AlertTriangle size={18} />
                  <span>{errorMessage}</span>
                </div>
              </div>
            )}

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full rounded-xl bg-blue-600 px-5 py-3 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
            >
              {isSubmitting ? "Signing in..." : "Sign in"}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}
