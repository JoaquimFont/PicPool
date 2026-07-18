import type {
  crearSalaRequestDto,
  crearSalaResponseDto,
  eliminarSalesUsuariRequestDto,
  LoginResponseDto,
  obtenirSalesUsuariRequestDto,
  obtenirSalesUsuariResponseDto,
  RegisterResponseDto,
  registerUsuariDto,
} from "./DTOs/UserDtos";

const API_BASE_URL = "https://localhost:7060/api";

export async function crearUsuari(
  request: registerUsuariDto,
): Promise<RegisterResponseDto> {
  const response = await fetch(`${API_BASE_URL}/usuari`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  const data: RegisterResponseDto = await response.json();

  if (!response.ok) {
    throw new Error(data.missatge || "Error creant usuari");
  }

  return data;
}

export async function loginUsuari(request: {
  username: string;
  password: string;
}): Promise<LoginResponseDto> {
  const response = await fetch(`${API_BASE_URL}/usuari/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error("Error fent login");
  }

  return await response.json();
}

export async function crearSala(
  request: crearSalaRequestDto,
): Promise<crearSalaResponseDto> {
  const response = await fetch(`${API_BASE_URL}/usuari/crearSala`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  const data: crearSalaResponseDto = await response.json();

  if (!response.ok) {
    throw new Error(data.missatge || "Error creant sala");
  }

  return data;
}

export async function obtenirSalesUsuari(
  request: obtenirSalesUsuariRequestDto,
): Promise<obtenirSalesUsuariResponseDto> {
  const response = await fetch(`${API_BASE_URL}/usuari/obtenirSalesUsuari`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });
  const data: obtenirSalesUsuariResponseDto = await response.json();

  if (!response.ok) {
    throw new Error("Error obtenint les sales de l'usuari");
  }

  return data;
}

export async function eliminarSalesUsuari(
  request: eliminarSalesUsuariRequestDto,
): Promise<boolean> {
  const response = await fetch(`${API_BASE_URL}/usuari/eliminarSalesUsuari`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });
  const data = await response.json();

  if (!response.ok) {
    throw new Error("Error obtenint les sales de l'usuari");
  }

  return data;
}
