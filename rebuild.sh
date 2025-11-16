rm -f LDtk.JsonSchema/FullSchema.json
rm -f LDtk.JsonSchema/MinSchema.json

cd LDtk.JsonSchema/
dotnet run

cd ../docs/
npm run build:api

cd ../
