using System;
using System.Collections.Generic;

namespace TimerSys
{

    internal static class HelpContent
    {
        public static string Changelog() => Lang.Current == Lang.EN ? ChangelogEn : ChangelogPl;

        public static string License() => Lang.Current == Lang.EN
            ? "Program licence\nTimer is released under the Creative Commons Attribution 3.0 licence (CC BY 3.0). You may copy, distribute, display and use the program, including commercially, provided the author is credited. Licence text: https://creativecommons.org/licenses/by/3.0/\n\nAuthor: Jakub Michalski (JMSoft.pl), 2019 - 2026. All rights reserved.\n\nThe program is provided “as is”, without any warranty. The author is not liable for damage caused by using it, in particular for data lost through actions carried out automatically (shutting the computer down, cleaning files, running your own scripts). Test those actions before you rely on them.\n\nComponents used\n• .NET 8 (Microsoft) – MIT licence – https://github.com/dotnet/runtime\n• Newtonsoft.Json 13.0.3 (James Newton-King) – MIT licence – https://github.com/JamesNK/Newtonsoft.Json\n• System.Security.Cryptography.ProtectedData 8.0.0 (Microsoft) – MIT licence – https://github.com/dotnet/runtime\n\nThe MIT licence allows any use, copying and distribution of a component as long as the copyright notice is kept. The full licence texts are available at the addresses above.\n\nThe program sends no information about you. It contacts the network only to check for a new version and to send e-mail, if you set that action up. Mailbox sign-in details are encrypted with the Windows DPAPI mechanism and never leave your computer."
            : "Licencja programu\nWyłącznik czasowy jest udostępniany na licencji Creative Commons Uznanie autorstwa 3.0 (CC BY 3.0). Możesz kopiować, rozpowszechniać, wyświetlać i używać programu, także w celach komercyjnych, pod warunkiem podania informacji o autorze. Treść licencji: https://creativecommons.org/licenses/by/3.0/deed.pl\n\nAutor: Jakub Michalski (JMSoft.pl), 2019 - 2026. Wszelkie prawa zastrzeżone.\n\nProgram jest dostarczany „tak jak jest”, bez żadnej gwarancji. Autor nie odpowiada za szkody wynikłe z używania programu, w szczególności za utratę danych spowodowaną czynnościami wykonywanymi automatycznie (wyłączanie komputera, czyszczenie plików, uruchamianie własnych skryptów). Przed użyciem tych czynności sprawdź ich działanie.\n\nWykorzystane składniki\n• .NET 8 (Microsoft) – licencja MIT – https://github.com/dotnet/runtime\n• Newtonsoft.Json 13.0.3 (James Newton-King) – licencja MIT – https://github.com/JamesNK/Newtonsoft.Json\n• System.Security.Cryptography.ProtectedData 8.0.0 (Microsoft) – licencja MIT – https://github.com/dotnet/runtime\n\nLicencja MIT zezwala na dowolne użycie, kopiowanie i rozpowszechnianie składnika pod warunkiem zachowania informacji o prawach autorskich. Pełne treści licencji są dostępne pod podanymi adresami.\n\nProgram nie wysyła żadnych danych o użytkowniku. Połączenia z siecią nawiązywane są wyłącznie w celu sprawdzenia nowej wersji oraz wysłania wiadomości e-mail, jeśli skonfigurujesz tę czynność. Dane logowania do skrzynki pocztowej są szyfrowane mechanizmem DPAPI systemu Windows i pozostają na Twoim komputerze.";

