import { Navigate, Outlet, useLocation } from "react-router-dom";

export default function ProtectedRoute() {
  const location = useLocation();
  const usuariPK = localStorage.getItem("usuariPK");
  const demoUserPK = new URLSearchParams(location.search).get("demoUserPK");

  if (!usuariPK && !demoUserPK) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ redirectTo: location.pathname }}
      />
    );
  }

  return <Outlet />;
}
