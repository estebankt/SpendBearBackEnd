# SpendBear i18n Plan (English + Spanish)

## Approach

- Backend returns **localized strings** (not keys) based on user's stored language preference
- Translations live in **.resx resource files** per module
- Only **system categories** get translated; user-created categories remain as typed
- Language preference stored on the **User entity** (`PreferredLanguage` column)
- A new **LocalizationMiddleware** sets `CultureInfo.CurrentUICulture` per request
- Domain layer stays language-agnostic; translation happens at the **handler/API boundary**

---

## Phase 1: Foundation

### 1a. Add `PreferredLanguage` to User entity
- **File**: `src/Modules/Identity/Identity.Domain/Entities/User.cs`
  - Add `public string PreferredLanguage { get; private set; } = "en";`
  - Add `UpdateLanguagePreference(string language)` method with validation (`en`/`es` only)
- **File**: `src/Modules/Identity/Identity.Infrastructure/Data/Configurations/UserConfiguration.cs`
  - Map `PreferredLanguage` as `varchar(5)`, not null, default `"en"`
- **Migration**: `AddPreferredLanguageToUser` in Identity module

### 1b. Create LocalizationMiddleware
- **New file**: `src/Api/SpendBear.Api/Middleware/LocalizationMiddleware.cs`
  - Runs **after** `UserResolutionMiddleware`
  - Reads `user_id` claim, looks up user's `PreferredLanguage` (with Redis cache)
  - Sets `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture`
  - Falls back to `"en"` for unauthenticated or missing users
- **File**: `src/Api/SpendBear.Api/Program.cs`
  - Register middleware after `UserResolutionMiddleware`, before `UseAuthorization()`

### 1c. Resource file helpers
- **New file**: `src/Shared/SpendBear.SharedKernel/Localization/LocalizedResource.cs`
  - Thin wrapper around `ResourceManager` to simplify `GetString(key, CultureInfo.CurrentUICulture)` calls
  - Each module creates its own static accessor using this helper

---

## Phase 2: Identity Module - Language Preference Endpoint

### 2a. Update language feature
- **New feature folder**: `Identity.Application/Features/UpdateLanguagePreference/`
  - `UpdateLanguagePreferenceCommand(string Language)`
  - `UpdateLanguagePreferenceHandler` - validates language, calls `user.UpdateLanguagePreference()`, invalidates Redis cache
- **File**: `src/Modules/Identity/Identity.Api/Controllers/IdentityController.cs`
  - Add `PUT /api/identity/me/language` endpoint

### 2b. Update GetProfile response
- **File**: `Identity.Application/Features/GetProfile/GetProfileResponse.cs` (or equivalent DTO)
  - Add `PreferredLanguage` to response so frontend knows current setting

### 2c. Identity resource files
- **New files**: `Identity.Application/Resources/IdentityMessages.resx` (en) and `IdentityMessages.es.resx`
  - Keys: `user_not_found`, `invalid_language`, `auth0_user_id_required`, `email_required`, etc.

---

## Phase 3: System Category Translation

### 3a. Add TranslationKey to Category
- **File**: `src/Modules/Spending/Spending.Domain/Entities/Category.cs`
  - Add `public string? TranslationKey { get; private set; }`
- **File**: `Spending.Infrastructure/Data/Configurations/CategoryConfiguration.cs`
  - Map `TranslationKey` as `varchar(50)`, nullable
- **Migration**: `AddTranslationKeyToCategory`
  - Add column
  - SQL UPDATE to set keys for all 28 system categories (e.g., `category_groceries`, `category_rent_mortgage`)

### 3b. Category resource files
- **New files**: `Spending.Application/Resources/SystemCategories.resx` + `.es.resx`
  - 28 name keys + 28 description keys (56 entries per language)
  - Example: `category_groceries` = "Groceries" / "Comestibles"
  - Example: `category_groceries_desc` = "Food and household essentials" / "Alimentos y articulos esenciales del hogar"