        private const string ChangelogPl = """
Wersja 1.0.7 (12.10.2026)

- Aktualizacja zasobów: Pomoc programu otwiera się teraz w formie podręcznika, dodano paczkę nowych domyślnych dźwięków. Poprawki działania aplikacji i optymalizacja kodu.
- Boczne menu można zwinąć do samych ikon, a motyw i język przełącza się przyciskami na dole menu.
- Dodano łatwiejszy mechanizm aktywacji zadania w trybie: "Stoper". Od teraz domyślnie odlicza podany czas (HH:MM:SS) - daty nie trzeba już podawać; przycisk obok modułu- przełącza oba sposoby.
- Jednorazowe powtórzenie akcji z trybu harmonogramu pojawia się także na liście zadań.
- Każdy tryb może mieć własny dźwięk albo korzystać ze wspólnego ustawienia.
- Lista zadań pokazuje odliczanie dla trybu bezczynności i odróżnia zadanie wstrzymane od wykonanego.
- Naprawiono błąd, w którym blokada hasłem wyłączała się sama przy próbie zamknięcia okna.
- Naprawiono błąd, w którym edycja zadania i ponowne uruchomienie skracały przypadkowo ustawiony czas o kilka sekund.
- Naprawiono błąd, w którym licznik bezczynności zawieszał się przy bardzo długiej pracy komputera bez ponownego uruchomienia.
- Naprawiono błąd, w którym odliczanie dłuższe niż doba pokazywało niepoprawną liczbę dni.
- Naprawiono błąd, w którym ukrywanie programu w zasobniku blokowało w niektórych przypadkach prawidłowe zamknięcie systemu Windows.
- Naprawiono błąd, w którym wstrzymanie zadania w trybie stopera faktycznie zatrzymywało odliczanie - wcześniej czas biegł dalej w tle.
- Nowa czynność: wysłanie wiadomości e-mail z gotowych szablonów (krótka wiadomość, data i godzina, załącznik, wiadomość HTML, wynik polecenia lub własny tekst).
- Odświeżony wygląd programu - okno, przyciski, pola i okna dialogowe zostały zbudowane od nowa, dzięki czemu działają płynniej i wyglądają spójnie w obu motywach. Menu z lewej strony zamiast zakładek u góry - łatwiej trafić do listy zadań, ustawień i informacji. Jedna karta "Aktywator" zamiast trzech osobnych zakładek - sposób działania wybierasz przełącznikiem u góry karty. Tryb ciemny bez jasnych ramek wokół zakładek, z czytelniejszymi przyciskami.
- Okno konfiguracji poczty mieści się na ekranie - treść przewija się, a port, szyfrowanie i gotowe ustawienia stoją w jednym wierszu.
- Okno wyboru dźwięku pokazuje pole własnego pliku dopiero po wybraniu opcji "Własny dźwięk".
- Poprawiono działanie ikony w zasobniku, która powinna pokazywać rzeczywisty stan liczników.
- Program mówi od teraz również po angielsku. Język można zmienić w Ustawieniach.
- Program sam informuje o dostępnej nowej wersji przy uruchomieniu i proponuje aktualizację. Dodano moduł sprawdzania aktualizacji.
- Ustawienia czynności mogą być wspólne dla wszystkich trybów albo osobne dla każdego - decyduje o tym przełącznik w oknie czynności.
- Ustawienia: sygnał dźwiękowy po każdym wykonanym zadaniu. Automatyczne resetowanie licznika bezczynności jest teraz włączone od początku. Zaktualizowano działanie opcji "Nie usypiaj".
- Zadanie z listy można uruchomić ponownie, nawet jeśli odliczanie już się skończyło.
- Zaktualizowano mechanizm działania blokady programu. Hasła są szyfrowane mocniejszym algorytmem.

Wersja 1.0.6 (02.05.2026)

-Aktualizacja zasobów.
-Zaktualizowano wygląd niektórych elementów interfejsu GUI.
-Dodano ustawienie, które umożliwia ukrycie odliczania licznika czasowego.
-Dodano funkcję uruchamiania licznika czasowego według określonego harmonogramu.
-Dodano kilka nowych aktywatorów dla każdej sekcji liczników.
-Dodano możliwość dodania więcej niż jednego aktywatora jednocześnie (system listy zadań).
-Dodano ustawienie, które umożliwia korygowanie czasu według sprawdzonej strefy czasowej.
-Dodano ustawienie, które umożliwia ukrycie ikonki programu nawet gdy program jest aktywny.
-Dodano ustawienie, które umożliwia wybranie ciemnego motywu kolorystycznego dla programu.
-Dodano aplet ustawienia funkcji aktywatora, który pozwala zmienić działanie wybranej opcji.
-Dodano krótki opis funkcji programu w sekcji pomocy programu.
-Naprawiono błąd graficzny, który nieprawidłowo wyświetlał odliczanie licznika czasu.

Wersja 1.0.5 (20.04.2024)

-Naprawiono problem z prawidłowym interpretowaniem poleceń przez program; problemy z uruchamianiem własnych skryptów powinny przestać się pojawiać.
-Naprawiono problem z nieprawidłowym ustawieniem statusu ustawienia oraz jego nieoczekiwanym resetowaniem po ponownym uruchomieniu aplikacji.
-Naprawiono problem z niektórymi ustawieniami w środowisku Windows 7.
-Naprawiono problem z włączeniem opcji wymuszającej kontrolę nad planem zasilania.
-Naprawiono problem, który uniemożliwiał ponowne uruchomienie programu, gdy użytkownik wpisał złą datę.
-Naprawiono problem z nieprawidłowym wyświetlaniem czasu w aktywatorze czasowym.
-Naprawiono problem z dwukrotnie uruchamianym procesem edycji pliku konfiguracyjnego.
-Dodano funkcję resetowania ustawień programu do wartości domyślnych.
-Dodano funkcję wymuszania uruchamiania programów przez opcję "Uruchom polecenie".
-Dodano funkcję ochrony programu hasłem.
-Dodano funkcję sprawdzania dostępności nowych aktualizacji programu w panelu ustawień.

Wersja 1.0.3 (06.04.2024)

-Dodano funkcję "Nie usypiaj", która uniemożliwia systemowi Windows podejmowanie działań w planie zasilania. Po włączeniu program tworzy własny plan zasilania i monitoruje, czy użytkownik nie przełączył się na inny.

Wersja 1.0.2 (26.03.2024)

-Dodano możliwość uruchomienia dźwięku jako akcji działania.
-Naprawiono problemy pewnych wyjątków aplikacji.
-Naprawiono problem z zakładkami w systemie starszym niż Windows 10.
-Drobne poprawki.

Wersja 1.0.1 (25.03.2024)

-Aktualizacja pakietów.
-Naprawiono błąd uniemożliwiający uruchamianie programu po zaznaczeniu odpowiedniej opcji.

Wersja 1.0 (24.03.2024)

-Program został wydany.
""";

