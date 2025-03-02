# AMORIA - Micro - Social - Platform

## Descriere
Amoria este o platformă de tip micro-rețea socială, dezvoltată în C# și ASP.NET Core, care oferă utilizatorilor posibilitatea de a interacționa, de a crea conexiuni și de a partaja conținut într-un mediu sigur și intuitiv. Inspirată de cele mai populare rețele sociale, Amoria pune un accent deosebit pe controlul vizibilității profilurilor, gestionarea interacțiunilor (postări, comentarii) și crearea grupurilor private.

Acest proiect reprezintă rezultatul unui efort colaborativ în cadrul materiei **Dezvoltarea Aplicațiilor Web**, fiind realizat de o echipă dedicată, care a respectat cerințele impuse și a implementat funcționalitățile esențiale pentru a crea o platformă modernă de socializare. Echipa a lucrat eficient folosind **Trello** pentru managementul task-urilor și platforme de comunicare precum **Google Meet**, **Discord** și **WhatsApp**, facilitând astfel o colaborare constantă și bine coordonată.

## Funcționalități Implementate

### Gestionarea Utilizatorilor
- **Vizitator neînregistrat**
- **Utilizator înregistrat**
- **Administrator** (control total asupra conținutului platformei)
- Creare și editare profil personal (nume, descriere, poza de profil, vizibilitate public/privat)
- Căutare utilizatori după nume complet sau părți din nume
- Vizualizarea profilurilor private doar cu informații de bază (nume, descriere, imagine profil)
- Sistem de cereri de prietenie (follow), în funcție de vizibilitatea profilului

### Postări și interacțiuni
- Creare, editare și ștergere postări (text, imagini, video embedded)
- Comentarii la postări, editabile și șterse doar de proprietar
- Administratorul poate șterge conținut inadecvat

### Grupuri și comunicare
- Crearea și administrarea grupurilor (denumire, descriere, moderator)
- Alăturarea la grupuri doar cu cont activ și aprobat de moderator
- Mesaje private în grupuri, editabile doar de autor
- Posibilitatea de a părăsi grupurile sau a fi eliminat de moderator

## Tehnologii utilizate
- **C# & ASP.NET Core** - Backend robust și scalabil
- **Entity Framework Core** - Gestionarea bazei de date
- **SQL Server** - Stocarea datelor utilizatorilor
- **Bootstrap & CSS** - Interfață modernă și responsive
- **JavaScript & jQuery** - Funcționalități dinamice pe frontend

## Metodologie & Organizare
- **Planificare prin sprinturi**: Fiecare etapă a fost organizată în sprinturi săptămânale, cu scopuri clar definite:
  - **Săptămâna 1** – Diagrama ER, clarificarea cerințelor, setarea mediului
  - **Săptămâna 2-3** – Dezvoltarea modelelor și controllerelor
  - **Săptămâna 4-5** – Implementarea interfeței și testare
- **Utilizarea Trello pentru organizare**: Am urmărit progresul și ne-am distribuit task-urile echitabil.
- **Comunicare eficientă prin Google Meet, Discord, WhatsApp**: Am lucrat sincronizat, discutând dificultățile și soluțiile în timp real.
- **Branding & Identitate**: Am realizat un logo personalizat pentru Amoria, adăugând un plus de profesionalism proiectului.

## Provocări și soluții
- **Dificultăți tehnice cu Docker**: Inițial, am încercat să folosim Docker, însă am întâmpinat dificultăți tehnice care ne-au făcut să revenim la o abordare clasică pentru a ne eficientiza munca.
- **Implementarea grupurilor și gestionarea relațiilor între utilizatori**: A necesitat o abordare iterativă, ajustând codul în funcție de testări și feedback.
- **Optimizarea bazei de date și gestionarea relațiilor între modele**: Am folosit tehnici eficiente de indexare și relaționare pentru a optimiza performanța.

## Cum se rulează proiectul
1. **Clonează repository-ul**:
```bash
   git clone https://github.com/utilizatorul-tau/Amoria.git
```
2. Navighează în folderul proiectului:
```bash
   cd Amoria
```
3. Instalează dependențele și rulează serverul:
```bash
  dotnet restore
  dotnet run
```
4. Accesează aplicația în browser:
```bash
  http://localhost:5000
```
---

## 🎓 Experiență de învățare și concluzii
Acest proiect a fost o oportunitate valoroasă de învățare, depășind cu mult cadrul unei simple cerințe academice. Ne-a permis să colaborăm într-un mediu dinamic, să ne organizăm eficient și să implementăm o aplicație sofisticată, care reflectă complexitatea rețelelor sociale moderne.

Pe parcursul acestui proces, am dobândit abilități esențiale, printre care:

- Colaborarea armonioasă într-o echipă diversă și bine coordonată
- Stăpânirea unui framework backend de înaltă performanță (ASP.NET Core)
- Gestionarea cu succes a bazelor de date relaționale utilizând Entity Framework Core
- Aplicarea principiilor arhitecturale MVC (Model-View-Controller) pentru o structură eficientă a aplicației
  
📌 Toate funcționalitățile planificate au fost implementate cu succes, iar provocările tehnice au fost depășite prin abordări inovative și soluții pragmatice, învățând cum să navigăm în complexitatea dezvoltării software cu profesionalism și perseverență.

🔹**Privind spre viitor** , Amoria este doar la începutul unui drum promițător, cu potențialul de a evolua continuu. Posibilitățile de extindere și perfecționare sunt nelimitate, iar acest proiect reprezintă un pas important în călătoria noastră de a crea platforme inovative și de impact! 
