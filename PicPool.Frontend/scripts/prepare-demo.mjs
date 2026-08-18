#!/usr/bin/env node

import process from "node:process";

const args = new Map();
for (const arg of process.argv.slice(2)) {
  const [key, value = "true"] = arg.replace(/^--/, "").split("=");
  args.set(key, value);
}

const apiBase = args.get("api") ?? "https://localhost:7060";
const frontendBase = args.get("frontend") ?? "http://localhost:5173";
const userCount = Number(args.get("users") ?? 6);
const skipUpload = args.get("skip-upload") === "true";
const runId =
  args.get("name") ??
  new Date().toISOString().replace(/[-:TZ.]/g, "").slice(0, 12);
const password = args.get("password") ?? "DemoPicPool2026!";

process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";

async function jsonRequest(path, body) {
  const response = await fetch(`${apiBase}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  const text = await response.text();
  const data = text ? JSON.parse(text) : null;

  if (!response.ok) {
    throw new Error(`${path} ha fallat (${response.status}): ${text}`);
  }

  return data;
}

async function registerOrLogin(user) {
  const registerResponse = await jsonRequest("/api/usuari", {
    nom: user.name,
    email: user.email,
    password,
  });

  if (registerResponse?.correcte && registerResponse.usuari?.usuariPK) {
    return {
      ...user,
      usuariPK: registerResponse.usuari.usuariPK,
    };
  }

  const loginResponse = await jsonRequest("/api/usuari/login", {
    username: user.name,
    password,
  });

  if (!loginResponse?.correcte || !loginResponse.usuariPK) {
    throw new Error(
      `No s'ha pogut crear ni iniciar sessio amb ${user.name}: ${registerResponse?.missatge ?? loginResponse?.missatge}`,
    );
  }

  return {
    ...user,
    usuariPK: loginResponse.usuariPK,
  };
}

async function createRoom(owner) {
  const roomName = `Demo Academica ${runId}`;
  const response = await jsonRequest("/api/usuari/crearSala", {
    nomSala: roomName,
    usuariPk: owner.usuariPK,
  });

  if (!response?.correcte || !response.sala?.salaPK) {
    const roomsResponse = await jsonRequest("/api/usuari/obtenirSalesUsuari", {
      usuariPk: owner.usuariPK,
      pagina: 1,
      quantitat: 50,
      ordre: "data",
      descendent: true,
    });

    const existingRoom = roomsResponse?.salas?.find(
      (room) => room.nom === roomName,
    );

    if (existingRoom?.salaPK) {
      return existingRoom;
    }

    throw new Error(response?.missatge ?? "No s'ha pogut crear la sala demo.");
  }

  return response.sala;
}

async function createShareLink(owner, room) {
  const response = await jsonRequest("/api/sala/crearLinkCompartit", {
    salaPK: room.salaPK,
    usuariCreadorPK: owner.usuariPK,
    nom: "Demo: equip amb permisos d'edicio",
    rol: "Editor",
    potVeure: true,
    potPujar: true,
    potDescarregar: true,
    potEliminarPropies: true,
    potEliminarQualsevol: false,
    potGestionarSala: false,
    dataExpiracio: null,
    limitUsos: null,
  });

  if (!response?.correcte || !response.link?.token) {
    throw new Error(response?.missatge ?? "No s'ha pogut crear el link demo.");
  }

  return response.link;
}

async function acceptLink(user, link) {
  const response = await jsonRequest("/api/sala/acceptarLinkCompartit", {
    token: link.token,
    usuariPK: user.usuariPK,
  });

  if (!response?.correcte) {
    throw new Error(
      response?.missatge ?? `No s'ha pogut afegir ${user.name} a la sala.`,
    );
  }
}

