# RedRat3 Controller Web Server

Aplikacja webowa do sterowania RedRat3, kamerą, przełącznikami COM i audio przez przeglądarkę. Serwer działa na jednym komputerze, a sterowanie odbywa się przez stronę w dowolnym komputerze w sieci.

---

## Wymagania sprzętowe

**Wymagane:**
- RedRat3 podlaczony przez USB
- Plik REDRAT.xml w katalogu RedRat3ControllerWebServer

**Opcjonalne:**
- Kamera (do podgladu na zywo)
- Przełącznik zasilania na porcie COM (115200 baud)
- Przełącznik USB na porcie COM (9600 baud) - zmiana TV/PC
- Mikrofon (do strumieniowania audio)

---

## Uruchomienie

### Na tym samym komputerze

1. Otwórz terminal w katalogu projektu
2. Wpisz: `cd RedRat3ControllerWebServer`
3. Wpisz: `dotnet run`
4. Otwórz przegladarke i wpisz: `http://localhost:5202`

### Sterowanie z innego komputera w sieci

1. Uruchom serwer na komputerze z podlaczonymi urzadzeniami (jak wyzej)
2. Sprawdz adres IP komputera - w terminalu wpisz: `ipconfig` i znajdz "IPv4 Address" (np. 192.168.1.100)
3. Dodaj regule firewalla na komputerze z serwerem (uruchom CMD jako Administrator):
   ```
   netsh advfirewall firewall add rule name="RedRat Web Server" dir=in action=allow protocol=TCP localport=5202
   ```
4. Na drugim komputerze otwórz przegladarke i wpisz: `http://ADRES_IP:5202` (np. http://192.168.1.20:5202)

Serwer nasluchuje na porcie 5202 (domyslnie). Wszystkie urzadzenia musza byc podlaczone do komputera z uruchomionym serwerem.

---

## Manual funkcji

### Device Status

Pokazuje stan polaczenia kazdego urzadzenia: RedRat3, port szeregowy, przełącznik USB, kamera, audio. Odswieza sie automatycznie.

---

### RedRat3 IR Control

Wysylanie sygnalow podczerwieni do TV lub innego urzadzenia przez RedRat3.

**Pole Send IR Command:**
- Przyciski przypisane do komend wysylaja od razu po wcisnieciu

**Lista dostepnych komend:**
- Lista wszystkich mozliwych sygnalow IR
- Przycisk Assign przy kazdej komendzie - kliknij, nastepnie wcisnij klawisz do przypisania
- Przypisania zapisuja sie w przegladarce i zostaja po ponownym otwarciu strony
- Escape podczas przypisywania anuluje

**Domyslne przypisania klawiszy:**
- P = Power, M = Menu, V = Mute, X = Exit
- 0-9 = Numery, Strzalki = Nawigacja
- Enter = OK
- +/- = Glosnosc, PageUp/PageDown = Kanal
- F1-F4 = Przyciski kolorowe (czerwony, zielony, zolty, niebieski)

---

### Power Switch

Sterowanie urzadzeniem na porcie COM (np. przelacznik zasilania).

1. Wybierz port COM z listy
2. Kliknij Connect
3. Przycisk ON wysyla 1, przycisk OFF wysyla 0
4. Polaczenie: 115200 baud, 8N1

---

### USB Switch

Zmiana urzadzenia TV/PC na przelaczniku USB.

1. Wybierz port COM (inny niz Power Switch)
2. Kliknij Connect
3. Przycisk TV wysyla MA01, przycisk PC wysyla MB01
4. Polaczenie: 9600 baud

---

### Camera Feed

Transmisja obrazu z kamery na zywo.

1. Wybierz kamere z listy
2. Kliknij Start
3. Obraz pojawia sie po lewej stronie
4. Kliknij Stop aby zatrzymac

---

### Audio Streaming

Transmisja dzwieku z mikrofonu do przegladarki.

1. Wybierz mikrofon z listy
2. Kliknij Start Audio
3. Suwak Volume reguluje glosnosc (0-100%)
4. Przycisk Mute wylacza dzwiek
5. Kliknij Stop Audio aby zatrzymac

---

### Action History

Lista ostatnich akcji z timestampami: wyslane sygnaly IR, polaczenia portow, operacje kamery i audio. Przycisk Clear History czyści liste.

---

## Release (plik exe)

**Budowanie release:**
1. Uruchom `BuildRelease.bat` w folderze RedRat3ControllerWebServer
2. Wynik bedzie w podfolderze `Release`

**Uruchomienie przez exe:**
1. Wejdz do folderu Release
2. Uruchom `RedRat3ControllerWebServer.exe
3. Otwórz przegladarke i wpisz `http://localhost:5202`
4. Aby sterowac z innego komputera - dodaj regule firewalla (CMD jako Administrator):
   ```
   netsh advfirewall firewall add rule name="RedRat Web Server" dir=in action=allow protocol=TCP localport=5202
   ```
5. Na drugim komputerze wpisz w przegladarce: `http://ADRES_IP:5202` (adres IP komputera z uruchomionym exe)

Wymaga .NET 8.0 na komputerze docelowym. Caly folder Release mozna przeniesc na inny komputer.

---

## Zatrzymanie serwera

Wcisnij Ctrl+C w terminalu.

---

## Wymagania systemowe

- Windows
- .NET 8.0
- Przegladarka z obsluga WebSocket (Chrome, Firefox, Edge)
