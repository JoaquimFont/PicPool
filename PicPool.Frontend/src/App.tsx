import { Routes, Route } from "react-router-dom";

import Home from "./pages/Home";
import LoginPage from "./pages/login/login";
import SignUpPage from "./pages/sign-up/sign-up";
import LoggedHomePage from "./pages/logged-home/logged-home";
import CrearSalaPage from "./pages/sala/views/crear-sala";
import SelectorSalas from "./pages/sala/views/selector-salas";
import Sala from "./pages/sala/views/sala";
import ObrirLinkSala from "./pages/sala/views/obrir-link";
import PlansPage from "./pages/plans/plans";
import ProtectedRoute from "./routes/RutaProtegida";
// import { Sala } from "./pages/sala/views/sala";

// import { Sala } from "./pages/sala/views/sala";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<SignUpPage />} />
      <Route path="/unir-sala/:token" element={<ObrirLinkSala />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/logged-home" element={<LoggedHomePage />} />
        <Route path="/crear-sala" element={<CrearSalaPage />} />
        <Route path="/selector-salas" element={<SelectorSalas />} />
        <Route path="/plans" element={<PlansPage />} />

        <Route path="/sala" element={<Sala />} />
      </Route>
    </Routes>
  );
}
