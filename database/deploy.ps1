param(
    [string]$Server = "(localdb)\AeChallenge",
    [string]$Database = "AeShipManagement",
    [string]$User,
    [string]$Password,
    [string]$ApiDbPassword
)

$ErrorActionPreference = "Stop"
$scripts = @("01_schema.sql", "02_functions_views.sql", "03_procedures.sql", "04_seed_reference.sql", "05_seed_sample.sql")

[string[]]$auth = if ($User) { @("-U", $User, "-P", $Password) } else { @("-E") }

$recreate = "IF DB_ID('$Database') IS NOT NULL BEGIN ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database]; END; CREATE DATABASE [$Database];"
& sqlcmd -S $Server @auth -C -b -Q $recreate
if ($LASTEXITCODE -ne 0) { throw "Failed to create database $Database" }

foreach ($script in $scripts) {
    Write-Host "Running $script"
    & sqlcmd -S $Server @auth -C -b -I -d $Database -i (Join-Path $PSScriptRoot $script)
    if ($LASTEXITCODE -ne 0) { throw "Failed while running $script" }
}

if ($ApiDbPassword) {
    Write-Host "Running 06_api_login.sql"
    & sqlcmd -S $Server @auth -C -b -I -d $Database -v "API_DB_PASSWORD=$ApiDbPassword" -i (Join-Path $PSScriptRoot "06_api_login.sql")
    if ($LASTEXITCODE -ne 0) { throw "Failed while running 06_api_login.sql" }
}

Write-Host "Database $Database deployed to $Server"
