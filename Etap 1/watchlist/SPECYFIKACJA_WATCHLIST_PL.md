# Specyfikacja Funkcjonalności: Lista Obserwowanych (Watchlist)

**Projekt:** Investe - Tracker Inwestycji Kryptowalutowych  
**Funkcjonalność:** Lista Obserwowanych Monet  
**Status:** Zaimplementowano  

---

## 1. Podsumowanie
Funkcjonalność **Watchlist** umożliwia użytkownikom śledzenie cen wybranych kryptowalut bez konieczności posiadania ich w portfelu. Pozwala na szybki podgląd aktualnej sytuacji rynkowej dla interesujących użytkownika aktywów, stanowiąc narzędzie wspierające decyzje inwestycyjne.

### Główne możliwości:
- Dodawanie monet do osobistej listy na podstawie symbolu (np. BTC, DOGE).
- Śledzenie cen w czasie rzeczywistym dzięki integracji z API CoinGecko.
- Inteligentne formatowanie cen (zaokrąglanie do 2 miejsc dla monet powyżej 1$ i do 3-4 miejsc dla monet groszowych jak DOGE).
- Zarządzanie listą (usuwanie niepotrzebnych pozycji).
- Ochrona przed duplikatami na liście.

---

## 2. Wymagania Biznesowe

### 2.1 Wymagania Funkcjonalne

#### WF-1: Dodawanie monet do listy
- Użytkownik może dodać monetę, wpisując jej symbol.
- System automatycznie mapuje symbol na unikalny identyfikator CoinGecko.
- System zapobiega dodaniu tej samej monety wielokrotnie przez jednego użytkownika.
- Każda pozycja zapisywana jest w bazie danych z datą dodania.

#### WF-2: Przeglądanie listy
- Użytkownik widzi tabelę ze wszystkimi obserwowanymi monetami.
- Tabela zawiera: Symbol, ID monety, Aktualną cenę (USD) oraz Datę dodania.
- Ceny są pobierane batchowo (jedno zapytanie dla całej listy), co optymalizuje limity API.

#### WF-3: Usuwanie z listy
- Użytkownik może w dowolnym momencie usunąć monetę z listy.
- Usunięcie jest trwałe, ale moneta może zostać dodana ponownie.

#### WF-4: Integracja z cenami
- System korzysta z serwera pośredniczącego, który cache'uje ceny na 30 sekund.
- Zastosowano formatowanie cen dostosowane do wartości monety (eliminacja zbyt długich ciągów cyfr).

---

## 3. Model Danych

### 3.1 Encja: WatchlistItem

- **Id**: Unikalny identyfikator (GUID).
- **UserId**: Klucz obcy powiązany z użytkownikiem.
- **CoinId**: Identyfikator systemowy (np. "bitcoin").
- **Symbol**: Symbol wyświetlany (np. "BTC").
- **AddedAt**: Data i czas dodania pozycji.

---

## 4. Specyfikacja API

### Podstawowy adres: `/api/watchlist`

| Metoda | Endpoint | Opis |
|--------|----------|-------------|
| GET | `/api/watchlist` | Pobiera wszystkie obserwowane monety wraz z cenami |
| POST | `/api/watchlist` | Dodaje nową monetę do listy |
| DELETE | `/api/watchlist/{id}` | Usuwa monetę z listy |
| GET | `/api/watchlist/check/{coinId}` | Sprawdza, czy moneta jest już na liście |

---

## 5. Design UI/UX
- Interfejs utrzymany w ciemnej kolorystyce (Dark Mode), spójny z resztą aplikacji.
- Tabela danych (`data-table`) z interaktywnymi wierszami (hover).
- Powiadomienia (Toast) informujące o sukcesie dodania lub błędach.
- Pasek filtrów wykorzystywany do wprowadzania nowych symboli.
