export interface CrearUsuariRequest {
  nom: string;
  email: string;
  password: string;
}

export interface Usuari {
  usuariPK: string;
  nom: string;
  email: string;
  passwordHash: string;
  dataAlta: string;
  actiu: boolean;
}
