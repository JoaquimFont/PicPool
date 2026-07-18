/* eslint-disable @typescript-eslint/no-unused-vars */
import React from "react";
import { useNavigate } from "react-router-dom";
import "./Home.css";

const Home: React.FC = () => {
  const navigate = useNavigate();

  function handleLoginClick() {
    navigate("/login");
  }

  function handleRegisterClick() {
    navigate("/register");
  }

  return (
    <div className="home">
      <section className="home-card">
        <div className="brand">
          <h1>PicPool</h1>
          <p>Comparteix fotos en alta qualitat sense límits</p>
        </div>

        <div className="home-actions">
          <button className="primary" onClick={handleLoginClick}>
            Iniciar sessió
          </button>

          <button className="secondary" onClick={handleRegisterClick}>
            Crear compte
          </button>
        </div>
      </section>
    </div>
  );
};

export default Home;