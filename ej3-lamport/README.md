# Ejercicio 3: relojes lógicos de Lamport

**Responsable:** Santiago

Tres servicios Web API; la marca viaja en el encabezado `X-Lamport-Time`.

Estructura prevista (según la guía):

```
ej3-lamport/
├── OrdersService/      Controllers/OrdersController.cs, LamportClock.cs, Program.cs, Dockerfile
├── InventoryService/   Controllers/InventoryController.cs, LamportClock.cs, Program.cs, Dockerfile
├── PaymentsService/    Controllers/PaymentsController.cs, LamportClock.cs, Program.cs, Dockerfile
├── LamportPedidos.sln
└── docker-compose.yml
```

Puertos: ver [`docs/CONVENCIONES.md`](../docs/CONVENCIONES.md) (orders 8281, inventory 8282, payments 8283).
Partir de [`plantilla/`](../plantilla).