function demoSvg(user, index) {
  const title = `${user.label} - imatge ${index + 1}`;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800">
  <rect width="1200" height="800" fill="#f7f7f8"/>
  <rect x="80" y="80" width="1040" height="640" rx="18" fill="#ffffff" stroke="#111111" stroke-width="3"/>
  <text x="120" y="170" fill="#111111" font-family="Segoe UI, Arial, sans-serif" font-size="54" font-weight="700">${title}</text>
  <text x="120" y="250" fill="#4b5563" font-family="Segoe UI, Arial, sans-serif" font-size="32">PicPool demo colaborativa</text>
  <line x1="120" y1="310" x2="1080" y2="310" stroke="#d9dce1" stroke-width="3"/>
  <circle cx="${260 + index * 160}" cy="500" r="110" fill="#111111"/>
  <rect x="520" y="420" width="420" height="34" fill="#111111"/>
  <rect x="520" y="485" width="330" height="28" fill="#4b5563"/>
  <rect x="520" y="545" width="470" height="28" fill="#aeb4bc"/>
</svg>`;
}

async function uploadDemoImages(user, room, userIndex) {
  const formData = new FormData();
  formData.append("SalaPk", room.salaPK);
  formData.append("UserPk", user.usuariPK);

  for (let imageIndex = 0; imageIndex < 2; imageIndex++) {
    const name = `${user.slug}-${imageIndex + 1}.svg`;
    const blob = new Blob([demoSvg(user, imageIndex + userIndex)], {
      type: "image/svg+xml",
    });

    formData.append(`Imatges[${imageIndex}].File`, blob, name);
    formData.append(`Imatges[${imageIndex}].Mida`, String(blob.size));
    formData.append(`Imatges[${imageIndex}].Resolucio`, "0");
    formData.append(`Imatges[${imageIndex}].Nom`, name);
    formData.append(`Imatges[${imageIndex}].Descripcio`, "Imatge generada per la demo");
    formData.append(`Imatges[${imageIndex}].Propietari`, user.usuariPK);
  }

  const response = await fetch(`${apiBase}/api/sala/pujarImatges`, {
    method: "POST",
    body: formData,
  });

  const text = await response.text();
  if (!response.ok) {
    throw new Error(`/api/sala/pujarImatges ha fallat (${response.status}): ${text}`);
  }

  const data = text ? JSON.parse(text) : null;
  if (data && data.correcte === false) {
    throw new Error(data.missatge ?? "El backend ha rebutjat la pujada demo.");
  }
}

const labels = ["Aina", "Biel", "Clara", "Dídac", "Emma", "Ferran"].slice(
  0,
  userCount,
);

const users = labels.map((label, index) => ({
  label,
  slug: label
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase(),
  name: `demo_${runId}_${index + 1}_${label
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase()}`,
  email: `demo_${runId}_${index + 1}@picpool.local`,
}));

console.log(`Preparant demo ${runId} contra ${apiBase}...`);

const readyUsers = [];
for (const user of users) {
  const readyUser = await registerOrLogin(user);
  readyUsers.push(readyUser);
  console.log(`Usuari preparat: ${readyUser.label} (${readyUser.usuariPK})`);
}

const room = await createRoom(readyUsers[0]);
console.log(`Sala creada: ${room.nom ?? room.Nom ?? "Demo"} (${room.salaPK})`);

const link = await createShareLink(readyUsers[0], room);
console.log(`Link compartit creat: ${link.url}`);

for (const user of readyUsers.slice(1)) {
  await acceptLink(user, link);
  console.log(`Afegit a la sala: ${user.label}`);
}

if (!skipUpload) {
  for (const [index, user] of readyUsers.entries()) {
    await uploadDemoImages(user, room, index);
    console.log(`Imatges demo pujades: ${user.label}`);
  }
}

console.log("\nObre aquestes pestanyes per gravar la demo:\n");
for (const user of readyUsers) {
  const url = `${frontendBase}/sala?demoUserPK=${encodeURIComponent(
    user.usuariPK,
  )}&demoSalaPK=${encodeURIComponent(room.salaPK)}`;
  console.log(`${user.label.padEnd(8)} ${url}`);
}

console.log("\nAltres URLs útils:");
console.log(`Owner    ${frontendBase}/logged-home?demoUserPK=${readyUsers[0].usuariPK}`);
console.log(`Invitacio ${frontendBase}/unir-sala/${link.token}`);
console.log(`\nPassword demo: ${password}`);
