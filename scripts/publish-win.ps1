param(
    [string]$Configuration = "Release"
)

dotnet publish CSharpHealth.Cli -c $Configuration -r win-x64 --self-contained false
