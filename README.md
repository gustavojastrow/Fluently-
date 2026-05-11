# Fluently 🗣️

**Fluently** é uma aplicação web moderna e interativa desenvolvida em **C# .NET Core**, que utiliza o poder do **Semantic Kernel** e **Agentes de IA** para oferecer uma experiência de aprendizado de inglês personalizada. Com foco na prática de conversação e gramática, a aplicação simula a interação com professores de inglês em diferentes níveis, adaptando-se às suas necessidades. Além disso, ele possui uma memória de contexto para ajudar em erros cometidos recentemente pelo usuário no aprendizado. 

## Habilidades e Tecnologias Utilizadas

*   **C# .NET Core**: Linguagem e framework principal para o desenvolvimento da aplicação web, garantindo alta performance e escalabilidade.
*   **Semantic Kernel**: Utilizado para orquestrar e integrar modelos de IA, permitindo a criação de agentes inteligentes com capacidades avançadas de linguagem.
*   **Agentes de IA (LLMs)**: Integração com Large Language Models (LLMs), possivelmente via Ollama, para alimentar os "professores" virtuais.
*   **ASP.NET Core MVC**: Arquitetura para a construção da aplicação web, separando responsabilidades entre Modelos, Views e Controladores.
*   **SQL Server**: Sistema de gerenciamento de banco de dados relacional para persistência de dados do usuário, progresso e histórico de conversas.
*   **JWT (JSON Web Tokens)**: Implementação de autenticação e autorização seguras para gerenciar sessões de usuário.
## O que contém

Este projeto oferece uma experiência de aprendizado inovadora, incluindo:

*   **Interface Web Interativa**: Uma plataforma intuitiva e amigável para interagir com os agentes de IA.
*   **Agentes de IA com Níveis de Inglês Variados**:
    *   **Professor Básico**: Ideal para iniciantes, foca em vocabulário fundamental, estruturas de frases simples e correção gentil.
    *   **Professor Intermediário**: Ajuda a desenvolver fluência, expandir vocabulário e praticar estruturas gramaticais mais complexas.
    *   **Professor Avançado**: Desafia o usuário com conversas mais profundas, nuances linguísticas e aprimoramento da expressão formal e informal.
*   **Chat Interativo**: Converse em tempo real com os agentes de IA, recebendo feedback e correções instantâneas.
*   **Gerenciamento de Progresso**: Acompanhe seu avanço no aprendizado (funcionalidade a ser desenvolvida ou mencionada se já existe).
*   **Plugins de IA**: Utilização de plugins como `ExercisePlugin.cs` e `ProgressPlugin.cs` (se aplicável) para funcionalidades específicas como geração de exercícios e acompanhamento de progresso.
*   **Estrutura de Projeto .NET Core**: Organização padrão de um projeto ASP.NET Core, facilitando o desenvolvimento e a manutenção.

## Exemplos de funcionamento 
1. Login
<img width="1917" height="867" alt="login" src="https://github.com/user-attachments/assets/48bf5447-b8d2-4bf8-8dc4-9ae835ea87c5" />
2. Nível Básico
<img width="1918" height="870" alt="basico" src="https://github.com/user-attachments/assets/e32b1a92-8eb4-423f-b227-7513b9cd0e2f" />
3. Nível Avançado
<img width="1917" height="865" alt="avancado" src="https://github.com/user-attachments/assets/5db64980-46d9-4074-89df-32d54e317b48" />

