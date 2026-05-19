# Specyfikacja Funkcjonalności: Moduł Transakcji (CRUD)

## 1. Cel funkcjonalności
Głównym celem modułu jest umożliwienie użytkownikowi pełnego zarządzania historią operacji na aktywach cyfrowych (kupno/sprzedaż). Moduł automatycznie integruje się z portfelem użytkownika, aktualizując stan posiadania (Assets) oraz wyliczając średnią cenę zakupu (Average Buy Price) w czasie rzeczywistym.

## 2. Zakres Funkcjonalny (CRUD)
System wspiera pełny cykl życia transakcji:
*   **Create (Dodawanie):** Rejestracja nowej operacji z automatycznym tworzeniem lub aktualizacją aktywów w portfelu.
*   **Read (Przeglądanie):** Zaawansowane wyświetlanie historii z filtrowaniem po symbolu, portfelu oraz zakresie dat, wspierane przez paginację serwerową.
*   **Update (Edycja):** Możliwość korekty dowolnego pola transakcji (ilość, cena, prowizja, data) z inteligentnym przeliczeniem stanu portfela wstecz.
*   **Delete (Usuwanie):** Cofnięcie skutków transakcji i przywrócenie stanu portfela sprzed operacji.

## 3. Szczegółowa Logika Biznesowa

### 3.1. Algorytm Średniej Ceny Zakupu (Average Buy Price)
System stosuje metodę wagową do obliczania kosztu aktywów. Przy każdej transakcji typu **Buy**, średnia cena zakupu jest aktualizowana wg wzoru:
`NowaŚrednia = (StaraIlość * StaraŚrednia + NowaIlość * CenaTransakcji) / (StaraIlość + NowaIlość)`

Przy edycji historycznej transakcji, system wykonuje proces "odwrócenia" (reverse update), aby zapewnić spójność danych bez konieczności ponownego przeliczania całego portfela od zera.

### 3.2. Obsługa Prowizji (Fees)
Każda transakcja uwzględnia pole `Fee` oraz `FeeCurrency`. Prowizja jest odejmowana od całkowitej wartości operacji, co pozwala na precyzyjne wyliczenie czystego kosztu nabycia (Cost Basis).

### 3.3. Walidacja i Zabezpieczenia
*   **Brak środków:** System blokuje transakcje typu **Sell** oraz edycję transakcji typu **Buy**, jeśli skutkowałoby to ujemnym stanem posiadania danej waluty.
*   **Weryfikacja własności:** Każda operacja na transakcji jest sprawdzana pod kątem uprawnień użytkownika do portfela źródłowego.

## 4. Architektura Danych

### 4.1. Model Transaction (Encja)
| Pole | Typ | Opis |
| :--- | :--- | :--- |
| Id | Guid | Unikalny identyfikator (PK) |
| WalletId | Guid | Powiązanie z portfelem (FK) |
| Type | Enum | Typ operacji: Buy / Sell |
| Quantity | Decimal(18,8) | Ilość jednostek aktywa |
| PriceAtTime | Decimal(18,8) | Cena jednostkowa w momencie transakcji |
| Fee | Decimal(18,8) | Kwota prowizji |
| ExecutedAt | DateTime | Data i czas wykonania operacji |

## 5. Dokumentacja API

### 5.1. Pobieranie transakcji (Paginacja)
`GET /api/transactions?page={int}&pageSize={int}&symbol={string}&startDate={date}&endDate={date}`
*   **Zaleta:** Paginacja odbywa się po stronie bazy danych (SQL `OFFSET/FETCH`), co minimalizuje zużycie pamięci RAM i transferu danych.

### 5.2. Aktualizacja (Update)
`PUT /api/transactions/{id}`
*   Obsługuje częściową aktualizację (JSON Merge Patch). System wykrywa zmiany w polach wpływających na portfel (Ilość/Cena) i wyzwala proces rekalkulacji Assetów.

## 6. Opis Interfejsu Użytkownika (Frontend)
*   **Reaktywność:** Zastosowanie Angular Signals do zarządzania stanem filtrów i paginacji.
*   **UX:** Dynamiczne ładowanie danych (Loading Spinner) oraz powiadomienia Toast o sukcesie lub błędzie operacji.
*   **Modale:** Intuicyjne formularze do dodawania i edycji z podziałem na sekcje danych finansowych i opisowych.
