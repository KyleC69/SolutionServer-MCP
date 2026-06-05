<#
.SYNOPSIS
    Installs the SolutionServer MCP executable into the user's global MCP tools directory.

.DESCRIPTION
    This script creates a persistent global installation directory at:
        ~/.solutionMcp

    It then copies the SolutionServer executable (located in the same directory
    as this script) into that folder.

    The script includes:
        - Safety checks
        - Logging
        - Error handling
        - Idempotent behavior
        - Directory creation with validation

.NOTES
    Author: SolutionServer Installer
    Purpose: Global installation of MCP server binary
    Version: 1.0.0
#>

# -----------------------------
# Logging helper
# -----------------------------
function Write-Log {
    param([string]$Message)
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Write-Host "[$timestamp] $Message"
}

Write-Log "Starting SolutionServer installation..."

try {
    # -----------------------------
    # Resolve paths
    # -----------------------------
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $InstallDir = Join-Path $HOME ".solutionMcp"

    Write-Log "Script directory: $ScriptDir"
    Write-Log "Install directory: $InstallDir"

    # -----------------------------
    # Ensure install directory exists
    # -----------------------------
    if (-Not (Test-Path $InstallDir)) {
        Write-Log "Creating install directory..."
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }
    else {
        Write-Log "Install directory already exists."
    }

    # -----------------------------
    # Locate the EXE in the script directory
    # -----------------------------
    $Exe = Get-ChildItem -Path $ScriptDir -Filter "*.exe" | Select-Object -First 1

    if (-Not $Exe) {
        throw "No .exe file found in script directory. Installer cannot continue."
    }

    Write-Log "Found executable: $($Exe.Name)"

    # -----------------------------
    # Copy EXE to install directory
    # -----------------------------
    $TargetExe = Join-Path $InstallDir $Exe.Name

    Write-Log "Copying executable to global install directory..."
    Copy-Item -Path $Exe.FullName -Destination $TargetExe -Force

    Write-Log "Executable installed at: $TargetExe"

    # -----------------------------
    # Final success message
    # -----------------------------
    Write-Log "SolutionServer installation completed successfully."

}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
}
