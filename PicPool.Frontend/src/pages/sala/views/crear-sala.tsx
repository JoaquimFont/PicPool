import React, { useState } from "react";
import "./crear-sala.css";
import { crearSalaHandler } from "../../../controllers/HomePageController";
import { useNavigate } from "react-router";

export default function CrearSalaPage() {
  const [nomSala, setNomSala] = useState("");
  const usuariPk = localStorage.getItem("usuariPK");
  const [missatgeError, setMissatgeError] = useState("");
  const [carregant, setCarregant] = useState(false);
   const navigate = useNavigate();

  async function crearSala(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setMissatgeError("");

    if (!nomSala.trim()) {
      setMissatgeError("Introdueix el nom de la sala.");
      return;
    }

    setCarregant(true);

    const resposta = await crearSalaHandler(nomSala, usuariPk ? usuariPk : "");

    setCarregant(false);

    console.log("Sala creada correctament", resposta);
    
    navigate("/sala", {
      state: {
        salaPk: resposta.sala?.salaPK,
      },
    });
  }

  return (
    <main className="crear-sala-page">
      <section className="crear-sala-card">
        <div className="crear-sala-header">
          <h1>Nova sala</h1>
          <p>Crea una sala privada per compartir fotos amb altres usuaris.</p>
        </div>

        <form className="crear-sala-form" onSubmit={crearSala}>
          <label>
            Nom de la sala
            <input
              type="text"
              placeholder="Ex: Viatge als Alps"
              value={nomSala}
              onChange={(event) => setNomSala(event.target.value)}
              disabled={carregant}
            />
          </label>

          {missatgeError && <p className="crear-sala-error">{missatgeError}</p>}

          <button type="submit" disabled={carregant}>
            {carregant ? "Creant sala..." : "Crear sala"}
          </button>
        </form>

        <a href="/logged-home" className="crear-sala-back">
          Tornar a l'inici
        </a>
      </section>
    </main>
  );
}
