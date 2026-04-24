document.addEventListener("DOMContentLoaded", () => {
    const defaultApiBaseUrl = 'http://localhost:5134';
    const authStorageKey = "request.user.profile";
    const authTokenStorageKey = "request.auth.token";
    const authModeStorageKey = "request.auth.mode";
    const historyStorageKey = "request.game.history";

    const authApp = document.getElementById("auth-app");
    if (authApp) {
        const apiBaseUrl = defaultApiBaseUrl;
        const authForms = Array.from(authApp.querySelectorAll("form[data-auth-form]"));
        const authCards = Array.from(authApp.querySelectorAll("[data-auth-card]"));
        const authSwitches = Array.from(authApp.querySelectorAll("[data-auth-switch]"));

        const setAuthMode = (mode) => {
            const nextMode = mode === "register" ? "register" : "login";
            localStorage.setItem(authModeStorageKey, nextMode);

            authCards.forEach((card) => {
                const cardMode = card.getAttribute("data-auth-card");
                const isActive = cardMode === nextMode;
                card.classList.toggle("auth-card--hidden", !isActive);
                card.setAttribute("aria-hidden", isActive ? "false" : "true");
            });
        };

        authSwitches.forEach((link) => {
            link.addEventListener("click", (event) => {
                event.preventDefault();
                setAuthMode(link.getAttribute("data-auth-switch") || "login");
            });
        });

        setAuthMode(localStorage.getItem(authModeStorageKey) || "login");

        const rawUser = localStorage.getItem(authStorageKey);
        if (rawUser) {
            try {
                const user = JSON.parse(rawUser);
                authForms.forEach((form) => {
                    const nameInput = form.querySelector("[data-auth-field='name']");
                    const emailInput = form.querySelector("[data-auth-field='email']");
                    if (nameInput && user.name) nameInput.value = user.name;
                    if (emailInput && user.email) emailInput.value = user.email;
                });
            } catch (error) {
                console.error(error);
            }
        }

        authForms.forEach((form) => {
            const mode = form.getAttribute("data-auth-form") || "login";
            const nameInput = form.querySelector("[data-auth-field='name']");
            const emailInput = form.querySelector("[data-auth-field='email']");
            const status = form.querySelector(".auth-status");

            form.addEventListener("submit", async (event) => {
                event.preventDefault();
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
                        body: JSON.stringify({name, email})
                    });

                    if (!response.ok) throw new Error(`HTTP ${response.status}`);

                    const payload = await response.json();
                    localStorage.setItem(authStorageKey, JSON.stringify({name: payload.name, email: payload.email}));
                    localStorage.setItem(authTokenStorageKey, payload.token);

                    if (status) status.textContent = mode === "register" ? "Регистрация успешна." : "Вход успешен.";
                    setAuthMode("login");
                    window.location.href = "/Home/Cabinet";
                } catch (error) {
                    if (status) status.textContent = "Не удалось выполнить вход. Проверь backend.";
                    console.error(error);
                }
            });
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

    const joinHomeApp = document.getElementById("join-home-app");
    if (joinHomeApp) {
        const joinHomeForm = document.getElementById("join-home-form");
        const joinHomeCodeInput = document.getElementById("join-home-code");
        const joinHomePlayerNameInput = document.getElementById("join-home-player-name");
        const joinHomeStatus = document.getElementById("join-home-status");
        const joinApiBaseUrl = defaultApiBaseUrl;

        const showHomeStatus = (text, isError = false) => {
            if (!joinHomeStatus) return;
            joinHomeStatus.textContent = text;
            joinHomeStatus.classList.toggle("status--error", isError);
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
                const response = await fetch(`${joinApiBaseUrl}/api/game/join`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        code: String(code),
                        playerName: playerName
                    })
                });

                if (!response.ok) throw new Error(`HTTP ${response.status}`);

                const lobby = await response.json();
                window.location.href = `/Home/Game?code=${encodeURIComponent(lobby.code)}&playerName=${encodeURIComponent(playerName)}`;
            } catch (error) {
                showHomeStatus("Не удалось подключиться. Проверь код игры.", true);
                console.error(error);
            }
        });

        joinHomeCodeInput?.addEventListener("input", () => {
            joinHomeCodeInput.value = joinHomeCodeInput.value.replace(/\D/g, "").slice(0, 6);
        });
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
    const controlPanel = document.getElementById("game-control-panel");
    const controlGameCode = document.getElementById("control-game-code");
    const controlStartGameButton = document.getElementById("control-start-game");
    const controlNextQuestionButton = document.getElementById("control-next-question");
    const controlFinishGameButton = document.getElementById("control-finish-game");
    const adminPlayersList = document.getElementById("admin-players-list");
    const adminQuestionsList = document.getElementById("admin-questions-list");
    const gamePanel = document.getElementById("game-panel");
    const gamePanelTitle = document.getElementById("game-panel-title");
    const gameStatus = document.getElementById("game-status");
    const gameProgress = document.getElementById("game-progress");
    const gameTimer = document.getElementById("game-timer");
    const gameQuestion = document.getElementById("game-question");
    const gameAnswers = document.getElementById("game-answers");
    const gameScoreboard = document.getElementById("game-scoreboard");
    const gameResult = document.getElementById("game-result");
    const kickedBanner = document.getElementById("kicked-banner");
    const apiBaseUrl = defaultApiBaseUrl;
    const authToken = localStorage.getItem(authTokenStorageKey);
    let currentCode = null;
    let lobbyTimerId = null;
    let gameStateTimerId = null;
    const hostStorageKey = "request.host.registration";
    let gameQuestions = [];
    let gameState = null;
    let localAnswerByQuestionIndex = {};
    let currentPlayerName = "";
    let questionTimerId = null;
    let isKickedFromGame = false;

    const getIsHost = () => !!gameState && sameName(gameState.hostName, currentPlayerName);

    const getQuestionDeadlineMs = () => {
        if (!gameState?.questionStartedAt || !gameState?.questionTimeLimitSeconds) return null;
        const started = Date.parse(gameState.questionStartedAt);
        if (Number.isNaN(started)) return null;
        return started + (gameState.questionTimeLimitSeconds * 1000);
    };

    const getIsCurrentPlayerInSession = () => {
        if (!gameState || !currentPlayerName) return false;
        if (getIsHost()) return true;
        return (gameState.players || []).some((player) => sameName(player, currentPlayerName));
    };

    const stopQuestionTimer = () => {
        if (!questionTimerId) return;
        clearInterval(questionTimerId);
        questionTimerId = null;
    };

    const renderWaitingQuestion = () => {
        if (!gameQuestion) return;
        gameQuestion.classList.add("game-question--waiting");
        gameQuestion.innerHTML = "Ожидаем старт игры от ведущего<span class='waiting-dots' aria-hidden='true'></span>";
    };

    const setTimerValue = () => {
        if (!gameTimer || !gameState?.isStarted || gameState.isFinished || getIsHost()) {
            gameTimer?.classList.add("hidden");
            stopQuestionTimer();
            return;
        }

        const deadlineMs = getQuestionDeadlineMs();
        if (!deadlineMs) {
            gameTimer.classList.add("hidden");
            return;
        }

        const remaining = Math.max(0, Math.ceil((deadlineMs - Date.now()) / 1000));
        gameTimer.textContent = `Осталось: ${remaining}с`;
        gameTimer.classList.remove("hidden");
        gameTimer.classList.toggle("game-timer--danger", remaining <= 5);
    };

    const startQuestionTimer = () => {
        stopQuestionTimer();
        if (getIsHost()) return;

        questionTimerId = setInterval(() => {
            setTimerValue();
            renderQuestion();
        }, 400);
    };

    const showKickedState = () => {
        isKickedFromGame = true;
        stopQuestionTimer();
        if (gameTimer) gameTimer.classList.add("hidden");
        if (kickedBanner) kickedBanner.classList.remove("hidden");
        if (gameAnswers) gameAnswers.innerHTML = "";
        if (gameQuestion) {
            gameQuestion.classList.remove("game-question--waiting");
            gameQuestion.textContent = "Доступ к сессии закрыт.";
        }
        if (gameResult) gameResult.textContent = "Вы были исключены из игры.";
        setGameStatus("Вы были исключены из игры", true);
    };

    const setRoleLayout = () => {
        const isHost = getIsHost();
        if (createPanel) createPanel.classList.toggle("hidden", !isHost && !!currentCode);
        if (gameWorkspace) gameWorkspace.classList.toggle("game-workspace--player", !isHost && !!currentCode);
        if (gamePanelTitle) gamePanelTitle.textContent = isHost ? "Раунд" : "Вопрос";
    };

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

    const renderLobby = (lobby) => {
        if (!lobbyCode || !lobbyHost || !lobbyQuestions || !playersList) return;

        lobbyCode.textContent = `Код: ${lobby.code}`;
        lobbyHost.textContent = `Ведущий: ${lobby.hostName}`;
        lobbyQuestions.textContent = `Вопросов: ${lobby.questionsCount}`;

        playersList.innerHTML = "";
        (lobby.players || []).filter((player) => !sameName(player, lobby.hostName)).forEach((player) => {
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
        await refreshGameQuestions(lobby.code);
        await refreshGameState();
        startGameStatePolling();
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

    const setGameStatus = (text, isError = false) => {
        if (!gameStatus) return;
        gameStatus.textContent = text;
        gameStatus.classList.toggle("status--error", isError);
    };

    const shuffleAnswers = (answers) => {
        const result = [...answers];
        for (let i = result.length - 1; i > 0; i -= 1) {
            const j = Math.floor(Math.random() * (i + 1));
            [result[i], result[j]] = [result[j], result[i]];
        }
        return result;
    };

    const prepareQuestions = (questions) => {
        return questions.map((question) => ({
            ...question,
            shuffledAnswers: shuffleAnswers([question.correctAnswer, ...(question.incorrectAnswers || [])])
        }));
    };

    const sameName = (left, right) => {
        return (left || "").trim().toLowerCase() === (right || "").trim().toLowerCase();
    };

    const getPlayerName = () => {
        const fromQuery = new URLSearchParams(window.location.search).get("playerName")?.trim();
        if (fromQuery) return fromQuery;

        const rawUser = localStorage.getItem(authStorageKey);
        if (!rawUser) return "";

        try {
            const user = JSON.parse(rawUser);
            return user?.name?.trim() || "";
        } catch {
            return "";
        }
    };

    const setControlButtons = () => {
        const isHost = getIsHost();
        const answeredPlayers = new Set((gameState?.answeredPlayers || []).map((player) => player.trim().toLowerCase()));
        const participants = (gameState?.players || []).filter((player) => !sameName(player, gameState?.hostName));
        const allParticipantsAnswered = participants.length === 0 || participants.every((player) => answeredPlayers.has(player.trim().toLowerCase()));
        const deadlineMs = getQuestionDeadlineMs();
        const timeExpired = typeof deadlineMs === "number" && Date.now() >= deadlineMs;

        if (controlStartGameButton) {
            controlStartGameButton.disabled = !isHost || !gameState || gameState.isStarted;
        }

        if (controlNextQuestionButton) {
            controlNextQuestionButton.disabled = !isHost || !gameState || !gameState.isStarted || gameState.isFinished || (!allParticipantsAnswered && !timeExpired);
        }

        if (controlFinishGameButton) {
            controlFinishGameButton.disabled = !isHost || !gameState || !gameState.isStarted || gameState.isFinished;
        }
    };

    const ensureHostAction = () => {
        if (!currentCode || !currentPlayerName || !gameState) return false;
        if (!getIsHost()) {
            setGameStatus("Эта вкладка доступна только ведущему.", true);
            return false;
        }
        return true;
    };

    const renderAdminPlayers = () => {
        if (!adminPlayersList || !gameState) return;
        adminPlayersList.innerHTML = "";

        const participants = (gameState.players || []).filter((player) => !sameName(player, gameState.hostName));
        if (participants.length === 0) {
            const empty = document.createElement("li");
            empty.textContent = "Пока нет участников для кика.";
            adminPlayersList.appendChild(empty);
            return;
        }

        participants.forEach((player) => {
            const item = document.createElement("li");
            item.className = "admin-list-item";

            const label = document.createElement("span");
            label.textContent = player;

            const kickButton = document.createElement("button");
            kickButton.type = "button";
            kickButton.className = "btn-danger";
            kickButton.textContent = "Кик";
            kickButton.addEventListener("click", () => {
                void kickPlayer(player);
            });

            item.appendChild(label);
            item.appendChild(kickButton);
            adminPlayersList.appendChild(item);
        });
    };

    const renderAdminQuestions = () => {
        if (!adminQuestionsList || !gameState) return;
        adminQuestionsList.innerHTML = "";

        if (!gameQuestions.length) {
            const empty = document.createElement("li");
            empty.textContent = "Список вопросов загружается...";
            adminQuestionsList.appendChild(empty);
            return;
        }

        gameQuestions.forEach((question, index) => {
            const item = document.createElement("li");
            item.className = "admin-list-item";

            const label = document.createElement("span");
            const mark = gameState.currentQuestionIndex === index ? " (текущий)" : "";
            label.textContent = `${index + 1}. ${question.question}${mark}`;

            const removeButton = document.createElement("button");
            removeButton.type = "button";
            removeButton.className = "btn-danger";
            removeButton.textContent = "Убрать";
            removeButton.disabled = gameQuestions.length <= 1;
            removeButton.addEventListener("click", () => {
                void removeQuestion(index);
            });

            item.appendChild(label);
            item.appendChild(removeButton);
            adminQuestionsList.appendChild(item);
        });
    };

    const renderScoreboard = () => {
        if (!gameScoreboard || !gameState) return;
        gameScoreboard.innerHTML = "";

        const sortedPlayers = [...(gameState.players || [])]
            .filter((player) => !sameName(player, gameState.hostName))
            .sort((a, b) => {
                const scoreA = gameState.scores?.[a] || 0;
                const scoreB = gameState.scores?.[b] || 0;
                return scoreB - scoreA;
            });

        sortedPlayers.forEach((player) => {
            const item = document.createElement("li");
            const score = gameState.scores?.[player] || 0;
            item.textContent = `${player} - ${score}`;
            gameScoreboard.appendChild(item);
        });
    };

    const refreshGameQuestions = async (code) => {
        if (!code) return;

        const response = await fetch(`${apiBaseUrl}/api/game/questions/${encodeURIComponent(code)}`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const questions = await response.json();
        if (!Array.isArray(questions)) {
            gameQuestions = [];
            return;
        }

        gameQuestions = prepareQuestions(questions);
    };

    const submitAnswer = async (answer) => {
        if (!gameState || !currentCode || !currentPlayerName) return;

        const questionIndex = gameState.currentQuestionIndex;
        if (questionIndex < 0) return;

        try {
            await fetch(`${apiBaseUrl}/api/game/answer`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    code: currentCode,
                    playerName: currentPlayerName,
                    answer
                })
            });

            localAnswerByQuestionIndex[questionIndex] = answer;
            await refreshGameState();
        } catch (error) {
            setGameStatus("Не удалось отправить ответ.", true);
            console.error(error);
        }
    };

    const kickPlayer = async (playerName) => {
        if (!ensureHostAction()) return;

        try {
            const response = await fetch(`${apiBaseUrl}/api/game/kick`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    code: currentCode,
                    hostName: currentPlayerName,
                    playerName
                })
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            await refreshGameState();
            setStatus(`Игрок ${playerName} удален из лобби.`);
        } catch (error) {
            setStatus("Не удалось кикнуть участника.", true);
            console.error(error);
        }
    };

    const removeQuestion = async (questionIndex) => {
        if (!ensureHostAction()) return;

        try {
            const response = await fetch(`${apiBaseUrl}/api/game/remove-question`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    code: currentCode,
                    hostName: currentPlayerName,
                    questionIndex
                })
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            await refreshGameQuestions(currentCode);
            await refreshGameState();
            setStatus("Вопрос убран из игры.");
        } catch (error) {
            setStatus("Не удалось убрать вопрос.", true);
            console.error(error);
        }
    };

    const triggerFinishGame = async () => {
        if (!ensureHostAction()) return;

        try {
            const response = await fetch(`${apiBaseUrl}/api/game/finish`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({code: currentCode, hostName: currentPlayerName})
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            await refreshGameState();
        } catch (error) {
            setGameStatus("Не удалось завершить игру.", true);
            console.error(error);
        }
    };

    const renderQuestion = () => {
        if (!gameState) return;

        const questionIndex = gameState.currentQuestionIndex;
        const total = gameState.questionsCount || 0;
        if (questionIndex < 0 || questionIndex >= total || total === 0) {
            if (gameProgress) gameProgress.textContent = "Вопрос 0/0";
            renderWaitingQuestion();
            if (gameAnswers) gameAnswers.innerHTML = "";
            return;
        }

        const question = gameQuestions[questionIndex];
        if (!question) {
            if (gameProgress) gameProgress.textContent = `Вопрос ${questionIndex + 1}/${total}`;
            if (gameQuestion) {
                gameQuestion.classList.remove("game-question--waiting");
                gameQuestion.textContent = "Вопрос загружается...";
            }
            if (gameAnswers) gameAnswers.innerHTML = "";
            return;
        }

        if (gameProgress) gameProgress.textContent = `Вопрос ${questionIndex + 1}/${total}`;
        if (gameQuestion) {
            gameQuestion.classList.remove("game-question--waiting");
            gameQuestion.textContent = question.question;
        }
        if (gameAnswers) gameAnswers.innerHTML = "";

        const currentPlayerAnswered = (gameState.answeredPlayers || []).some((player) => sameName(player, currentPlayerName));
        const selectedAnswer = localAnswerByQuestionIndex[questionIndex] || "";
        const deadlineMs = getQuestionDeadlineMs();
        const timeExpired = !getIsHost() && typeof deadlineMs === "number" && Date.now() >= deadlineMs;

        (question.shuffledAnswers || []).forEach((answer) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "answer-btn";
            button.textContent = answer;

            button.disabled = currentPlayerAnswered || gameState.isFinished || timeExpired;

            if (currentPlayerAnswered && selectedAnswer) {
                if (answer === question.correctAnswer) button.classList.add("is-correct");
                if (answer === selectedAnswer && answer !== question.correctAnswer) button.classList.add("is-wrong");
            }

            button.addEventListener("click", () => {
                if (button.disabled) return;
                void submitAnswer(answer);
            });

            gameAnswers?.appendChild(button);
        });
    };

    const renderGameState = () => {
        if (!gameState) return;
        setRoleLayout();

        if (isKickedFromGame) {
            showKickedState();
            return;
        }

        const isHost = getIsHost();
        const isCurrentPlayerInSession = getIsCurrentPlayerInSession();
        if (!isHost && currentPlayerName && !isCurrentPlayerInSession) {
            showKickedState();
            return;
        }

        if (kickedBanner) kickedBanner.classList.add("hidden");

        if (controlPanel) {
            controlPanel.classList.toggle("hidden", !getIsHost());
        }

        if (controlGameCode) controlGameCode.textContent = `Код: ${gameState.code}`;
        gamePanel?.classList.remove("hidden");
        renderScoreboard();
        renderAdminPlayers();
        renderAdminQuestions();
        setControlButtons();

        if (!currentPlayerName) {
            setGameStatus("Имя игрока не определено. Подключись заново с главной страницы.", true);
        }

        if (!gameState.isStarted) {
            if (gameResult) gameResult.textContent = "";
            renderWaitingQuestion();
            if (!isHost) {
                setGameStatus("Ожидаем старт игры от ведущего.");
                if (gameTimer) gameTimer.classList.add("hidden");
            } else {
                setGameStatus("Лобби собрано. Ждем старта.");
            }
            renderQuestion();
            return;
        }

        if (gameState.isFinished) {
            stopQuestionTimer();
            setGameStatus("Игра завершена");
            if (gameResult) gameResult.textContent = "Раунд завершен. Итоговая таблица выше.";
            if (gameAnswers) gameAnswers.innerHTML = "";
            if (gameQuestion) gameQuestion.textContent = "Спасибо за игру";
            if (gameProgress) gameProgress.textContent = `Итог: ${gameState.questionsCount}/${gameState.questionsCount}`;
            if (gameTimer) gameTimer.classList.add("hidden");
            return;
        }

        if (gameResult) gameResult.textContent = "";
        if (isHost) {
            const answeredPlayers = (gameState.answeredPlayers || []).length;
            const participantsCount = (gameState.players || []).filter((player) => !sameName(player, gameState.hostName)).length;
            setGameStatus(`Ответили ${answeredPlayers}/${participantsCount}. Переключение доступно после ответов.`);
            stopQuestionTimer();
            if (gameTimer) gameTimer.classList.add("hidden");
        } else {
            const deadlineMs = getQuestionDeadlineMs();
            const timeExpired = typeof deadlineMs === "number" && Date.now() >= deadlineMs;
            setGameStatus(timeExpired ? "Время вышло. Ожидаем следующий вопрос." : "Выбери один из допустимых вариантов ответа");
            startQuestionTimer();
            setTimerValue();
        }
        renderQuestion();
    };

    async function refreshGameState() {
        if (!currentCode) return;

        try {
            const response = await fetch(`${apiBaseUrl}/api/game/state/${encodeURIComponent(currentCode)}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            gameState = await response.json();
            renderGameState();
        } catch (error) {
            setGameStatus("Не удалось получить состояние игры.", true);
            console.error(error);
        }
    }

    const startGameStatePolling = () => {
        if (gameStateTimerId) {
            clearInterval(gameStateTimerId);
            gameStateTimerId = null;
        }

        if (!currentCode) return;

        gameStateTimerId = setInterval(() => {
            void refreshGameState();
        }, 1000);
    };

    const triggerStartGame = async () => {
        if (!ensureHostAction()) return;

        try {
            const response = await fetch(`${apiBaseUrl}/api/game/start`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({code: currentCode, hostName: currentPlayerName})
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            localAnswerByQuestionIndex = {};
            await refreshGameState();
        } catch (error) {
            setGameStatus("Не удалось запустить игру.", true);
            console.error(error);
        }
    };

    const triggerNextQuestion = async () => {
        if (!ensureHostAction()) return;

        try {
            const response = await fetch(`${apiBaseUrl}/api/game/next`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({code: currentCode, hostName: currentPlayerName})
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            await refreshGameState();
        } catch (error) {
            setGameStatus("Не удалось переключить вопрос.", true);
            console.error(error);
        }
    };

    const showRoundControls = () => {
        currentPlayerName = getPlayerName();
        isKickedFromGame = false;
        gamePanel?.classList.remove("hidden");
        if (gameResult) gameResult.textContent = "";
        if (gameAnswers) gameAnswers.innerHTML = "";
        renderWaitingQuestion();
        if (gameProgress) gameProgress.textContent = "Вопрос 0/0";
        if (gameTimer) gameTimer.classList.add("hidden");
        if (kickedBanner) kickedBanner.classList.add("hidden");
        setGameStatus("Ожидание старта");
        setRoleLayout();
        setControlButtons();
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
        localStorage.setItem(hostStorageKey, JSON.stringify({hostName, hostEmail}));
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
            const {value, done} = await reader.read();
            if (done) break;

            tail += decoder.decode(value, {stream: true});
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
            currentPlayerName = payload.hostName;
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
            await refreshGameQuestions(lobby.code);
            await refreshGameState();
            startGameStatePolling();
            showRoundControls();
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
        showRoundControls();
    } else if (createButton && !authToken) {
        createButton.disabled = true;
        setStatus("Для создания игры нужна авторизация.", true);
        showMode("create");
    } else {
        showMode("create");
    }

    controlStartGameButton?.addEventListener("click", () => {
        void triggerStartGame();
    });

    controlNextQuestionButton?.addEventListener("click", () => {
        void triggerNextQuestion();
    });

    controlFinishGameButton?.addEventListener("click", () => {
        void triggerFinishGame();
    });
});
