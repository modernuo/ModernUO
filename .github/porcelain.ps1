$untrackedOrModified = git status --porcelain | Select-String -Pattern "^\?\? |^ M"

if ($untrackedOrModified) {
    Write-Host "Untracked or modified files found."
    Exit 1
}
else {
    Write-Host "No untracked or modified files."
    Exit 0
}
