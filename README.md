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
| Raekkefoelge (`Ordering`) | Svarene skrevet i den rigtige orden |
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

## Hosting paa Simply.com / UnoEuro (vigtigt)

Siden ligger paa et Windows-webhotel (IIS 10), og udbyderen har kun .NET-runtimes
installeret **op til .NET 4.8**. Nyere .NET og .NET Core kan alligevel koere, men
**kun** som *Self-Contained Deployment* (SCD), hvor appen har sin egen runtime med.
Derfor goer workflowet to ting, som ikke maa aendres uden grund:

1. **Self-contained publish til `win-x86`** - den runtime udbyderen anbefaler.
   Uden SCD starter appen ikke, og IIS svarer `500` paa alle stier under `/quiz`.
2. **`OutOfProcess` hosting** (`<AspNetCoreHostingModel>` i `QuizApp.csproj`).
   Et webhotel har kun een app-pool, saa InProcess (standard fra .NET 5) kan ikke
   dele plaen med resten af sitet.

Kilder:
- <https://www.simply.com/en/support/faq/asp/842-which-net-versions-do-we-support/>
- <https://www.simply.com/en/support/faq/asp/361-deploy-net-with-self-contained-deployment-scd/>
- <https://www.simply.com/en/support/faq/asp/827-multiple-asp-net-core-5-apps-on-the-same-web-hosting/>

Filerne lægges i `/public_html/quiz/`, som er webroden for `villadsclaes.dk/quiz`.
SCD-publish fylder ca. 107 MB, saa foerste FTP-upload tager et par minutter -
efterfoelgende deploys sender kun det aendrede.

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
| `DB_HOST` | fx `mysql94.simply.com` |
| `DB_DATABASE` | fx `villadsclaes_dk_db` |
| `DB_USERNAME` | database-bruger |
| `DB_PASSWORD` | database-adgangskode |
| `FTP_HOST` | fx `ftp.simply.com` |
| `FTP_USER` | FTP-bruger |
| `FTP_PASSWORD` | FTP-adgangskode |

Ved push til `master` bygger workflowet appen, skriver
`appsettings.Production.json` ud fra DB-secrets og uploader `publish/` til
`/public_html/quiz/` via FTP. Workflowet fejler bevidst, hvis `QuizApp.exe`,
`web.config` eller OutOfProcess-indstillingen mangler i publish - saa en
regression ikke bliver deployet i stilhed.
