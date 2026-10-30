# Ejercicio 2: algoritmo de Berkeley

**Responsables:** Valentino (coordinador y nodos) y Melina (pruebas, escenarios de desfase/desconexión, registros)

Estructura prevista (según la guía):

```
ej2-berkeley/
├── ClockNode/             Web API con controladores (Controllers/ClockController.cs)
├── BerkeleyCoordinator/   Consola
├── BerkeleyLab.sln
├── docker-compose.yml
└── logs/                  Registros de ejecución de Melina
```

Puertos: ver [`docs/CONVENCIONES.md`](../docs/CONVENCIONES.md) (`node1..3` → host 8181–8183).
Partir de [`plantilla/`](../plantilla).

## Pruebas (Melina)

Cada escenario del enunciado es un archivo en [`escenarios/`](escenarios) que se suma al `docker-compose.yml` base y solo cambia las variables necesarias. El script [`correr-escenarios.ps1`](correr-escenarios.ps1) los levanta uno por uno, los deja correr unas cuatro rondas y guarda el registro en [`logs/`](logs).

```powershell
cd ej2-berkeley
.\correr-escenarios.ps1          # todos (unos 5 minutos)
.\correr-escenarios.ps1 02a 03   # solo algunos, por prefijo
```

| Escenario | Prueba del enunciado | Resultado |
|---|---|---|
| `01-desfases` | Diferencias iniciales de 5, 10 y 20 s | Desde la ronda 3 los cuatro relojes quedan a menos de 5 ms entre sí (todos a unos −1,25 s de la hora real) |
| `02a-nodo-5min-sin-descarte` | Nodo con 5 min, entra en el promedio | El promedio da +78,8 s y todo el grupo queda unos 79 s adelantado |
| `02b-nodo-5min-con-descarte` | Nodo con 5 min, umbral de 60 s | node3 queda fuera del promedio en la ronda 1 pero recibe su corrección (−295 s); desde la ronda 2 participa y los cuatro relojes convergen cerca de +5 s |
| `03-desconexion` | Desconexión de un participante | El coordinador sigue con los nodos disponibles; node2 vuelve con +10 s y arrastra al grupo unos 2,8 s |
| `04-coordinador-caido` | (Extra) Caída del coordinador | Los nodos conservan su último offset y dejan de recibir ajustes |

Los registros se guardan como `.txt` porque el `.gitignore` del repo ignora `*.log`.
