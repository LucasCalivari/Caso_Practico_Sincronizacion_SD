# Ejercicio 1: Sincronización de relojes físicos mediante el algoritmo de Cristian

**Responsable:** Ana Lucía Colazo  
**Tecnologías:** C#, .NET 10, ASP.NET Core Web API, Docker, Docker Compose.

---

## 1. Descripción del problema

En un sistema distribuido de registro de operaciones financieras y transaccionales, una empresa dispone de tres servidores que atienden usuarios de forma concurrente. Cada servidor posee su propio oscilador de cuarzo y reloj local:

| Servidor | Hora local inicial | Offset respecto a base |
|---|---|---|
| **Servidor A** | `10:00:00` | `+0 s` (`0 ms`) |
| **Servidor B** | `10:00:08` | `+8 s` (`+8000 ms`) |
| **Servidor C** | `09:59:55` | `-5 s` (`-5000 ms`) |
| **Servidor de tiempo (NTP central)** | `10:00:02` | `+2 s` (`+2000 ms`) |

### El problema de inconsistencia
Debido al desfase de los osciladores locales:
- Una transacción registrada en el Servidor B aparenta haber ocurrido 8 segundos en el futuro respecto al Servidor A, y 13 segundos después que en el Servidor C.
- Si un usuario deposita fondos en el Servidor C y luego los transfiere en el Servidor B, el registro cronológico central mostraría la transferencia antes que el depósito si los relojes no están sincronizados, violando la causalidad del negocio.

---

## 2. Arquitectura de la solución

```
ej1-cristian/
├── TimeServer/               # Web API ASP.NET Core (puerto host 8080:8080)
│   ├── Controllers/
│   │   ├── HealthController.cs
│   │   └── TimeController.cs # GET /api/time (admite ?delayMs= y logging)
│   ├── AppLog.cs             # Convención de log común
│   ├── SimulatedClock.cs     # Reloj simulado del servidor (+2000 ms)
│   ├── Program.cs
│   ├── Dockerfile
│   └── TimeServer.csproj
├── TimeClient/               # Aplicación de consola (.NET 10 Runtime)
│   ├── AppLog.cs
│   ├── SimulatedClock.cs     # Reloj lógico de aplicación desacoplado del SO
│   ├── Program.cs            # Ejecución secuencial de los 7 pasos de Cristian
│   ├── Dockerfile
│   └── TimeClient.csproj
├── escenarios/               # Archivos compose overlay para pruebas
│   ├── 01-desfases-base.yml
│   ├── 02-alta-latencia.yml
│   └── 03-servidor-caido.yml
├── logs/                     # Registros de ejecución reales obtenidos
│   ├── 01-desfases-base.txt
│   ├── 02-alta-latencia.txt
│   ├── 03-servidor-caido.txt
│   └── README.md
├── CristianSync.sln          # Solución Visual Studio / .NET
├── docker-compose.yml        # Orquestación de time-server y los 3 clientes
├── correr-escenarios.ps1     # Automatización de pruebas y captura de logs
└── README.md
```

---

## 3. Implementación de los 7 puntos requeridos

Cada cliente (`client-a`, `client-b`, `client-c`) implementa el algoritmo de Cristian siguiendo rigurosamente los 7 puntos de la consigna:

1. **Consultar la hora del servidor de tiempo mediante una API REST:**  
   Envío de solicitud HTTP GET a `http://time-server:8080/api/time?client={clientName}`.
2. **Registrar el instante de envío de la solicitud ($T_0$):**  
   Se captura la marca de tiempo local del cliente $T_0$ mediante su reloj simulado y un temporizador de alta resolución (`Stopwatch`).
3. **Registrar el instante de recepción de la respuesta ($T_1$):**  
   Al recibir la respuesta HTTP del servidor de tiempo, se registra inmediatamente el instante local $T_1$.
4. **Calcular el tiempo de ida y vuelta (RTT):**  
   $$RTT = T_1 - T_0$$
   Se asume retardo de red simétrico, estimando el tiempo de viaje unidireccional de la respuesta en:
   $$\text{Latencia estimada} = \frac{RTT}{2}$$
5. **Estimar la hora actual utilizando el algoritmo de Cristian:**  
   Si el servidor generó la respuesta en el instante $T_{\text{server}}$, en el momento de recepción $T_1$ la hora real del servidor se estima como:
   $$T_{\text{estimada}} = T_{\text{server}} + \frac{RTT}{2}$$
   El margen de incertidumbre máxima es:
   $$\text{Incertidumbre} = \pm \left(\frac{RTT}{2} - T_{\min}\right) \le \pm \frac{RTT}{2}$$
6. **Ajustar su reloj lógico de aplicación, sin modificar el reloj del sistema operativo:**  
   Se utiliza la clase `SimulatedClock`, la cual mantiene un offset en milisegundos (`_offsetMs`) sumado a `DateTimeOffset.UtcNow`.  
   La corrección aplicada es:
   $$\Delta = T_{\text{estimada}} - T_1$$
   El reloj del sistema operativo anfitrión permanece intacto.
7. **Mostrar la diferencia entre la hora anterior y la hora sincronizada:**  
   Se emiten por consola y log los valores explícitos:
   - Hora anterior ($T_1$)
   - Hora sincronizada estimada ($T_{\text{estimada}}$)
   - Diferencia / corrección $\Delta$ en milisegundos
   - Nuevo offset del reloj local de la aplicación

---

## 4. Ejecución del sistema

### Ejecución básica con Docker Compose
```bash
cd ej1-cristian
docker compose up --build
```

Para verificar el estado de salud del servidor de tiempo:
```bash
curl http://localhost:8080/api/health
```

### Ejecución de todos los escenarios de prueba
```powershell
cd ej1-cristian
.\correr-escenarios.ps1
```
O escenarios específicos:
```powershell
.\correr-escenarios.ps1 01 02
```
