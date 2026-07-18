import * as React from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  acceptarLinkCompartitSalaHandler,
  obtenirInfoLinkCompartitSalaHandler,
} from "../controllers/salaController";
import "./sala.css";

type InfoLinkSala = {
  salaPK: string;
  nomSala: string;
  nomLink: string;
  rol: string;
  potVeure: boolean;
  potPujar: boolean;
  potDescarregar: boolean;
  potEliminarPropies: boolean;
  potEliminarQualsevol: boolean;
  potGestionarSala: boolean;
  jaFormaPart: boolean;
};

export default function ObrirLinkSala() {
  const { token } = useParams();
  const navigate = useNavigate();

  const usuariPK = localStorage.getItem("usuariPK");

  const [carregant, setCarregant] = React.useState(true);
  const [acceptant, setAcceptant] = React.useState(false);
  const [missatgeError, setMissatgeError] = React.useState("");
  const [info, setInfo] = React.useState<InfoLinkSala | null>(null);

  React.useEffect(() => {
    if (!token) {
      return;
    }

    if (!usuariPK) {
      navigate("/login", {
        state: {
          redirectTo: `/unir-sala/${token}`,
        },
        replace: true,
      });
      return;
    }

    let componentActiu = true;

    async function carregarInfoLink() {
      try {
        const resposta = await obtenirInfoLinkCompartitSalaHandler(
          token!,
          usuariPK,
        );

        if (!componentActiu) return;

        if (!resposta.correcte || !resposta.info) {
          setMissatgeError(
            resposta.missatge || "No s'ha pogut carregar la invitació.",
          );
          setInfo(null);
          return;
        }

        setInfo(resposta.info);
        setMissatgeError("");
      } catch (error) {
        if (!componentActiu) return;

        setMissatgeError(
          error instanceof Error
            ? error.message
            : "Error carregant la invitació.",
        );
        setInfo(null);
      } finally {
        if (componentActiu) {
          setCarregant(false);
        }
      }
    }

    void carregarInfoLink();

    return () => {
      componentActiu = false;
    };
  }, [token, usuariPK, navigate]);

  const acceptar = React.useCallback(async () => {
    if (!token || !usuariPK) return;

    try {
      setAcceptant(true);
      setMissatgeError("");

      const resposta = await acceptarLinkCompartitSalaHandler(token, usuariPK);

      if (!resposta.correcte || !resposta.salaPK) {
        setMissatgeError(
          resposta.missatge || "No s'ha pogut acceptar la invitació.",
        );
        return;
      }

      navigate("/sala", {
        state: {
          salaPk: resposta.salaPK,
          usuariPK,
        },
      });
    } catch (error) {
      setMissatgeError(
        error instanceof Error
          ? error.message
          : "Error acceptant la invitació.",
      );
    } finally {
      setAcceptant(false);
    }
  }, [token, usuariPK, navigate]);

  const rebutjar = React.useCallback(() => {
    navigate("/logged-home");
  }, [navigate]);

  if (!token) {
    return (
      <main className="sala-view-page">
        <p className="sala-view-error">El link no és vàlid.</p>
      </main>
    );
  }

  return (
    <main className="sala-view-page">
      {missatgeError && <p className="sala-view-error">{missatgeError}</p>}

      <section className="sala-view-section sala-link-open-section">
        <div className="sala-view-header">
          <div>
            <p className="sala-view-eyebrow">Invitació a sala</p>
            <h2>Vols unir-te a aquesta sala?</h2>
          </div>
        </div>

        {carregant && <p className="sala-view-loading">Carregant link...</p>}

        {!carregant && info && (
          <>
            <div className="sala-link-open-card">
              <p className="sala-link-open-label">Sala</p>

              <h3>{info.nomSala}</h3>

              <p>
                Aquest link és <strong>{info.nomLink}</strong> i t’assignarà el
                rol <strong>{info.rol}</strong>.
              </p>

              {info.jaFormaPart && (
                <p className="sala-share-success">
                  Ja formes part d’aquesta sala. Si acceptes, només es podrien
                  ampliar permisos, no reduir-los.
                </p>
              )}

              <div className="sala-link-permissions-list">
                {info.potVeure && <span>Veure</span>}
                {info.potPujar && <span>Pujar</span>}
                {info.potDescarregar && <span>Descarregar</span>}
                {info.potEliminarPropies && <span>Eliminar pròpies</span>}
                {info.potEliminarQualsevol && <span>Eliminar qualsevol</span>}
                {info.potGestionarSala && <span>Gestionar sala</span>}
              </div>
            </div>

            <div className="sala-link-open-actions">
              <button
                type="button"
                className="sala-view-upload-button"
                onClick={acceptar}
                disabled={acceptant}
              >
                {acceptant ? "Acceptant..." : "Acceptar invitació"}
              </button>

              <button
                type="button"
                className="sala-view-delete-main-button"
                onClick={rebutjar}
                disabled={acceptant}
              >
                Rebutjar
              </button>
            </div>
          </>
        )}

        {!carregant && !info && !missatgeError && (
          <p className="sala-view-empty">No s'ha pogut carregar la invitació.</p>
        )}
      </section>
    </main>
  );
}