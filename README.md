# Quiz

Et MVC-webprojekt i ASP.NET Core (.NET 8) hvor en quiz-master kan oprette quizzer
med mange forskellige spoergsmaaltyper, og deltagere kan tage quizzerne og aflevere
deres besvarelse sammen med navn, e-mail og telefon - uden at oprette en bruger.

## Spoergsmaaltyper

| Type | Saadan oprettes den |
| --- | --- |
| Enkeltvalg (`MultipleChoiceSingleAnswer`) | Et svar pr. linje, `*` foran det rigtige |
| Flere svar (`MultipleChoiceMultipleAnswers`) | Et svar pr. linje, `*` foran de rigtige |
| Sandt/falsk (`TrueFalse`) | To linjer, `*` foran det rigtige |
| Kort svar (`ShortAnswer`) | Godkendte svar, et pr. linje, `*` foran |
| Tal (`NumericAnswer`) | Det rigtige tal med `*` foran |
| Rækkefølge (`Ordering`) | Svarene skrevet i den rigtige orden |
| Par-match (`MatchingPairs`) | `venstre\|hoejre` pr. linje |
| Billedvalg (`ImageChoice`) | Som enkeltvalg, evt. med billede-URL |

## Struktur

```
QuizApp/
  Controllers/     HomeController, QuizController (offentlig), AdminController (login)
  Models/          Quiz, Question, AnswerChoice, Answer, QuizResult, QuizManager
  ViewModels/      View-modeller til spil, aflevering og admin
  Services/        QuizScoring (retter alle typer), PasswordHasher (PBKDF2)
  Data/            QuizDbContext, SeedData, design-time factory, mysql_schema.sql
  Migrations/      EF Core-migrationer (MySQL)
  Views/           Razor-views
.github/workflows/deploy.yml   Build + FTP-deploy
```

## Database

- **Produktion (webhotel):** MySQL/MariaDB. Forbindelsen laeses fra
  `ConnectionStrings__DefaultConnection` (miljoevariabel) eller
  `appsettings.Production.json`. Ved opstart koeres `Database.Migrate()`
  automatisk, saa tabellerne oprettes.
- **Lokal udvikling:** Hvis der ikke er sat en forbindelsesstreng, bruges en
  lokal SQLite-fil (`quizapp.db`), som oprettes automatisk med `EnsureCreated()`.

`Data/mysql_schema.sql` indeholder det fulde MySQL-script, hvis du hellere vil
oprette tabellerne manuelt i phpMyAdmin.

### Foerste gang

Der oprettes automatisk en admin-bruger:

- Brugernavn: `admin` (kan aendres med `Admin__Username`)
- Adgangskode: `Admin123!` **hvis** miljoevariablen `ADMIN_PASSWORD` (eller
  `Admin__Password`) ikke er sat. Saet den altid i produktion.

Der seedes ogsaa en eksempelquiz med alle spoergsmaaltyper, saa du kan se
hvordan det virker.

## Lokal koersel

```bash
cd QuizApp
dotnet restore
dotnet run
```

Aabn http://localhost:5078

## Migrations (MySQL)

```bash
cd QuizApp
dotnet ef migrations add NavnPaaMigration     # ny migration
dotnet ef migrations script -o Data/mysql_schema.sql
```

## GitHub Secrets

Foelgende secrets skal vaere sat i repoet (Settings -> Secrets and variables -> Actions):

| Secret | Indhold |
| --- | --- |
| `DB_HOST` | fx `mysql94.unoeuro.com` |
| `DB_DATABASE` | fx `villadsclaes_dk_db` |
| `DB_USERNAME` | database-bruger |
| `DB_PASSWORD` | database-adgangskode |
| `FTP_HOST` | fx `ftp.simply.com` |
| `FTP_USER` | FTP-bruger |
| `FTP_PASSWORD` | FTP-adgangskode |

Ved push til `master` bygger workflowet appen, skriver
`appsettings.Production.json` ud fra DB-secrets og uploader `publish/` til
`/public_html/quiz/` via FTP.

> **Vigtigt om hosting:** ASP.NET Core kraever en applikationsserver
> (fx IIS, Kestrel bag et reverse proxy eller en host med .NET-stoette).
> Alminidelig dansk shared hosting med FTP + PHP kan normalt ikke koere
> ASP.NET Core. Tjek derfor hos Simply.com at pakken understoetter .NET 8,
> foer du forventer at appen svarer paa `villadsclaes.dk/quiz`.
