import {
  acceptarLinkCompartitSala,
  crearLinkCompartitSala,
  descarregarImatgesSala,
  eliminarImatgesSala,
  obtenirImatgesSala,
  obtenirInfoLinkCompartitSala,
  obtenirLinksCompartitsSala,
  uploadImages,
} from "../api/salaApi";
import type {
  acceptarLinkCompartitSalaResponseDTO,
  crearLinkCompartitSalaResponseDTO,
  descarregarImatgesSalaRequestDTO,
  eliminarImatgesSalaRequestDTO,
  obtenirImatgesSalaRequestDTO,
  obtenirImatgesSalaResponseDTO,
  obtenirInfoLinkCompartitSalaResponseDTO,
  obtenirLinksCompartitsSalaResponseDTO,
  uploadImagesRequest,
  uploadImagesResponse,
} from "../api/DTOs/SalaDtos";

export async function uploadSalaImages(
  salaPk: string,
  userPk: string,
  imageFiles: File[],
  signalRConnectionId?: string,
): Promise<uploadImagesResponse> {
  const request: uploadImagesRequest = {
    salaPk,
    userPk,
    signalRConnectionId,
    images: imageFiles.map((file) => ({
      file,
      mida: file.size,
      resolucio: 0,
      nom: file.name,
      descripcio: "",
      propietari: userPk,
    })),
  };

  return await uploadImages(request);
}

export async function obtenirImatgesSalaHandler(
  salaPk: string,
  usuariPK: string,
  pagines: number = 1,
  quantitat: number = 10,
  ordre: "data" | "nom" = "data",
  descendent: boolean = true,
): Promise<obtenirImatgesSalaResponseDTO> {
  const request: obtenirImatgesSalaRequestDTO = {
    salaPk,
    usuariPK,
    pagina: pagines,
    quantitat,
    ordre,
    descendent,
  };

  const resposta = await obtenirImatgesSala(request);
  console.log("Resposta obtenirImatgesSalaHandler:", resposta);
  return resposta;
}

export async function eliminarImatgesSalaHandler(
  salaPk: string,
  usuariPk: string,
  imatgePks: string[],
  signalRConnectionId?: string,
): Promise<boolean> {
  const request: eliminarImatgesSalaRequestDTO = {
    salaPk,
    usuariPk,
    imatgePks,
    signalRConnectionId,
  };

  const resposta = await eliminarImatgesSala(request);
  console.log("Resposta obtenirImatgesSalaHandler:", resposta);
  return resposta;
}

export async function descarregarTotesImatgesSalaHandler(
  salaPk: string,
): Promise<void> {
  const request: descarregarImatgesSalaRequestDTO = {
    salaPk,
    imatgePks: [],
    totes: true,
  };

  await descarregarImatgesSala(request);
}

export async function descarregarImatgesSeleccionadesSalaHandler(
  salaPk: string,
  imatgePks: string[],
): Promise<void> {
  const request: descarregarImatgesSalaRequestDTO = {
    salaPk,
    imatgePks,
    totes: false,
  };

  await descarregarImatgesSala(request);
}

export async function crearLinkCompartitSalaHandler(
  salaPK: string,
  usuariCreadorPK: string,
  nom: string,
  rol: string,
  potVeure: boolean,
  potPujar: boolean,
  potDescarregar: boolean,
  potEliminarPropies: boolean,
  potEliminarQualsevol: boolean,
  potGestionarSala: boolean,
  dataExpiracio: string | null = null,
  limitUsos: number | null = null,
): Promise<crearLinkCompartitSalaResponseDTO> {
  return await crearLinkCompartitSala({
    salaPK,
    usuariCreadorPK,
    nom,
    rol,
    potVeure,
    potPujar,
    potDescarregar,
    potEliminarPropies,
    potEliminarQualsevol,
    potGestionarSala,
    dataExpiracio,
    limitUsos,
  });
}

export async function obtenirLinksCompartitsSalaHandler(
  salaPK: string,
  usuariPK: string,
): Promise<obtenirLinksCompartitsSalaResponseDTO> {
  return await obtenirLinksCompartitsSala({
    salaPK,
    usuariPK,
  });
}

export async function obtenirInfoLinkCompartitSalaHandler(
  token: string,
  usuariPK?: string | null,
): Promise<obtenirInfoLinkCompartitSalaResponseDTO> {
  return await obtenirInfoLinkCompartitSala({
    token,
    usuariPK,
  });
}

export async function acceptarLinkCompartitSalaHandler(
  token: string,
  usuariPK: string,
): Promise<acceptarLinkCompartitSalaResponseDTO> {
  return await acceptarLinkCompartitSala({
    token,
    usuariPK,
  });
}
