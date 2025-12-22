param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

docker buildx build `
    --platform linux/amd64 `
    -t photoatomic/garageunderground:latest `
    -t "photoatomic/garageunderground:$Version" `
    --push `
    .