        private const string ChangelogEn = """
Version 1.0.7 (11.10.2026)

- Updated resources: Program Help now opens as a more extensive manual; a set of new default sounds has been added. Other code fixes.
- The side menu can be collapsed to icons only, and the theme and language switch with buttons at the bottom of the menu.
- Added an easier task activation mechanism in "Timer" mode. It now counts down the specified time (HH:MM:SS) by default—no need to enter a date anymore; the button next to the module toggles between both methods.
- One-time repetition of an action from Schedule mode now also appears on the task list.
- Each mode can now have its own sound or use a shared setting.
- Mail server errors are now explained in plain words, including a refusal to send from an address other than the account you signed in with.
- The task list shows a countdown for Idle mode and distinguishes between a paused task and a completed one.
- Signing in to Outlook.com and Gmail uses the provider's own form in the browser - the program asks for no technical details.
- Fixed a bug where the password lock switched itself off when the window was closed.
- Fixed a bug where editing a task and restarting it accidentally shortened the set time by a few seconds.
- Fixed a bug where text on the pink buttons was hard to read in the light theme.
- Fixed a bug where the idle counter froze after very long periods of computer operation without a restart.
- Fixed a bug where countdowns longer than 24 hours displayed an incorrect number of days.
- Fixed a bug where the "Do not show the notification area icon" and "Do not show the countdown" options took effect only after switching pages.
- Fixed a bug where the program crashed unexpectedly after entering a time in an unusual format.
- Fixed a bug where hiding the application to the system tray blocked proper Windows shutdown in some cases.
- Fixed a bug where pausing a task in Timer mode actually paused the countdown—previously the time kept running in the background.
- New action: sending an email using ready-made templates (short message, date and time, attachment, HTML message, command output, or custom text).
- Refreshed program interface: windows, buttons, fields, and dialog boxes have been rebuilt from scratch for smoother operation and a consistent look in both themes. Left-side menu instead of top tabs—easier access to the task list, settings, and info. A single "Activator" tab replaces three separate tabs—choose the operating mode via a switch at the top of the tab. Dark mode without bright borders around tabs, featuring clearer buttons.
- The mail configuration window fits on the screen - the content scrolls and the port, encryption and presets sit on one line.
- The sound selection window now shows the custom file field only after selecting the "Custom Sound" option.
- The captions under the settings are shorter, simpler and fit on one line.
- Improved tray icon behavior to accurately display the actual state of the counters.
- The program is now also available in English. You can change the language in Settings.
- The program now automatically notifies you of available new versions upon startup and offers to update. Added an update checker module.
- Action settings can now be shared across all modes or set individually for each—controlled by a switch in the action window.
- Settings: sound notification after each completed task. Automatic idle counter reset is now enabled by default. Updated the behavior of the "Prevent Sleep" option.
- Tasks on the list can now be restarted even if the countdown has already finished.
- Updated program lock mechanism. Passwords are now encrypted with a stronger algorithm.

Version 1.0.6 (02.05.2026)

-Resource update.
-Refreshed the look of several interface elements.
-Added a setting that hides the countdown display.
-Added the ability to run the timer according to a defined schedule.
-Added several new activators for each timer section.
-Added support for more than one activator at a time (the task-list system).
-Added a setting that corrects the time against the verified time zone.
-Added a setting that hides the program icon even while the program is running.
-Added a setting that enables the dark colour scheme.
-Added an activator settings applet that changes how the selected option behaves.
-Added a short description of the program's features in the help section.
-Fixed a graphical bug that displayed the countdown incorrectly.

Version 1.0.5 (20.04.2024)

-Fixed command interpretation; problems running custom scripts should no longer occur.
-Fixed incorrect setting status and its unexpected reset after restarting the application.
-Fixed several settings on Windows 7.
-Fixed enabling the option that enforces control over the power plan.
-Fixed a problem that prevented the program from starting again after the user entered an invalid date.
-Fixed incorrect time display in the timer activator.
-Fixed the configuration-file editor being opened twice.
-Added resetting the program settings to their defaults.
-Added forcing programs to launch through the "Run a command" option.
-Added password protection for the program.
-Added an update check in the settings panel.

Version 1.0.3 (06.04.2024)

-Added the "do not sleep" feature, which stops Windows from acting on its power plan. When enabled the program creates its own power plan and watches whether the user switched to a different one.

Version 1.0.2 (26.03.2024)

-Added playing a sound as an action.
-Fixed certain application exceptions.
-Fixed the tabs on systems older than Windows 10.
-Minor fixes.

Version 1.0.1 (25.03.2024)

-Package update.
-Fixed an error that prevented the program from starting after a particular option was selected.

Version 1.0 (24.03.2024)

-Initial release.
""";
    }
}
