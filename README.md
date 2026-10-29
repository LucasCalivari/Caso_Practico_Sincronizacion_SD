# Caso práctico: sincronización en sistemas distribuidos

TP de Sistemas Operativos / Sistemas Distribuidos (UADER). Tres ejercicios en C# (.NET 10, ASP.NET Core Web API) ejecutados con Docker Compose.

## Estructura

```
.
├── ej1-cristian/   Ej. 1: algoritmo de Cristian (time-server + 3 clientes)
├── ej2-berkeley/   Ej. 2: algoritmo de Berkeley (coordinador + 3 nodos)
├── ej3-lamport/    Ej. 3: relojes lógicos de Lamport (orders / inventory / payments)
├── plantilla/      Esqueleto mínimo Web API + Dockerfile + docker-compose.yml
├── docs/           Convenciones, informe y comparativa (Ej. 4)
└── README.md
```

## Reparto

| Integrante | Parte |
|---|---|
| Ana | Ej. 1: servidor de tiempo y los 3 clientes |
| Valentino | Ej. 2: coordinador y nodos de Berkeley |
| Melina | Ej. 2: pruebas, escenarios de desfase y desconexión, registros |
| Santiago | Ej. 3: los 3 servicios con relojes de Lamport |
| Lucas | Repo base, Dockerfiles y Compose; Ej. 4 (tabla comparativa); informe final |

## Antes de empezar

Leer [`docs/CONVENCIONES.md`](docs/CONVENCIONES.md): versión de .NET, puertos, formato de log y reglas de Git.

## Probar la plantilla

```bash
cd plantilla
docker compose up --build
curl http://localhost:8080/api/health
```
