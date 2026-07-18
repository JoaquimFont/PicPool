import type { Sala } from "../../types/salaObjects";
import type { Usuari } from "../../types/usuari";

export interface LoginResponseDto {
  correcte: boolean;
  missatge: string;
  usuariPK?: string;
  nom?: string;
  email?: string;
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
}

export interface crearSalaResponseDto {
    correcte: boolean;
    missatge: string;
    sala?: Sala;
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