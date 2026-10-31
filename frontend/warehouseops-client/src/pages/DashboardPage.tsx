import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getDashboardSummary } from "../api/dashboardApi";
import { getApiErrorMessage } from "../utils/getApiErrorMessage";

function formatDateTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Date unavailable";
  return date.toLocaleString("sv-SE", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

function formatShortId(id: string) {
  return `#${id.slice(0, 8).toUpperCase()}`;
}

function isAttentionStatus(status: string) {
  return ["delayed", "cancelled"].includes(status.toLowerCase());
}

export default function DashboardPage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: getDashboardSummary,
  });

  const metrics = [
    {
      label: "Active orders",
      value: data?.activeOrdersCount,
      detail: "Orders currently in process",
      attention: false,
    },
    {
      label: "Low stock",
      value: data?.lowStockItemsCount,
      detail: "Items requiring attention",
      attention: true,
    },
    {
      label: "Open incidents",
      value: data?.openIncidentsCount,
      detail: "Unresolved operational issues",
      attention: true,
    },
    {
      label: "Active shipments",
      value: data?.activeShipmentsCount,
      detail: "Deliveries currently in progress",
      attention: false,
    },
  ];

  const statusCounts = data?.orderStatusCounts ?? [];
  const statusTotal = statusCounts.reduce((total, item) => total + item.count, 0);
  const activities = data?.recentActivities ?? [];
  const shipments = data?.recentShipments ?? [];

  return (
    <div className="ops-page">
      <section className="ops-page__masthead">
        <div>
          <p className="ops-eyebrow ops-page__kicker">01 / Operations overview</p>
          <h1 className="ops-display ops-page__title">Warehouse operations.</h1>
          <p className="ops-page__description">
            An overview of order flow, stock levels, shipments and recorded incidents.
            Figures are retrieved from the WarehouseOps API.
          </p>
        </div>
        <div className="ops-page__aside">
          <p className="ops-page__aside-title">Data source</p>
          <span className="ops-page__aside-text">
            {isLoading
              ? "Retrieving information"
              : isError
                ? "Connection unavailable"
                : "WarehouseOps database"}
          </span>
        </div>
      </section>

      {isError && (
        <div className="ops-notice" role="alert">
          {getApiErrorMessage(error, "Could not load dashboard data. Check the API connection.")}
        </div>
      )}

      <section className="ops-overview" aria-labelledby="operations-today">
        <div className="ops-section-head">
          <h2 className="ops-section-head__title" id="operations-today">
            Current position
          </h2>
          <span className="ops-section-head__aux">Operational indicators / 04</span>
        </div>

        <div className="ops-metrics">
          {metrics.map((metric) => (
            <article className="ops-metric" key={metric.label}>
              <h3 className="ops-metric__label">{metric.label}</h3>
              {isLoading ? (
                <div className="ops-skeleton ops-skeleton--number" aria-label="Loading" />
              ) : (
                <p
                  className={`ops-metric__value${
                    metric.attention && (metric.value ?? 0) > 0
                      ? " ops-metric__value--attention"
                      : ""
                  }`}
                >
                  {isError || metric.value === undefined ? "—" : metric.value}
                </p>
              )}
              <p className="ops-metric__foot">{metric.detail}</p>
            </article>
          ))}
        </div>
      </section>

      <div className="ops-dataviews">
        <section className="ops-panel" aria-labelledby="order-distribution">
          <div className="ops-panel__header">
            <h2 className="ops-panel__title" id="order-distribution">
              Order distribution
            </h2>
            <span className="ops-panel__number">02 / Orders</span>
          </div>

          {isLoading ? (
            <div aria-label="Loading order distribution">
              {[0, 1, 2].map((index) => (
                <div key={index} className="ops-skeleton ops-skeleton--wide" />
              ))}
            </div>
          ) : statusCounts.length === 0 || statusTotal === 0 ? (
            <p className="ops-empty">
              {isError ? "Order distribution unavailable." : "No order records available."}
            </p>
          ) : (
            <div className="ops-status-list">
              {statusCounts.map(({ status, count }) => {
                const percent = Math.max(0, Math.min(100, (count / statusTotal) * 100));
                return (
                  <div className="ops-status" key={status}>
                    <div className="ops-status__labelrow">
                      <span className="ops-status__name">{status}</span>
                      <span className="ops-status__number">
                        {count} <span className="ops-section-head__aux">/ {statusTotal}</span>
                      </span>
                    </div>
                    <div
                      className="ops-status__track"
                      role="meter"
                      aria-label={`${status} share of orders`}
                      aria-valuemin={0}
                      aria-valuemax={100}
                      aria-valuenow={Math.round(percent)}
                    >
                      <div className="ops-status__fill" style={{ width: `${percent}%` }} />
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </section>

        <section className="ops-panel" aria-labelledby="recent-activity">
          <div className="ops-panel__header">
            <h2 className="ops-panel__title" id="recent-activity">
              Recent activity
            </h2>
            <span className="ops-panel__number">03 / History</span>
          </div>

          {isLoading ? (
            <div aria-label="Loading recent activity">
              {[0, 1, 2].map((index) => (
                <div key={index} className="ops-skeleton ops-skeleton--wide" />
              ))}
            </div>
          ) : activities.length === 0 ? (
            <p className="ops-empty">
              {isError ? "Activity records unavailable." : "No changes recorded yet."}
            </p>
          ) : (
            <ol className="ops-activity">
              {activities.map((activity, index) => (
                <li className="ops-activity__item" key={`${activity.title}-${activity.performedAt}-${index}`}>
                  <p className="ops-activity__title">{activity.title}</p>
                  <p className="ops-activity__description">{activity.description}</p>
                  <div className="ops-activity__meta">
                    <span>{formatDateTime(activity.performedAt)}</span>
                    <span>{activity.performedBy || "User unavailable"}</span>
                  </div>
                </li>
              ))}
            </ol>
          )}
        </section>
      </div>

      <section className="ops-table-section" aria-labelledby="recent-shipments">
        <div className="ops-table-section__head">
          <h2 className="ops-panel__title" id="recent-shipments">
            Recent shipments
          </h2>
          <Link className="ops-section-head__aux" to="/shipments">
            View all shipments
          </Link>
        </div>

        {isLoading ? (
          <div className="ops-panel" aria-label="Loading recent shipments">
            {[0, 1, 2].map((index) => (
              <div key={index} className="ops-skeleton ops-skeleton--wide" />
            ))}
          </div>
        ) : shipments.length === 0 ? (
          <p className="ops-empty" style={{ padding: "22px 28px" }}>
            {isError ? "Shipments unavailable." : "No shipments have been recorded."}
          </p>
        ) : (
          <div className="ops-data-scroll">
            <table className="ops-table">
              <thead>
                <tr>
                  <th scope="col">Shipment ID</th>
                  <th scope="col">Customer</th>
                  <th scope="col">Status</th>
                  <th scope="col">Tracking</th>
                  <th scope="col">Created</th>
                </tr>
              </thead>
              <tbody>
                {shipments.map((shipment) => (
                  <tr key={shipment.id}>
                    <td className="ops-table__strong ops-table__id">{formatShortId(shipment.id)}</td>
                    <td>{shipment.customerName || "Not specified"}</td>
                    <td>
                      <span className={`ops-status-tag${isAttentionStatus(shipment.status) ? " ops-status-tag--warning" : ""}`}>
                        {shipment.status}
                      </span>
                    </td>
                    <td className="ops-table__id">{shipment.trackingNumber || "Not assigned"}</td>
                    <td>{formatDateTime(shipment.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
