# Architektura aplikacji Investe

## Ogólny podział

Aplikacja składa się z trzech niezależnych warstw, które komunikują się ze sobą przez dobrze zdefiniowane interfejsy:

```
[ Angular 17 SPA ]  ←→  [ ASP.NET Core Web API ]  ←→  [ MS SQL Server ]
                                    ↕
                           [ CoinGecko API ]
```

Nie ma żadnej logiki biznesowej po stronie frontendu — Angular odpowiada wyłącznie za prezentację danych i interakcję z użytkownikiem. Cała logika (obliczenia P&L, walidacja, pobieranie cen) żyje w API.

---

## Frontend — Angular 17 SPA

Angular działa jako klasyczna aplikacja jednostronicowa. Przeglądarka pobiera aplikację raz, a następnie cała nawigacja odbywa się po stronie klienta bez przeładowania strony.

**Routing** — Angular Router zarządza widokami. Trasy chronione przez `AuthGuard`, który sprawdza obecność i ważność tokenu JWT przed załadowaniem widoku.

**HTTP Interceptor** — każde wychodzące żądanie HTTP jest automatycznie wzbogacane o nagłówek `Authorization: Bearer <token>`. Użytkownik nigdy nie dodaje tokenu ręcznie.

**Serwisy** — logika komunikacji z API jest wydzielona do serwisów (`PortfolioService`, `TransactionService`, itp.), które są wstrzykiwane do komponentów przez mechanizm Dependency Injection.

**Reaktywność** — komponenty subskrybują dane przez `Observable` (RxJS), co umożliwia automatyczne odświeżanie widoku przy zmianie danych bez ręcznego odpytywania.

---

## Backend — ASP.NET Core Web API (architektura warstwowa)

Backend zorganizowany jest w klasyczny układ warstw, gdzie każda warstwa ma jedną odpowiedzialność i komunikuje się tylko z warstwą bezpośrednio poniżej:

```
Controllers
    ↓
Services
    ↓
Repositories
    ↓
EF Core DbContext → MS SQL Server
```

**Controllers** — odbierają żądania HTTP, sprawdzają autoryzację JWT, delegują pracę do serwisów i zwracają odpowiedź. Nie zawierają logiki biznesowej.

**Services** — serce aplikacji. Tu żyją obliczenia P&L, walidacja danych transakcji, koordynacja między repozytoriami i wywołania do CoinGecko API.

**Repositories** — jedyna warstwa, która zna Entity Framework Core. Odpowiada za zapytania do bazy danych. Serwisy nigdy nie piszą LINQ bezpośrednio — idą przez repozytorium.

**DbContext (EF Core)** — mapuje obiekty C# na tabele SQL Server. Migracje zarządzają schematem bazy danych.

---

## Pobieranie cen z CoinGecko — dwa tryby

Ceny działają na dwóch poziomach, co pozwala łączyć aktualność danych z poszanowaniem limitów darmowego planu CoinGecko (~30 req/min):

**Background Job (`IHostedService`)** — uruchamiany cyklicznie (np. co 5 minut) w tle serwera. Pobiera ceny dla wszystkich kryptowalut, które posiadają użytkownicy, i zapisuje wyniki w bazie (tabela cache). Użytkownicy nieaktywni też mają aktualne dane przy następnym logowaniu.

**On-demand** — gdy użytkownik otworzy dashboard, API sprawdza wiek danych w cache. Jeśli dane są świeże — zwraca je natychmiast z bazy bez wywołania CoinGecko. Jeśli są przestarzałe (np. serwer był wyłączony) — pobiera je na żywo i aktualizuje cache.

Dzięki temu CoinGecko nie jest odpytywane przy każdym żądaniu użytkownika, a dane są zawsze wystarczająco aktualne.

---

## Uwierzytelnianie — ASP.NET Identity + JWT

Rejestracja i logowanie obsługiwane przez ASP.NET Identity (hashowanie haseł, zarządzanie użytkownikami). Po poprawnym logowaniu API generuje token JWT podpisany kluczem tajnym, który zawiera identyfikator użytkownika i czas wygaśnięcia.

Token nie jest przechowywany na serwerze — API jest **bezstanowe**. Przy każdym żądaniu token jest weryfikowany kryptograficznie bez odpytywania bazy danych. Każdy zasób (portfele, transakcje, alerty) jest filtrowany po `UserId` wyciągniętym z tokenu, więc użytkownik nigdy nie może zobaczyć danych innego użytkownika.

```
POST /api/auth/login
        ↓
ASP.NET Identity weryfikuje hasło
        ↓
API generuje i zwraca token JWT
        ↓
Angular przechowuje token lokalnie
        ↓
Każde kolejne żądanie → Interceptor dołącza token → API weryfikuje → zwraca dane
```