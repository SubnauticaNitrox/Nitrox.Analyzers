# Nitrox.Analyzers

Analyzers and source generators to assist with Nitrox mod development

## Local Testing
1. Run `dotnet build -c Release`
2. Add NuGET source to your IDE, pointing to the Release folder of this project.
3. Change NuGET source in your IDE to use local
4. Change version number of the Nitrox.Analyzers dependency
5. Run `dotnet restore` in the project referencing Nitrox.Analyzers

## Deploy
1. Run `dotnet build -c Release`
2. Upload nuget package to NuGET


## Helpful links for contributors
https://andrewlock.net/series/creating-a-source-generator/
