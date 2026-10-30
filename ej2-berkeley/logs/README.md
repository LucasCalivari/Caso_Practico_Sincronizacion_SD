# Registros de ejecución del Ej. 2 (Berkeley)

Generados con `..\correr-escenarios.ps1`. Un archivo por escenario, con las líneas de todos los contenedores ordenadas por hora real y, al final, el estado de los nodos consultado por la API (`GET /api/clock`).

| Archivo | Prueba del enunciado |
|---|---|
| `01-desfases.txt` | Diferencias iniciales de 5, 10 y 20 s; valores antes y después de sincronizar |
| `02a-nodo-5min-sin-descarte.txt` | Nodo con 5 min de diferencia que entra en el promedio |
| `02b-nodo-5min-con-descarte.txt` | El mismo nodo, descartado por superar el umbral de 60 s |
| `03-desconexion.txt` | Desconexión de node2 entre rondas y su reincorporación |
| `04-coordinador-caido.txt` | (Opcional) Caída del coordinador |

Los archivos son `.txt` porque el `.gitignore` del repo ignora `*.log`.
