import React, { useState } from "react";
import "./crear-sala.css";
import { crearSalaHandler } from "../../../controllers/HomePageController";
import { useNavigate } from "react-router";
import type { resumPlaUsuariDto } from "../../../api/DTOs/UserDtos";

function llegirResumPlaUsuari(): resumPlaUsuariDto | null {
  const plaGuardat = localStorage.getItem("usuariPla");

  if (!plaGuardat) {
    return null;
  }

  try {
    return JSON.parse(plaGuardat) as resumPlaUsuariDto;
  } catch {
    localStorage.removeItem("usuariPla");
    return null;
  }
}

export default function CrearSalaPage() {
  const [nomSala, setNomSala] = useState("");
  const searchParams = new URLSearchParams(window.location.search);
  const demoUserPK = searchParams.get("demoUserPK");
  const usuariPk = demoUserPK ?? localStorage.getItem("usuariPK");
  const [missatgeError, setMissatgeError] = useState("");
  const [carregant, setCarregant] = useState(false);
  const [plaUsuari, setPlaUsuari] = useState<resumPlaUsuariDto | null>(() =>
    demoUserPK ? null : llegirResumPlaUsuari(),
  );
  const navigate = useNavigate();
  const plaBloquejaCrearSala = plaUsuari?.potCrearSala === false;

  async function crearSala(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setMissatgeError("");

    if (plaBloquejaCrearSala && plaUsuari) {
      setMissatgeError(
        `Has arribat al límit de ${plaUsuari.limitSales} sala(es) del teu pla.`,
      );
      return;
    }

    if (!usuariPk) {
      setMissatgeError("No s'ha trobat l'usuari actual.");
      return;
    }

    if (!nomSala.trim()) {
      setMissatgeError("Introdueix el nom de la sala.");
      return;
    }

    setCarregant(true);

    const resposta = await crearSalaHandler(nomSala, usuariPk ? usuariPk : "");

    setCarregant(false);

    console.log("Sala creada correctament", resposta);
    
    const salaPk = resposta.sala?.salaPK;

    if (!resposta.correcte || !salaPk) {
      setMissatgeError(resposta.missatge || "No s'ha pogut crear la sala.");
      return;
    }

    if (resposta.pla) {
      setPlaUsuari(resposta.pla);
    }

    const demoQuery =
      demoUserPK && salaPk
        ? `?demoUserPK=${encodeURIComponent(demoUserPK)}&demoSalaPK=${encodeURIComponent(salaPk)}`
        : "";

    navigate(`/sala${demoQuery}`, {
      state: {
        salaPk,
        usuariPK: usuariPk,
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

          {plaUsuari && (
            <p className="crear-sala-plan-status">
              Sales creades: {plaUsuari.salesCreades}/{plaUsuari.limitSales}
            </p>
          )}

          {missatgeError && <p className="crear-sala-error">{missatgeError}</p>}

          <button type="submit" disabled={carregant || plaBloquejaCrearSala}>
            {carregant ? "Creant sala..." : "Crear sala"}
          </button>
        </form>

        <a
          href={
            demoUserPK
              ? `/logged-home?demoUserPK=${encodeURIComponent(demoUserPK)}`
              : "/logged-home"
          }
          className="crear-sala-back"
        >
          Tornar a l'inici
        </a>
      </section>
    </main>
  );
}
