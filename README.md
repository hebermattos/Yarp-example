# YARP Example

Exemplo mínimo de **ASP.NET Core 8** usando **YARP** (*Yet Another Reverse Proxy*) para criar um gateway/reverse proxy.

A ideia do projeto é mostrar como uma aplicação ASP.NET Core pode receber chamadas em rotas locais e encaminhar essas chamadas para APIs externas, sem que o cliente precise conhecer diretamente os destinos reais.

## O que este exemplo demonstra

Este projeto demonstra:

- Como instalar e registrar o pacote `Yarp.ReverseProxy`.
- Como configurar rotas de proxy via `appsettings.json`.
- Como apontar rotas locais para APIs públicas diferentes.
- Como usar `Routes`, `Clusters`, `Destinations` e `Transforms`.
- Como expor uma aplicação simples funcionando como API Gateway.

## APIs públicas usadas

O gateway expõe três rotas locais e encaminha cada uma para uma API pública diferente:

| Rota local | API pública | Destino real |
|---|---|---|
| `/todos/{**catch-all}` | JSONPlaceholder | `https://jsonplaceholder.typicode.com/` |
| `/dogs/random` | Dog CEO | `https://dog.ceo/api/breeds/image/random` |
| `/countries/{**catch-all}` | REST Countries | `https://restcountries.com/v3.1/` |

## Requisitos

- .NET 8 SDK
- Git
- Terminal, PowerShell, Bash ou similar

## Como executar

Clone o repositório:

```bash
git clone https://github.com/hebermattos/Yarp-example.git
cd Yarp-example
```

Restaure os pacotes:

```bash
dotnet restore
```

Execute a aplicação:

```bash
dotnet run
```

Ou force uma URL específica:

```bash
dotnet run --urls http://localhost:5000
```

## Como testar

Abra a rota raiz para ver um resumo das rotas disponíveis:

```bash
curl http://localhost:5000/
```

Teste a API de todos:

```bash
curl http://localhost:5000/todos/1
```

Essa chamada entra no gateway em:

```text
http://localhost:5000/todos/1
```

E o YARP encaminha para:

```text
https://jsonplaceholder.typicode.com/todos/1
```

Teste a API de imagem aleatória de cachorro:

```bash
curl http://localhost:5000/dogs/random
```

Essa chamada entra no gateway em:

```text
http://localhost:5000/dogs/random
```

E o YARP encaminha para:

```text
https://dog.ceo/api/breeds/image/random
```

Teste a API de países:

```bash
curl http://localhost:5000/countries/name/brazil
```

Essa chamada entra no gateway em:

```text
http://localhost:5000/countries/name/brazil
```

E o YARP encaminha para:

```text
https://restcountries.com/v3.1/name/brazil
```

## Como o YARP está registrado

No arquivo `Program.cs`, o YARP é registrado no container de DI da aplicação:

```csharp
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
```

Esse trecho faz duas coisas importantes:

1. `AddReverseProxy()` adiciona os serviços necessários do YARP.
2. `LoadFromConfig(...)` diz ao YARP para ler a configuração da seção `ReverseProxy` no `appsettings.json`.

Depois, o proxy é mapeado no pipeline HTTP:

```csharp
app.MapReverseProxy();
```

Isso faz com que as requisições compatíveis com as rotas configuradas sejam tratadas pelo YARP.

## Conceitos principais

### Route

Uma `Route` define **qual requisição local será capturada** pelo gateway.

Exemplo:

```json
"todos-route": {
  "ClusterId": "jsonplaceholder-cluster",
  "Match": {
    "Path": "/todos/{**catch-all}"
  }
}
```

Neste caso, qualquer chamada que comece com `/todos/` será capturada por essa rota.

Exemplo:

```text
/todos/1
/todos/10
/todos/99
```

### Cluster

Um `Cluster` define **para onde a requisição será encaminhada**.

Exemplo:

