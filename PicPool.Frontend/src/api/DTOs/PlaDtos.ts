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
}

export interface seleccionarPlaUsuariRequestDto {
  usuariPK: string;
  plaPK: string;
}

export interface seleccionarPlaUsuariResponseDto {
  correcte: boolean;
  missatge: string;
  plaActualPK?: string;
}
