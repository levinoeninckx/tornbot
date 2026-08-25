## Build
build:
    dotnet build ./TornBot.sln 

run:
    dotnet run --project ./TornBot.Bot.csproj

test: 
    dotnet test ./TornBot.sln

release:
    dotnet build --configuration Release ./TornBot.sln
