import { useState, type FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { z } from "zod";
import { useAuth } from "../auth/AuthContext";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

const loginSchema = z.object({
  email: z.string().trim().email("Enter a valid email address."),
  password: z.string().min(1, "Password is required."),
});

type LoginFormValues = z.infer<typeof loginSchema>;

const demoAccounts = [
  { role: "Admin", email: "admin@warehouseops.local" },
  { role: "Warehouse staff", email: "staff@warehouseops.local" },
  { role: "Manager", email: "manager@warehouseops.local" },
];

export default function LoginPage() {
  const navigate = useNavigate();
  const { login, isAuthenticated } = useAuth();

  const [formValues, setFormValues] = useState<LoginFormValues>({
    email: "admin@warehouseops.local",
    password: "",
  });
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  function handleChange(field: keyof LoginFormValues, value: string) {
    setFormValues((current) => ({ ...current, [field]: value }));
  }

  function selectDemoAccount(email: string) {
    setFormValues({ email, password: "" });
    setValidationErrors({});
    setErrorMessage("");
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage("");

    const result = loginSchema.safeParse(formValues);
    if (!result.success) {
      const nextErrors: Record<string, string> = {};
      result.error.issues.forEach((issue) => {
        const field = issue.path.join(".");
        if (field) nextErrors[field] = issue.message;
      });
      setValidationErrors(nextErrors);
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

  return (
    <div className="ops-login">
      <section className="ops-login__editorial" aria-label="WarehouseOps introduction">
        <div className="ops-login__identity">
          <p className="ops-eyebrow" style={{ color: "#B3C3B8" }}>
            Internal / Logistics
          </p>
          <p className="ops-display ops-login__identity-title">WarehouseOps</p>
        </div>

        <div className="ops-login__content">
          <p className="ops-eyebrow" style={{ color: "#B3C3B8" }}>
            Operations management platform
          </p>
          <h1 className="ops-display ops-login__headline">
            Every operation,<br />in its place.
          </h1>
          <p className="ops-login__lead">
            A single workspace for inventory, orders, customer records,
            shipments and operational incidents.
          </p>
        </div>

        <div className="ops-login__footer">
          <span>WAREHOUSE / OPERATIONS</span>
          <span>INTERNAL SYSTEM</span>
        </div>
      </section>

      <main className="ops-login__formside">
        <div className="ops-login__formwrap">
          <p className="ops-eyebrow" style={{ color: "#62796F" }}>
            Account access / 01
          </p>
          <h2 className="ops-display ops-login__title">Sign in.</h2>
          <p className="ops-login__subtitle">
            Enter your workspace credentials to continue.
          </p>

          {errorMessage && (
            <div className="ops-notice" role="alert">{errorMessage}</div>
          )}

          <form className="ops-form" onSubmit={handleSubmit} noValidate>
            <div>
              <label className="ops-form__label" htmlFor="login-email">Email address</label>
              <input
                id="login-email"
                type="email"
                className="ops-form__input"
                autoComplete="username"
                value={formValues.email}
                aria-invalid={Boolean(validationErrors.email)}
                aria-describedby={validationErrors.email ? "login-email-error" : undefined}
                onChange={(event) => handleChange("email", event.target.value)}
              />
              {validationErrors.email && (
                <p className="ops-form__error" id="login-email-error">
                  {validationErrors.email}
                </p>
              )}
            </div>

            <div>
              <label className="ops-form__label" htmlFor="login-password">Password</label>
              <input
                id="login-password"
                type="password"
                className="ops-form__input"
                autoComplete="current-password"
                value={formValues.password}
                aria-invalid={Boolean(validationErrors.password)}
                aria-describedby={validationErrors.password ? "login-password-error" : undefined}
                onChange={(event) => handleChange("password", event.target.value)}
              />
              {validationErrors.password && (
                <p className="ops-form__error" id="login-password-error">
                  {validationErrors.password}
                </p>
              )}
            </div>

            <button className="ops-form__submit" type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Signing in..." : "Sign in to workspace"}
            </button>
          </form>

          <section className="ops-demo" aria-labelledby="demo-account-heading">
            <h3 className="ops-demo__title" id="demo-account-heading">
              Local demonstration accounts
            </h3>
            <p>Passwords are configured locally and are not included in the frontend.</p>
            {demoAccounts.map((account) => (
              <button
                className="ops-demo__row"
                type="button"
                key={account.email}
                onClick={() => selectDemoAccount(account.email)}
                aria-label={`Fill in demo email for ${account.role}`}
              >
                <span className="ops-demo__role">{account.role}</span>
                <span className="ops-demo__email">{account.email}</span>
              </button>
            ))}
          </section>
        </div>
      </main>
    </div>
  );
}
