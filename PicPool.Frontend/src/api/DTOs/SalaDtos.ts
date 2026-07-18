import type { SalaImage } from "../../types/salaObjects";

export interface uploadImagesRequest {
  salaPk: string;
  userPk: string;
  images: UploadImage[];
  signalRConnectionId?: string;
}

export interface UploadImage {
  file: File;
  mida: number;
  resolucio: number;
  nom: string;
  descripcio?: string;
  propietari: string;
}

export interface uploadImagesResponse {
  correcte: boolean;
  missatge: string;
}

export interface obtenirImatgesSalaRequestDTO {
  salaPk: string;
  usuariPK: string;
  pagina: number;
  quantitat: number;
  ordre: "data" | "nom";
  descendent: boolean;
}

export interface obtenirImatgesSalaResponseDTO {
  correcte: boolean;
  missatge: string;
  imatges: SalaImage[];
  totalImatges: number;
  imatgePksExistents: string[];
}

export interface deleteImageRequest {
  imagePk: string;
  salaPk: string;
  ownerPk: string;
}

export interface eliminarImatgesSalaRequestDTO {
  salaPk: string;
  usuariPk: string;
  imatgePks: string[];
  signalRConnectionId?: string;
}

export interface descarregarImatgesSalaRequestDTO {
  salaPk: string;
  imatgePks: string[];
  totes: boolean;
}
export interface salaLinkCompartitDTO {
  salaLinkCompartitPK: string;
  salaPK: string;
  nom: string;
  token: string;
  url: string;
  rol: string;
  potVeure: boolean;
  potPujar: boolean;
  potDescarregar: boolean;
  potEliminarPropies: boolean;
  potEliminarQualsevol: boolean;
  potGestionarSala: boolean;
  actiu: boolean;
  dataCreacio: string;
  dataExpiracio?: string | null;
  limitUsos?: number | null;
  usosActuals: number;
}

export interface crearLinkCompartitSalaRequestDTO {
  salaPK: string;
  usuariCreadorPK: string;
  nom: string;
  rol: string;
  potVeure: boolean;
  potPujar: boolean;
  potDescarregar: boolean;
  potEliminarPropies: boolean;
  potEliminarQualsevol: boolean;
  potGestionarSala: boolean;
  dataExpiracio?: string | null;
  limitUsos?: number | null;
}

export interface crearLinkCompartitSalaResponseDTO {
  correcte: boolean;
  missatge?: string;
  link?: salaLinkCompartitDTO;
}

export interface obtenirLinksCompartitsSalaRequestDTO {
  salaPK: string;
  usuariPK: string;
}

export interface obtenirLinksCompartitsSalaResponseDTO {
  correcte: boolean;
  missatge?: string;
  links: salaLinkCompartitDTO[];
}

export interface obtenirInfoLinkCompartitSalaRequestDTO {
  token: string;
  usuariPK?: string | null;
}

export interface infoLinkCompartitSalaDTO {
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
}

export interface obtenirInfoLinkCompartitSalaResponseDTO {
  correcte: boolean;
  missatge?: string;
  info?: infoLinkCompartitSalaDTO;
}

export interface acceptarLinkCompartitSalaRequestDTO {
  token: string;
  usuariPK: string;
}

export interface acceptarLinkCompartitSalaResponseDTO {
  correcte: boolean;
  missatge?: string;
  salaPK?: string;
}
