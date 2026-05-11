function processMarkdown(message) {
    let formatted = convertMarkdownTables(message);

    formatted = formatted
        .replace(/\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/gim, '<a href="$2" target="_blank" class="chat-link">$1</a>')
        .replace(/^### (.*$)/gim, '<h3>$1</h3>')
        .replace(/^## (.*$)/gim, '<h2>$1</h2>')
        .replace(/^# (.*$)/gim, '<h1>$1</h1>')
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
        .replace(/\*(.*?)\*/g, '<em>$1</em>')
        // ⭐ CORRIGIDO: Regex mais robusto para LaTeX com quebras de linha
        .replace(/\\\[([\s\S]*?)\\\]/g, '<div class="math">\\[$1\\]</div>')
        .replace(/\\\(([\s\S]*?)\\\)/g, '<span class="math">\\($1\\)</span>')
        // ⭐ NOVO: Suporte para formato alternativo $$
        .replace(/\$\$([\s\S]*?)\$\$/g, '<div class="math">$$$$1$$</div>')
        .replace(/\$((?!\$)[^$]+)\$/g, '<span class="math">$$$1$$</span>')
        .replace(/\n/g, '<br>');

    return formatted;
}

function renderMathInElement(element) {
    if (window.MathJax && window.MathJax.typesetPromise) {
        window.MathJax.typesetPromise([element]).catch((err) => {
            console.warn('Erro ao renderizar MathJax:', err);
        });
    }
}

function normalizeAssistantName(name) {
    return (name || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase();
}

function showLoading() {
    const overlay = document.getElementById('loadingOverlay');
    overlay.style.display = 'flex';
    // Pequeno delay para permitir a transição
    setTimeout(() => {
        overlay.classList.add('active');
    }, 10);
}

function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    overlay.classList.remove('active');
    // Aguarda a transição antes de esconder
    setTimeout(() => {
        overlay.style.display = 'none';
    }, 300);
}
function populateChatHistory(chatHistory) {
    const chatContainer = document.getElementById("chatMessages");
    chatContainer.innerHTML = "";

    chatHistory.forEach(chat => {
        const messageDiv = document.createElement("div");
        messageDiv.className = chat.sender === "Assistant" ? "chat-message assistant" : "chat-message user";

        let formattedMessage = processMarkdown(chat.message);

        if (chat.sender === "Assistant") {
            const logoSrc = getAssistantAvatar(currentAssistant);
            messageDiv.innerHTML = `
            <img src="${logoSrc}" alt="Assistente" class="assistant-profile-image">
            <div class="message-content">${formattedMessage}</div>
        `;
        } else {
            messageDiv.innerHTML = formattedMessage;
        }

        chatContainer.appendChild(messageDiv);
    });

    setTimeout(() => {
        if (window.MathJax && window.MathJax.typesetPromise) {
            window.MathJax.typesetPromise([chatContainer]).catch((err) => {
                console.warn('Erro ao renderizar MathJax:', err);
            });
        }
    }, 100);

    scrollToBottom();
}

async function loadChatHistory(assistantType = "Nível Básico") {
    const token = localStorage.getItem('jwtToken');

    showLoading();

    const loadingElement = document.querySelector(".loading-message");
    if (loadingElement) {
        loadingElement.style.display = "block";
    }

    try {
        const url = `${buscarChatAssistenteUrl}?assistantType=${encodeURIComponent(assistantType)}`;
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                populateChatHistory(data.chatHistory);
            } else {
                console.error("Erro:", data.error);
            }
        } else {
            console.error("Erro ao carregar histórico do chat:", response.statusText);
        }
    } catch (error) {
        console.error("Erro ao buscar o histórico:", error);
    } finally {
        hideLoading();

        if (loadingElement) {
            loadingElement.style.display = "none";
        }
    }
}


let isFirstLoad = true;

document.addEventListener("DOMContentLoaded", function () {
    setTimeout(() => {
        const urlParams = new URLSearchParams(window.location.search);
        const assistantFromUrl = urlParams.get('assistant');
        const savedAssistant = sessionStorage.getItem('selectedAssistant');

        let selectedAssistant = null;
        let shouldCleanUrl = false;

        const decodedAssistant = assistantFromUrl ? decodeURIComponent(assistantFromUrl) : null;

        const validAssistants = ["Nível Básico", "Nível Intermediário", "Nível Avançado"];
        if (decodedAssistant && validAssistants.includes(decodedAssistant)) {
            selectedAssistant = decodedAssistant;
            sessionStorage.setItem('selectedAssistant', decodedAssistant);
            shouldCleanUrl = true;
        } else if (savedAssistant && validAssistants.includes(savedAssistant)) {
            selectedAssistant = savedAssistant;
        } else {
            selectedAssistant = "Nível Básico";
        }

        currentAssistant = selectedAssistant;

        const assistantLinks = document.querySelectorAll("#assistantList li");
        let selectedElement = null;

        assistantLinks.forEach(link => {
            const linkText = normalizeAssistantName(link.textContent.trim());
            const assistantName = normalizeAssistantName(currentAssistant);
            if ((assistantName === "nivel basico" && linkText.includes("basico")) ||
                (assistantName === "nivel intermediario" && linkText.includes("intermediario")) ||
                (assistantName === "nivel avancado" && linkText.includes("avancado"))) {
                selectedElement = link;
            }
        });

        if (selectedElement) {
            selectAssistant(currentAssistant, selectedElement);
        } else {
            const firstAssistantLink = document.querySelector("#assistantList li");
            if (firstAssistantLink) {
                selectAssistant(currentAssistant, firstAssistantLink);
            } else {
                loadChatHistory(currentAssistant);
            }
        }

        if (shouldCleanUrl && assistantFromUrl) {
            const newUrl = window.location.pathname;
            window.history.replaceState({}, document.title, newUrl);
        }

        isFirstLoad = false;
    }, 100);

    const textarea = document.getElementById("userMessageInput");
    const wordCountElement = document.getElementById("wordCount");
    const errorMessage = document.getElementById("errorMessage");
    const sendMessageButton = document.getElementById("sendMessageButton");
    const maxWords = 1000;

    if (textarea && wordCountElement) {
        textarea.addEventListener("input", () => {
            const words = textarea.value.trim().split(/\s+/);
            const wordCount = textarea.value.trim() === "" ? 0 : words.length;
            wordCountElement.textContent = `${wordCount} / ${maxWords}`;
            if (wordCount > maxWords) {
                if (errorMessage) errorMessage.style.display = "block";
                textarea.style.borderColor = "#e30613";
                if (sendMessageButton) sendMessageButton.disabled = true;
            } else {
                if (errorMessage) errorMessage.style.display = "none";
                textarea.style.borderColor = "#ddd";
                if (sendMessageButton) sendMessageButton.disabled = false;
            }
        });
    }
});

document.getElementById("sendMessageButton").addEventListener("click", async function (event) {
    event.preventDefault();
    const messageInput = document.getElementById("userMessageInput");
    const sendMessageButton = document.getElementById("sendMessageButton");
    const message = messageInput.value.trim();

    if (!message) return;

    const validAssistants = ["Nível Básico", "Nível Intermediário", "Nível Avançado"];
    if (!currentAssistant || !validAssistants.includes(currentAssistant)) {
        showModalError("Por favor, selecione um assistente antes de enviar a mensagem.");
        return;
    }

    sendMessageButton.disabled = true;
    messageInput.disabled = true;
    messageInput.value = "";

    const chatMessages = document.getElementById("chatMessages");

    // Mensagem do usuário
    const userMessageDiv = document.createElement("div");
    userMessageDiv.className = "chat-message user";
    userMessageDiv.textContent = message;
    chatMessages.appendChild(userMessageDiv);
    scrollToBottom();

    // Bolha do assistente — conteúdo preenchido conforme chunks chegam
    const assistantDiv = document.createElement("div");
    assistantDiv.className = "chat-message assistant";
    assistantDiv.innerHTML = `
        <img src="${getAssistantAvatar(currentAssistant)}" alt="Assistente" class="assistant-profile-image">
        <div class="message-content typing-cursor">▍</div>`;
    chatMessages.appendChild(assistantDiv);
    scrollToBottom();

    const contentEl = assistantDiv.querySelector(".message-content");
    const token = localStorage.getItem("jwtToken");
    const formData = new FormData();
    formData.append("message", message);
    formData.append("assistantType", currentAssistant);

    try {
        // fetch com ReadableStream: lê o SSE chunk a chunk sem esperar a resposta completa
        const response = await fetch(streamChatUrl, {
            method: "POST",
            body: formData,
            headers: { Authorization: `Bearer ${token}` }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let accumulatedText = "";
        let buffer = "";

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });

            // Processa linhas SSE ("data: ...\n\n")
            const lines = buffer.split("\n\n");
            buffer = lines.pop(); // último fragmento pode estar incompleto

            for (const line of lines) {
                const dataLine = line.startsWith("data: ") ? line.slice(6).trim() : null;
                if (!dataLine || dataLine === "[DONE]") continue;

                try {
                    const parsed = JSON.parse(dataLine);
                    if (parsed.error) {
                        showModalError(parsed.error);
                        break;
                    }
                    if (parsed.text) {
                        accumulatedText += parsed.text;
                        // Renderiza markdown progressivamente
                        contentEl.innerHTML = processMarkdown(accumulatedText);
                        contentEl.classList.add("typing-cursor");
                        scrollToBottom();
                    }
                } catch {
                    // chunk inválido — ignora
                }
            }
        }

        // Renderização final: remove cursor e aplica MathJax se necessário
        contentEl.classList.remove("typing-cursor");
        contentEl.innerHTML = processMarkdown(accumulatedText);
        if (/\\\[|\\\(|\$\$|\$/.test(accumulatedText)) {
            setTimeout(() => renderMathInElement(contentEl), 50);
        }

    } catch (error) {
        console.error("Erro no streaming:", error);
        contentEl.classList.remove("typing-cursor");
        contentEl.textContent = "Não foi possível obter uma resposta. Tente novamente.";
        showModalError("Erro de conexão. Tente sair e fazer login novamente.");
    } finally {
        sendMessageButton.disabled = false;
        messageInput.disabled = false;
        scrollToBottom();
    }
});

function showModalError(errorMessage) {
    const errorModal = document.getElementById("errorModal");
    const errorMessageElement = document.getElementById("errorMessageContent");

    if (errorMessageElement) {
        errorMessageElement.textContent = errorMessage;
    }
    if (errorModal) {
        errorModal.style.display = "block";
    }

    const closeModalButton = document.getElementById("closeErrorModal");
    if (closeModalButton) {
        closeModalButton.onclick = function () {
            errorModal.style.display = "none";
        };
    }

    window.onclick = function (event) {
        if (event.target == errorModal) {
            errorModal.style.display = "none";
        }
    };
}

function convertMarkdownTables(text) {
    const lines = text.split('\n');
    const output = [];
    let inTable = false;
    let tableLines = [];

    for (let line of lines) {
        if (line.trim().startsWith('|') && line.includes('|')) {
            tableLines.push(line.trim());
            inTable = true;
        } else {
            if (inTable) {
                output.push(renderMarkdownTable(tableLines));
                tableLines = [];
                inTable = false;
            }
            output.push(line);
        }
    }

    if (inTable && tableLines.length) {
        output.push(renderMarkdownTable(tableLines));
    }

    return output.join('\n');
}

function renderMarkdownTable(lines) {
    if (lines.length < 2) return lines.join('<br>');

    const headers = lines[0].split('|').map(cell => cell.trim()).filter(cell => cell);
    const rows = lines.slice(2).map(line =>
        line.split('|').map(cell => cell.trim()).filter(cell => cell)
    );

    let html = '<table class="markdown-table"><thead><tr>';
    headers.forEach(header => {
        html += `<th>${header}</th>`;
    });
    html += '</tr></thead><tbody>';

    rows.forEach(row => {
        html += '<tr>';
        row.forEach(cell => {
            html += `<td>${cell}</td>`;
        });
        html += '</tr>';
    });

    html += '</tbody></table>';
    return html;
}

async function updateChatMessages(chatHistory) {
    const chatMessages = document.getElementById("chatMessages");
    chatMessages.innerHTML = "";

    let lastUserMessageIndex = -1;
    for (let i = chatHistory.length - 1; i >= 0; i--) {
        if ((chatHistory[i].sender || "").toLowerCase() === "user") {
            lastUserMessageIndex = i;
            break;
        }
    }

    for (let i = 0; i <= lastUserMessageIndex; i++) {
        const chat = chatHistory[i];
        const messageDiv = document.createElement("div");
        messageDiv.className = `chat-message ${chat.sender.toLowerCase()}`;
        const formattedMessage = processMarkdown(chat.message);

        if (chat.sender.toLowerCase() === "assistant") {
            const logoSrc = getAssistantAvatar(currentAssistant);
            messageDiv.innerHTML = `
        <img src="${logoSrc}" alt="Assistente" class="assistant-profile-image">
        <div class="message-content">${formattedMessage}</div>
    `;
        } else {
            messageDiv.innerHTML = formattedMessage;
        }
        chatMessages.appendChild(messageDiv);
    }

    for (let i = lastUserMessageIndex + 1; i < chatHistory.length; i++) {
        const chat = chatHistory[i];
        const messageDiv = document.createElement("div");
        messageDiv.className = `chat-message ${chat.sender.toLowerCase()}`;
        const formattedMessage = processMarkdown(chat.message);

        if (chat.sender.toLowerCase() === "assistant") {
            const logoSrc = getAssistantAvatar(currentAssistant);
            messageDiv.innerHTML = `
            <img src="${logoSrc}" alt="Assistente" class="assistant-profile-image">
            <div class="message-content"></div>
        `;

            chatMessages.appendChild(messageDiv);

            const contentElement = messageDiv.querySelector('.message-content');

            if (/<[^>]+>/.test(formattedMessage) || /\\\[|\\\(|\$\$|\$/.test(formattedMessage)) {
                contentElement.innerHTML = formattedMessage;
                setTimeout(() => renderMathInElement(contentElement), 50);
            } else {
                await simulateTyping(contentElement, formattedMessage);
            }
        } else {
            messageDiv.innerHTML = formattedMessage;
            chatMessages.appendChild(messageDiv);
        }
    }

    setTimeout(() => {
        if (window.MathJax && window.MathJax.typesetPromise) {
            window.MathJax.typesetPromise([chatMessages]).catch((err) => {
                console.warn('Erro ao renderizar MathJax final:', err);
            });
        }
    }, 200);

    scrollToBottom();
}

function simulateTyping(element, message) {
    return new Promise((resolve) => {
        let index = 0;
        const typingSpeed = 9;
        element.innerHTML = "";

        const typingInterval = setInterval(() => {
            element.innerHTML += message.charAt(index);
            index++;
            scrollToBottom();
            if (index === message.length) {
                clearInterval(typingInterval);
                resolve();
            }
        }, typingSpeed);
    });
}

function scrollToBottom() {
    const chatMessages = document.getElementById("chatMessages");
    if (chatMessages) {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}

const userMessageInput = document.getElementById("userMessageInput");
if (userMessageInput) {
    userMessageInput.addEventListener("input", () => {
        userMessageInput.style.height = "auto";
        userMessageInput.style.height = userMessageInput.scrollHeight + "px";
    });
}

const teacherMeta = {
    "Nível Básico":        { name: "Luna",   subtitle: "Professora de Inglês · Nível Básico",        avatar: "/img/avatar-luna.svg" },
    "Nível Intermediário": { name: "Alex",   subtitle: "Professor de Inglês · Nível Intermediário",  avatar: "/img/avatar-alex.svg" },
    "Nível Avançado":      { name: "Jordan", subtitle: "Coach de Inglês · Nível Avançado",           avatar: "/img/avatar-jordan.svg" }
};

function getAssistantAvatar(assistantName) {
    return teacherMeta[assistantName]?.avatar ?? "/img/avatar-luna.svg";
}

function selectAssistant(name, element) {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay && overlay.classList.contains('active')) return;

    currentAssistant = name;
    sessionStorage.setItem('selectedAssistant', name);

    const meta = teacherMeta[name] ?? { name, subtitle: "Pratique seu inglês com IA", avatar: "/img/avatar-luna.svg" };
    const comprocardModal = document.getElementById("comprocardModal");

    if (comprocardModal) comprocardModal.style.display = "none";

    const titleEl    = document.getElementById("selectedAssistantTitle");
    const subtitleEl = document.getElementById("selectedAssistantSubtitle");
    const avatarEl   = document.getElementById("headerAvatar");
    if (titleEl)    titleEl.textContent = meta.name;
    if (subtitleEl) subtitleEl.textContent = meta.subtitle;
    if (avatarEl)   avatarEl.src = meta.avatar;

    const links = document.querySelectorAll("#assistantList li");
    links.forEach(link => link.classList.remove("selected"));
    if (element) {
        element.classList.add("selected");
    }

    const infoButton = document.getElementById('infoButton');
    if (infoButton) {
        infoButton.disabled = false;
        infoButton.title = `Informações do ${typeof assistantInfo !== 'undefined' && assistantInfo[name]?.title || name}`;
    }

    const assistantLinks = document.querySelectorAll("#assistantList li");
    assistantLinks.forEach(link => {
        link.style.pointerEvents = 'none';
        link.style.opacity = '0.7';
    });

    loadChatHistory(name).finally(() => {
        assistantLinks.forEach(link => {
            link.style.pointerEvents = 'auto';
            link.style.opacity = '1';
        });
    });
}
const closeModalButton = document.getElementById("closeModalButton");
if (closeModalButton) {
    closeModalButton.addEventListener("click", () => {
        const comprocardModal = document.getElementById("comprocardModal");
        if (comprocardModal) {
            comprocardModal.style.display = "none";
        }
    });
}

const comprocardModal = document.getElementById("comprocardModal");
if (comprocardModal) {
    comprocardModal.addEventListener("click", (event) => {
        if (event.target === comprocardModal) {
            comprocardModal.style.display = "none";
        }
    });
}
