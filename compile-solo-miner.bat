																					
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net8.0 --output Output\SoloMiner\Linux\x64\Release\Net8\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net8.0 --output Output\SoloMiner\Windows\x64\Release\Net8\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net8.0 --output Output\SoloMiner\Linux\x64\Debug\Net8\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net8.0 --output Output\SoloMiner\Windows\x64\Debug\Net8\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true


dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net7.0 --output Output\SoloMiner\Linux\x64\Debug\Net7\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net7.0 --output Output\SoloMiner\Windows\x64\Debug\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net7.0 --output Output\SoloMiner\Linux\x64\Release\Net7\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net7.0 --output Output\SoloMiner\Windows\x64\Release\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net6.0 --output Output\SoloMiner\Linux\x64\Debug\Net6\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net6.0 --output Output\SoloMiner\Windows\x64\Debug\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net6.0 --output Output\SoloMiner\Linux\x64\Release\Net6\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net6.0 --output Output\SoloMiner\Windows\x64\Release\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net5.0 --output Output\SoloMiner\Linux\x64\Debug\Net5\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Debug --framework net5.0 --output Output\SoloMiner\Windows\x64\Debug\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net5.0 --output Output\SoloMiner\Linux\x64\Release\Net5\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Solo-Miner\SeguraChain-Solo-Miner.csproj --configuration Release --framework net5.0 --output Output\SoloMiner\Windows\x64\Release\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true


pause