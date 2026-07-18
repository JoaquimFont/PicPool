import * as React from "react";
import type { salaLinkCompartitDTO } from "../../../api/DTOs/SalaDtos";
import {
  crearLinkCompartitSalaHandler,
  obtenirLinksCompartitsSalaHandler,
} from "../controllers/salaController";

type CrearICompartirLinkProps = {
  salaPK: string;
  usuariPK: string;
  onTancar: () => void;
};

export default function CrearICompartirLink({
  salaPK,
  usuariPK,
  onTancar,
}: CrearICompartirLinkProps) {
  const [nom, setNom] = React.useState("Poden veure i pujar imatges");
  const [rol, setRol] = React.useState("Editor");

  const [potVeure, setPotVeure] = React.useState(true);
  const [potPujar, setPotPujar] = React.useState(true);
  const [potDescarregar, setPotDescarregar] = React.useState(true);
  const [potEliminarPropies, setPotEliminarPropies] = React.useState(false);
  const [potEliminarQualsevol, setPotEliminarQualsevol] = React.useState(false);
  const [potGestionarSala, setPotGestionarSala] = React.useState(false);

  const [links, setLinks] = React.useState<salaLinkCompartitDTO[]>([]);
  const [carregant, setCarregant] = React.useState(true);
  const [creant, setCreant] = React.useState(false);
  const [missatgeError, setMissatgeError] = React.useState("");
  const [missatgeOk, setMissatgeOk] = React.useState("");

  React.useEffect(() => {
    let componentActiu = true;

    async function carregarLinks() {
      try {
        const resposta = await obtenirLinksCompartitsSalaHandler(
          salaPK,
          usuariPK,
        );

        if (!componentActiu) return;

        if (!resposta.correcte) {
          setMissatgeError(
            resposta.missatge || "No s'han pogut obtenir els links.",
          );
          setLinks([]);
          return;
        }

        setLinks(resposta.links || []);
        setMissatgeError("");
      } catch (error) {
        if (!componentActiu) return;

        setMissatgeError(
          error instanceof Error
            ? error.message
            : "Error obtenint els links compartits.",
        );
        setLinks([]);
      } finally {
        if (componentActiu) {
          setCarregant(false);
        }
      }
    }

    void carregarLinks();

    return () => {
      componentActiu = false;
    };
  }, [salaPK, usuariPK]);

  const copiarLink = React.useCallback(async (url: string) => {
    try {
      await navigator.clipboard.writeText(url);
      setMissatgeError("");
      setMissatgeOk("Link copiat al porta-retalls.");
    } catch {
      setMissatgeOk("");
      setMissatgeError("No s'ha pogut copiar el link.");
    }
  }, []);

  const crearLink = React.useCallback(async () => {
    if (!nom.trim()) {
      setMissatgeOk("");
      setMissatgeError("El nom del link és obligatori.");
      return;
    }

    try {
      setCreant(true);
      setMissatgeError("");
      setMissatgeOk("");

      const resposta = await crearLinkCompartitSalaHandler(
        salaPK,
        usuariPK,
        nom,
        rol,
        potVeure,
        potPujar,
        potDescarregar,
        potEliminarPropies,
        potEliminarQualsevol,
        potGestionarSala,
      );

      if (!resposta.correcte || !resposta.link) {
        setMissatgeError(resposta.missatge || "No s'ha pogut crear el link.");
        return;
      }

      setLinks((linksActuals) => [resposta.link!, ...linksActuals]);

      if (resposta.link.url) {
        await navigator.clipboard.writeText(resposta.link.url);
        setMissatgeOk("Link creat i copiat al porta-retalls.");
      } else {
        setMissatgeOk("Link creat correctament.");
      }
    } catch (error) {
      setMissatgeError(
        error instanceof Error ? error.message : "Error creant el link.",
      );
    } finally {
      setCreant(false);
    }
  }, [
    salaPK,
    usuariPK,
    nom,
    rol,
    potVeure,
    potPujar,
    potDescarregar,
    potEliminarPropies,
    potEliminarQualsevol,
    potGestionarSala,
  ]);

  return (
    <div className="sala-share-overlay">
      <section className="sala-share-modal">
        <div className="sala-share-header">
          <div>
            <p className="sala-view-eyebrow">Compartir sala</p>
            <h2>Crear i compartir link</h2>
          </div>

          <button
            type="button"
            className="sala-share-close"
            onClick={onTancar}
            aria-label="Tancar"
          >
            ✕
          </button>
        </div>

        {missatgeError && <p className="sala-view-error">{missatgeError}</p>}
        {missatgeOk && <p className="sala-share-success">{missatgeOk}</p>}

        <div className="sala-share-grid">
          <div className="sala-share-form">
            <label>
              Nom del link
              <input
                value={nom}
                onChange={(event) => setNom(event.target.value)}
                disabled={creant}
                placeholder="Ex: Familiars que poden veure"
              />
            </label>

            <label>
              Rol
              <select
                value={rol}
                onChange={(event) => setRol(event.target.value)}
                disabled={creant}
              >
                <option value="Lector">Lector</option>
                <option value="Editor">Editor</option>
                <option value="Gestor">Gestor</option>
              </select>
            </label>

            <div className="sala-share-permissions">
              <label>
                <input
                  type="checkbox"
                  checked={potVeure}
                  onChange={(event) => setPotVeure(event.target.checked)}
                  disabled={creant}
                />
                Pot veure
              </label>

              <label>
                <input
                  type="checkbox"
                  checked={potPujar}
                  onChange={(event) => setPotPujar(event.target.checked)}
                  disabled={creant}
                />
                Pot pujar imatges
              </label>

              <label>
                <input
                  type="checkbox"
                  checked={potDescarregar}
                  onChange={(event) =>
                    setPotDescarregar(event.target.checked)
                  }
                  disabled={creant}
                />
                Pot descarregar
              </label>

              <label>
                <input
                  type="checkbox"
                  checked={potEliminarPropies}
                  onChange={(event) =>
                    setPotEliminarPropies(event.target.checked)
                  }
                  disabled={creant}
                />
                Pot eliminar pròpies
              </label>

              <label>
                <input
                  type="checkbox"
                  checked={potEliminarQualsevol}
                  onChange={(event) =>
                    setPotEliminarQualsevol(event.target.checked)
                  }
                  disabled={creant}
                />
                Pot eliminar qualsevol
              </label>

              <label>
                <input
                  type="checkbox"
                  checked={potGestionarSala}
                  onChange={(event) =>
                    setPotGestionarSala(event.target.checked)
                  }
                  disabled={creant}
                />
                Pot gestionar sala
              </label>
            </div>

            <button
              type="button"
              className="sala-view-upload-button"
              onClick={crearLink}
              disabled={creant}
            >
              {creant ? "Creant..." : "Crear link"}
            </button>
          </div>

          <div className="sala-share-links">
            <h3>Links existents</h3>

            {carregant && <p className="sala-view-loading">Carregant...</p>}

            {!carregant && links.length === 0 && (
              <p className="sala-view-empty">Encara no hi ha links creats.</p>
            )}

            {!carregant &&
              links.map((link) => (
                <article
                  key={link.salaLinkCompartitPK}
                  className={
                    link.actiu
                      ? "sala-share-link-card"
                      : "sala-share-link-card disabled"
                  }
                >
                  <div>
                    <h4>{link.nom}</h4>
                    <p>
                      {link.rol} · {link.usosActuals}
                      {link.limitUsos ? `/${link.limitUsos}` : ""} usos
                    </p>
                  </div>

                  <input value={link.url} readOnly />

                  <button
                    type="button"
                    className="sala-view-select-button"
                    onClick={() => copiarLink(link.url)}
                  >
                    Copiar
                  </button>
                </article>
              ))}
          </div>
        </div>
      </section>
    </div>
  );
}