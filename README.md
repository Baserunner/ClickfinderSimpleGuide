# Clickfinder Simple Guide
## Overview
This is a Mediaportal Plugin that shows TV-Movie Data in a simple list form. 
The data becomes originally downloaded from Clickfinder and loaded into MediaPortal Database by the TvMovieImport plugin.

This plugin is based on [Clickfinder ProgramGuide Plugin](http://forum.team-mediaportal.com/threads/clickfinder-programguide-1-6-1-0.108586/) from Scrounger. 
All credit goes to him for all his excellent effort.

**Requirements:**
* [Mediaportal](http://www.team-mediaportal.de)
* [TvMovieImport 1.6.4.2](http://forum.team-mediaportal.com/threads/clickfinder-programguide-1-6-1-0.108586/)
* [TV Movie ClickFinder](http://www.tvmovie.de/news/tv-movie-clickfinder-84949) (no Premium Account required)
* [ClickFinder Simple Guide Plugin itself](https://github.com/Baserunner/ClickfinderSimpleGuide/tree/master/mpe1)

## Introduction
(As this is, most probably for German users only, I switch to German now)

Das Plugin soll TV-Movie EPG-Daten auf eine möglichst einfache und schnelle Art und Weise anzeigen. Es soll per Tastendruck zeigen, was zur "Prime-Time" im Fernsehen läuft, welche Spielfilme heute laufen oder in der nächsten Woche kommen, und so weiter.
Für diesen Zweck stellt das Plugin verschiedene Anzeigen, im Folgenden Views genannt, zur Verfügung. Insgesamt können bis zu 8 verschiedene Views definiert und über die Tasten 1-8 aufgerufen werden.

Mit der Taste 0 wird das Programm für einen einzelnen Kanal aufgerufen.

Die Bedienung soll dem Blättern in einem normalen TV-Magazin ähnlich sein. Man kann einen Tag, eine Stunde vor- und zurückblättern, durch Kanalgruppen zappen und IMDB/TMDB Infos (Taste 9) abrufen.

Die Views 1-8 sind über das Setup Programm konfigurierbar. Prinzipiell können die Views, SQL-Know-How vorrausgesetzt, unter gewissen Einschränkungen, frei definiert werden. Es sind 8 Default-Views vordefiniert.

Ursprünglich hatte ich das Plugin als Kategorie vom ProgramGuide Plugin geplant. Leider hatte ich Probleme damit, weshalb ich Funktionen, die ich nicht nutze, ausgebaut habe. Irgendwann habe ich daraus ein eigenständiges Plugin gebaut.
Das *Clickfinder Simple Guide Plugin* hat bei weitem nicht die Funktionalität des *Clickfinder Program Guides*. Wer Serien, Video, Moving-Picture Unterstützung erwartet, wird von diesem Plugin entäuscht sein. Deswegen auch der Name *Simple Guide*.

Seid der Version 0.93 gibt es verbesserte TMDb-Anzeigen. Wer diese nutzen möchte, muss sich einen (kostenlosen) TMDb-API-Key holen ([TMDb API FAQ](https://www.themoviedb.org/faq/api), Section "How do I apply for an API key?") und ihn in der Configuration eintragen.

Das Plugin nutzt einen Cache, um die Anzahl der TMDb-Requests zu minimieren und die Steuerung so schnell wie möglich zu halten. Welche Views gecached werden, wird über das Feld "Use TMDb" im jeweiligen View-Screen der Konfiguration gesteuert.
Natürlich macht es für die Filme-View, Default ist sie auf View 5, am meisten Sinn.
Der Cache wird, sobald er für eine View komplett durchlaufen ist, erst wieder am nächsten Tag upgedated. 

Es werden nur 40 TMDb-API Requests alle 10 sec. erlaubt. Bitte achtet darauf, dass ihr keine Views mit sehr vielen Program-Items cached. 
Am Besten erstmal mit der Filme-View anfangen. Bei mir dauert das Cachen dieser View ungefähr 10 sec. 

Der Cache selber befindet sich im Verzeichnis *%programdata%\Team MediaPortal\MediaPortal\skin\\<skin_name>\Media\CSGuide\CSGuideTMDbCache.json*. Das Cachefile ist "human-readable". Wer dort weitere Informationen findet, der er in seinem Skin einbauen möchte, kann sich bei mir melden.

Es werden nur die Texte im voraus gecached. Die Hintergrundbilder, Actors und Posters werden erst dann heruntergeladen und gecached, wenn das entsprechende Program-Item aufgerufen wird. 

## Installation
[Hier](https://github.com/Baserunner/ClickfinderSimpleGuide/tree/master/mpe1) befindet sich das .mpe1 File. 
(Auf das .mpe1 klicken, *View Raw* ruft den *Öffnen/Speichern-Dialog* auf)

Der Rest dürfte bekannt sein.

## Bedienung

**(1)**: Ruft View 1 auf (Default ist die Übersicht "Jetzt")

**(2)**: Ruft View 2 auf (Default ist die Übersicht "Prime Time" - 20:15)

**(3)**: Ruft View 3 auf (Default ist die Übersicht "Late Time" - 22:00)

**(4)**: Ruft View 4 auf (Default ist die Übersicht "Night" - 01:00)

**(5)**: Ruft View 5 auf (Default ist die Übersicht "Filme")

**(6)**: Ruft View 6 auf (Default ist die Übersicht "Filme Vorschau")

**(7)**: Ruft View 7 auf (Default ist die Übersicht "TV-Movie Rating")

**(8)**: Ruft View 8 auf (Default ist die Übersicht "Tagestipps") *(funktioniert nicht bei allen Installationen)*

**(9)**: Ruft TMDb Informationen vom jeweiligen Programm ab

**(0)**: Ruft die Einzelkanal View auf

**(up/down)**: Navigiere +1/-1

**(page up/down)**: Navigiere +7/-7

**(right)**: Eine Kanalgruppe vorwärts, in der Einzelkanal Sicht einen Kanal vorwärts

**(left)**: Eine Kanalgruppe zurück, in der Einzelkanal Sicht einen Kanal zurück

**Next Item (F8)**: Einen Tag vorwärts

**Previous Item (F7)**: Einen Tag zurück

**Forward (F6)**: Einen Stunde vorwärts

**Rewind (F5)**: Einen Stunde zurück

**Play Button (P)**: Schalte auf den Kanal

**Record Button (R)**: Aufnehmen

**Menu Button (F9)**: Hilfe

**OSD Info Button (Y)**: Hilfe

## Screenshots
### Plugin
![TagesTipps View](https://cloud.githubusercontent.com/assets/8837272/18563645/b9ff4d9e-7b88-11e6-90a3-3c588b08caa6.png)
![TMDb Plot](https://cloud.githubusercontent.com/assets/8837272/18563728/11010fb0-7b89-11e6-8eed-a3760f1adf09.png)
![TMDb Besetzung](https://cloud.githubusercontent.com/assets/8837272/18563802/5828a8a8-7b89-11e6-8c7d-c99d6d72708e.png)
![Single Channel View](https://cloud.githubusercontent.com/assets/8837272/16706449/228d3428-45ad-11e6-8d94-12c9056d13d3.png)

### Configuration
<div align="center">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/18563927/b1eef018-7b89-11e6-8246-81112adeba9d.png" alt="Config general" title="Config general"</img>
        <img height="0" width="8px">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/18563958/cce33b68-7b89-11e6-94af-d605a6698c44.png" alt="Config View 5" title="Config View 5"></img>
</div>