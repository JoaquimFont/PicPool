import type { Sala } from "../../types/salaObjects";
import type { Usuari } from "../../types/usuari";

export interface LoginResponseDto {
  correcte: boolean;
  missatge: string;
  usuariPK?: string;
  nom?: string;
  email?: string;
  pla?: resumPlaUsuariDto | null;
}

export interface loginUsuariDto {
    username: string;
    password: string;
}


export interface registerUsuariDto {
    nom: string;
    email: string;
    password: string;
}

export interface RegisterResponseDto {
    correcte: boolean;
    missatge: string;
    usuari?: Usuari;
    pla?: resumPlaUsuariDto | null;
}

export interface crearSalaResponseDto {
    correcte: boolean;
    missatge: string;
    sala?: Sala;
    pla?: resumPlaUsuariDto | null;
}

export interface crearSalaRequestDto {
    nomSala: string;
    usuariPk: string;
}

export interface obtenirSalesUsuariRequestDto {
    usuariPk: string;
    pagina: number;
    quantitat: number;
    ordre: "data" | "nom";
    descendent: boolean;
}

export interface obtenirSalesUsuariResponseDto {
    correcte: boolean;
    missatge?: string;
    salas?: Sala[];
}

export interface userDto
{
    usuariPK: string;
    nom: string;
    email: string;
}

export interface eliminarSalesUsuariRequestDto{
    usuariPK: string;
    salaPks: string[];
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
