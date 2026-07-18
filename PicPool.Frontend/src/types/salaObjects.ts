import type { userDto } from "../api/DTOs/UserDtos";

export interface SalaImage {
    imatgePK: string; //Clau primària de la imatge a mostrar.
    nomOriginal: string; //Nom original de la imatge.
    sourceUrl: string; //URL de la imatge a mostrar.
    midaBytes: number; //Mida de la imatge en bytes.
    resolucio: number; //Resolució de la imatge.
    tipusMime: string; //Tipus MIME de la imatge.
    usuariCreador: userDto; //Usuari que ha pujat la imatge.
    dataPujada: Date; //Data de pujada de la imatge.
}

export interface Sala{

    salaPK: string; //Clau primària de la sala

    nom: string; //Nom de la sala

    tokenAcces: string; //Token d'accés a la sala

    usuariCreador: userDto; //Clau primària de l'usuari que ha creat la sala

    dataCreacio: Date; //Data de creació de la sala

    dataExpiracio: Date; //Data d'expiració de la sala (opcional)

    activa: boolean; //Indica si la sala està activa o no

    pesTotal: number;

    totalImatges: number;
    

}