```json
"jsonplaceholder-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "https://jsonplaceholder.typicode.com/"
    }
  }
}
```

A rota aponta para o cluster usando `ClusterId`:

```json
"ClusterId": "jsonplaceholder-cluster"
```

Ou seja:

```text
Route -> Cluster -> Destination
```

### Destination

Uma `Destination` é o endereço final para onde o YARP envia a chamada.

Exemplo:

```json
"Address": "https://jsonplaceholder.typicode.com/"
```

Um cluster pode ter uma ou mais destinations. Em cenários reais, isso permite balanceamento de carga entre múltiplas instâncias de uma API.

### Transform

Um `Transform` permite alterar partes da requisição antes de enviá-la ao destino.

Neste exemplo, a rota local `/dogs/random` é convertida para o caminho real da API Dog CEO:

```json
"Transforms": [
  {
    "PathSet": "/api/breeds/image/random"
  }
]
```

Assim, o cliente chama:

```text
/dogs/random
```

Mas o destino recebe:

```text
/api/breeds/image/random
```

Também usamos `PathPattern` para preservar parte da rota capturada:

```json
"PathPattern": "/v3.1/{**catch-all}"
```

Com isso:

```text
/countries/name/brazil
```

Vira:

```text
/v3.1/name/brazil
```

## Fluxo de uma requisição

Exemplo usando `/countries/name/brazil`:

```text
Cliente
  ↓
GET http://localhost:5000/countries/name/brazil
  ↓
ASP.NET Core recebe a requisição
  ↓
YARP identifica a rota countries-route
  ↓
YARP aplica o transform PathPattern
  ↓
YARP encaminha para https://restcountries.com/v3.1/name/brazil
  ↓
A resposta volta pelo YARP
  ↓
Cliente recebe a resposta
```

## Estrutura do projeto

```text
.
├── .github/
│   └── workflows/
│       └── build.yml
├── Properties/
│   └── launchSettings.json
├── .gitignore
├── Program.cs
├── README.md
├── YarpExample.csproj
└── appsettings.json
```

## Arquivos principais

### `YarpExample.csproj`

Contém a referência ao pacote do YARP:

```xml
<PackageReference Include="Yarp.ReverseProxy" Version="2.3.0" />
```

### `Program.cs`

Configura a aplicação ASP.NET Core, registra o YARP e mapeia o reverse proxy.

### `appsettings.json`

Contém toda a configuração de rotas, clusters, destinos e transforms.

### `.github/workflows/build.yml`

Workflow simples do GitHub Actions para restaurar e compilar o projeto em cada push ou pull request para a branch `main`.

## Como adicionar uma nova API

Para adicionar uma nova API, você precisa criar:

1. Uma nova rota em `ReverseProxy:Routes`.
2. Um novo cluster em `ReverseProxy:Clusters`.
3. Uma destination apontando para a API real.

Exemplo conceitual:

```json
"my-api-route": {
  "ClusterId": "my-api-cluster",
  "Match": {
    "Path": "/my-api/{**catch-all}"
  },
  "Transforms": [
    {
      "PathPattern": "/{**catch-all}"
    }
  ]
}
```

E o cluster:

```json
"my-api-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "https://example.com/"
    }
  }
}
```

## Quando usar YARP

YARP é útil quando você precisa de um gateway em .NET para cenários como:

- API Gateway para múltiplos serviços.
- Reverse proxy para APIs internas.
- Roteamento centralizado.
- Migração gradual entre sistemas legados e novos serviços.
- Aplicação de autenticação, autorização, headers, rate limiting ou observabilidade em um ponto único.
- Balanceamento entre múltiplos destinos.

## Observações

Este projeto é propositalmente simples. Ele não implementa autenticação, rate limiting, cache, health checks ou observabilidade avançada.

A intenção é servir como ponto de partida para entender a configuração básica do YARP em uma aplicação ASP.NET Core.
