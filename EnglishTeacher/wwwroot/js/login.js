
document.getElementById("loginForm").addEventListener("submit", async function (event) {
    event.preventDefault();


const authenticateUrl = this.getAttribute('data-authenticate-url');

const login = document.getElementById("login").value.trim();
const senha = document.getElementById("senha").value.trim();

document.getElementById("loginError").style.display = "none";
document.getElementById("senhaError").style.display = "none";
document.getElementById("serverError").style.display = "none";

let hasError = false;

if (!login) {
    document.getElementById("loginError").style.display = "block";
hasError = true;
    }

if (!senha) {
    document.getElementById("senhaError").style.display = "block";
hasError = true;
    }

if (hasError) return;

    try {
        const response = await fetch(authenticateUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ login, senha })
        });

if (response.ok) {
    const data = await response.json();
    document.getElementById("successMessage").style.display = "block";
    localStorage.setItem('jwtToken', data.token);

    await fetchAssistenteData();
} else {
    const errorData = await response.json();
    document.getElementById("serverError").textContent = errorData.message || "Erro ao realizar login. Verifique suas credenciais.";
    document.getElementById("serverError").style.display = "block";
}
} catch (error) {
    console.error("Erro no login:", error);
    document.getElementById("serverError").textContent = "Ocorreu um erro ao tentar fazer login. Tente novamente mais tarde.";
    document.getElementById("serverError").style.display = "block";
}
});

async function fetchAssistenteData() {
    const token = localStorage.getItem('jwtToken');
    try {
        const response = await fetch(assistenteUrl, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        if (response.ok) {
            window.location.href = assistenteUrl; 
        } else {
            console.error("Erro ao buscar dados do assistente. Acesso não autorizado.");
            document.getElementById("serverError").textContent = "Acesso não autorizado. Faça login novamente.";
            document.getElementById("serverError").style.display = "block";
        }
    } catch (error) {
        console.error("Erro ao conectar com o assistente:", error);
        document.getElementById("serverError").textContent = "Erro ao conectar com o assistente.";
        document.getElementById("serverError").style.display = "block";
    }
}
