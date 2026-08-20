import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import type { plaDto } from "../../api/DTOs/PlaDtos";
import {
  obtenirPlansUsuariHandler,
  seleccionarPlaUsuariHandler,
} from "../../controllers/PlaController";
import "./plans.css";

interface LocationState {
  usuariPK?: string;
}

function formatBytes(bytes: number) {
  const gb = bytes / 1024 / 1024 / 1024;
  return `${gb.toLocaleString("ca-ES", { maximumFractionDigits: 0 })} GB`;
}

function formatLimitImatges(limitImatges: number) {
  return limitImatges >= 2147483647 ? "Sense limit" : limitImatges.toString();
}

function formatPreu(preu: number) {
  if (preu === 0) {
    return "Gratis";
  }

  return new Intl.NumberFormat("ca-ES", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 0,
  }).format(preu);
}

export default function PlansPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as LocationState | null;
  const searchParams = useMemo(
    () => new URLSearchParams(location.search),
    [location.search],
  );
  const demoUserPK = searchParams.get("demoUserPK");
  const usuariPK =
    demoUserPK ?? state?.usuariPK ?? localStorage.getItem("usuariPK") ?? "";
  const demoQuery = demoUserPK
    ? `?demoUserPK=${encodeURIComponent(demoUserPK)}`
    : "";

  const [plans, setPlans] = useState<plaDto[]>([]);
  const [plaActualPK, setPlaActualPK] = useState<string | undefined>();
  const [carregant, setCarregant] = useState(true);
  const [plaSeleccionantPK, setPlaSeleccionantPK] = useState<string | null>(
    null,
  );
  const [missatge, setMissatge] = useState<string>("");
  const [error, setError] = useState<string>("");

  useEffect(() => {
    let mounted = true;

    async function carregarPlans() {
      if (!usuariPK) {
        setError("No s'ha trobat l'usuari actual.");
        setCarregant(false);
        return;
      }

      setCarregant(true);
      setError("");

      const resposta = await obtenirPlansUsuariHandler(usuariPK);

      if (!mounted) {
        return;
      }

      if (!resposta.correcte) {
        setError(resposta.missatge || "No s'han pogut carregar els plans.");
        setPlans([]);
        setCarregant(false);
        return;
      }

      setPlans(resposta.plans ?? []);
      setPlaActualPK(resposta.plaActualPK);
      setCarregant(false);
    }

    carregarPlans();

    return () => {
      mounted = false;
    };
  }, [usuariPK]);

  async function handleSeleccionarPla(plaPK: string) {
    if (!usuariPK || plaPK === plaActualPK) {
      return;
    }

    setPlaSeleccionantPK(plaPK);
    setMissatge("");
    setError("");

    const resposta = await seleccionarPlaUsuariHandler(usuariPK, plaPK);

    if (!resposta.correcte || !resposta.plaActualPK) {
      setError(resposta.missatge || "No s'ha pogut seleccionar el pla.");
      setPlaSeleccionantPK(null);
      return;
    }

    setPlaActualPK(resposta.plaActualPK);
    setMissatge(resposta.missatge || "Pla seleccionat correctament.");
    setPlaSeleccionantPK(null);
  }

  function handleTornar() {
    navigate(`/logged-home${demoQuery}`, {
      state: {
        usuariPK,
      },
    });
  }

  return (
    <main className="plans-page">
      <section className="plans-shell">
        <header className="plans-header">
          <button className="plans-back-button" type="button" onClick={handleTornar}>
            Tornar
          </button>
          <div>
            <h1>Revisar pla</h1>
            <p>Selecciona el pla actiu del teu compte.</p>
          </div>
        </header>

        {carregant && <p className="plans-status">Carregant plans...</p>}
        {error && <p className="plans-error">{error}</p>}
        {missatge && <p className="plans-success">{missatge}</p>}

        <div className="plans-grid">
          {plans.map((pla) => {
            const esActual = pla.plaPK === plaActualPK;
            const seleccionant = plaSeleccionantPK === pla.plaPK;

            return (
              <article
                className={`plan-card${esActual ? " current-plan" : ""}`}
                key={pla.plaPK}
              >
                <div className="plan-card-header">
                  <h2>{pla.nom}</h2>
                  {esActual && <span>Pla actual</span>}
                </div>

                <p className="plan-price">{formatPreu(pla.preu)}</p>

                <dl className="plan-limits">
                  <div>
                    <dt>Sales</dt>
                    <dd>{pla.limitSales}</dd>
                  </div>
                  <div>
                    <dt>Espai</dt>
                    <dd>{formatBytes(pla.limitEmmagatzematgeBytes)}</dd>
                  </div>
                  <div>
                    <dt>Imatges</dt>
                    <dd>{formatLimitImatges(pla.limitImatges)}</dd>
                  </div>
                </dl>

                <button
                  className="plan-select-button"
                  disabled={esActual || seleccionant || plaSeleccionantPK !== null}
                  type="button"
                  onClick={() => handleSeleccionarPla(pla.plaPK)}
                >
                  {esActual
                    ? "Seleccionat"
                    : seleccionant
                      ? "Seleccionant..."
                      : "Seleccionar"}
                </button>
              </article>
            );
          })}
        </div>
      </section>
    </main>
  );
}
