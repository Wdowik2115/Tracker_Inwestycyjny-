# Dokumentacja Funkcjonalności: Zarządzanie Profilem i Bezpieczeństwem

## 1. Opis Funkcjonalności
Funkcjonalność pozwala użytkownikowi na:
- Przeglądanie swoich danych podstawowych (Email, Data utworzenia konta).
- Aktualizację imienia i nazwiska.
- Bezpieczną zmianę hasła (wymagane podanie starego hasła).
- Wybór preferowanej waluty (USD/EUR) zapisywany w bazie danych/profilu.

## 2. Diagramy

### Diagram Klas (Backend)
```mermaid
classDiagram
    class User {
        +Guid Id
        +string Email
        +string PasswordHash
        +string FirstName
        +string LastName
        +string PreferredCurrency
        +DateTime CreatedAt
    }
    class IUserService {
        <<interface>>
        +GetProfileAsync(Guid userId) Task~UserDto~
        +UpdateProfileAsync(Guid userId, UpdateProfileDto dto) Task
        +ChangePasswordAsync(Guid userId, ChangePasswordDto dto) Task
    }
    class UserService {
        -IUserRepository _userRepository
        +GetProfileAsync(Guid userId) Task~UserDto~
        +UpdateProfileAsync(Guid userId, UpdateProfileDto dto) Task
        +ChangePasswordAsync(Guid userId, ChangePasswordDto dto) Task
    }
    class UserController {
        -IUserService _userService
        +GetProfile() Task~IActionResult~
        +UpdateProfile(UpdateProfileDto dto) Task~IActionResult~
        +ChangePassword(ChangePasswordDto dto) Task~IActionResult~
    }
    UserController --> IUserService
    UserService ..|> IUserService
    UserService --> User
```

### Diagram Sekwencji: Zmiana Hasła
```mermaid
sequenceDiagram
    participant U as Użytkownik (Frontend)
    participant C as UserController
    participant S as UserService
    participant R as UserRepository
    participant DB as Database

    U->>C: ChangePassword(old, new)
    C->>S: ChangePasswordAsync(userId, old, new)
    S->>R: GetByIdAsync(userId)
    R->>DB: SELECT User
    DB-->>R: User Entity
    R-->>S: User Entity
    S->>S: Verify oldPasswordHash
    alt Password Correct
        S->>S: Hash newPassword
        S->>R: UpdateAsync(User)
        R->>DB: UPDATE User
        DB-->>R: OK
        R-->>S: OK
        S-->>C: Success
        C-->>U: 200 OK
    else Password Incorrect
        S-->>C: Throw Exception
        C-->>U: 400 Bad Request
    end
```

## 3. Instrukcja Testowania

### Testy Jednostkowe (Backend)
Uruchom testy w terminalu:
```bash
dotnet test Serwer/Serwer.sln
```
Zwróć szczególną uwagę na `UserServiceTests.cs`.

### Testy Manualne (Frontend + Backend)
1. **Logowanie/Rejestracja:**
   - Zarejestruj nowe konto lub zaloguj się na istniejące.
   - Po zalogowaniu powinieneś zostać przekierowany na `/dashboard`.
   - Spróbuj wejść na `/login` będąc zalogowanym - powinieneś zostać automatycznie cofnięty do dashboardu.
2. **Ustawienia Profilu:**
   - Przejdź do zakładki **Settings** (klikając w profil w sidebarze).
   - Wypełnij Imię i Nazwisko, a następnie kliknij **Save Profile**.
   - Odśwież stronę - Twoje imię powinno pojawić się w sidebarze nad adresem email.
3. **Zmiana Hasła:**
   - W sekcji **Security** wpisz stare hasło oraz nowe hasło (min. 8 znaków).
   - Kliknij **Update Password**. Powinien pojawić się komunikat o sukcesie.
   - Wyloguj się i spróbuj zalogować nowym hasłem.
4. **Preferencje Waluty:**
   - Zmień walutę na EUR i kliknij **Save Preferences**.
   - Waluta powinna zostać zapisana w Twoim profilu (można sprawdzić w bazie lub odświeżając stronę).

