import * as DTOs from "../api/DTOs/UserDtos";
import * as UserApi from "../api/usuariApi";

export async function loginUsuariHandler(username: string, password: string) {
  try {
    const loginData: DTOs.loginUsuariDto = {
      username: username,
      password: password,
    };
    const resposta = await UserApi.loginUsuari(loginData);
    if (!resposta.correcte) {
      return resposta;
    }

    localStorage.setItem("usuariPK", resposta.usuariPK || "");

    return resposta;
  } catch (error) {
    console.error(error);

    return {
      correcte: false,
      missatge: "Error fent login",
    };
  }
}

export async function crearUsuariHandler(nom: string, email: string, password: string) {
  try {
    const registerData: DTOs.registerUsuariDto = {
      nom: nom,
      email: email,
      password: password,
    };
    const resposta = await UserApi.crearUsuari(registerData);

    if (!resposta.correcte || !resposta.usuari?.usuariPK) {
      return resposta;
    }

    localStorage.setItem("usuariPK", resposta.usuari.usuariPK);

    return resposta;
  } catch (error) {
    console.error(error);

    return {
      correcte: false,
      missatge: error instanceof Error ? error.message : "Error creant usuari",
    };
  }
}

export async function crearSalaHandler(
  nomSala: string, usuariPk: string
) {
  try {
    const crearSalaData: DTOs.crearSalaRequestDto = {
      nomSala: nomSala,
      usuariPk: usuariPk,
    };
    const resposta = await UserApi.crearSala(crearSalaData);

    if (!resposta.correcte || !resposta.sala?.salaPK) {
      return resposta;
    }

    localStorage.setItem("salaPk", resposta.sala.salaPK);

    return resposta;
  } catch (error) {
    console.error(error);

    return {
      correcte: false,
      missatge: error instanceof Error ? error.message : "Error creant sala",
    };
  }
}

export async function obtenirSalesUsuariHandler(usuariPk: string, pagines: number = 1, quantitat: number = 10, ordre: "data" | "nom" = "data", descendent: boolean = true) {
  try {
    console.log("Pagines", pagines);
    const request : DTOs.obtenirSalesUsuariRequestDto = {
      usuariPk: usuariPk,
      pagina: pagines,
      quantitat: quantitat,
      ordre: ordre,
      descendent: descendent
    };
    const resposta = await UserApi.obtenirSalesUsuari(request);
    console.log("Resposta obtenirSalesUsuariHandler:", resposta);
    return resposta;
  } catch (error) {
    console.error(error);
    return {
      correcte: false,
      missatge:
        error instanceof Error
          ? error.message
          : "Error obtenint les sales de l'usuari",
    };
  }
}

export async function eliminarSalesUsuariHandler(usuariPk: string, salaPks: string[]){
  try{
    const request : DTOs.eliminarSalesUsuariRequestDto = {
      usuariPK: usuariPk,
      salaPks: salaPks,
    };
    const resposta = await UserApi.eliminarSalesUsuari(request);
    return resposta;
  } catch (error) {
    console.error(error);
    return {
      correcte: false,
      missatge:
        error instanceof Error
          ? error.message
          : "Error obtenint les sales de l'usuari",
    };
  }
}

export const usuariController = {
  loginUsuariHandler,
  crearUsuariHandler,
  crearSalaHandler,
  obtenirSalesUsuariHandler,
};
