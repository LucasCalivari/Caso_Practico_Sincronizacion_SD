# Convenciones del grupo

Acordar y respetar estas tres convenciones para que el código y los registros de los tres ejercicios sean consistentes en el informe.

## 1. Versión de .NET

- **.NET 10** (`net10.0`) en todos los proyectos.
- Imágenes Docker: `mcr.microsoft.com/dotnet/sdk:10.0` (build), `aspnet:10.0` (servicios web) y `runtime:10.0` (consolas).
- ASP.NET Core Web API **con controladores** (`--use-controllers`), sin HTTPS.
- Dentro del contenedor, todo servicio web escucha en `http://+:8080`.

## 2. Puertos

El puerto interno es siempre `8080`. Lo que cambia es el puerto publicado en el host, distinto por ejercicio para poder levantar los tres a la vez sin choques.

| Ejercicio | Servicio | Puerto host → contenedor |
|---|---|---|
| Ej. 1 Cristian | `time-server` | `8080:8080` |
| Ej. 1 Cristian | `client-a`, `client-b`, `client-c` | (sin puerto, son consolas) |
| Ej. 2 Berkeley | `node1` | `8181:8080` |
| Ej. 2 Berkeley | `node2` | `8182:8080` |
| Ej. 2 Berkeley | `node3` | `8183:8080` |
| Ej. 2 Berkeley | `coordinator` | (sin puerto, es consola) |
| Ej. 3 Lamport | `orders-service` | `8281:8080` |
| Ej. 3 Lamport | `inventory-service` | `8282:8080` |
| Ej. 3 Lamport | `payments-service` | `8283:8080` |

> Las guías originales usan 8081–8083 tanto en el Ej. 2 como en el Ej. 3. Se corrigen acá (8181–8183 y 8281–8283) para que no choquen. Los puertos internos y las URLs entre contenedores (`http://node1:8080`, etc.) no cambian.

## 3. Formato de log

```
[hora real] [servicio] [reloj] mensaje
```

- **hora real**: UTC del host, ISO 8601 con milisegundos (`2026-10-29T15:04:05.123Z`).
- **servicio**: nombre del contenedor (`client-a`, `node1`, `orders-service`, ...).
- **reloj**: valor del reloj propio del servicio en ese momento: reloj simulado ISO en Ej. 1 y 2, marca de Lamport en Ej. 3. Si no aplica, `-`.
- **mensaje**: texto libre.

Ejemplos:

```
[2026-10-29T15:04:05.123Z] [client-b] [2026-10-29T15:04:13.120Z] RTT=4ms desfase=-7998ms
[2026-10-29T15:04:05.200Z] [orders-service] [3] EVENTO pedido_creado
```

Implementación de referencia: [`plantilla/Servicio/AppLog.cs`](../plantilla/Servicio/AppLog.cs). Cada ejercicio la copia tal cual.

## 4. Git

- Nadie trabaja directo sobre `main`.
- Una rama por persona: `ana`, `valentino`, `melina`, `santiago`, `lucas`.
- Integración a `main` por Pull Request, con la revisión cruzada: Ana → Valentino → Melina → Santiago → Lucas → Ana (cada uno revisa al que nombra la flecha).
- Cada ejercicio vive solo en su carpeta; nadie edita la carpeta de otro ejercicio sin avisar.

## 5. Cómo usar la plantilla

1. Copiar `plantilla/Servicio/` dentro de la carpeta del ejercicio y renombrar (`Servicio` → `TimeServer`, `ClockNode`, `OrdersService`, ...), incluyendo el `.csproj`, el `Dockerfile` (`COPY X.csproj`, `X.dll`) y el `docker-compose.yml`.
2. Copiar `plantilla/docker-compose.yml` a la carpeta del ejercicio y ajustar servicios y puertos según la tabla de arriba.
3. Probar el esqueleto: `docker compose up --build` y `curl http://localhost:<puerto>/api/health`.
