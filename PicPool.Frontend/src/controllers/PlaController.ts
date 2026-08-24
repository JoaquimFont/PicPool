import * as PlaApi from "../api/plaApi";

export async function obtenirPlansUsuariHandler(usuariPK: string) {
  try {
    return await PlaApi.obtenirPlansUsuari({
      usuariPK,
    });
  } catch (error) {
    console.error(error);

    return {
      correcte: false,
      missatge:
        error instanceof Error ? error.message : "Error obtenint els plans",
      plans: [],
    };
  }
}

export async function seleccionarPlaUsuariHandler(
  usuariPK: string,
  plaPK: string,
) {
  try {
    return await PlaApi.seleccionarPlaUsuari({
      usuariPK,
      plaPK,
    });
  } catch (error) {
    console.error(error);

    return {
      correcte: false,
      missatge:
        error instanceof Error ? error.message : "Error seleccionant el pla",
    };
  }
}

export const plaController = {
  obtenirPlansUsuariHandler,
  seleccionarPlaUsuariHandler,
};
