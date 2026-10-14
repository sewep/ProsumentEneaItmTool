# Prosument Enea Itm Tool

Aplikacja dla prosumentów Enea do analizy energii wysłanej do sieci i pobranej z sieci. Importuje pliki CSV z godzinowymi danymi pomiarowymi pobrane z eBOK Enea i pokazuje sumy oraz wykres narastający, także po uwzględnieniu opustu (współczynnik 0,8 lub 0,7).

*Application for Enea (Poland) prosumers: imports hourly metering CSV files from the Enea customer panel and analyses the energy fed into and taken from the grid.*

![image](https://github.com/user-attachments/assets/4025c446-6571-4caf-8896-7877711564f0)

## Jak pobrać dane z Enea

1. W panelu klienta Enea otwórz dane godzinowe i wybierz zakres (np. rok).
2. Kliknij przycisk pobrania danych w formacie CSV.
3. W aplikacji kliknij **Importuj z pliku CSV** i wskaż pobrany plik.

Importowane dane są dopisywane do już zapisanych. Godziny, które już są w bazie, zostają zastąpione danymi z nowego pliku, więc ten sam plik można wczytać ponownie bez dublowania danych.

## Instalacja

Pobierz najnowsze wydanie z zakładki [Releases](https://github.com/sewep/ProsumentEneaItmTool/releases):

- `ProsumentEneaItmToolSetup_X.Y.Z.exe` – instalator,
- `ProsumentEneaItmTool_X.Y.Z.zip` – wersja bez instalacji (wystarczy rozpakować).

Wymagany jest [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (Windows 10 lub nowszy). Instalator sprawdza jego obecność.

## Dane

Zaimportowane dane są zapisywane w bazie SQLite:

```
%LOCALAPPDATA%\ProsumentEneaItmTool\datastorage.sqlite
```

Wersje do 1.0.5 trzymały bazę w katalogu programu. Przy pierwszym uruchomieniu nowsza wersja kopiuje ją automatycznie do nowego miejsca (stary plik zostaje jako kopia zapasowa).

## Budowanie

Wymagany .NET 10 SDK.

```bash
dotnet build
dotnet test ProsumentEneaItmTool.Tests
dotnet publish ProsumentEneaItmTool/ProsumentEneaItmTool.csproj -c Release -o ./publish
```

Instalator buduje [Inno Setup 6](https://jrsoftware.org/isinfo.php) ze skryptu `SetupBuilder/ProsumentEneaItmToolSetup.iss` (oczekuje plików w `./publish`):

```bash
ISCC /DMyAppVersion=1.1.0 SetupBuilder/ProsumentEneaItmToolSetup.iss
```

Migracje bazy: `dotnet ef migrations add <Nazwa> --project ProsumentEneaItmTool`.

## Wydanie

Wersja jest ustawiana w jednym miejscu – `Directory.Build.props`. Workflow **Build main** buduje i testuje każdy push oraz pull request. Uruchomiony ręcznie (*Run workflow* na `main`) tworzy wydanie `vX.Y.Z` z instalatorem i archiwum zip.
