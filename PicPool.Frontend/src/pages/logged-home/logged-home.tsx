// import React from "react";
import { useLocation, useNavigate } from "react-router-dom";
import "./logged-home.css";

interface LocationState {
  usuariPK: string;
}

export default function LoggedHomePage() {
  const navigate = useNavigate();
  const location = useLocation();

  const state = location.state as LocationState | null;
  const usuariPK = state?.usuariPK;

  function handleCrearSalaClick() {
    navigate("/crear-sala", {
      state: {
        usuariPK,
      },
    });
  }

  function handleEntrarSalaClick() {
    navigate("/selector-salas", {
      state: {
        usuariPK,
      },
    });
  }

  return (
    <main className="logged-home-page">
      <section className="logged-home-card">
        <div className="logged-home-header">
          <h1>PicPool</h1>
          <p>Gestiona les teves sales i comparteix imatges en alta qualitat.</p>
        </div>

        <div className="logged-home-actions">
          <button
            className="logged-action primary-action"
            onClick={handleCrearSalaClick}
          >
            Crear sala
          </button>

          <button
            className="logged-action secondary-action"
            onClick={handleEntrarSalaClick}
          >
            Les meves sales
          </button>

          <button className="logged-action ghost-action">
            Revisar pla
          </button>
        </div>
      </section>
    </main>
  );
}