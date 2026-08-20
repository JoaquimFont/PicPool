import type {
  obtenirPlansUsuariRequestDto,
  obtenirPlansUsuariResponseDto,
  seleccionarPlaUsuariRequestDto,
  seleccionarPlaUsuariResponseDto,
} from "./DTOs/PlaDtos";

const API_BASE_URL = "https://localhost:7060/api";

export async function obtenirPlansUsuari(
  request: obtenirPlansUsuariRequestDto,
): Promise<obtenirPlansUsuariResponseDto> {
  const response = await fetch(`${API_BASE_URL}/usuari/obtenirPlansUsuari`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  const data: obtenirPlansUsuariResponseDto = await response.json();

  if (!response.ok) {
    throw new Error(data.missatge || "Error obtenint els plans");
  }

  return data;
}

export async function seleccionarPlaUsuari(
  request: seleccionarPlaUsuariRequestDto,
): Promise<seleccionarPlaUsuariResponseDto> {
  const response = await fetch(`${API_BASE_URL}/usuari/seleccionarPlaUsuari`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  const data: seleccionarPlaUsuariResponseDto = await response.json();

  if (!response.ok) {
    throw new Error(data.missatge || "Error seleccionant el pla");
  }

  return data;
}