### 3c. Translate at handler level
- **File**: `Spending.Application/Features/Categories/GetCategories/GetCategoriesHandler.cs` (and any other handler returning CategoryDto)
  - When `IsSystemCategory && TranslationKey != null`, resolve Name/Description from resource file
  - Otherwise return as-is (user categories)

---

## Phase 4: Error & Validation Message Localization

### 4a. Module resource files
Create `.resx` / `.es.resx` pairs for each module:
- `Spending.Application/Resources/SpendingMessages.resx` (~15 keys)
- `Budgets.Application/Resources/BudgetsMessages.resx` (~10 keys)
- `Notifications.Application/Resources/NotificationsMessages.resx` (~5 keys)
- `StatementImport.Application/Resources/StatementImportMessages.resx` (~8 keys)
- `SharedKernel/Resources/SharedMessages.resx` (~10 keys for global exception handler messages)

### 4b. Update validators and domain entities
- Replace hardcoded English strings with resource lookups
- Error `Code` stays the same (machine-readable); only `Message` gets localized
- Validators: use lambda `.WithMessage(x => SpendingMessages.Get("key"))` for FluentValidation
- Domain entities: use resource lookups in `Result.Failure(new Error("Code", Messages.Get("key")))`

### 4c. Update GlobalExceptionHandlerMiddleware
- **File**: `src/Api/SpendBear.Api/Middleware/GlobalExceptionHandlerMiddleware.cs`
  - Localize ProblemDetails `Title` fields (e.g., "Domain Rule Violation" / "Violacion de Regla de Dominio")

---

## Phase 5: Notification Template Localization

### 5a. Notification template resource files
- **New files**: `Notifications.Application/Resources/NotificationTemplates.resx` + `.es.resx`
  - `budget_warning_title` = "Budget Warning: {0}% of {1}"
  - `budget_warning_message` = "You have spent ${0} of your ${1} budget for {2}..."
  - Spanish equivalents

### 5b. Update event handlers
- **Files**:
  - `Notifications.Application/Features/EventHandlers/BudgetWarningEventHandler.cs`
  - `Notifications.Application/Features/EventHandlers/BudgetExceededEventHandler.cs`
- Inject `IUserRepository` to look up user's preferred language
- Set culture before building notification title/message
- Use `string.Format(template, args)` with localized templates

---

## Phase 6: Caching & Testing

### 6a. Redis caching for language lookup
- Cache key: `user:language:{userId}`, TTL 24h
- Invalidate on language preference update

### 6b. Verification
- Run EF migrations for both Identity and Spending modules
- Test `PUT /api/identity/me/language` with `"es"` and `"en"`
- Test `GET /api/spending/categories` returns Spanish names when user's language is `"es"`
- Test validation errors return Spanish messages
- Test notification creation produces Spanish templates
- Verify fallback to English for unset/invalid preferences

---

## Key Files to Modify

| File | Change |
|------|--------|
| `Identity.Domain/Entities/User.cs` | Add `PreferredLanguage` property + method |
| `Identity.Infrastructure/Data/Configurations/UserConfiguration.cs` | Map new column |
| `Identity.Api/Controllers/IdentityController.cs` | Add language endpoint |
| `Spending.Domain/Entities/Category.cs` | Add `TranslationKey` property |
| `Spending.Infrastructure/Data/Configurations/CategoryConfiguration.cs` | Map new column |
| `Spending handlers returning CategoryDto` | Translate system categories |
| `Notifications event handlers` | Use localized templates |
| `All validators + domain entities` | Replace hardcoded strings with resource lookups |
| `Api/SpendBear.Api/Middleware/GlobalExceptionHandlerMiddleware.cs` | Localize titles |
| `Api/SpendBear.Api/Program.cs` | Register LocalizationMiddleware |

## New Files

| File | Purpose |
|------|---------|
| `Api/SpendBear.Api/Middleware/LocalizationMiddleware.cs` | Set culture per request |
| `SharedKernel/Localization/LocalizedResource.cs` | Resource helper |
| `*.resx` + `*.es.resx` files per module | Translation strings |
| 2 EF migrations (Identity + Spending) | Schema changes |
| `Identity.Application/Features/UpdateLanguagePreference/` | New feature folder |
