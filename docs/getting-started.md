# Getting Started

This document explains how to run and test the YARP example locally.

## Requirements

- .NET 8 SDK
- Git
- Terminal, PowerShell, Bash, or similar shell

## Clone the repository

```bash
git clone https://github.com/hebermattos/Yarp-example.git
cd Yarp-example
```

## Restore packages

```bash
dotnet restore
```

## Run the application

```bash
dotnet run
```

Or force a specific URL:

```bash
dotnet run --urls http://localhost:5000
```

## Test the root endpoint

```bash
curl http://localhost:5000/
```

The root endpoint returns a small JSON document with available routes and enabled policies.

## Test the proxied APIs

### JSONPlaceholder

```bash
curl http://localhost:5000/todos/1
```

Gateway URL:

```text
http://localhost:5000/todos/1
```

Upstream URL:

```text
https://jsonplaceholder.typicode.com/todos/1
```

### Dog CEO

```bash
curl http://localhost:5000/dogs/random
```

Gateway URL:

```text
http://localhost:5000/dogs/random
```

Upstream URL:

```text
https://dog.ceo/api/breeds/image/random
```

### REST Countries

```bash
curl http://localhost:5000/countries/name/brazil
```

Gateway URL:

```text
http://localhost:5000/countries/name/brazil
```

Upstream URL:

```text
https://restcountries.com/v3.1/name/brazil
```

## Public APIs used

| Local route | Public API | Real destination |
|---|---|---|
| `/todos/{**catch-all}` | JSONPlaceholder | `https://jsonplaceholder.typicode.com/` |
| `/dogs/random` | Dog CEO | `https://dog.ceo/api/breeds/image/random` |
| `/countries/{**catch-all}` | REST Countries | `https://restcountries.com/v3.1/` |
