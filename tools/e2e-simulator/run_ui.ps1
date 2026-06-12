# Launch the E2E Simulator Streamlit UI
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$env:STREAMLIT_BROWSER_GATHER_USAGE_STATS = "false"

Write-Host "Starting E2E Simulator UI at http://localhost:8501"
Write-Host "Press Ctrl+C to stop."
python -m streamlit run app.py --server.port 8501 --browser.gatherUsageStats false
