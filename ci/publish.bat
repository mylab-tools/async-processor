echo off

IF [%1]==[] goto noparam

echo "Build project ..."
dotnet publish ..\src\MyLab.AsyncProcessor.Api\MyLab.AsyncProcessor.Api.csproj -c Release -o .\out\app

echo "Build image '%1' and 'latest'..."
docker build -t ozzyext/mylab-async-proc-api:%1 -t ozzyext/mylab-async-proc-api:latest .

echo "Publish image '%1' ..."
docker push ozzyext/mylab-async-proc-api:%1

echo "Publish image 'latest' ..."
docker push ozzyext/mylab-async-proc-api:latest

goto done

:noparam
echo "Please specify image version"
goto done

:done
echo "Done!"