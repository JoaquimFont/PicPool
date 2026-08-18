import * as React from "react";
import "./selector-salas.css";
import { InfoSala } from "../../../components/info-sala";
import { eliminarSalesUsuariHandler, usuariController } from "../../../controllers/HomePageController";
import type { Sala } from "../../../types/salaObjects";
import { useNavigate } from "react-router-dom";

export default function SelectorSalas() {
  const [missatgeSeleccio, setMissatgeSeleccio] = React.useState("");
  const [missatgeError, setMissatgeError] = React.useState("");
  const [carregant, setCarregant] = React.useState(true);
  const [sales, setSales] = React.useState<Sala[]>([]);
  const [pagina, setPagina] = React.useState(1);
  const [quantitat, setQuantitat] = React.useState(5);
  const [ordre, setOrdre] = React.useState<"data" | "nom">("data");
  const [descendent, setDescendent] = React.useState(false);
  const [refreshKey, setRefreshKey] = React.useState(0);
  const [modeSeleccio, setModeSeleccio] = React.useState(false);
  const [salesSeleccionades, setSalesSeleccionades] = React.useState<
    Set<string>
  >(new Set());

  const navigate = useNavigate();
  const searchParams = new URLSearchParams(window.location.search);
  const demoUserPK = searchParams.get("demoUserPK");
  const usuariPk = demoUserPK ?? localStorage.getItem("usuariPK");

  const obtenirSalesUsuari = React.useCallback(async () => {
    if (!usuariPk) {
      return {
        correcte: false,
        missatge: "No s'ha trobat l'usuari loguejat.",
        salas: [],
      };
    }

    return await usuariController.obtenirSalesUsuariHandler(
      usuariPk,
      pagina,
      quantitat,
      ordre,
      descendent,
    );
  }, [usuariPk, pagina, quantitat, ordre, descendent]);

  React.useEffect(() => {
    let componentActiu = true;

    async function carregarSalesInicials() {
      try {
        setCarregant(true);

        const resposta = await obtenirSalesUsuari();

        if (!componentActiu) {
          return;
        }

        if (resposta?.correcte) {
          setSales(resposta.salas || []);
          setMissatgeError("");
        } else {
          setSales([]);
          setMissatgeError(
            resposta?.missatge || "Error obtenint les sales de l'usuari",
          );
        }
      } catch (error) {
        if (componentActiu) {
          setSales([]);
          setMissatgeError(
            error instanceof Error
              ? error.message
              : "Error obtenint les sales de l'usuari",
          );
        }
      } finally {
        if (componentActiu) {
          setCarregant(false);
        }
      }
    }

    void carregarSalesInicials();

    return () => {
      componentActiu = false;
    };
  }, [obtenirSalesUsuari, refreshKey]);

  const entrarSala = React.useCallback(
    (salaPk: string) => {
      const demoQuery = demoUserPK
        ? `?demoUserPK=${encodeURIComponent(demoUserPK)}&demoSalaPK=${encodeURIComponent(salaPk)}`
        : "";

      navigate(`/sala${demoQuery}`, {
        state: {
          salaPk,
          usuariPK: usuariPk,
        },
      });
    },
    [demoUserPK, navigate, usuariPk],
  );

  const onClickSala = React.useCallback(
    (salaPk: string) => {
      if (modeSeleccio) {
        setMissatgeSeleccio("");

        setSalesSeleccionades((prev) => {
          const newSet = new Set(prev);

          if (newSet.has(salaPk)) {
            newSet.delete(salaPk);
          } else {
            newSet.add(salaPk);
          }

          return newSet;
        });

        return;
      }

      entrarSala(salaPk);
    },
    [entrarSala, modeSeleccio],
  );

  const toggleModeSeleccio = React.useCallback(() => {
    setModeSeleccio((valorActual) => {
      const nouValor = !valorActual;

      if (!nouValor) {
        setSalesSeleccionades(new Set());
        setMissatgeSeleccio("");
      }

      return nouValor;
    });
  }, []);

  const canviarDireccio = React.useCallback(() => {
    setDescendent((valorActual) => !valorActual);
    setPagina(1);
  }, []);

  const eliminarSalesSeleccionades = React.useCallback(async () => {
    if (salesSeleccionades.size === 0) {
      setModeSeleccio(true);
      setMissatgeSeleccio("Selecciona les sales que vols eliminar.");
      return;
    }

    const salaPks = Array.from(salesSeleccionades);

    try {
      setMissatgeError("");

      const resposta = await eliminarSalesUsuariHandler(
        usuariPk!,
        salaPks,
      );

      if (resposta === false) {
        setMissatgeError("Error eliminant les sales.");
        return;
      }

      setSalesSeleccionades(new Set());
      setModeSeleccio(false);
      setMissatgeSeleccio("");
      setCarregant(true);
      setRefreshKey((valorActual) => valorActual + 1);
    } catch (error) {
      setMissatgeError(
        error instanceof Error ? error.message : "Error eliminant les sales.",
      );
    }
  }, [salesSeleccionades, usuariPk]);

  const renderitzarSales = React.useCallback(() => {
    if (carregant) {
      return <p className="entrar-sala-loading">Carregant sales...</p>;
    }

    if (sales.length === 0) {
      return <p className="entrar-sala-empty">Encara no tens cap sala.</p>;
    }

    return (
      <div className="sales-panel">
        <div className="sales-grid">
          {sales.map((sala) => {
            const seleccionada = salesSeleccionades.has(sala.salaPK);

            return (
              <InfoSala
                key={sala.salaPK}
                salaPk={sala.salaPK}
                nom={sala.nom}
                onClick={onClickSala}
                creador={sala.usuariCreador.nom}
                dataCreacio={new Date(sala.dataCreacio).toLocaleDateString()}
                dataExpiracio={
                  sala.dataExpiracio
                    ? new Date(sala.dataExpiracio).toLocaleDateString()
                    : "Sense expiració"
                }
                nombreImatges={sala.totalImatges}
                pes={sala.pesTotal}
                selected={modeSeleccio && seleccionada}
              />
            );
          })}
        </div>
      </div>
    );
  }, [carregant, modeSeleccio, onClickSala, sales, salesSeleccionades]);

  return (
    <main className="entrar-sala-page">
      {missatgeError && <p className="entrar-sala-error">{missatgeError}</p>}

      <section className="sales-section">
        <div className="sales-section-header">
          <div>
            <p className="sales-eyebrow">Sales compartides</p>
            <h2>Les teves sales</h2>
          </div>

          <div className="sales-header-actions">
            <button
              type="button"
              className={
                modeSeleccio
                  ? "sales-select-button active"
                  : "sales-select-button"
              }
              onClick={toggleModeSeleccio}
              disabled={carregant || sales.length === 0}
            >
              {modeSeleccio ? "Cancel·lar selecció" : "Seleccionar"}
            </button>

            <button
              type="button"
              className={
                salesSeleccionades.size > 0
                  ? "sales-delete-button active"
                  : "sales-delete-button"
              }
              onClick={eliminarSalesSeleccionades}
              disabled={carregant || sales.length === 0}
            >
              {salesSeleccionades.size > 0
                ? `Eliminar ${salesSeleccionades.size}`
                : "Eliminar"}
            </button>
          </div>
        </div>

        <div className="sales-toolbar">
          <div className="sales-options">
            <label>
              Ordenar
              <select
                value={ordre}
                onChange={(event) => {
                  setOrdre(event.target.value as "data" | "nom");
                  setPagina(1);
                }}
                disabled={carregant}
              >
                <option value="data">Data</option>
                <option value="nom">Nom</option>
              </select>
            </label>

            <label>
              Per pàgina
              <select
                value={quantitat}
                onChange={(event) => {
                  setQuantitat(Number(event.target.value));
                  setPagina(1);
                }}
                disabled={carregant}
              >
                <option value={5}>5</option>
                <option value={10}>10</option>
                <option value={20}>20</option>
              </select>
            </label>

            <button
              type="button"
              className={
                descendent
                  ? "sales-direction-button desc"
                  : "sales-direction-button asc"
              }
              onClick={canviarDireccio}
              disabled={carregant}
              title={descendent ? "Ordre descendent" : "Ordre ascendent"}
            >
              <span className="sales-direction-icon">
                {descendent ? "↓" : "↑"}
              </span>
              <span>{descendent ? "Descendent" : "Ascendent"}</span>
            </button>
          </div>
        </div>

        {modeSeleccio && (
          <div className="sales-selected-info">
            <span>
              {missatgeSeleccio ||
                `${salesSeleccionades.size} sala/es seleccionada/es`}
            </span>
          </div>
        )}

        {renderitzarSales()}

        <div className="sales-pagination">
          <button
            type="button"
            onClick={() =>
              setPagina((paginaActual) => Math.max(1, paginaActual - 1))
            }
            disabled={carregant || pagina === 1}
          >
            Anterior
          </button>

          <span>Pàgina {pagina}</span>

          <button
            type="button"
            onClick={() => setPagina((paginaActual) => paginaActual + 1)}
            disabled={carregant || sales.length < quantitat}
          >
            Següent
          </button>
        </div>
      </section>

      <button
        type="button"
        className="entrar-sala-back"
        onClick={() =>
          navigate(
            demoUserPK
              ? `/logged-home?demoUserPK=${encodeURIComponent(demoUserPK)}`
              : "/logged-home",
          )
        }
      >
        Tornar a l'inici
      </button>
    </main>
  );
}
