# Registros de ejecución del Ej. 1 (Cristian)

Generados con `.\correr-escenarios.ps1`. Un archivo por escenario, con las líneas de todos los contenedores ordenadas cronológicamente por hora real (UTC host) y, al final, el estado de salud del servidor de tiempo (`GET /api/health`).

| Archivo | Prueba / Escenario |
|---|---|
| `01-desfases-base.txt` | Desfases iniciales del enunciado: Servidor A (0 s), Servidor B (+8 s), Servidor C (-5 s) y Servidor de tiempo (+2 s). Demostración de los 7 pasos y corrección de operaciones pre y post sincronización. |
| `02-alta-latencia.txt` | Simulación de alta latencia de red (demora artificial de 300 ms en el servidor). Análisis del impacto en el RTT y el margen de incertidumbre (+- RTT/2). |
| `03-servidor-caido.txt` | Simulación de caída del servidor central de tiempo. Demostración del comportamiento de los clientes: timeout, registro del error, conservación del reloj de aplicación autónomo sin bloqueo y recuperación tras el reinicio del servidor. |

Los archivos son `.txt` porque el `.gitignore` del repositorio ignora los archivos `*.log`.
