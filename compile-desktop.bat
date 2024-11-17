dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net8.0-windows --output Output\Desktop-Wallet\Windows\x64\Release\Net8\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net7.0-windows --output Output\Desktop-Wallet\Windows\x64\Release\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net6.0-windows --output Output\Desktop-Wallet\Windows\x64\Release\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net5.0-windows --output Output\Desktop-Wallet\Windows\x64\Release\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Debug --framework net7.0-windows --output Output\Desktop-Wallet\Windows\x64\Debug\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Debug --framework net6.0-windows --output Output\Desktop-Wallet\Windows\x64\Debug\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Debug --framework net5.0-windows --output Output\Desktop-Wallet\Windows\x64\Debug\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

pause