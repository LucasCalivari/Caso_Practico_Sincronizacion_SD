<#
  Corre los escenarios de prueba del Ej. 2 (Berkeley) y guarda un registro por escenario
  en logs/. Ejecutar desde la carpeta ej2-berkeley:

      .\correr-escenarios.ps1            # todos
      .\correr-escenarios.ps1 02a 03     # solo algunos (por prefijo)

  Requiere Docker Desktop encendido. Cada escenario tarda alrededor de 1 minuto.
#>
param([string[]]$Solo)

$ErrorActionPreference = 'Stop'
# Los contenedores escriben UTF-8; sin esto PowerShell muestra 'Correcci├│n'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Inv = [System.Globalization.CultureInfo]::InvariantCulture
Set-Location $PSScriptRoot
New-Item -ItemType Directory -Force -Path logs | Out-Null

$Proyecto = 'berkeley-pruebas'
$Nodos = [ordered]@{ node1 = 8181; node2 = 8182; node3 = 8183 }

function Compose([string]$Escenario) {
    $cmd = @('compose', '-p', $Proyecto, '-f', 'docker-compose.yml', '-f', "escenarios/$Escenario.yml") + $script:ComposeArgs
    & docker @cmd
    if ($LASTEXITCODE -ne 0) { throw "docker $($cmd -join ' ') falló" }
}

function Estado-Nodos([string]$Momento) {
    $lineas = @("", "=== Estado de los nodos ($Momento) ===")
    foreach ($n in $Nodos.Keys) {
        try {
            $r = Invoke-RestMethod -Uri "http://localhost:$($Nodos[$n])/api/clock" -TimeoutSec 3
            $hora = if ($r.utcTime -is [datetime]) { $r.utcTime.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ', $Inv) } else { "$($r.utcTime)" }
            $lineas += [string]::Format($Inv, "{0}: hora={1} offset={2:F1} ms", $n, $hora, [double]$r.offsetMs)
        } catch {
            $lineas += "${n}: no responde"
        }
    }
    return $lineas
}

function Correr([string]$Escenario, [scriptblock]$Pasos) {
    if ($Solo -and -not ($Solo | Where-Object { $Escenario.StartsWith($_) })) { return }

    Write-Host "`n>>> Escenario $Escenario" -ForegroundColor Cyan
    $script:ComposeArgs = @('down', '--remove-orphans'); Compose $Escenario | Out-Null
    $script:ComposeArgs = @('up', '-d', '--build');      Compose $Escenario

    $estado = @()
    Start-Sleep -Seconds 2
    $estado += Estado-Nodos 'antes de la primera ronda'
    $estado += & $Pasos
    $estado += Estado-Nodos 'al final'

    # Log completo, ordenado por hora real (primer campo de cada línea)
    $script:ComposeArgs = @('logs', '--no-color', '--no-log-prefix')
    $log = Compose $Escenario | Where-Object { $_ -match '^\[' } | Sort-Object

    $archivo = "logs/$Escenario.txt"
    @("# Ej. 2 Berkeley - escenario $Escenario", "# Generado: $(Get-Date -Format s)", "") + $log + $estado |
        Set-Content -Path $archivo -Encoding utf8
    Write-Host "Registro guardado en $archivo" -ForegroundColor Green

    $script:ComposeArgs = @('down', '--remove-orphans'); Compose $Escenario | Out-Null
}

# Rondas cada 15 s; la primera arranca unos 3 s después de levantar el coordinador.

Correr '01-desfases' { Start-Sleep -Seconds 50 }

Correr '02a-nodo-5min-sin-descarte' { Start-Sleep -Seconds 50 }

Correr '02b-nodo-5min-con-descarte' { Start-Sleep -Seconds 50 }

Correr '03-desconexion' {
    Start-Sleep -Seconds 10                       # ronda 1 con los tres nodos
    $script:ComposeArgs = @('stop', 'node2'); Compose '03-desconexion' | Out-Null
    $e = Estado-Nodos 'con node2 detenido'
    Start-Sleep -Seconds 30                       # rondas 2 y 3 sin node2
    $script:ComposeArgs = @('start', 'node2'); Compose '03-desconexion' | Out-Null
    Start-Sleep -Seconds 25                       # node2 vuelve (con su offset inicial) y se resincroniza
    return $e
}

Correr '04-coordinador-caido' {
    Start-Sleep -Seconds 25                       # rondas 1 y 2
    $script:ComposeArgs = @('stop', 'coordinator'); Compose '04-coordinador-caido' | Out-Null
    $e = Estado-Nodos 'recién caído el coordinador'
    Start-Sleep -Seconds 30                       # sin coordinador: nadie ajusta
    return $e
}

Write-Host "`nListo. Registros en ej2-berkeley/logs/" -ForegroundColor Green
