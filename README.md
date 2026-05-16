# YARP Example

Exemplo mínimo de **ASP.NET Core 8** usando **YARP** (*Yet Another Reverse Proxy*) para criar um gateway/reverse proxy.

A ideia do projeto é mostrar como uma aplicação ASP.NET Core pode receber chamadas em rotas locais e encaminhar essas chamadas para APIs externas, sem que o cliente precise conhecer diretamente os destinos reais.

Além do YARP, o projeto também inclui **Serilog** para logging estruturado.

## O que este exemplo demonstra

Este projeto demonstra:

- Como instalar e registrar o pacote `Yarp.ReverseProxy`.
- Como configurar rotas de proxy via `appsettings.json`.
- Como apontar rotas locais para APIs públicas diferentes.
- Como usar `Routes`, `Clusters`, `Destinations` e `Transforms`.
- Como expor uma aplicação simples funcionando como API Gateway.
- Como configurar Serilog no ASP.NET Core.
- Como logar no console em build `DEBUG`.
- Como gravar logs em arquivo nos ambientes `Development` e `Production`.

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

## Como o Serilog está configurado

O projeto usa Serilog no host do ASP.NET Core:

```csharp
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Yarp.ReverseProxy", LogEventLevel.Information);

#if DEBUG
    loggerConfiguration.WriteTo.Console();
#endif

    if (context.HostingEnvironment.IsDevelopment() || context.HostingEnvironment.IsProduction())
    {
        loggerConfiguration.WriteTo.File(
            path: "logs/yarp-example-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            restrictedToMinimumLevel: LogEventLevel.Information);
    }
});
```

### Regra aplicada

| Condição | Saída de log |
|---|---|
| Build `DEBUG` | Console |
| Ambiente `Development` | Arquivo em `logs/` |
| Ambiente `Production` | Arquivo em `logs/` |
| Outros ambientes | Sem arquivo, a menos que também estejam em build `DEBUG` |

### Console somente em DEBUG

O console é registrado dentro de uma diretiva de compilação:

```csharp
#if DEBUG
    loggerConfiguration.WriteTo.Console();
#endif
```

Isso significa que o sink de console só entra no binário quando o projeto é compilado em modo `Debug`.

### Arquivo em Development e Production

O sink de arquivo é ativado em tempo de execução quando o ambiente é `Development` ou `Production`:

```csharp
if (context.HostingEnvironment.IsDevelopment() || context.HostingEnvironment.IsProduction())
{
    loggerConfiguration.WriteTo.File(...);
}
```

Os arquivos são gerados em:

```text
logs/yarp-example-YYYYMMDD.log
```

A configuração usa:

- `rollingInterval: RollingInterval.Day`: cria um arquivo por dia.
- `retainedFileCountLimit: 14`: mantém até 14 arquivos de log.
- `restrictedToMinimumLevel: LogEventLevel.Information`: grava logs a partir de `Information`.

A pasta `logs/` foi adicionada ao `.gitignore` para evitar versionar arquivos de log.

### Log de requisições HTTP

O projeto também usa:

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
```

Esse middleware gera logs estruturados para as requisições HTTP processadas pela aplicação, incluindo chamadas que passam pelo YARP.

Exemplo de informação registrada:

```text
HTTP GET /todos/1 responded 200 in 123.4567 ms
```

## Conceitos principais do YARP

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

Contém as referências dos pacotes usados pelo projeto:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.3" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageReference Include="Yarp.ReverseProxy" Version="2.3.0" />
```

### `Program.cs`

Configura a aplicação ASP.NET Core, registra o Serilog, registra o YARP e mapeia o reverse proxy.

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

A intenção é servir como ponto de partida para entender a configuração básica do YARP em uma aplicação ASP.NET Core com Serilog.
