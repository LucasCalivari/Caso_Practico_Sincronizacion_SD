<#
  Corre los escenarios de prueba del Ej. 1 (Cristian) y guarda un registro por escenario
  en logs/. Ejecutar desde la carpeta ej1-cristian:

      .\correr-escenarios.ps1            # todos
      .\correr-escenarios.ps1 01 02      # solo algunos (por prefijo)

  Requiere Docker Desktop encendido. Cada escenario tarda alrededor de 35 segundos.
#>
param([string[]]$Solo)

$ErrorActionPreference = 'Continue'
# Los contenedores escriben UTF-8; sin esto PowerShell muestra caracteres extraños
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Inv = [System.Globalization.CultureInfo]::InvariantCulture
Set-Location $PSScriptRoot
New-Item -ItemType Directory -Force -Path logs | Out-Null

$Proyecto = 'cristian-pruebas'
$ServerUrl = 'http://localhost:8080'

function Compose([string]$Escenario) {
    $cmd = @('compose', '-p', $Proyecto, '-f', 'docker-compose.yml', '-f', "escenarios/$Escenario.yml") + $script:ComposeArgs
    & docker @cmd
    if ($LASTEXITCODE -ne 0) { throw "docker $($cmd -join ' ') falló" }
}

function Estado-Servidor([string]$Momento) {
    $lineas = @("", "=== Estado del servidor de tiempo ($Momento) ===")
    try {
        $r = Invoke-RestMethod -Uri "$ServerUrl/api/health" -TimeoutSec 3
        $hora = if ($r.serverTime -is [datetime]) { $r.serverTime.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ', $Inv) } else { "$($r.serverTime)" }
        $lineas += [string]::Format($Inv, "time-server: status={0} hora={1} offset={2:F1} ms", $r.status, $hora, [double]$r.offsetMs)
    } catch {
        $lineas += "time-server: no responde ($($_.Exception.Message))"
    }
    return $lineas
}

function Correr([string]$Escenario, [scriptblock]$Pasos) {
    if ($Solo -and -not ($Solo | Where-Object { $Escenario.StartsWith($_) })) { return }

    Write-Host "`n>>> Escenario $Escenario" -ForegroundColor Cyan
    # Detener tanto el proyecto base como el de pruebas para liberar el puerto 8080 si quedaron corriendo
    & docker compose down --remove-orphans 2>$null | Out-Null
    $script:ComposeArgs = @('down', '--remove-orphans'); Compose $Escenario 2>$null | Out-Null
    $script:ComposeArgs = @('up', '-d', '--build');      Compose $Escenario

    $estado = @()
    Start-Sleep -Seconds 3
    $estado += Estado-Servidor 'al iniciar'
    $estado += & $Pasos
    $estado += Estado-Servidor 'al final'

    # Log completo, ordenado por hora real (primer campo de cada línea)
    $script:ComposeArgs = @('logs', '--no-color', '--no-log-prefix')
    $log = Compose $Escenario | Where-Object { $_ -match '^\[' } | Sort-Object

    $archivo = "logs/$Escenario.txt"
    @("# Ej. 1 Cristian - escenario $Escenario", "# Generado: $(Get-Date -Format s)", "") + $log + $estado |
        Set-Content -Path $archivo -Encoding utf8
    Write-Host "Registro guardado en $archivo" -ForegroundColor Green

    $script:ComposeArgs = @('down', '--remove-orphans'); Compose $Escenario | Out-Null
}

# 1. Escenario base con desfases iniciales del enunciado (+0s, +8s, -5s vs +2s del servidor)
Correr '01-desfases-base' { Start-Sleep -Seconds 35 }

# 2. Escenario de alta latencia (servidor con delay de 300 ms, aumento de RTT y margen de error)
Correr '02-alta-latencia' { Start-Sleep -Seconds 35 }

# 3. Escenario de tolerancia a fallos ante caída del servidor de tiempo central
Correr '03-servidor-caido' {
    Start-Sleep -Seconds 12                        # Primera ronda normal sincronizada
    $script:ComposeArgs = @('stop', 'time-server'); Compose '03-servidor-caido' | Out-Null
    $e = Estado-Servidor 'con time-server detenido'
    Start-Sleep -Seconds 20                        # Ronda con servidor caído (clientes capturan timeout y continúan)
    $script:ComposeArgs = @('start', 'time-server'); Compose '03-servidor-caido' | Out-Null
    Start-Sleep -Seconds 15                        # Servidor se recupera y clientes se re-sincronizan
    return $e
}

Write-Host "`nListo. Registros en ej1-cristian/logs/" -ForegroundColor Green
