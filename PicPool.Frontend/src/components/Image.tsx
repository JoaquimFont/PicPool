import type { CSSProperties, MouseEventHandler } from "react";
import React from "react";
import "./Image.css";

interface ComponentProps {
  imagePk: string; //Clau primària de la imatge a mostrar.
  source: string; //Fitxer de la imatge a mostrar.
  size: number; //Pes de la imatge
  resolution: number; //Resolució de la imatge
  type: string; //Tipus de la imatge (jpg, png, etc.)
  name: string;
  selected?: boolean; //Si es true, es mostra el component com seleccionat.
  hidden?: boolean;
  containerSize?: "small" | "medium" | "big";
  cssOverrideContainer?: CSSProperties;
  onMouseDown?: (value: string) => void;
  onMouseUp?: MouseEventHandler<HTMLButtonElement>;
  onClick?: (value: string) => void;
}

export const ImageContainer: React.FunctionComponent<ComponentProps> =
  React.memo((props) => {
    const getContainerClassName = React.useCallback(() => {
      let className = "image-card-external-container";

      if (props.containerSize) {
        className += ` ${props.containerSize}`;
      }

      if (props.selected) {
        className += " selected";
      }

      if (props.hidden) {
        className += " hidden";
      }

      return className;
    }, [props.selected, props.hidden, props.containerSize]);

    const getImageCardInternalContainerClassName = React.useCallback(() => {
      let className = "image-card-internal-container";

      if (props.selected) {
        className += " selected";
      }
      console.log(
        "Renderitzant ImageContainer amb imagePk:",
        props.imagePk,
        "selected:",
        props.selected,
        "hidden:",
        props.hidden,
        "containerSize:",
        props.containerSize,
      );
      return className;
    }, [props.selected]);

    //EN AQUEST CAS, AL CANVIAR EL CLASSNAME DEL PRIMER DIV, PODREM FER EFECTES VISUALS DE SELECCIÓ I OCULTACIÓ DE LA IMATGE, PERÒ NO CALDRÀ RE-RENDERITZAR EL COMPONENT,
    //TORNAR A RENDERITZAR LA IMATGE, JA QUE REACT COMPARA EL VIRTUAL DOM AMB EL REAL DOM I DETECTE QUE NOMÉS CAL RENDERITZAR EL DIV EXTERIOR, PERÒ NO LA IMATGE INTERIOR,
    // JA QUE EL CLASSNAME NO AFECTA A LA LÒGICA INTERNA DEL COMPONENT, SINO QUE NOMÉS CANVIA L'APARENÇA VISUAL. AIXÍ, PODEM OPTIMITZAR EL RENDIMENT EVITANT RE-RENDERITZAR EL COMPONENT INNECESSARIAMENT QUAN CANVIA EL CLASSNAME.
    return (
      <div
        className={getContainerClassName()}
        onMouseDown={() => props.onMouseDown?.(props.imagePk)}
        onClick={() => props.onClick?.(props.imagePk)}
      >
        <div className={getImageCardInternalContainerClassName()}>
          <img className={"image-card-preview"} src={props.source} />
        </div>
      </div>
    );
  });
