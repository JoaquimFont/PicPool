import { loginUsuariHandler } from "../../controllers/HomePageController";
import React, { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import "./login.css";

export default function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [missatgeError, setMissatgeError] = useState("");
  const [carregant, setCarregant] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const redirectTo = location.state?.redirectTo;

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setMissatgeError("");
    setCarregant(true);

    const resposta = await loginUsuariHandler(username, password);

    setCarregant(false);

    if (!resposta.correcte) {
      setMissatgeError(resposta.missatge);
      return;
    }

    console.log("Login correcte", resposta);

    localStorage.setItem("usuariPK", resposta.usuariPK!);

    navigate(redirectTo || "/logged-home", {
      state: {
        usuariPK: resposta.usuariPK,
      },
    });
  }

  return (
    <main className="login-page">
      <section className="login-card">
        <div className="login-header">
          <h1>PicPool</h1>
          <p>Entra al teu compte per accedir a les teves sales privades.</p>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <label>
            Usuari
            <input
              type="text"
              placeholder="El teu usuari"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
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

          {missatgeError && <p className="login-error">{missatgeError}</p>}

          <button type="submit" disabled={carregant}>
            {carregant ? "Entrant..." : "Iniciar sessió"}
          </button>
        </form>

        <p className="login-register">
          Encara no tens compte? <a href="/register">Crea'n un</a>
        </p>
      </section>
    </main>
  );
}
