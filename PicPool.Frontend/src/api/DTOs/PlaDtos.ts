export interface plaDto {
  plaPK: string;
  nom: string;
  limitEmmagatzematgeBytes: number;
  limitSales: number;
  limitImatges: number;
  preu: number;
  actiu: boolean;
}

export interface obtenirPlansUsuariRequestDto {
  usuariPK: string;
}

export interface obtenirPlansUsuariResponseDto {
  correcte: boolean;
  missatge: string;
  plans: plaDto[];
  plaActualPK?: string;
  plaActual?: resumPlaUsuariDto | null;
}

export interface seleccionarPlaUsuariRequestDto {
  usuariPK: string;
  plaPK: string;
}

export interface seleccionarPlaUsuariResponseDto {
  correcte: boolean;
  missatge: string;
  plaActualPK?: string;
  plaActual?: resumPlaUsuariDto | null;
}

export interface resumPlaUsuariDto {
  plaPK: string;
  nom: string;
  limitEmmagatzematgeBytes: number;
  limitSales: number;
  limitImatges: number;
  espaiConsumitBytes: number;
  salesCreades: number;
  imatgesPujades: number;
  potCrearSala: boolean;
  bytesDisponibles: number;
}
