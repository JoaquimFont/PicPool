import React, { useState } from "react";
import "./sign-up.css";
import { crearUsuariHandler } from "../../controllers/HomePageController";
import { useNavigate } from "react-router-dom";

export default function SignUpPage() {
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmarPassword, setConfirmarPassword] = useState("");
  const [missatgeError, setMissatgeError] = useState("");
  const [carregant, setCarregant] = useState(false);
  
  const navigate = useNavigate();

  async function crearCompte(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setMissatgeError("");

    if (!username || !password || !confirmarPassword) {
      setMissatgeError("Omple tots els camps.");
      return;
    }

    if (password !== confirmarPassword) {
      setMissatgeError("Les contrasenyes no coincideixen.");
      return;
    }

    setCarregant(true);

    const resposta = await crearUsuariHandler(username, email, password);

    setCarregant(false);

    console.log("Compte creat correctament", resposta);

    if (!resposta.correcte) {
      setMissatgeError(resposta.missatge || "No s'ha pogut crear el compte.");
      return;
    }

    navigate("/login");
  }

  return (
    <main className="signup-page">
      <section className="signup-card">
        <div className="signup-header">
          <h1>PicPool</h1>
          <p>
            Crea el teu compte i comença a compartir fotos en alta qualitat.
          </p>
        </div>

        <form className="signup-form" onSubmit={crearCompte}>
          <label>
            Nom
            <input
              type="text"
              placeholder="El teu nom d'usuari"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              disabled={carregant}
            />
          </label>
          <label>
            Email
            <input
              type="text"
              placeholder="El teu email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              disabled={carregant}
            />
          </label>

          <label>
            Contrasenya
            <input
              type="password"
              placeholder="La teva contrasenya"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              disabled={carregant}
            />
          </label>

          <label>
            Confirma la contrasenya
            <input
              type="password"
              placeholder="Repeteix la contrasenya"
              value={confirmarPassword}
              onChange={(event) => setConfirmarPassword(event.target.value)}
              disabled={carregant}
            />
          </label>

          {missatgeError && <p className="signup-error">{missatgeError}</p>}

          <button type="submit" disabled={carregant}>
            {carregant ? "Creant compte..." : "Crear compte"}
          </button>
        </form>

        <p className="signup-login">
          Ja tens compte? <a href="/login">Inicia sessió</a>
        </p>
      </section>
    </main>
  );
}
