import React from "react";
import "./info-sala.css";

interface ComponentProps {
  salaPk: string;
  nom: string;
  creador: string;
  dataCreacio: string;
  dataExpiracio: string;
  nombreImatges: number;
  pes: number;
  selected?: boolean;
  containerSize?: "small" | "medium" | "big";
  onClick?: (value: string) => void;
}

export const InfoSala: React.FunctionComponent<ComponentProps> = React.memo(
  (props) => {
    const getContainerClassName = React.useCallback(() => {
      let className = "external-container";

      if (props.containerSize) {
        className += ` ${props.containerSize}`;
      }

      if (props.selected) {
        className += " selected";
      }

      return className;
    }, [props.selected, props.containerSize]);

    const onClickSalaInfo = React.useCallback(() => {
      props.onClick?.(props.salaPk);
    }, [props.salaPk, props.onClick]);

    return (
      <div className={getContainerClassName()} onMouseDown={onClickSalaInfo}>
        {props.selected && (
          <span className="room-selected-badge" aria-hidden="true">
            ✓
          </span>
        )}

        <div className="internal-container">
          <div className="room-header">
            <div>
              <span className="room-eyebrow">Sala</span>
              <span className="room-name">{props.nom}</span>
            </div>
          </div>

          <div className="room-info">
            <div className="room-info-row">
              <span className="room-label">Creador</span>
              <span className="room-value">{props.creador}</span>
            </div>

            <div className="room-info-row">
              <span className="room-label">Creació</span>
              <span className="room-value">{props.dataCreacio}</span>
            </div>

            <div className="room-info-row">
              <span className="room-label">Expiració</span>
              <span className="room-value">{props.dataExpiracio}</span>
            </div>

            <div className="room-info-row">
              <span className="room-label">Imatges</span>
              <span className="room-value">{props.nombreImatges}</span>
            </div>

            <div className="room-info-row">
              <span className="room-label">Pes</span>
              <span className="room-value">{(props.pes / 1024 / 1024).toFixed(2)} MB</span>
            </div>
          </div>
        </div>
      </div>
    );
  },
);
