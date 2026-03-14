# RedRat3 Controller - Web Interface

Prosta aplikacja webowa do sterowania urządzeniami RedRat3, kamerą i portami COM przez przeglądarkę.

## Szybki start (Debug)

### 1. Uruchomienie
Otwórz terminal i wpisz:
```bash
cd RedRat3ControllerWebServer
dotnet run
```

### 2. Otwórz przeglądarkę
Wpisz: `http://localhost:5000`

---

## Jak używać

### Sterowanie IR (RedRat3)
Steruj pilotem w dwóch sposobach:

**Sposób 1 - Kliknij przyciski:**
- Kliknij dowolny przycisk na ekranie (Power, Menu, Numery, itp.)

**Sposób 2 - Użyj klawiatury (szybciej!):**
- `P` = Power
- `M` = Menu
- `V` = Mute
- `X` = Exit
- `0-9` = Numery
- `Strzałki` = Nawigacja
- `Enter` = OK
- `+` / `-` = Głośność
- `Page Up/Down` = Kanały
- `F1` = Czerwony, `F2` = Zielony, `F3` = Żółty, `F4` = Niebieski

### Kamera
1. Wybierz kamerę z listy
2. Kliknij "Start"
3. Obraz pojawi się po lewej stronie

### Power Switch (Port COM)
1. Wybierz port COM
2. Kliknij "Connect"
3. Użyj przycisków ON/OFF

### USB Switch
1. Wybierz port COM
2. Kliknij "Connect"
3. Użyj przycisków TV/PC

### Audio Streaming
1. Kliknij "Start Audio"
2. Użyj suwaka do regulacji głośności
3. Kliknij "Mute" aby wyciszyć

---

## Dostęp z innego komputera

### 1. Znajdź IP adresu
Wpisz w terminalu:
```bash
ipconfig
```
Szukaj "IPv4 Address" (np. 192.168.1.100)

### 2. Otwórz firewall
Uruchom jako Administrator:
```bash
netsh advfirewall firewall add rule name="RedRat Web Server" dir=in action=allow protocol=TCP localport=5000
```

### 3. Otwórz przeglądarkę
Na innym komputerze w tej samej sieci wpisz np (ip komputera + :5000):
```
http://192.168.1.100:5000
```

---

## Wymagane sprzęt

- Kamera podłączona do komputera
- Urządzenie RedRat3 (podłączone przez USB)
- Opcjonalnie: urządzenia na portach COM (Power_Switch, USB_Switch)

---

## Zatrzymanie

W terminalu wciśnij `Ctrl+C`

---

## Problemy

### Kamera nie działa
- Sprawdź czy nie jest używana przez inny program
- Spróbuj innej kamery z listy

### Nie mogę połączyć z innego komputera
- Sprawdź firewall
- Sprawdź czy oba komputery są w tej samej sieci
- Sprawdź IP adres

### RedRat3 nie działa
- Sprawdź czy urządzenie jest podłączone przez USB
- Sprawdź czy plik REDRAT.xml istnieje w katalogu projektu

---

## Klawisze skrótów - pełna lista

```
P       = Power
M       = Menu
V       = Mute
X       = Exit
0-9     = Numery
↑↓←→    = Nawigacja
Enter   = OK
+/-     = Głośność
PgUp    = Kanał +
PgDn    = Kanał -
F1      = Czerwony
F2      = Zielony
F3      = Żółty
F4      = Niebieski
```

---

## Architektura

- **Backend:** ASP.NET Core 8.0 (działa na komputerze stacjonarnym)
- **Frontend:** HTML/CSS/JavaScript (działa w przeglądarce)
- **Komunikacja:** REST API + WebSocket

Wszystkie urządzenia fizyczne są podłączone do komputera stacjonarnego. Przeglądarka steruje nimi przez sieć lokalną.