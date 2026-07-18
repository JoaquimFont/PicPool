import type {
  acceptarLinkCompartitSalaRequestDTO,
  acceptarLinkCompartitSalaResponseDTO,
  crearLinkCompartitSalaRequestDTO,
  crearLinkCompartitSalaResponseDTO,
  descarregarImatgesSalaRequestDTO,
  eliminarImatgesSalaRequestDTO,
  obtenirImatgesSalaRequestDTO,
  obtenirImatgesSalaResponseDTO,
  obtenirInfoLinkCompartitSalaRequestDTO,
  obtenirInfoLinkCompartitSalaResponseDTO,
  obtenirLinksCompartitsSalaRequestDTO,
  obtenirLinksCompartitsSalaResponseDTO,
  uploadImagesRequest,
  uploadImagesResponse,
} from "./DTOs/SalaDtos";

export const API_BASE_ORIGIN = "https://localhost:7060";
const API_BASE_URL = `${API_BASE_ORIGIN}/api`;

export async function uploadImages(
  request: uploadImagesRequest,
): Promise<uploadImagesResponse> {
  const formData = new FormData();
  if (request.signalRConnectionId) {
    formData.append("SignalRConnectionId", request.signalRConnectionId);
  }
  formData.append("SalaPk", request.salaPk);
  formData.append("UserPk", request.userPk);

  request.images.forEach((image, index) => {
    formData.append(`Imatges[${index}].File`, image.file);
    formData.append(`Imatges[${index}].Mida`, image.mida.toString());
    formData.append(`Imatges[${index}].Resolucio`, image.resolucio.toString());
    formData.append(`Imatges[${index}].Nom`, image.nom);
    formData.append(`Imatges[${index}].Descripcio`, image.descripcio ?? "");
    formData.append(`Imatges[${index}].Propietari`, image.propietari);
  });

  const response = await fetch(`${API_BASE_URL}/sala/pujarImatges`, {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    const errorText = await response.text();
    console.error("Status:", response.status);
    console.error("Error backend:", errorText);
    throw new Error(errorText || "Error pujant les imatges");
  }

  return response.json();
}

export async function obtenirImatgesSala(
  request: obtenirImatgesSalaRequestDTO,
): Promise<obtenirImatgesSalaResponseDTO> {
  const response = await fetch(`${API_BASE_URL}/sala/obtenirImatgesSala`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });
  if (!response.ok) {
    throw new Error("Error obtenint les imatges de la sala");
  }
  return response.json();
}

export async function eliminarImatgesSala(
  request: eliminarImatgesSalaRequestDTO,
): Promise<boolean> {
  const response = await fetch(`${API_BASE_URL}/sala/eliminarImatgesSala`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error("Error eliminant les imatges de la sala");
  }
  return response.json();
}

export async function descarregarImatgesSala(
  request: descarregarImatgesSalaRequestDTO,
): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/sala/descarregarImatgesSala`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Error descarregant les imatges de la sala");
  }

  const blob = await response.blob();

  const url = window.URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;
  link.download = request.totes
    ? "imatges-sala.zip"
    : "imatges-seleccionades.zip";

  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  window.URL.revokeObjectURL(url);
}

export async function crearLinkCompartitSala(
  request: crearLinkCompartitSalaRequestDTO,
): Promise<crearLinkCompartitSalaResponseDTO> {
  const response = await fetch(`${API_BASE_URL}/sala/crearLinkCompartit`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Error creant el link compartit");
  }

  return response.json();
}

export async function obtenirLinksCompartitsSala(
  request: obtenirLinksCompartitsSalaRequestDTO,
): Promise<obtenirLinksCompartitsSalaResponseDTO> {
  const response = await fetch(`${API_BASE_URL}/sala/obtenirLinksCompartits`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Error obtenint els links compartits");
  }

  return response.json();
}

export async function obtenirInfoLinkCompartitSala(
  request: obtenirInfoLinkCompartitSalaRequestDTO,
): Promise<obtenirInfoLinkCompartitSalaResponseDTO> {
  const response = await fetch(
    `${API_BASE_URL}/sala/obtenirInfoLinkCompartit`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    },
  );

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Error obtenint la informació del link");
  }

  return response.json();
}

export async function acceptarLinkCompartitSala(
  request: acceptarLinkCompartitSalaRequestDTO,
): Promise<acceptarLinkCompartitSalaResponseDTO> {
  const response = await fetch(`${API_BASE_URL}/sala/acceptarLinkCompartit`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Error acceptant el link compartit");
  }

  return response.json();
}
