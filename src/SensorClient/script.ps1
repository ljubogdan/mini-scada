# start-sensors.ps1

$baseId = "44444444-4444-4444-4444-44444444444"
$names = @("Pera", "Mika", "Laza", "Jova", "Sava", "Tosa", "Bora", "Zika", "Goca", "Maca")

for ($i = 0; $i -le 9; $i++) {
    $id = "${baseId}${i}"
    $name = $names[$i]
    Write-Host "Starting sensor: Id=$id Name=$name"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '${PWD}'; dotnet run -- Sensor:Id=${id} Sensor:Name=${name}"
}