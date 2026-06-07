# Investe 📈

> Pełnostackowa aplikacja do śledzenia inwestycji kryptowalutowych — projekt akademicki

Investe to aplikacja webowa umożliwiająca użytkownikom śledzenie portfeli kryptowalutowych na wielu kontach, monitorowanie cen na żywo, rejestrowanie transakcji, analizę zysków i strat oraz otrzymywanie alertów cenowych — wszystko w jednym miejscu.

---

## Stos technologiczny

| Warstwa | Technologia |
|---|---|
| Frontend | Angular 17 (SPA) |
| Backend | ASP.NET Core Web API (.NET 8) |
| Baza danych | SQLite (Development) / SQL Server |
| Uwierzytelnianie | ASP.NET Identity + JWT |
| Dane krypto | CoinGecko API (plan bezpłatny) |

---

## Funkcionalności

- **Przegląd portfela** — bieżąca łączna wartość portfela we wszystkich portfelach
- **Współdzielenie portfeli** — możliwość udostępniania portfela innym użytkownikom do wspólnego śledzenia i zarządzania transakcjami
- **Ceny kryptowalut na żywo** — dane pobierane z CoinGecko API i automatycznie odświeżane
- **Wiele portfeli / kont** — organizowanie aktywów w oddzielnych portfelach dla każdego użytkownika
- **Historia transakcji kupna / sprzedaży** — rejestrowanie i przeglądanie wszystkich transakcji z datą i ceną
- **Obliczanie zysku i straty** — wyniki dla każdego aktywa oraz dla całego portfela
- **Wykresy i analityka** — wizualizacja alokacji, wyników w czasie i P&L
- **Alerty i powiadomienia** — ustawianie progów cenowych i otrzymywanie powiadomień w aplikacji
- **Uwierzytelnianie użytkowników** — bezpieczna rejestracja i logowanie za pomocą ASP.NET Identity z tokenami JWT

---

## Jak uruchomić projekt

### Wymagania
- .NET 8 SDK
- Node.js & npm

### Backend
1. Przejdź do folderu `Serwer/Serwer`.
2. Przywróć narzędzia: `dotnet tool restore`.
3. Uruchom serwer: `dotnet run`. (Baza SQLite zostanie automatycznie zainicjalizowana i zaktualizowana).

### Frontend
1. Przejdź do folderu `Client`.
2. Zainstaluj zależności: `npm install`.
3. Uruchom aplikację: `npm start`.
4. Otwórz `http://localhost:4200`.

---

## Frontend — Angular 17

Warstwa frontendowa to jednostronicowa aplikacja (SPA) zbudowana w Angular 17. Interfejs użytkownika jest responsywny i zaprojektowany z myślą o czytelności danych finansowych.

Aplikacja składa się z następujących widoków:

**Uwierzytelnianie** — strony rejestracji i logowania z walidacją formularzy po stronie klienta. Po zalogowaniu token JWT jest przechowywany lokalnie i dołączany automatycznie do każdego żądania HTTP za pomocą interceptora.

**Dashboard** — główny ekran aplikacji prezentujący łączną wartość portfela, procentowe zmiany w ciągu ostatnich 24 godzin oraz skrócony przegląd posiadanych aktywów. Dane odświeżają się automatycznie w określonych interwałach.

**Portfele** — widok listy wszystkich portfeli użytkownika z możliwością tworzenia nowych i przełączania się między nimi. Każdy portfel wyświetla przypisane do niego aktywa wraz z ich aktualną wartością.

**Transakcje** — tabela historii wszystkich operacji kupna i sprzedaży z możliwością filtrowania według daty, aktywa i typu transakcji. Nowe transakcje dodaje się przez formularz z autouzupełnianiem nazw kryptowalut.

**Analityka** — widok z wykresami prezentującymi alokację portfela (wykres kołowy), historię wartości portfela w czasie (wykres liniowy) oraz zestawienie zysków i strat dla każdego aktywa (wykres słupkowy).

**Alerty** — panel zarządzania alertami cenowymi. Użytkownik może ustawić próg cenowy dla dowolnej kryptowaluty (powyżej lub poniżej wartości) i otrzymać powiadomienie w aplikacji w momencie jego przekroczenia.

---

## Backend — ASP.NET Core Web API

API zbudowane w ASP.NET Core (.NET 8) udostępnia endpointy REST obsługujące całą logikę biznesową aplikacji. Dane przechowywane są w bazie Microsoft SQL Server, dostęp do nich realizowany jest przez Entity Framework Core. Ceny kryptowalut pobierane są cyklicznie z CoinGecko API i buforowane po stronie serwera, aby nie przekraczać limitów bezpłatnego planu.

Uwierzytelnianie opiera się na ASP.NET Identity — po poprawnym zalogowaniu użytkownik otrzymuje token JWT, który jest weryfikowany przy każdym chronionym żądaniu.

---

## Kontekst akademicki

Projekt został opracowany w ramach zajęć akademickich. Demonstruje:

- Architekturę SPA z Angular 17 (komponenty standalone, signals, lazy loading)
- Projektowanie RESTful API z ASP.NET Core
- Modelowanie relacyjnej bazy danych z Entity Framework Core
- Bezstanowe uwierzytelnianie z wykorzystaniem JWT
- Integrację z zewnętrznym API (CoinGecko)
- Wizualizację danych finansowych (wykresy, P&L)
- Użuwanie technologi AI w tworzeniu oprogramnowania

---

## Licencja

Wyłącznie do użytku akademickiego. Nie jest licencjonowany do wdrożeń produkcyjnych ani zastosowań komercyjnych.