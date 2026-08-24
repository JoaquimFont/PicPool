import * as React from "react";
import type { SalaImage } from "../../../types/salaObjects";
import { ImageContainer } from "../../../components/Image";
import {
  descarregarImatgesSeleccionadesSalaHandler,
  descarregarTotesImatgesSalaHandler,
  eliminarImatgesSalaHandler,
  obtenirImatgesSalaHandler,
  uploadSalaImages,
} from "../../../controllers/salaController";
import { useLocation, useNavigate } from "react-router-dom";
import "./sala.css";
import CrearICompartirLink from "./crear-i-compartir-link";
import * as signalR from "@microsoft/signalr";
import { API_BASE_ORIGIN } from "../../../api/salaApi";

export default function SalaComponent() {
  const inputFileRef = React.useRef<HTMLInputElement>(null);

  const location = useLocation();
  const navigate = useNavigate();
  const searchParams = new URLSearchParams(location.search);
  const demoUserPK = searchParams.get("demoUserPK");
  const demoSalaPK = searchParams.get("demoSalaPK");
  const [missatgeSeleccio, setMissatgeSeleccio] = React.useState("");
  const [missatgeError, setMissatgeError] = React.useState("");
  const [carregant, setCarregant] = React.useState(true);
  const [images, setImages] = React.useState<SalaImage[]>([]);
  const [uploadingImages, setUploadingImages] = React.useState(false);
  const [mostrarCompartir, setMostrarCompartir] = React.useState(false);
  const [pagina, setPagina] = React.useState(1);
  const [quantitat, setQuantitat] = React.useState(5);
  const [ordre, setOrdre] = React.useState<"data" | "nom">("data");
  const [descendent, setDescendent] = React.useState(false);

  const salaPk = demoSalaPK ?? location.state?.salaPk;
  const usuariPk =
    demoUserPK ?? location.state?.usuariPK ?? localStorage.getItem("usuariPK");

  const [refreshKey, setRefreshKey] = React.useState(0);
  const [imatgesSeleccionades, setImatgesSeleccionades] = React.useState<
    Set<string>
  >(new Set());
  const [modeSeleccio, setModeSeleccio] = React.useState(false);

  const [signalRConnectionId, setSignalRConnectionId] =
    React.useState<string>();
  const [missatgeOperacioSala, setMissatgeOperacioSala] = React.useState("");

  const openFileExplorer = React.useCallback(() => {
    inputFileRef.current?.click();
  }, []);

  const obtenirImatgesSala = React.useCallback(async () => {
    if (!salaPk || !usuariPk) return;

    return await obtenirImatgesSalaHandler(
      salaPk,
      usuariPk,
      pagina,
      quantitat,
      ordre,
      descendent,
    );
  }, [salaPk, usuariPk, pagina, quantitat, ordre, descendent]);
  React.useEffect(() => {
    if (!salaPk) return;

    let componentActiu = true;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_ORIGIN}/hubs/sala`)
      .withAutomaticReconnect()
      .build();

    connection.on("OperacioSalaEnCua", (event) => {
      if (!componentActiu || event.salaPk !== salaPk) return;

      setMissatgeOperacioSala(
        event.missatge ||
          "Un altre usuari està pujant o eliminant imatges. Quan acabi, les teves accions es realitzaran.",
      );
    });

    connection.on("ImatgesSalaActualitzades", (event) => {
      if (!componentActiu || event.salaPk !== salaPk) return;

      if (event.usuariPk !== usuariPk) {
        setMissatgeOperacioSala(
          event.operacio === "eliminacio"
            ? "Un altre usuari ha eliminat imatges. Actualitzant la sala..."
            : "Un altre usuari ha pujat imatges. Actualitzant la sala...",
        );
      }

      setRefreshKey((valorActual) => valorActual + 1);
    });

    async function iniciarSignalR() {
      try {
        await connection.start();

        if (!componentActiu) return;

        setSignalRConnectionId(connection.connectionId ?? undefined);

        await connection.invoke("EntrarSala", salaPk);
      } catch (error) {
        console.error("Error connectant SignalR:", error);
      }
    }

    void iniciarSignalR();

    return () => {
      componentActiu = false;

      void connection
        .invoke("SortirSala", salaPk)
        .catch(() => undefined)
        .finally(() => {
          void connection.stop();
        });
    };
  }, [salaPk, usuariPk]);

  React.useEffect(() => {
    let componentActiu = true;

    async function carregarImatgesSala() {
      try {
        setCarregant(true);

        const resposta = await obtenirImatgesSala();

        if (!componentActiu) return;

        if (resposta?.correcte) {
          const novesImatges = resposta.imatges || [];

          if (
            novesImatges.length === 0 &&
            pagina > 1 &&
            resposta.totalImatges > 0
          ) {
            setPagina((paginaActual) => Math.max(1, paginaActual - 1));
            return;
          }

          setImages(novesImatges);

          const pksExistents = new Set(resposta.imatgePksExistents || []);

          setImatgesSeleccionades((seleccionadesActuals) => {
            const seleccionadesQueEncaraExisteixen = new Set<string>();

            seleccionadesActuals.forEach((imatgePk) => {
              if (pksExistents.has(imatgePk)) {
                seleccionadesQueEncaraExisteixen.add(imatgePk);
              }
            });

            return seleccionadesQueEncaraExisteixen;
          });

          setMissatgeError("");

          if (missatgeOperacioSala) {
            setTimeout(() => {
              setMissatgeOperacioSala("");
            }, 3500);
          }
        } else {
          setImages([]);
          setMissatgeError(
            resposta?.missatge || "Error obtenint les imatges de la sala",
          );
        }
      } catch (error) {
        if (!componentActiu) return;

        setImages([]);
        setMissatgeError(
          error instanceof Error
            ? error.message
            : "Error obtenint les imatges de la sala",
        );
      } finally {
        if (componentActiu) {
          setCarregant(false);
        }
      }
    }

    void carregarImatgesSala();

    return () => {
      componentActiu = false;
    };
  }, [obtenirImatgesSala, refreshKey, pagina, missatgeOperacioSala]);

  const onUploadImages = React.useCallback(
    async (imageFiles: File[]) => {
      if (!salaPk || !usuariPk || imageFiles.length === 0) return;

      try {
        setUploadingImages(true);
        setMissatgeError("");
        setMissatgeOperacioSala("Preparant la pujada d'imatges...");
        const response = await uploadSalaImages(
          salaPk,
          usuariPk,
          imageFiles,
          signalRConnectionId,
        );

        if (response.correcte) {
          setMissatgeOperacioSala("Imatges pujades correctament.");
        } else {
          setMissatgeError(response.missatge || "Error pujant les imatges.");
        }
      } catch (error) {
        setMissatgeError(
          error instanceof Error ? error.message : "Error pujant les imatges.",
        );
      } finally {
        setUploadingImages(false);
      }
    },
    [salaPk, usuariPk, signalRConnectionId],
  );

  const onFilesSelected = React.useCallback(
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const files = event.target.files;

      if (!files || files.length === 0) {
        return;
      }

      const imageFiles = Array.from(files).filter((file) =>
        file.type.startsWith("image/"),
      );

      if (imageFiles.length === 0) {
        event.target.value = "";
        setMissatgeError("Selecciona com a mínim una imatge vàlida.");
        return;
      }

      void onUploadImages(imageFiles);

      event.target.value = "";
    },
    [onUploadImages],
  );

  const onClickImatge = React.useCallback(
    (imatgePk: string) => {
      if (modeSeleccio) {
        setMissatgeSeleccio("");

        setImatgesSeleccionades((prev) => {
          const newSet = new Set(prev);

          if (newSet.has(imatgePk)) {
            newSet.delete(imatgePk);
          } else {
            newSet.add(imatgePk);
          }

          return newSet;
        });

        return;
      }

      console.log("Obrir imatge:", imatgePk);
    },
    [modeSeleccio],
  );

  const toggleModeSeleccio = React.useCallback(() => {
    setModeSeleccio((valorActual) => {
      const nouValor = !valorActual;

      if (!nouValor) {
        setImatgesSeleccionades(new Set());
        setMissatgeSeleccio("");
      }

      return nouValor;
    });
  }, []);

  const canviarDireccio = React.useCallback(() => {
    setDescendent((valorActual) => !valorActual);
    setPagina(1);
  }, []);

  const descarregarTotes = React.useCallback(async () => {
    if (!salaPk) {
      return;
    }

    try {
      setMissatgeError("");

      await descarregarTotesImatgesSalaHandler(salaPk);
    } catch (error) {
      setMissatgeError(
        error instanceof Error
          ? error.message
          : "Error descarregant totes les imatges.",
      );
    }
  }, [salaPk]);

  const descarregarSeleccionades = React.useCallback(async () => {
    if (!salaPk || imatgesSeleccionades.size === 0) {
      return;
    }

    try {
      setMissatgeError("");

      await descarregarImatgesSeleccionadesSalaHandler(
        salaPk,
        Array.from(imatgesSeleccionades),
      );
    } catch (error) {
      setMissatgeError(
        error instanceof Error
          ? error.message
          : "Error descarregant les imatges seleccionades.",
      );
    }
  }, [salaPk, imatgesSeleccionades]);

  const eliminarSeleccionades = React.useCallback(async () => {
    if (imatgesSeleccionades.size === 0) {
      setModeSeleccio(true);
      setMissatgeSeleccio("Selecciona les imatges que vols eliminar.");
      return;
    }

    if (!salaPk || !usuariPk) return;

    const imatgePks = Array.from(imatgesSeleccionades);

    try {
      setCarregant(true);
      setMissatgeError("");
      setMissatgeOperacioSala("Eliminant imatges...");

      const resposta = await eliminarImatgesSalaHandler(
        salaPk,
        usuariPk,
        imatgePks,
        signalRConnectionId,
      );

      if (!resposta) {
        setMissatgeError("No s'han pogut eliminar les imatges.");
        return;
      }

      setMissatgeOperacioSala("Imatges eliminades correctament.");
      setImatgesSeleccionades(new Set());
      setModeSeleccio(false);
    } catch (error) {
      setMissatgeError(
        error instanceof Error
          ? error.message
          : "Error eliminant les imatges seleccionades.",
      );
    } finally {
      setCarregant(false);
    }
  }, [imatgesSeleccionades, signalRConnectionId, usuariPk, salaPk]);

  const renderImages = () => {
    if (carregant) {
      return <p className="sala-view-loading">Carregant imatges...</p>;
    }

    if (images.length === 0) {
      return (
        <p className="sala-view-empty">Aquesta sala encara no té imatges.</p>
      );
    }

    return (
      <div className="sala-view-images-panel">
        <div className="sala-view-images-grid">
          {images.map((image) => (
            <ImageContainer
              key={image.imatgePK}
              imagePk={image.imatgePK}
              source={image.sourceUrl}
              size={image.midaBytes}
              resolution={image.resolucio}
              type={image.tipusMime}
              name={image.nomOriginal}
              containerSize="medium"
              selected={
                modeSeleccio && imatgesSeleccionades.has(image.imatgePK)
              }
              onClick={onClickImatge}
            />
          ))}
        </div>
      </div>
    );
  };

  if (!salaPk || !usuariPk) {
    return (
      <main className="sala-view-page">
        <p className="sala-view-error">
          No s'ha pogut obrir la sala perquè falten dades.
        </p>

        <button
          type="button"
          className="sala-view-back"
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

  return (
    <main className="sala-view-page">
      {missatgeError && <p className="sala-view-error">{missatgeError}</p>}
      {missatgeOperacioSala && (
        <p className="sala-view-operation-message">{missatgeOperacioSala}</p>
      )}
      <section className="sala-view-section">
        <div className="sala-view-header">
          <div>
            <p className="sala-view-eyebrow">Sala compartida</p>
            <h2>Imatges de la sala</h2>
          </div>

          <div className="sala-view-header-actions">
            <button
              type="button"
              className="sala-view-share-button"
              onClick={() => setMostrarCompartir(true)}
              disabled={carregant}
            >
              Compartir
            </button>
            <button
              type="button"
              className="sala-view-upload-button"
              onClick={openFileExplorer}
              disabled={uploadingImages || carregant}
            >
              Pujar imatges
            </button>

            <button
              type="button"
              className={
                imatgesSeleccionades.size > 0
                  ? "sala-view-delete-main-button active"
                  : "sala-view-delete-main-button"
              }
              onClick={eliminarSeleccionades}
              disabled={carregant || images.length === 0}
            >
              {imatgesSeleccionades.size > 0
                ? `Eliminar ${imatgesSeleccionades.size}`
                : "Eliminar"}
            </button>
          </div>
        </div>

        {uploadingImages && (
          <p className="sala-view-uploading">Pujant imatges...</p>
        )}

        <div className="sala-view-toolbar">
          <div className="sala-view-options">
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
                  ? "sala-view-direction-button desc"
                  : "sala-view-direction-button asc"
              }
              onClick={canviarDireccio}
              disabled={carregant}
              title={descendent ? "Ordre descendent" : "Ordre ascendent"}
            >
              <span className="sala-view-direction-icon">
                {descendent ? "↓" : "↑"}
              </span>
              <span>{descendent ? "Descendent" : "Ascendent"}</span>
            </button>
          </div>

          <div className="sala-view-image-actions">
            <button
              type="button"
              className={
                modeSeleccio
                  ? "sala-view-select-button active"
                  : "sala-view-select-button"
              }
              onClick={toggleModeSeleccio}
              disabled={carregant || images.length === 0}
            >
              {modeSeleccio ? "Cancel·lar selecció" : "Seleccionar"}
            </button>

            <button
              type="button"
              className="sala-view-download-all-button"
              onClick={descarregarTotes}
              disabled={carregant || images.length === 0}
            >
              Descarregar totes
            </button>

            {imatgesSeleccionades.size > 0 && (
              <button
                type="button"
                className="sala-view-download-selected-button"
                onClick={descarregarSeleccionades}
                disabled={carregant}
              >
                Descarregar seleccionades
              </button>
            )}
          </div>
        </div>

        {modeSeleccio && (
          <div className="sala-view-selected-info">
            <span>
              {missatgeSeleccio ||
                `${imatgesSeleccionades.size} imatge(s) seleccionada(es)`}
            </span>
          </div>
        )}

        <input
          ref={inputFileRef}
          type="file"
          accept="image/*"
          multiple
          className="sala-view-file-input"
          onChange={onFilesSelected}
        />

        {renderImages()}

        <div className="sala-view-pagination">
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
            disabled={carregant || images.length < quantitat}
          >
            Següent
          </button>
        </div>
      </section>

      <button
        type="button"
        className="sala-view-back"
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
      {mostrarCompartir && (
        <CrearICompartirLink
          salaPK={salaPk}
          usuariPK={usuariPk}
          onTancar={() => setMostrarCompartir(false)}
        />
      )}
    </main>
  );
}
