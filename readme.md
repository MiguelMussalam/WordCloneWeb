# WordCloneWeb

WordCloneWeb é uma aplicação web construída com ASP.NET que fornece uma interface web simples para criação, validação e gerenciamento de documentos e assinaturas. O servidor backend é escrito em C# e serve páginas HTML estáticas localizadas no diretório `wwwroot`.

## Tecnologias utilizadas

* .NET (ASP.NET)
* C#
* HTML
* Servidor web embutido do ASP.NET

## Estrutura do projeto

```
WordCloneWeb/
│
├── Program.cs
├── WordCloneWeb.csproj
├── appsettings.json
├── appsettings.Development.json
│
├── Properties/
│   └── launchSettings.json
│
├── wwwroot/
│   ├── index.html
│   ├── create.html
│   ├── signatures.html
│   └── validate.html
│
└── obj/
```

### Descrição dos diretórios principais

Program.cs
Arquivo principal da aplicação ASP.NET. Contém a configuração do servidor e dos endpoints da aplicação.

WordCloneWeb.csproj
Arquivo de projeto do .NET que define dependências e configuração de build.

wwwroot
Diretório público servido pelo ASP.NET. Contém as páginas HTML da interface web.

Properties/launchSettings.json
Configurações utilizadas durante a execução local da aplicação.

obj
Arquivos temporários gerados pelo processo de build do .NET.

## Pré-requisitos

Antes de executar o projeto, é necessário ter instalado:

* .NET SDK

Para verificar se o .NET está instalado:

```
dotnet --version
```

## Como executar o projeto

1. Clone o repositório

```
git clone https://github.com/SEU_USUARIO/WordCloneWeb.git
```

2. Entre no diretório do projeto

```
cd WordCloneWeb
```

3. Execute a aplicação

```
dotnet run
```

4. Abra o navegador

Após iniciar, o terminal exibirá um endereço como:

```
http://localhost:5000
```

ou

```
https://localhost:5001
```

Abra esse endereço no navegador.

## Páginas disponíveis

A aplicação serve as seguintes páginas:

* `/index.html` — página inicial
* `/create.html` — criação de documentos
* `/signatures.html` — gerenciamento de assinaturas
* `/validate.html` — validação de documentos

## Desenvolvimento

Para desenvolvimento local, é possível utilizar:

```
dotnet watch run
```

Isso reinicia automaticamente o servidor sempre que um arquivo for modificado.

## Build

Para compilar o projeto:

```
dotnet build
```

## Observações

Diretórios como `bin` e `obj` são gerados automaticamente pelo .NET durante a compilação e não precisam ser versionados no Git.
