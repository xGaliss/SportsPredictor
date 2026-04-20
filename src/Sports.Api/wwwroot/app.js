const teamsCountEl = document.getElementById("teamsCount");
const gamesCountEl = document.getElementById("gamesCount");
const predictionsCountEl = document.getElementById("predictionsCount");
const lastSyncEl = document.getElementById("lastSync");
const statusLineEl = document.getElementById("statusLine");
const apiBadgeEl = document.getElementById("apiBadge");

const gamesEmptyEl = document.getElementById("gamesEmpty");
const gamesListEl = document.getElementById("gamesList");

const predictionsEmptyEl = document.getElementById("predictionsEmpty");
const predictionsListEl = document.getElementById("predictionsList");

const refreshBtn = document.getElementById("refreshBtn");
const syncBtn = document.getElementById("syncBtn");

async function getJson(url, options) {
    const response = await fetch(url, options);
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }
    return response.json();
}

function formatDateTime(value) {
    if (!value) return "-";
    const date = new Date(value);
    return new Intl.DateTimeFormat("es-ES", {
        dateStyle: "short",
        timeStyle: "short"
    }).format(date);
}

function formatPercent(value) {
    const number = Number(value);
    if (Number.isNaN(number)) return "-";
    return `${number.toFixed(1)}%`;
}

function renderStatus(status) {
    teamsCountEl.textContent = status.teamsCount ?? "-";
    gamesCountEl.textContent = status.gamesWindowCount ?? "-";
    predictionsCountEl.textContent = status.predictionsWindowCount ?? "-";
    lastSyncEl.textContent = formatDateTime(status.lastSyncUtc);

    apiBadgeEl.textContent = status.providerConfigured ? "API lista" : "API sin key";
    apiBadgeEl.className = `badge ${status.providerConfigured ? "badge-ok" : "badge-warn"}`;

    statusLineEl.textContent =
        `Ventana ${status.windowStart} → ${status.windowEnd}. ` +
        `Proveedor: ${status.providerBaseUrl}. ` +
        `${status.useMockFallbackData ? "Fallback mock activo." : "Fallback mock desactivado."}`;
}

function renderGames(games) {
    if (!Array.isArray(games) || games.length === 0) {
        gamesEmptyEl.classList.remove("hidden");
        gamesListEl.classList.add("hidden");
        gamesListEl.innerHTML = "";
        return;
    }

    gamesEmptyEl.classList.add("hidden");
    gamesListEl.classList.remove("hidden");

    gamesListEl.innerHTML = games.map(game => `
    <article class="item-card">
      <div class="item-top">
        <div>
          <strong>${game.awayTeam?.name ?? "Away"} @ ${game.homeTeam?.name ?? "Home"}</strong>
          <div class="muted small">${game.sportsDateLocal ?? "-"} · ${game.localTime ?? "-"}</div>
        </div>
        <span class="pill">${game.status ?? "-"}</span>
      </div>

      <div class="score-row">
        <div class="team-line">
          <span>${game.awayTeam?.abbreviation ?? "-"}</span>
          <strong>${game.awayScore ?? "-"}</strong>
        </div>
        <div class="team-line">
          <span>${game.homeTeam?.abbreviation ?? "-"}</span>
          <strong>${game.homeScore ?? "-"}</strong>
        </div>
      </div>
    </article>
  `).join("");
}

function renderPredictions(predictions) {
    if (!Array.isArray(predictions) || predictions.length === 0) {
        predictionsEmptyEl.classList.remove("hidden");
        predictionsListEl.classList.add("hidden");
        predictionsListEl.innerHTML = "";
        return;
    }

    predictionsEmptyEl.classList.add("hidden");
    predictionsListEl.classList.remove("hidden");

    predictionsListEl.innerHTML = predictions.map(prediction => `
    <article class="item-card">
      <div class="item-top">
        <div>
          <strong>${prediction.awayTeam?.name ?? "Away"} @ ${prediction.homeTeam?.name ?? "Home"}</strong>
          <div class="muted small">${prediction.sportsDateLocal ?? "-"} · ${prediction.localTime ?? "-"}</div>
        </div>
      </div>

      <div class="prediction-grid">
        <div class="prediction-side">
          <span>${prediction.homeTeam?.abbreviation ?? "HOME"}</span>
          <strong>${formatPercent(prediction.homeWinProbability)}</strong>
        </div>

        <div class="prediction-side">
          <span>${prediction.awayTeam?.abbreviation ?? "AWAY"}</span>
          <strong>${formatPercent(prediction.awayWinProbability)}</strong>
        </div>
      </div>

      <p class="muted">${prediction.summary ?? ""}</p>
    </article>
  `).join("");
}

async function refreshAll() {
    refreshBtn.disabled = true;
    syncBtn.disabled = true;

    try {
        const [status, games, predictions] = await Promise.all([
            getJson("/api/status"),
            getJson("/api/games/window"),
            getJson("/api/predictions/window")
        ]);

        renderStatus(status);
        renderGames(games);
        renderPredictions(predictions);
    } catch (error) {
        statusLineEl.textContent = `Error cargando datos: ${error.message}`;
    } finally {
        refreshBtn.disabled = false;
        syncBtn.disabled = false;
    }
}

async function forceSync() {
    syncBtn.disabled = true;
    refreshBtn.disabled = true;
    statusLineEl.textContent = "Lanzando sync manual...";

    try {
        await getJson("/api/admin/sync/now", { method: "POST" });
        await refreshAll();
    } catch (error) {
        statusLineEl.textContent = `Error en sync manual: ${error.message}`;
        syncBtn.disabled = false;
        refreshBtn.disabled = false;
    }
}

refreshBtn.addEventListener("click", refreshAll);
syncBtn.addEventListener("click", forceSync);

refreshAll();