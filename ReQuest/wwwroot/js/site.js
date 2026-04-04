document.addEventListener("DOMContentLoaded", () => {
	const authStorageKey = "request.user.profile";
	const authTokenStorageKey = "request.auth.token";
	const historyStorageKey = "request.game.history";

	const authApp = document.getElementById("auth-app");
	if (authApp) {
		const form = document.getElementById("auth-form");
		const modeInput = document.getElementById("auth-mode");
		const nameInput = document.getElementById("auth-name");
		const emailInput = document.getElementById("auth-email");
		const status = document.getElementById("auth-status");
		const apiBaseUrl = authApp.getAttribute("data-api-base-url") || "http://localhost:5134";

		const rawUser = localStorage.getItem(authStorageKey);
		if (rawUser) {
			try {
				const user = JSON.parse(rawUser);
				if (nameInput && user.name) nameInput.value = user.name;
				if (emailInput && user.email) emailInput.value = user.email;
			} catch (error) {
				console.error(error);
			}
		}

		form?.addEventListener("submit", async (event) => {
			event.preventDefault();
			const mode = modeInput?.value || "login";
			const name = nameInput?.value?.trim() || "";
			const email = emailInput?.value?.trim() || "";

			if (name.length < 2 || !email.includes("@")) {
				if (status) status.textContent = "Проверь имя и email.";
				return;
			}

			if (status) status.textContent = mode === "register" ? "Регистрирую профиль..." : "Выполняю вход...";

			try {
				const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
					method: "POST",
					headers: {
						"Content-Type": "application/json"
					},
					body: JSON.stringify({ name, email })
				});

				if (!response.ok) throw new Error(`HTTP ${response.status}`);

				const payload = await response.json();
				localStorage.setItem(authStorageKey, JSON.stringify({ name: payload.name, email: payload.email }));
				localStorage.setItem(authTokenStorageKey, payload.token);

				if (status) status.textContent = mode === "register" ? "Регистрация успешна." : "Вход успешен.";
				window.location.href = "/Home/Cabinet";
			} catch (error) {
				if (status) status.textContent = "Не удалось выполнить вход. Проверь backend.";
				console.error(error);
			}
		});

		return;
	}

	const cabinetApp = document.getElementById("cabinet-app");
	if (cabinetApp) {
		const title = document.getElementById("cabinet-title");
		const rawUser = localStorage.getItem(authStorageKey);
		if (!rawUser) {
			window.location.href = "/Home/Auth";
			return;
		}

		try {
			const user = JSON.parse(rawUser);
			if (title && user.name) title.textContent = `Привет, ${user.name}!`;
		} catch (error) {
			console.error(error);
		}

		return;
	}

	const historyApp = document.getElementById("history-app");
	if (historyApp) {
		const list = document.getElementById("history-list");
		const empty = document.getElementById("history-empty");
		const raw = localStorage.getItem(historyStorageKey);
		if (!raw) return;

		try {
			const history = JSON.parse(raw);
			if (!Array.isArray(history) || history.length === 0) return;
			if (empty) empty.style.display = "none";

			history.slice(0, 25).forEach((entry) => {
				const item = document.createElement("li");
				item.textContent = `${entry.date} | Код ${entry.code} | ${entry.questionsCount} вопросов | ${entry.hostName}`;
				list?.appendChild(item);
			});
		} catch (error) {
			console.error(error);
		}

		return;
	}

	const appRoot = document.getElementById("quest-app");
	if (!appRoot) return;

	const createForm = document.getElementById("create-game-form");
	const createButton = createForm?.querySelector("button[type='submit']");
	const openCreateButton = document.getElementById("open-create");
	const createPanel = document.getElementById("create-panel");
	const createSide = document.getElementById("create-side");
	const gameWorkspace = document.getElementById("game-workspace");
	const status = document.getElementById("status");
	const lobbyCode = document.getElementById("lobby-code");
	const lobbyHost = document.getElementById("lobby-host");
	const lobbyQuestions = document.getElementById("lobby-questions");
	const playersList = document.getElementById("players-list");
	const generationPreview = document.getElementById("generation-preview");
	const apiBaseUrl = appRoot.getAttribute("data-api-base-url") || "http://localhost:5134";
	const authToken = localStorage.getItem(authTokenStorageKey);
	let currentCode = null;
	let lobbyTimerId = null;
    const hostStorageKey = "request.host.registration";

	const setStatus = (text, isError = false) => {
		if (!status) return;
		status.textContent = text;
		status.classList.toggle("status--error", isError);
	};

	const buildCreatePayload = () => {
		const hostNameInput = document.getElementById("host-name");
		const hostEmailInput = document.getElementById("host-email");
		const countInput = document.getElementById("count");
		const difficultyInput = document.getElementById("difficulty");
		const choiceInput = document.getElementById("choice");

		const hostName = hostNameInput?.value?.trim() || "";
		const hostEmail = hostEmailInput?.value?.trim() || "";
		const count = Number.parseInt(countInput?.value || "0", 10);
		const difficulty = difficultyInput?.value || null;
		const choice = choiceInput?.value || null;

		return {
			hostName,
			hostEmail,
			count,
			difficulty,
			choice
		};
	};

	const buildJoinPayload = () => {
		const codeInput = document.getElementById("join-code");
		const playerNameInput = document.getElementById("player-name");

		return {
			code: codeInput?.value?.trim() || "",
			playerName: playerNameInput?.value?.trim() || ""
		};
	};

	const renderLobby = (lobby) => {
		if (!lobbyCode || !lobbyHost || !lobbyQuestions || !playersList) return;

		lobbyCode.textContent = `Код: ${lobby.code}`;
		lobbyHost.textContent = `Ведущий: ${lobby.hostName}`;
		lobbyQuestions.textContent = `Вопросов: ${lobby.questionsCount}`;

		playersList.innerHTML = "";
		(lobby.players || []).forEach((player) => {
			const item = document.createElement("li");
			item.textContent = player;
			playersList.appendChild(item);
		});
	};

	const stopLobbyPolling = () => {
		if (!lobbyTimerId) return;
		clearInterval(lobbyTimerId);
		lobbyTimerId = null;
	};

	const startLobbyPolling = () => {
		stopLobbyPolling();
		if (!currentCode) return;

		lobbyTimerId = setInterval(async () => {
			try {
				const response = await fetch(`${apiBaseUrl}/api/game/lobby/${currentCode}`);
				if (!response.ok) return;
				const data = await response.json();
				renderLobby(data);
			} catch (error) {
				console.error(error);
			}
		}, 3000);
	};

	const resetTracker = () => {
		if (generationPreview) generationPreview.innerHTML = "";
	};

	const renderPlaceholders = (total) => {
		if (!generationPreview) return;
		generationPreview.innerHTML = "";
		for (let i = 0; i < total; i += 1) {
			const placeholder = document.createElement("div");
			placeholder.className = "preview-placeholder";
			placeholder.dataset.index = String(i);
			generationPreview.appendChild(placeholder);
		}
	};

	const loadLobbyByCode = async (code) => {
		if (!code) return;

		const response = await fetch(`${apiBaseUrl}/api/game/lobby/${code}`);
		if (!response.ok) throw new Error(`HTTP ${response.status}`);

		const lobby = await response.json();
		currentCode = lobby.code;
		renderLobby(lobby);
		startLobbyPolling();
		setStatus(`Подключено к игре ${lobby.code}`);
	};

	const setPreviewQuestion = (index, questionText, category) => {
		if (!generationPreview) return;

		const target = generationPreview.querySelector(`[data-index='${index}']`);
		if (!target) return;

		const item = document.createElement("div");
		item.className = "preview-item";
		item.dataset.index = String(index);

		const categoryTag = document.createElement("span");
		categoryTag.className = "preview-item__category";
		categoryTag.textContent = category || "Вопрос";

		const questionNode = document.createElement("div");
		questionNode.className = "preview-item__question";
		questionNode.textContent = questionText || "Вопрос создан";

		item.appendChild(categoryTag);
		item.appendChild(questionNode);
		target.replaceWith(item);
	};

	const showMode = (mode) => {
		if (mode === "create") {
			createPanel?.classList.remove("hidden");
			createPanel?.classList.remove("is-active");
			createPanel?.classList.remove("read-only");
			createSide?.classList.remove("hidden");
			gameWorkspace?.classList.remove("is-active");
		}
	};

	const loadHostRegistration = () => {
		const hostNameInput = document.getElementById("host-name");
		const hostEmailInput = document.getElementById("host-email");
		const raw = localStorage.getItem(hostStorageKey);
		const rawUser = localStorage.getItem(authStorageKey);

		try {
			if (rawUser) {
				const user = JSON.parse(rawUser);
				if (hostNameInput && user.name) hostNameInput.value = user.name;
				if (hostEmailInput && user.email) hostEmailInput.value = user.email;
			}

			if (raw) {
				const parsed = JSON.parse(raw);
				if (hostNameInput && parsed.hostName) hostNameInput.value = parsed.hostName;
				if (hostEmailInput && parsed.hostEmail) hostEmailInput.value = parsed.hostEmail;
			}
		} catch (error) {
			console.error(error);
		}
	};

	const saveHostRegistration = (hostName, hostEmail) => {
		localStorage.setItem(hostStorageKey, JSON.stringify({ hostName, hostEmail }));
	};

	const addHistoryEntry = (entry) => {
		let history = [];
		const raw = localStorage.getItem(historyStorageKey);
		if (raw) {
			try {
				history = JSON.parse(raw);
			} catch (error) {
				console.error(error);
			}
		}

		history.unshift(entry);
		localStorage.setItem(historyStorageKey, JSON.stringify(history.slice(0, 100)));
	};

	const parseSseChunk = (chunk, onEvent) => {
		const blocks = chunk.split("\n\n");
		for (let i = 0; i < blocks.length - 1; i += 1) {
			const block = blocks[i];
			if (!block.trim()) continue;

			let eventName = "message";
			let data = "";

			block.split("\n").forEach((line) => {
				if (line.startsWith("event:")) eventName = line.slice(6).trim();
				if (line.startsWith("data:")) data += line.slice(5).trim();
			});

			if (!data) continue;

			try {
				onEvent(eventName, JSON.parse(data));
			} catch (error) {
				console.error("SSE parse error", error);
			}
		}

		return blocks[blocks.length - 1];
	};

	const createGameWithStream = async (payload) => {
		const response = await fetch(`${apiBaseUrl}/api/game/create`, {
			method: "POST",
			headers: {
				"Content-Type": "application/json",
				"Accept": "text/event-stream",
				"Authorization": `Bearer ${localStorage.getItem(authTokenStorageKey) || ""}`
			},
			body: JSON.stringify(payload)
		});

		if (!response.ok) throw new Error(`HTTP ${response.status}`);
		if (!response.body) throw new Error("Пустой stream-ответ от сервера.");

		const reader = response.body.getReader();
		const decoder = new TextDecoder();
		let tail = "";
		let completedLobby = null;
		let streamError = null;

		while (true) {
			const { value, done } = await reader.read();
			if (done) break;

			tail += decoder.decode(value, { stream: true });
			tail = parseSseChunk(tail, (eventName, payloadData) => {
				if (eventName === "progress") {
					if (payloadData.stage === "fetched") {
						renderPlaceholders(payloadData.total || 0);
					}

					if (payloadData.stage === "progress") {
						setPreviewQuestion(
							Math.max((payloadData.created || 1) - 1, 0),
							payloadData.questionText,
							payloadData.category
						);
					} else if (payloadData.stage === "error") {
						setStatus(payloadData.message || "Ошибка генерации.", true);
					}
				}

				if (eventName === "error") {
					streamError = payloadData.message || "Ошибка stream-генерации.";
				}

				if (eventName === "completed") {
					completedLobby = payloadData;
				}
			});
		}

		if (streamError) throw new Error(streamError);
		if (!completedLobby) throw new Error("Сервер не прислал финальное событие completed.");

		return completedLobby;
	};

	createForm?.addEventListener("submit", async (event) => {
		event.preventDefault();
		const payload = buildCreatePayload();
        resetTracker();

		if (!localStorage.getItem(authTokenStorageKey)) {
			setStatus("Сначала войдите или зарегистрируйтесь, чтобы создать игру.", true);
			return;
		}

		if (!payload.hostName || payload.hostName.length < 2) {
			setStatus("Имя ведущего должно быть минимум 2 символа.", true);
			return;
		}

		if (!payload.hostEmail || !payload.hostEmail.includes("@")) {
			setStatus("Нужен корректный email ведущего.", true);
			return;
		}

		if (!Number.isInteger(payload.count) || payload.count < 1 || payload.count > 50) {
			setStatus("Количество вопросов должно быть от 1 до 50.", true);
			return;
		}

		createPanel?.classList.add("is-active");
		createPanel?.classList.remove("read-only");
		createSide?.classList.remove("hidden");
		gameWorkspace?.classList.add("is-active");
		setStatus("Создаю игру и код подключения...");
		if (createButton) createButton.disabled = true;

		try {
			saveHostRegistration(payload.hostName, payload.hostEmail);
			setStatus("Игра создается...");
			const lobby = await createGameWithStream(payload);
			addHistoryEntry({
				hostName: payload.hostName,
				questionsCount: lobby.questionsCount,
				code: lobby.code,
				date: new Date().toLocaleString("ru-RU")
			});
			currentCode = lobby.code;
			renderLobby(lobby);
			startLobbyPolling();
			setStatus(`Игра создана. Код: ${lobby.code}`);
		} catch (error) {
			setStatus("Ошибка создания игры. Проверь backend и базу данных.", true);
			console.error(error);
		} finally {
			if (createButton) createButton.disabled = false;
		}
	});

	openCreateButton?.addEventListener("click", () => showMode("create"));
	loadHostRegistration();
	const joinedCode = new URLSearchParams(window.location.search).get("code");

	if (joinedCode) {
		createPanel?.classList.remove("hidden");
		createPanel?.classList.add("is-active", "read-only");
		createSide?.classList.remove("hidden");
		gameWorkspace?.classList.add("is-active");
		if (createButton) createButton.disabled = true;
		void loadLobbyByCode(joinedCode).catch((error) => {
			setStatus("Не удалось загрузить лобби.", true);
			console.error(error);
		});
	} else if (createButton && !authToken) {
		createButton.disabled = true;
		setStatus("Для создания игры нужна авторизация.", true);
		showMode("create");
	} else {
		showMode("create");
	}

	const joinHomeApp = document.getElementById("join-home-app");
	if (joinHomeApp) {
		const joinHomeForm = document.getElementById("join-home-form");
		const joinHomeCodeInput = document.getElementById("join-home-code");
		const joinHomePlayerNameInput = document.getElementById("join-home-player-name");
		const joinHomeStatus = document.getElementById("join-home-status");

		const showHomeStatus = (text, isError = false) => {
			if (joinHomeStatus) {
				joinHomeStatus.textContent = text;
				joinHomeStatus.classList.toggle("status--error", isError);
				return;
			}

			setStatus(text, isError);
		};

		joinHomeForm?.addEventListener("submit", async (event) => {
			event.preventDefault();
			const code = joinHomeCodeInput?.value?.trim() || "";
			const playerName = joinHomePlayerNameInput?.value?.trim() || "";

			if (!/^\d{6}$/.test(code)) {
				showHomeStatus("Код игры должен состоять из 6 цифр.", true);
				return;
			}

			if (!playerName || playerName.length < 2) {
				showHomeStatus("Имя игрока должно быть минимум 2 символа.", true);
				return;
			}

			showHomeStatus("Подключаю к игре...");

			try {
				const response = await fetch(`${apiBaseUrl}/api/game/join`, {
					method: "POST",
					headers: {
						"Content-Type": "application/json"
					},
					body: JSON.stringify({ code, playerName })
				});

				if (!response.ok) throw new Error(`HTTP ${response.status}`);

				const lobby = await response.json();
				window.location.href = `/Home/Game?code=${encodeURIComponent(lobby.code)}`;
			} catch (error) {
				showHomeStatus("Не удалось подключиться. Проверь код игры.", true);
				console.error(error);
			}
		});

		joinHomeCodeInput?.addEventListener("input", () => {
			joinHomeCodeInput.value = joinHomeCodeInput.value.replace(/\D/g, "").slice(0, 6);
		});
	}
});
