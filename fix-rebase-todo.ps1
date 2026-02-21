# Git rebase todo dosyasinda 5e2c28f satirini "pick" -> "edit" yapar
param([string]$TodoFile = $args[0])
$content = Get-Content -Path $TodoFile -Raw
$content = $content -replace '^pick 5e2c28f ', 'edit 5e2c28f '
Set-Content -Path $TodoFile -Value $content.TrimEnd() -NoNewline
